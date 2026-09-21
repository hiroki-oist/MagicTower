using UnityEngine;

/// <summary>
/// ゲーム冒頭の状態。原典 PC98版 MTE.EXE より。
///
/// 【原典の流れ】
/// <code>
///   1. 塔1階から始まる。生命力1000 / 攻撃力100 / 防御力100、神剣と神盾を装備
///      （初期化 0x06536。INITIAL.DAT の先頭 1000,100,100,0,1,1,5,10 と一致し、
///        末尾の 1,1,5,10 は 到達最高階=1 / 現在階=1 / X=5 / Y=10 に対応する）
///   2. 塔3階で魔導師ゼノに遭遇する
///      「時は来たれり・・・我、全能の力を用いて、神剣を砕き、偉大なる知恵を手に入れん・・・」
///   3. イベントで全てを失う（0x06f1b〜0x06f49）
///        装備剣 = 0 / 装備盾 = 0  … 神剣と神盾を奪われる
///        生命力 = 400 / 攻撃力 = 10 / 防御力 = 10
///        現在階 = 2                … 2階の牢に入れられる
///   4. ここから本編が始まる
/// </code>
///
/// 盗賊のセリフ「あんたの剣と盾、あいつらに取り上げられちまったみたいだな。
/// この塔の中を丸腰で歩き回るのは、やめた方がいい。確か５階に剣、９階に盾があったはず」
/// がこの流れに対応する。
///
/// ⚠ かつて「初期化の 100/100 と動画の 10/10 が食い違う」として長く未解決にしていたが、
///    両方とも正しく、序章とイベント後という別の場面の値だった。
/// </summary>
public static class GameStart
{
    /// <summary>序章。1階から神剣・神盾を持って始まる。</summary>
    public struct State
    {
        public int Hp, Attack, Defense, Gold, Floor, SwordRank, ShieldRank;
        public Vector2Int Position;
    }

    /// <summary>ゲーム開始時（塔1階、神剣・神盾を装備）。</summary>
    public static readonly State Prologue = new State
    {
        Hp = 1000, Attack = 100, Defense = 100, Gold = 0,
        Floor = 1, SwordRank = 5, ShieldRank = 5,      // 5 = 神剣 / 神盾
        Position = new Vector2Int(0, -5),              // 原典は X=5,Y=10（盤面の中央下）
    };

    /// <summary>ゼノのイベント直後（2階の牢、丸腰）。ここから本編。</summary>
    public static readonly State AfterZeno = new State
    {
        Hp = 400, Attack = 10, Defense = 10, Gold = 0,
        Floor = 2, SwordRank = 0, ShieldRank = 0,
        // 原典 0x0701e / 0x07029 が X=2, Y=7 を書く。盤面では x = 2-5 = -3、y = 5-7 = -2。
        // そこは 2階の牢の中で、**盗賊(-3,-1) の真下**にあたる（起こしてくれる相手の隣）。
        Position = new Vector2Int(-3, -2),
    };

    /// <summary>ゼノがこのイベントを起こす階。</summary>
    public const int ZenoEncounterFloor = 3;
}
