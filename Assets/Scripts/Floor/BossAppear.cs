using UnityEngine;

/// <summary>
/// 20階のボスが現れる動き。原典 PC98版 MTE.EXE の `0x03044`（ボス登場）の中。
///
/// <code>
///   0x3119  LCG#6（0x7577）を作業域 0x3c00 に敷く   ; 幅 0x3c=60バイト=480、高さ 0xc0=192
///   di = 0; do {                                   ; di: 0 → 0x1e00（96行ぶん）… 縦2段
///     si = 0; do {                                 ; si: 0,0xc,0x18,0x24,0x30 … 横5列
///       f(0x2fa2, 6, 0x60, di + si);               ; 幅6ワード=96ドット x 高さ0x60=96ドット
///       0x19(25) 待つ                              ; lcall 0xf5f,0x3f2
///     } while ((si += 0xc) &lt; 0x3c);
///   } while ((di += 0x1e00) &lt; 0x3c00);
/// </code>
///
/// つまり **96x96（＝3x3マス）の絵を 横5 x 縦2 の10コマ**として、盤面の同じ場所へ
/// 順に貼る。貼り先 `0x2fa2` は `VRAM = y*80 + xバイト` を解くと y=152 / x=34バイト＝272ドット、
/// 盤面の式（x*32+144 / y*32+24）に直すと**左上のマスが (-1,+1)**、つまり**中心は (0,0)**。
///
/// 書き出しは `docs/lcg_to_doors.py`（`Assets/Sprites/Effects/BossAppear_0..9.png`）。
/// </summary>
public static class BossAppear
{
    /// <summary>コマ数（横5 x 縦2）。</summary>
    public const int Frames = 10;

    /// <summary>1コマの待ち（原典 0x19）。</summary>
    public const int FrameWait = 0x19;

    /// <summary>1コマの長さ（秒）。待ちは垂直帰線の回数なので 1回 = 1/60秒。</summary>
    public static float FrameSeconds { get { return FrameWait / 60f; } }

    /// <summary>始めから終わりまで（秒）。</summary>
    public static float Duration { get { return Frames * FrameSeconds; } }

    /// <summary>絵の1辺（ドット）。3x3マスぶん。</summary>
    public const int Size = 96;

    /// <summary>出る場所（3x3の中心）。原典の貼り先 0x2fa2 から出した。</summary>
    public static Vector2Int Center { get { return new Vector2Int(0, 0); } }

    /// <summary>絵のある場所。</summary>
    public static string SpritePath(int frame)
    {
        return $"Assets/Sprites/Effects/BossAppear_{frame}.png";
    }
}
