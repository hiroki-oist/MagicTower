using UnityEngine;

/// <summary>
/// 会話ウィンドウの寸法。原典 PC98版の会話表示（MTE.EXE 0x94ba）から読み取ったもの。
///
/// <code>
///   // 窓の上の帯（LCG#1、320x32）を上端の行に貼る
///   f(上端 * 0x50 + 0x14, 0x14, 0x20, 0x6e00);
///   // 本体を 4行ずつ塗る。繰り返し回数 = 0x14 * 行数 / 4 + 1
///   si = (上端 + 0x10) * 0x50 + 0x14;
///   do { f(si, 0x14, 0x10, 0x7300); si += 0x140; } while (--回数);
/// </code>
///
/// PC98 の画面は 640x400、1行 80バイト。上の式から
///   左端  = 0x14 バイト = 160 ドット
///   横幅  = 0x14 ワード = 40 バイト = 320 ドット（帯 LCG#1 の幅と一致）
///   高さ  = (0x14 * 行数 / 4 + 1) * 4 行 = 20 * 行数 + 4 ドット
/// となる。上端は <see cref="DialogueBox.TopEdge"/>（120）、
/// 姿の見えない相手の声だけ <see cref="DialogueBox.HighTopEdge"/>（50）。
/// </summary>
public static class DialogueWindow
{
    /// <summary>原典の画面の大きさ。</summary>
    public const int ScreenWidth = 640, ScreenHeight = 400;

    /// <summary>窓の左端と横幅（ドット）。</summary>
    public const int Left = 160, Width = 320;

    /// <summary>上端に貼る帯（LCG#1）の高さ。</summary>
    public const int StripHeight = 32;

    /// <summary>本体の塗りの単位（原典は4行ずつ進む）。</summary>
    public const int RowStep = 4;

    /// <summary>行数から窓の高さ（ドット）を出す。原典 = 20 * 行数 + 4。</summary>
    public static int Height(int lines)
    {
        if (lines < 1) lines = 1;
        return (0x14 * lines / RowStep + 1) * RowStep;
    }

    /// <summary>窓の位置と大きさ。原典の画面座標（左上が原点）。</summary>
    public static RectInt For(int lines, bool high = false)
    {
        int top = high ? DialogueBox.HighTopEdge : DialogueBox.TopEdge;
        return new RectInt(Left, top, Width, Height(lines));
    }

    /// <summary>そのセリフを出す窓。行数は本文から数える。</summary>
    public static RectInt ForBody(string body, bool high = false)
    {
        return For(DialogueBox.WindowLines(body), high);
    }

    /// <summary>
    /// Unity の Canvas（基準 1152x720）と原典の画面（640x400）の倍率。
    /// 1152/640 = 720/400 = 1.8 で縦横とも同じなので、原典の座標をそのまま使える。
    /// </summary>
    public const float CanvasScale = 1.8f;

    public const float CanvasWidth = ScreenWidth * CanvasScale;
    public const float CanvasHeight = ScreenHeight * CanvasScale;

    /// <summary>原典の画面座標（左上原点）を Canvas の左上基準の位置に直す。</summary>
    public static Vector2 ToCanvas(RectInt r)
    {
        return new Vector2(r.xMin * CanvasScale, -r.yMin * CanvasScale);
    }

    /// <summary>原典の大きさを Canvas の大きさに直す。</summary>
    public static Vector2 SizeOnCanvas(RectInt r)
    {
        return new Vector2(r.width * CanvasScale, r.height * CanvasScale);
    }

    /// <summary>画面からはみ出さないか。</summary>
    public static bool Fits(RectInt r)
    {
        return r.xMin >= 0 && r.yMin >= 0
            && r.xMax <= ScreenWidth && r.yMax <= ScreenHeight;
    }
}
