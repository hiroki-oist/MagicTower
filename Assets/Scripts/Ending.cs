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
    /// パレットの表（原典が `0x0c4d8` でスタックに積む 8本 x 16語）。
    /// 1語は **`NGRB`**（上位4ビットが色の番号、残りが緑・赤・青の濃さ）で、
    /// 番号7〜15はどの本でも `fff`（白のまま）、**動くのは番号0〜6だけ**。
    ///
    /// <code>
    ///   [bp-0x248] 0fff 1fff 2fff 3fff 4fff 5fff 6fff 7fff …   ← 全部まっ白
    ///   [bp-0x228] 0bbb 1bbb 2bbb 3bbb 4bbb 5bbc 6ddd 7fff …
    ///   [bp-0x208] 0999 1999 2999 3999 499a 599c 6ddd 7fff …
    ///   [bp-0x1e8] 0777 1777 2777 3778 477a 588c 6ddd 7fff …
    ///   [bp-0x1c8] 0555 1555 2556 3558 466a 588c 6ddd 7fff …
    ///   [bp-0x1a8] 0333 1334 2336 3448 466a 588c 6ddd 7fff …
    ///   [bp-0x188] 0112 1114 2226 3448 466a 588c 6ddd 7fff …
    ///   [bp-0x168] 0002 1004 2226 3448 466a 588c 6fff 7fff …   ← ふだんの色
    /// </code>
    ///
    /// 番号0（下地）の濃さを並べると **15, 11, 9, 7, 5, 3, 1, 0** の段になる。
    /// これを「**白へどれだけ寄せるか**」（0＝ふだんの色 / 1＝まっ白）として持つ。
    /// </summary>
    static readonly int[] TowerWhiteSteps = { 15, 11, 9, 7, 5, 3, 1, 0 };

    /// <summary>
    /// その回の「白への寄せ具合」（0〜1）。原典の輪（0x0ce83）の当て方そのまま:
    ///
    /// <code>
    ///   i &lt; 7      … [bp-0x228] から 32ずつ後ろへ  → 表の 1番目から7番目（11/15 → 0）
    ///   7〜0x12    … 差し替えないので ふだんの色のまま
    ///   i &gt; 0x12   … [bp+0xd8] から 32*i を引く   → 表の6番目から0番目（1/15 → 15/15）
    /// </code>
    ///
    /// つまり **白からすうっと現れて、しばらくそのまま、最後はまっ白に消える**。
    /// ⚠ 以前ここを「黒から現れて黒へ消える」と近似していたが**逆だった**。
    /// 終幕の流れ（塔の絵 → 真っ白 → 終わりの言葉）とも、こちらが合う。
    /// </summary>
    public static float TowerWhiteMix(int step)
    {
        int level;
        if (step < TowerFadeInSteps) level = TowerWhiteSteps[1 + step];
        else if (step <= TowerFadeOutAfter) level = 0;
        else level = TowerWhiteSteps[Mathf.Max(0, 6 - (step - TowerFadeOutAfter - 1))];
        return level / 15f;
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
