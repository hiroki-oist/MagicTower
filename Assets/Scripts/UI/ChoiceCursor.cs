using UnityEngine;

/// <summary>
/// 選ぶときの印。原典 PC98版 MTE.EXE の `0x9e78`（「◎はい／◎いいえ」や祭壇の望み）。
///
/// 絵は **PICTURE.P58 の4枚目と5枚目**。まとめ読みの buffer の中では
/// 5枚が隙間なく並んでいて、実際の大きさと変位がぴったり合う:
///
/// <code>
///   #0 黄の鍵  65バイト → -0x40c
///   #1 赤の鍵  74バイト → -0x3cb
///   #2 紫の鍵  74バイト → -0x381
///   #3 印       111バイト → -0x337   ← 0x9f92 / 0x9ed9 あたりが貼る
///   #4 印2      112バイト → -0x2c8   ← 0x9fc7 が貼る
/// </code>
///
/// 選ばせている間、原典は数え（`[bp-1]`）で2枚を切り替える:
///
/// <code>
///   0x9f7f  if ([bp-1] &lt;= 0x14) { #3 を貼る; [bp-1]++ }
///   0x9fae  else if ([bp-1] &lt;= 0x28) { #4 を貼る; [bp-1]++ }
///   0x9fcc  else [bp-1] = 0
/// </code>
///
/// 1回ぶんの待ちは `lcall 0xf5f,0x3f2` の 0xa(10) と縦帰線1回で **11/60秒**。
/// ⚠ ただし**この輪がキー入力を待って回っているのか、待たずに回っているのか**は
/// 読み切れていない（`lcall 0x107e,0x528` の性質が未確定）。
/// ここでは**待たずに回る**方に取って、21回ぶん＝ `21 * 11/60 ≒ 3.85秒`ごとに
/// 切り替える形にしてある。速さが違っていたらここだけ直せばよい。
/// </summary>
public static class ChoiceCursor
{
    /// <summary>1枚を出しておく回数（原典 0〜0x14 と 0x15〜0x28）。</summary>
    public const int StepsPerFrame = 0x15;

    /// <summary>1回ぶんの待ち（0xa の待ち＋縦帰線1回）。</summary>
    public const float StepSeconds = 11f / 60f;

    /// <summary>1枚を出しておく時間（秒）。</summary>
    public static float FrameSeconds { get { return StepsPerFrame * StepSeconds; } }

    /// <summary>今どちらの絵か（0 = PICTURE #3 / 1 = PICTURE #4）。</summary>
    public static int FrameAt(float seconds)
    {
        if (FrameSeconds <= 0f) return 0;
        int n = Mathf.FloorToInt(seconds / FrameSeconds);
        return ((n % 2) + 2) % 2;
    }
}
