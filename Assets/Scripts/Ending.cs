using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 終幕。原典 PC98版は3つの関数に分かれている。
///
/// <code>
///   0xd284  大魔導師の口上   セリフ155（4行・上端50）「よくぞ、ここまで、たどりついたものだ。…」
///   0xc4d8  ゼノとの決着     セリフ161・162（3行の窓）→ 163〜170（8行の窓）を順に
///                            （`ax = 数え + 0xa1` と `ax = 数え + 0xa3` のループで出している）
///   0xd45c  最後の画面       MTOWER1 / MTOWER2 / MTENDMJ / MTFIN を読み込んでパレットを重ね、
///                            「迷宮ＲＰＧ『魔法の塔』」(29,170) と
///                            「Ｈｉｔ　ａｎｙ　ｋｅｙ！」(28,210) を出してキー待ち、
///                            最後にパレットを 0x8e8（真っ暗）にして終わる
/// </code>
///
/// **この版で使われていないセリフ**: 102〜105・123・154（「このテスト版はここまで」の類）と
/// 180〜183（終幕の語り・作者の文・シェアウェアの案内）。
/// モンスターの送金ヒント201〜238と同じく、完成版向けに用意されたまま出番が無い。
/// </summary>
public static class Ending
{
    /// <summary>塔を駆け上がった先。原典 0x23a6 が 0x32（50）まで上げる。</summary>
    public const int LastFloor = 50;

    /// <summary>
    /// 駆け上がりの始まり。原典 0x237c が現在階に 0x18（24）を書いてから数え上げる。
    /// </summary>
    public const int ClimbFrom = 24;

    /// <summary>
    /// 1階ぶんの待ち（垂直帰線の回数）。原典 0x2391 の `mov ax, 5`。
    /// 待つ前に階の数字だけ描き直している（`lcall 0x68b, 0x50bc` の引数5）ので、
    /// **地図は変わらず階の数字だけが 24→50 と上がっていく**演出。
    /// </summary>
    public const int ClimbWait = 5;

    /// <summary>数字が出る順。25階から50階まで。</summary>
    public static IEnumerable<int> ClimbFloors()
    {
        for (int f = ClimbFrom + 1; f <= LastFloor; f++) yield return f;
    }

    /// <summary>駆け上がりにかかる時間（秒）。</summary>
    public static float ClimbSeconds
    {
        get { return (LastFloor - ClimbFrom) * ClimbWait * BattlePresentation.FrameSeconds; }
    }

    /// <summary>そこでの立ち位置。原典 0x23be / 0x23cd が X=5, Y=6 を書く。</summary>
    public static readonly Vector2Int LastPosition = new Vector2Int(0, -1);

    /// <summary>大魔導師の口上。</summary>
    public const int ArchmageRecord = 155;

    /// <summary>ゼノとの決着で順に出るセリフ。</summary>
    public static readonly int[] FinaleRecords =
    {
        161, 162, 163, 164, 165, 166, 167, 168, 169, 170,
    };

    /// <summary>最後の画面に出る文字（原典 DS:0x1ab7 / DS:0x1ace）。</summary>
    public const string TitleLine = "迷宮ＲＰＧ『魔法の塔』";
    public const string AnyKeyLine = "Ｈｉｔ　ａｎｙ　ｋｅｙ！";

    /// <summary>
    /// その位置（原典 0xd4d2 / 0xd4ea が 0x107e:0x21 に渡す値）。
    /// **x はバイト（＝8ドット）、y はドット**。
    /// </summary>
    public static readonly Vector2Int TitlePos = new Vector2Int(0x1d, 0xaa);
    public static readonly Vector2Int AnyKeyPos = new Vector2Int(0x1c, 0xd2);

    /// <summary>ドットに直した位置。</summary>
    public static Vector2Int TitleDot { get { return ScreenLayout.FromTextPos(TitlePos.x, TitlePos.y); } }
    public static Vector2Int AnyKeyDot { get { return ScreenLayout.FromTextPos(AnyKeyPos.x, AnyKeyPos.y); } }

    /// <summary>最後の画面で読み込む絵の順番。</summary>
    public static readonly string[] Images =
    {
        "Ending_Tower1", "Ending_Tower2", "Ending_Text", "Ending_Art",
    };

    /// <summary>この版で一度も出ないセリフ。</summary>
    public static readonly int[] UnusedRecords =
    {
        102, 103, 104, 105, 123, 154, 180, 181, 182, 183,
    };

    /// <summary>終幕で順に出るセリフを並べる。</summary>
    public static IEnumerable<int> Records()
    {
        yield return ArchmageRecord;
        foreach (var r in FinaleRecords) yield return r;
    }
}
