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

    /// <summary>
    /// 1回で**下へ**送るドット数（原典 0xcee0〜: 写す高さが `0x190 - 15*i`、
    /// 貼る先が `15*i` 行下）。
    ///
    /// ⚠ 動く（下へ滑る）のは**塔（MTOWER1）だけ**で、荒野（MTOWER2）は動かない。
    /// 荒野は空を透かして塔の**前**に重なっているので、
    /// **塔が地面の向こうへ沈んでいく**ように見える。
    /// 終わりの言葉「ながい戦いは終わりを告げ、虚栄の塔は崩れさる。」がこれを指している
    /// （動画 Part2 t≈1789〜1801）。
    /// 26回 x 15 ＝ 390ドットで、塔は画面（400）から消える。
    /// </summary>
    public const int TowerScrollPerStep = 15;

    /// <summary>はじめの何回でパレットが明るくなるか（原典 `i &lt; 7`）。</summary>
    public const int TowerFadeInSteps = 7;

    /// <summary>この回を**超えたら**パレットが暗くなる（原典 `i &gt; 0x12`）。</summary>
    public const int TowerFadeOutAfter = 0x12;

    /// <summary>1回ごとに鳴らす音（原典 0xcf9f の 7）。</summary>
    public const int TowerSound = 7;

    /// <summary>
    /// 終幕の絵の**空の色**。原典 0xc4d8 の表の最後の1本（＝ふだんの色）の索引0
    /// `0002`（NGRB）＝ 緑0・赤0・青2 → (0,0,34)。荒野の絵は空（索引13）を透かしてあるので、
    /// その後ろに敷くのがこの色。
    /// </summary>
    public static readonly Color SkyColor = new Color(0f, 0f, 34f / 255f);

    /// <summary>
    /// 塔（MTOWER1、320x400）を貼る左端。画面（640）の真ん中に置く。
    /// </summary>
    public const int TowerLeft = 160;
    public const int TowerWidth = 320;

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

    /// <summary>
    /// ★**最後にゼノへ踏み出す1歩**（作者の指定。原典に無い）。
    /// セリフ170のあと、ここへ1秒かけて歩きながら画面が暗くなり、
    /// 暗くなりきってから塔の1枚絵に入る。ゼノは (0,+1) にいる。
    /// </summary>
    public static readonly Vector2Int LastStep = new Vector2Int(0, 0);

    /// <summary>大魔導師の口上。</summary>
    public const int ArchmageRecord = 155;

    /// <summary>ゼノとの決着で順に出るセリフ。</summary>
    public static readonly int[] FinaleRecords =
    {
        161, 162, 163, 164, 165, 166, 167, 168, 169, 170,
    };

    // ------------------------------------------------------------ Java版の終幕
    //
    // ★作者の指定「エンディングは全てJava準拠にしたい」。
    //
    // Java版 MTower.class の `eventMT` は、最上階（PC98版の50階）で会話が終わると
    // `OpenningZeno(4)` → `OpenningZeno(5)` を呼ぶ。その中で読むのが
    // `data2/edata01.gif`〜`edata18.gif`:
    //   edata01〜15 … **塔が地面へ沈んでいく15枚**（1枚ずつ描いてある）
    //   edata16/17  … 終わりの言葉（英語／日本語。旗は mapObject[76][121]）
    //   edata18     … 「The End」（(C)1998-2000 Oz &amp; Kenichi ／ (C)1996 N.W ／ See you again!）
    // 1枚ごとの切り替えは **40x40 のマス目のモザイク**で溶かし、`twait(100)` ずつ待つ。
    //
    // ⚠ 原典 PC98版はこれと作りが違う（MTOWER1 と MTOWER2 を重ねて塔だけを
    //    15ドットずつ26回下げ、パレットで白へ寄せる）。**どちらを使うかは
    //    <see cref="AuthorStyle.JavaEnding"/>**。絵は `docs/make_ending_java.py` が
    //    GIF から書き出す（`Assets/Sprites/Screens/EndJava_*.png`）。

    /// <summary>塔が沈んでいく絵の枚数（edata01〜15）。</summary>
    public const int JavaFrames = 15;

    /// <summary>モザイクのマスの大きさ（ドット）。Java版の PixelGrabber と同じ 40x40。</summary>
    public const int JavaMosaic = 40;

    /// <summary>
    /// 1枚ぶんの時間（秒）。Java版の `twait(100)` は**待ちだけ**で、
    /// その前に 640x400 の画素を組み直す時間（2001年の Java では数百ミリ秒）が乗る。
    /// 見て分かる速さに合わせて 0.4 秒にしてある。
    /// </summary>
    public const float JavaFrameSeconds = 0.4f;

    /// <summary>最後の1枚（沈みきったところ）を見せておく時間（秒）。</summary>
    public const float JavaLastHoldSeconds = 1.5f;

    /// <summary>終わりの言葉（edata17）を見せておく時間（秒）。Java版は1枚絵。</summary>
    public const float JavaWordsSeconds = 12f;

    /// <summary>塔が沈む絵ぜんぶにかかる時間（秒）。</summary>
    public static float JavaMovieSeconds
    {
        get { return JavaFrames * JavaFrameSeconds + JavaLastHoldSeconds; }
    }

    /// <summary>終わりの言葉を何枚に分けて出すか（Java版は1枚絵、原典は段落4つ）。</summary>
    public static int WordsPages
    {
        get { return AuthorStyle.JavaEnding ? 1 : Cutscenes.EndingTextParagraphs; }
    }

    // ------------------------------------------------------------ ひとりでに進む所
    //
    // ★**終幕は塔の絵から終わりの言葉まで、キーを一度も待たない**。
    // 原典 0xc4d8 の後半（0xcfc5〜0xd27a）に出てくるのは
    //   `lcall 0xf5f,0x3f2`（垂直帰線をN回待つ）と `lcall 0xf5f,0x3d9`（1回待つ）だけで、
    //   キー待ち（`lcall 0x107e,0x4fa`）は**いちばん最後の1回**しか無い。
    //     0xd064 / 0xd07e  0x3c(60) 待ち ＝ 1秒
    //     0xd0dc〜0xd20f    終わりの言葉を段落ごとに1行ずつ出す（`0x3d9` を挟む輪）
    //     0xd216 / 0xd250  0x3c 待ち
    //     0xd25c           音2（題名の曲）を鳴らす
    //     0xd268           0x258(600) 待ち ＝ 10秒
    //     0xd274           **ここで初めてキー待ち** → 最後の画面（0xd45c）
    // つまり「触らなくても勝手に進む」のが原典の姿。こちらは1枚絵を送る形なので、
    // **その1枚ごとに待つ秒数**として持つ（長さは動画 Part2 t≈1801〜1822 から。
    // 白4秒 → 段落3秒・3秒・4秒 → 4段落そろえて6秒）。

    /// <summary>塔が沈みきってから終わりの言葉へ移るまで（原典 0xd064 の 0x3c）。</summary>
    public const float TowerHoldSeconds = 1f;

    /// <summary>終わりの言葉の1枚ごとに待つ秒数（0枚目＝まっ白から4段落そろうまで）。</summary>
    public static readonly float[] WordsSeconds = { 4f, 3f, 3f, 4f, 6f };

    /// <summary>
    /// その画面がひとりでに進むまでの秒数。0 なら触るまで待つ
    /// （最後の画面だけは原典も「Ｈｉｔ　ａｎｙ　ｋｅｙ！」でキーを待つ）。
    /// </summary>
    public static float AutoSeconds(TowerGame.PageKind kind, int record)
    {
        if (kind == TowerGame.PageKind.EndingTower)
            return AuthorStyle.JavaEnding ? JavaMovieSeconds
                                          : TowerDuration + TowerHoldSeconds;
        if (kind == TowerGame.PageKind.EndingText)
            return AuthorStyle.JavaEnding ? JavaWordsSeconds
                   : WordsSeconds[Mathf.Clamp(record, 0, WordsSeconds.Length - 1)];
        return 0f;
    }

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
