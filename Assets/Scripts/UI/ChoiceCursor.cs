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
/// ⚠ **1回ぶんの間隔は読み切れていない**。輪はキーを読む関数
/// （`lcall 0x107e,0x528` ＝ linear 0x10d08）を毎回叩いていて、そこは
/// **押されている鍵の表 `cs:[0x42d..]` を頭から走査して番号を返す**作り
/// （見つからなければ 0xff、頭に `out 0x5f` の短い間合いが入る）。
/// **押されるまで待つのか、押されていなければすぐ戻るのか**が読み切れない。
///
/// ただし **2枚あること自体が「時間で切り替わる」ことの裏づけ**である
/// （押したときにしか変わらないなら、2枚目を用意する意味がない）。
/// そこで**待たずに回る**方に取って、21回ぶん＝ `21 * 11/60 ≒ 3.85秒`ごとの
/// 切り替えにしてある。遊んでみて速さが違っていたら、ここの定数だけ直せばよい。
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
