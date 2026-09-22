using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Java版（作者の旧版）の曲を鳴らす決まり。**原典 PC98版には曲が2つしか無い**
/// （音1＝本編 / 音2＝題名・終幕）ので、これは作者の指定で足したもの。
///
/// Java版 MTower.class の作り:
/// <code>
///   BGMset()       … mapObject[69][124] に曲番号を入れて AudioPlay
///                     27(B_067) / 26(B_058) / 28(B_110) / 23(A_118) / 16(A_019)
///   attackMonster()… 戦いに入るところで BGM を止めて 20(A_046) に切り替える
///   AudioPlay(n)   … n=5..12 が wav（20f/3f/Get/Kai/Kai2/Open/Taisen/Zeno）、
///                     n=13..29 が midi/*.mid（並びは定数表そのまま）
/// </code>
///
/// 階のかたまりの5本は Java版の割り当てをそのまま使っている
/// （Java版は13階ごとに切り替えていたが、この遊びは10階ごとに区切られているため
///  かたまりの境目だけ合わせた）。
/// **ボス戦の5本は作者が全17本を聴いて選んだもの**で、Java版の 20(A_046) は使わない
/// （A_046 は53BPM・打楽器ゼロのチェレスタ曲で、戦いの曲ではなかった）。
///
/// 曲は <c>docs/make_music.py</c> が MIDI から書き出して
/// <c>Assets/Resources/Music</c> に置く。
/// </summary>
public static class JavaMusic
{
    /// <summary>Java版の曲を使うか（作者の指定）。</summary>
    public const bool Enabled = true;

    /// <summary>かたまりごとの曲。1〜10階 / 11〜20 / 21〜30 / 31〜40 / 41〜50。</summary>
    static readonly string[] Blocks =
        { "Music/Block1", "Music/Block2", "Music/Block3", "Music/Block4", "Music/Block5" };

    /// <summary>
    /// ボス戦の曲（作者の指定）。10階 B_117 / 20階 A_008 / 40階 A_005 /
    /// 49階 A_023 / 50階 A_050。32階の金騎士は40階と同じ相手なので同じ曲にしてある。
    /// </summary>
    static readonly Dictionary<int, string> BossTracks = new Dictionary<int, string>
    {
        { 10, "Music/Boss10" },
        { 20, "Music/Boss20" },
        { 32, "Music/Boss40" },
        { 40, "Music/Boss40" },
        { 49, "Music/Boss49" },
        { 50, "Music/Boss50" },
    };

    /// <summary>
    /// 戦いの曲にする相手。Java版は**どの戦いでも**曲を変えるが、
    /// 作者の指定で**ボスだけ**にしてある。
    /// 大ダコとドラゴンは（3x3の大物ではあるが）ボス扱いにしない、という指定。
    ///
    /// なお曲を切り替える主な合図はこの表ではなく
    /// <see cref="TowerGame.BossMusic"/>（退路の門が閉まった時点）で、
    /// この表は**門の閉まらない戦い**（32階の金騎士）と、
    /// ボスを倒して曲を戻すところに使う。
    /// </summary>
    public static readonly HashSet<string> BossEnemies = new HashSet<string>
    {
        "Enemy_BossSkeleton",      // 10階
        "Enemy_ID95",              // 20階（ヴァンパイア）
        "Enemy_Worrior",           // 32階・40階（金騎士）
        "Boss_Zeno",               // 49・50階
    };

    /// <summary>その階で流す曲。</summary>
    public static string ForFloor(int floor)
    {
        int i = Mathf.Clamp((Mathf.Max(1, floor) - 1) / 10, 0, Blocks.Length - 1);
        return Blocks[i];
    }

    /// <summary>その階のボス戦の曲。ボスの居ない階なら null。</summary>
    public static string BossFor(int floor)
    {
        string track;
        return BossTracks.TryGetValue(floor, out track) ? track : null;
    }

    /// <summary>その相手はボスか（戦いの曲に切り替えるか）。</summary>
    public static bool IsBoss(string enemy)
    {
        return !string.IsNullOrEmpty(enemy) && BossEnemies.Contains(enemy);
    }
}
