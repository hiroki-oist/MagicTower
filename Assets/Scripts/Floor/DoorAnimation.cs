using UnityEngine;

/// <summary>
/// 鍵で扉を開けたときの動き。原典 PC98版 MTE.EXE の `0x07be6`
/// （扉を開ける処理 `0x023ee` から `lcall 0x68b, 0x1336` で呼ばれる）。
///
/// <code>
///   ; 9コマぶんの元絵を並べる（0xa00 = 盤面の1段ぶんと同じ間隔）
///   si = 0x48; do { … } while ((si += 0xa00) &lt; 0x5a48);      ; 9回
///   色1 → 0x5d7c / 色2 → 0x641b / 色3 → 0x6aa2 の扉の絵を
///   透過（索引1）で9コマに重ねる                              （lcall 0xf5f,0xd95 の 0x11）
///   音 11 を鳴らす                                             （lcall 0x10da,0x20b）
///   ; 扉のマス（向き 1=+X / 2=-X / 3=+Y / 4=-Y）
///   VRAM = Y*0xa00 + X*4 + 0x792                               ; 盤面の式と同じ
///   si = 0x48;
///   do { そのマスに1コマ貼る                                   （lcall 0xf5f,0x892）
///        0x92a4();                    ; 盤面の2コマ動きを1つ進める
///   } while ((si += 0xa00) &lt; 0x5a48);                         ; 9コマ
///   0x9288(8);                          ; さらに8コマぶん 0x92a4 を回す
/// </code>
///
/// **1コマ = 1/60秒**。`0x92a4` の中に縦帰線待ち（`lcall 0xf5f,0x3d9`）が1回あるため。
/// 絵は LCG.P58 の #2〜#5（32x288 ＝ 32x32 が縦に9つ）で、色を盤面のタイルと
/// 突き合わせて **#2=黄 / #3=紫 / #4=赤 / #5=門** と確定した。
/// 書き出しは `docs/lcg_to_doors.py`（`Assets/Sprites/Doors/<色>_<コマ>.png`）。
/// </summary>
public static class DoorAnimation
{
    /// <summary>コマ数（原典の輪は 0x48 から 0xa00 きざみで 0x5a48 まで）。</summary>
    public const int Frames = 9;

    /// <summary>動きのあと、盤面の動きだけを回すコマ数（原典 0x9288(8)）。</summary>
    public const int TailFrames = 8;

    /// <summary>
    /// 門（封印扉）が開くときの後ろの間（原典 `0x07a68` は `0x9288(0x14)`）。
    /// 門の方は鍵の扉と別の関数だが、**9コマ・音11・貼り方は同じ**で、
    /// 絵が LCG #5、後ろの間だけ 20コマと長い。
    /// 呼ばれるのは解錠（0x4762）から25か所と、位置イベント・金騎士の突撃・
    /// ボススケルトン登場から各1〜2か所。
    /// </summary>
    public const int SealedTailFrames = 0x14;

    /// <summary>その色の後ろの間（コマ数）。</summary>
    public static int TailOf(string cell)
    {
        return cell == "s" ? SealedTailFrames : TailFrames;
    }

    /// <summary>1コマの長さ。0x92a4 の中の縦帰線待ち1回ぶん。</summary>
    public const float FrameSeconds = 1f / 60f;

    /// <summary>始めから終わりまでの長さ（秒）。</summary>
    public static float Duration { get { return (Frames + TailFrames) * FrameSeconds; } }

    /// <summary>鳴らす音（原典 0x7be6 の中の 0xb）。</summary>
    public const int Sound = 11;

    /// <summary>
    /// 盤面の記号 → 絵の名前。原典が渡す色の番号は 黄=1 / 紫=2 / 赤=3 で、
    /// 門（封印扉）は別の所から使う。
    /// </summary>
    public static string ColorOf(string cell)
    {
        switch (cell)
        {
            case "y": return "Yellow";
            case "b": return "Purple";
            case "r": return "Red";
            case "s": return "Sealed";
        }
        return null;
    }

    /// <summary>原典が 0x7be6 に渡す色の番号。門は渡されないので0。</summary>
    public static int NumberOf(string cell)
    {
        switch (cell)
        {
            case "y": return 1;
            case "b": return 2;
            case "r": return 3;
        }
        return 0;
    }

    /// <summary>絵のある場所。</summary>
    public static string SpritePath(string color, int frame)
    {
        return $"Assets/Sprites/Doors/{color}_{frame}.png";
    }
}
