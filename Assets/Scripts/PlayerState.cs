using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレーヤーの状態。**原典 PC98版が持っている変数をそのまま写した**もの。
///
/// 原典のメモリ上の対応（MTE.EXE の逆アセンブルより）:
/// <code>
///   生命力 [0x98] / 攻撃力 [0x9c] / 防御力 [0x16] / 所持金 [0xa2]
///   現在の階 [0x9e] / 到達最高階 [0x96]
///   鍵 [0x1c] / 紫の鍵 [0x9a] / 赤い鍵 [0xa0]
///   装備中の剣 [0x21] / 装備中の盾 [0x22]   … どちらも 0〜5 の段階
///   特殊アイテムの所持フラグ [0x23]〜[0x31]  … 番号は「物体ID − 1」
/// </code>
///
/// セーブもこの並びをそのまま書き出している（空きスロットは全項目 0xffff）。
/// </summary>
[System.Serializable]
public class PlayerState
{
    public int Hp;
    public int Attack;
    public int Defense;
    public int Gold;

    public int Floor;
    public int HighestFloor;

    public int YellowKeys;
    public int PurpleKeys;
    public int RedKeys;

    /// <summary>0=無し, 1=長剣 … 5=神剣。</summary>
    public int SwordRank;
    /// <summary>0=無し, 1=鉄の盾 … 5=神盾。神盾(5)は魔法攻撃を無効にする。</summary>
    public int ShieldRank;

    public SpecialItems.Kind Items = SpecialItems.Kind.None;

    /// <summary>祭壇に寄進した回数。寄進額と回復量がこれで決まる。</summary>
    public int DonationCount;

    public Vector2Int Position;

    /// <summary>
    /// 一度でも入った階。飛翔のオーブは「行った事のある階」へしか飛べない
    /// （原典の説明「今までに行った事のある階を自由に移動できる能力を持つ。
    ///   ただし、階段の隣でなければ使えない。」）。
    /// </summary>
    public readonly HashSet<int> Visited = new HashSet<int>();

    /// <summary>
    /// 一度きりのできごとの覚え書き。原典は [0x554a] の1バイト配列にまとめて置いていて、
    /// アイテム所持フラグ（0x23〜0x31）と仙人の「もう話した」フラグ（0x45〜0x56）が同居している。
    /// ここではアイテムは <see cref="Items"/> が持つので、それ以外の番号だけを入れる。
    /// </summary>
    public readonly HashSet<int> EventFlags = new HashSet<int>();

    /// <summary>
    /// 高速モードの旗。原典もこれをフラグ置き場の **31番**（`es:[0x1f]`）に持っていて、
    /// セーブにもそのまま入る（0xa92d〜0xa956 が切り替える）。
    /// 立っているあいだ、原典は窓の開け閉めの待ち（0x9478 / 0x9546）と
    /// 1マス動くときの滑らかな移動（0xa29a）を飛ばし、数字を数え上げる回数を
    /// 10回から3回に減らす（0xb3bf / 0xb519）。
    /// </summary>
    public const int FastModeFlag = 31;

    public bool FastMode
    {
        get { return EventFlags.Contains(FastModeFlag); }
        set { if (value) EventFlags.Add(FastModeFlag); else EventFlags.Remove(FastModeFlag); }
    }

    /// <summary>原典の生命力の上限（マニュアル「上限は９９９０ポイントです」）。</summary>
    public const int MaxHp = 9990;

    /// <summary>
    /// 原典の攻撃力・防御力・所持金の上限。状態表示の関数（MTE.EXE 0xb96c）が
    /// 描き直すたびに頭を押さえている:
    /// <code>
    ///   if (生命力 > 0x2706) 生命力 = 0x2706;   // 9990
    ///   if (攻撃力 > 0x03de) 攻撃力 = 0x03de;   //  990
    ///   if (防御力 > 0x03de) 防御力 = 0x03de;   //  990
    ///   if (金     > 0x2706) 金     = 0x2706;   // 9990
    /// </code>
    /// この関数は能力値を変えるたびに呼ばれているので、実質いつでもこの上限がかかる。
    /// </summary>
    public const int MaxAttack = 990, MaxDefense = 990, MaxGold = 9990;

    /// <summary>原典の上限まで頭を押さえる。能力値を変えたら呼ぶ。</summary>
    public void ClampStats()
    {
        if (Hp > MaxHp) Hp = MaxHp;
        if (Attack > MaxAttack) Attack = MaxAttack;
        if (Defense > MaxDefense) Defense = MaxDefense;
        if (Gold > MaxGold) Gold = MaxGold;
    }

    public bool Has(SpecialItems.Kind k) { return (Items & k) == k; }
    public void Give(SpecialItems.Kind k) { Items |= k; }
    public void Consume(SpecialItems.Kind k) { Items &= ~k; }

    public void Heal(int amount) { Hp = Mathf.Min(MaxHp, Hp + amount); }

    /// <summary>その階に入ったことを記録する。</summary>
    public void MarkVisited(int floor)
    {
        Visited.Add(floor);
        if (floor > HighestFloor) HighestFloor = floor;
    }

    /// <summary>ゲーム開始時（序章）。1階を神剣・神盾で進む。</summary>
    public static PlayerState NewGame()
    {
        var s = GameStart.Prologue;
        return new PlayerState
        {
            Hp = s.Hp, Attack = s.Attack, Defense = s.Defense, Gold = s.Gold,
            Floor = s.Floor, HighestFloor = s.Floor,
            SwordRank = s.SwordRank, ShieldRank = s.ShieldRank,
            Position = s.Position,
        };
    }

    /// <summary>
    /// 3階でゼノに遭遇したあと。神剣と神盾を奪われ、2階の牢に入れられる。
    /// 原典 0x06f1b〜0x06f49 がこの通りに書き換えている。
    /// </summary>
    public void ApplyZenoEvent()
    {
        var s = GameStart.AfterZeno;
        Hp = s.Hp; Attack = s.Attack; Defense = s.Defense;
        SwordRank = s.SwordRank; ShieldRank = s.ShieldRank;
        Floor = s.Floor; Position = s.Position;
        // 原典は0本。調べもの用に持たせているだけ（DebugPlay.KeysAfterZeno）。
        YellowKeys = s.YellowKeys; PurpleKeys = s.PurpleKeys; RedKeys = s.RedKeys;
    }
}
