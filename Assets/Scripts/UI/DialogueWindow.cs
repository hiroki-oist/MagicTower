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
/// となる。上端は <see cref="DialogueBox.TopEdge"/>（120）、
/// 姿の見えない相手の声だけ <see cref="DialogueBox.HighTopEdge"/>（50）。
///
/// **高さ**は塗りの届く範囲で決まる。本体の塗りは「16行ぶんを4行ずつずらして
/// 回数ぶん」なので、届くのは 上端+0x10 から
///   16 + 4 * (回数 - 1) = 16 + 20 * 行数 ドット
/// ＝ 上端+32+20*行数 まで。帯は 上端から32行なので、合わせて
///   **高さ = 20 * 行数 + 32 ドット**。
/// （以前ここを「繰り返し回数×4 ＝ 20*行数+4」と書いていたが、
///   それは塗りの**進む量**で、最後の塗りの16行ぶんが抜けていた。）
///
/// 文字は窓の中の (176, 上端+12) から。x=0x16 バイト＝176 で左端から16ドット内側、
/// y は 上端+0xc（原典 0x9563〜0x9571）。行送りは <see cref="ScreenLayout.LineAdvance"/>。
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

    /// <summary>本体の塗り1回ぶんの高さ（原典は16行を4行ずつずらして塗る）。</summary>
    public const int FillHeight = 16;

    /// <summary>窓の中の文字の位置（窓の左上からのずれ、ドット）。</summary>
    public const int TextLeft = 16, TextTop = 12;

    /// <summary>行数から窓の高さ（ドット）を出す。原典 = 20 * 行数 + 32。</summary>
    public static int Height(int lines)
    {
        if (lines < 1) lines = 1;
        // 本体は 上端+16 から、16行ぶんの塗りを4行ずつずらして times 回。
        // 届く下端 = 16 + (16 + 4*(times-1)) = 32 + 20*行数。帯（上端から32行）も含む。
        int times = 0x14 * lines / RowStep + 1;
        int body = FillHeight + FillHeight + RowStep * (times - 1);
        return Mathf.Max(StripHeight, body);
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
