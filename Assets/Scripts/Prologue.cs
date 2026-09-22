using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム冒頭の演出。原典 PC98版では **3階の (-1,-3) に立つと発動する位置イベント**
/// （PositionEvents.txt の1行目、フラグ89）で、そこから関数 0x6ae0 が流れる。
/// 起動直後ではない。
///
/// <code>
///   1階を神剣・神盾で進む（生命力1000 / 攻撃力100 / 防御力100）
///   3階で魔導師ゼノに会う
///     110「ようこそ、『魔法の塔』へ。お前がちょうど１００人目の侵入者だ。…」
///     111「なんだっ！？　うわぁーっ！！？」
///   ここで**題名画面**が入る（<see cref="TitleScreen"/>。0x6db2〜0x6e24）
///   ここで全てを奪われる（原典 0x06f1b〜0x06f49）
///     装備剣=0 / 装備盾=0 / 生命力=400 / 攻撃力=10 / 防御力=10 / 現在階=2
///   2階の牢で目を覚ます
///     112「・・・・・」 113「・・・・・おい・・・」 114「・・・・・おいっ！　起きろ！」
///     115「おお、無事か！　おまえ、ここの魔導師に捕まって、この牢屋の中に…」
///     116「…あんたの剣と盾、あいつらに取り上げられちまったみたいだな。
///         　この塔の中を丸腰で歩き回るのは、やめた方がいい。
///         　確か５階に剣、９階に盾があったはずだから…」
/// </code>
///
/// 116 のセリフが、直前に能力と装備を奪われたことと噛み合っている。
/// </summary>
public static class Prologue
{
    /// <summary>ゼノに会う階。</summary>
    public const int ZenoFloor = 3;

    /// <summary>牢のある階。</summary>
    public const int PrisonFloor = 2;

    /// <summary>
    /// 序章の最後に床になるマス（原典 0x070c8 / 0x070d7 が 6 を書く）。
    /// 盗賊が居た (-3,-1) と、その左の壁 (-4,-1)。ここが開くと左端の通路へ出られ、
    /// 列を下れば 2階の上り階段 (-5,-5) に着く。
    /// </summary>
    public static readonly Vector2Int[] PrisonOpens =
    {
        new Vector2Int(-4, -1), new Vector2Int(-3, -1),
    };

    /// <summary>牢に居た盗賊（Mikata, ID21）。</summary>
    public const string ThiefPrefab = "Mikata";

    /// <summary>盗賊が最初に居るマス（Floor_2.txt の ID21）。</summary>
    public static readonly Vector2Int ThiefStart = new Vector2Int(-3, -1);

    /// <summary>
    /// 牢が開いたあと、盗賊が歩いていく道順（原典 0x070dc〜0x07129）。
    ///
    /// 原典は **向きを1バイトずつ並べた表**（<see cref="KnightCharge"/> と同じ決まり）を
    /// 関数の頭（0x6b04〜0x6b10）に積んでいる:
    /// <code>
    ///   [bp-4]=2 [bp-3]=2 [bp-2]=3 [bp-1]=5   ; 2=X-1（左） 3=Y+1（画面の下） 5=区切り
    /// </code>
    /// 始まりは X=2, Y=6（0x70b5 / 0x70d4）＝盤面の (-3,-1)。1歩ごとに
    /// **主人公と同じ1マス滑らせる処理（0xa276）**を呼ぶので、歩く速さも同じ。
    /// 歩き終えると 0x07153 が最後のマスに ID21(0x15) を書く＝**そこに立ったまま残る**。
    /// </summary>
    public static readonly Vector2Int[] ThiefWalk =
    {
        new Vector2Int(-4, -1),    // 左
        new Vector2Int(-5, -1),    // 左
        new Vector2Int(-5, -2),    // 下
    };

    /// <summary>
    /// 歩き終えて盗賊が立つマス。ここで話すとセリフ116 が出て、
    /// そのマスが床になる（ThiefEvents.txt の1行目 `2 | フラグ94=0 | 116 | - | 2:(-5,-2)=床`）。
    /// </summary>
    public static Vector2Int ThiefGoal { get { return ThiefWalk[ThiefWalk.Length - 1]; } }

    /// <summary>
    /// 話しかけたあと、盗賊が去っていく道順（原典 0x003e0 の向きの表 `{3,3,5}`）。
    /// 立っている (-5,-2) から**下へ2歩**（上り階段の方向）で、そこで消える。
    /// 原典はセリフ116 を出して**キーを待ってから**歩かせる（0x47e のあと 0x4c9）。
    /// </summary>
    public static readonly Vector2Int[] ThiefLeave =
    {
        new Vector2Int(-5, -3), new Vector2Int(-5, -4),
    };

    /// <summary>ゼノに会うまでに出るセリフ（Dialogue.txt のレコード番号）。</summary>
    public static readonly int[] BeforeFall = { 110, 111 };

    /// <summary>牢で目を覚ましてから出るセリフ。</summary>
    public static readonly int[] AfterFall = { 112, 113, 114, 115, 116 };

    /// <summary>演出で出すセリフを順に返す。</summary>
    public static IEnumerable<string> Lines(IEnumerable<int> ids)
    {
        foreach (var id in ids)
        {
            var line = GameText.Instance.Dialogue(id);
            if (line.HasValue) yield return line.Value.Body;
        }
    }

    /// <summary>
    /// 冒頭を終えた状態を作る。ゼノに全てを奪われ、2階の牢から本編が始まる。
    /// </summary>
    public static PlayerState Finish(PlayerState player)
    {
        var p = player ?? PlayerState.NewGame();
        p.ApplyZenoEvent();
        return p;
    }
}
