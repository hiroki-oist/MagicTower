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

    // ------------------------------------------------ 終幕の塔の絵（原典 0x0ce83 の輪）
    //
    //   i = 0;
    //   do {
    //     if (i < 7)     パレットを 入りの表[i] に差し替える   （lcall 0xf5f,0x416、1つ32バイト）
    //     if (i > 0x12)  パレットを 出の表[…]   に差し替える   （降り順、同じく32バイト刻み）
    //     絵を 15ドットぶん上へずらして描き直す               （高さ 0x190 - 15*i の領域を写す）
    //     音 7 を鳴らす                                       （lcall 0x10da,0x20b）
    //     0x1e(30) 待つ                                       （lcall 0xf5f,0x3f2）
    //   } while (++i < 0x1a);
    //
    // 26回 x 30コマ ＝ **13秒**、送り合計 26x15 ＝ 390ドットで画面（400）をほぼ流し切る。
    // 音の表の「遠ざかる」はこの見せ方を指している。

    /// <summary>塔の絵を送る回数（原典 0x1a）。</summary>
    public const int TowerSteps = 0x1a;

    /// <summary>1回ぶんの待ち（原典 0x1e）。単位は垂直帰線の回数＝1/60秒。</summary>
    public const int TowerStepWait = 0x1e;

    /// <summary>1回で上へ送るドット数（原典は 15*i で高さを縮めている）。</summary>
    public const int TowerScrollPerStep = 15;

    /// <summary>はじめの何回でパレットが明るくなるか（原典 `i &lt; 7`）。</summary>
    public const int TowerFadeInSteps = 7;

    /// <summary>この回を**超えたら**パレットが暗くなる（原典 `i &gt; 0x12`）。</summary>
    public const int TowerFadeOutAfter = 0x12;

    /// <summary>1回ごとに鳴らす音（原典 0xcf9f の 7）。</summary>
    public const int TowerSound = 7;

    /// <summary>塔の絵の長さ（秒）。</summary>
    public static float TowerDuration
    {
        get { return TowerSteps * TowerStepWait / 60f; }
    }

    /// <summary>
    /// その回の明るさ（0〜1）。**原典はパレットの表を差し替えている**が、
    /// 表の中身（32バイト x 7本 x 2組）はまだ読めていないので、
    /// **入りと出を7段の直線で近似**している。濃さの変わり方が分かったら差し替える。
    /// </summary>
    public static float TowerBrightness(int step)
    {
        if (step < TowerFadeInSteps) return (step + 1) / (float)TowerFadeInSteps;
        if (step > TowerFadeOutAfter)
            return Mathf.Max(0f, 1f - (step - TowerFadeOutAfter) / (float)TowerFadeInSteps);
        return 1f;
    }

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
