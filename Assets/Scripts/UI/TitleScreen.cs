using UnityEngine;

/// <summary>
/// 題名画面。原典 PC98版では**序章の途中**に入る（起動直後ではない）。
///
/// 原典の流れ（MTE.EXE 0x6ae0。3階の (-1,-3) に立つと呼ばれる）:
/// <code>
///   セリフ110「ようこそ、『魔法の塔』へ。…」      ← 魔導師ゼノ
///   セリフ111「なんだっ！？　うわぁーっ！！？」
///   …画面を作り直して パレット 0x908 を入れ直し、題名の文字を2行出す（0x6db2 / 0x6dcd）
///     「Ｔｏｗｅｒ　ｏｆ　ｔｈｅ　Ｓｏｒｃｅｒｅｒ」
///     「　　　　　　　　　　（Ｃ）１９９６　Ｎ．Ｗ」
///   …0x3c(60) 待って パレット 0x928 でフェード、0x78(120) 待ち、また 0x3c 待ち
///   …MTMOJI.P58 と MTOPSTR.P58 を読み込み、まとめ読みの表（DS:0x240）も読む
///   セリフ112「・・・・・」113「おい・・・」114「おいっ！　起きろ！」115「おお、無事か！…」
/// </code>
///
/// 動画の冒頭で題名が出るのはこの場面で、以前これを「起動時のアトラクト」と
/// 読み違えていた。実際は**ゼノに襲われて気を失っている間**の表示。
/// </summary>
public static class TitleScreen
{
    /// <summary>題名の文字（原典 DS:0x113c / DS:0x1167 の Shift-JIS そのまま）。</summary>
    public const string Title = "Ｔｏｗｅｒ　ｏｆ　ｔｈｅ　Ｓｏｒｃｅｒｅｒ";
    public const string Copyright = "　　　　　　　　　　（Ｃ）１９９６　Ｎ．Ｗ";

    /// <summary>
    /// 原典が文字を出す位置（0x6db2 / 0x6dcd が `lcall 0x107e, 0x21` に渡す値）。
    /// **x はバイト（＝8ドット）、y はドット**。描画側（linear 0x10883）が
    /// `VRAM位置 = y × 80 + x` で書いているので、この単位で合っている。
    ///
    /// 以前はここの x と y が入れ替わっていた（「TitleX=160, TitleY=18」）。
    /// 原典は 題名が (x=0x12, y=0xa0)、副題が (x=0x12, y=0xbe)。
    /// </summary>
    public const int TitleX = 0x12, TitleY = 0xa0;
    public const int CopyrightX = 0x12, CopyrightY = 0xbe;

    /// <summary>題名の文字の左上（ドット）。</summary>
    public static Vector2Int TitleDot { get { return ScreenLayout.FromTextPos(TitleX, TitleY); } }
    public static Vector2Int CopyrightDot { get { return ScreenLayout.FromTextPos(CopyrightX, CopyrightY); } }

    /// <summary>このセリフのあとに題名が入る。</summary>
    public const int AfterRecord = 111;

    /// <summary>題名のあとに続くセリフ。</summary>
    public const int NextRecord = 112;

    /// <summary>原典の待ち時間（0xf5f:0x3f2 に渡す値）。単位は垂直帰線の回数。</summary>
    public static readonly int[] Waits = { 0x3c, 0x78, 0x3c };
}
