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
///                     n=13..29 が midi/*.mid
/// </code>
///
/// 曲は <c>docs/tools/MidiRender.swift</c>（macOS 内蔵の音源で MIDI を書き出す道具）で
/// WAV にして <c>Assets/Resources/Music</c> に置いてある。
/// 割り当ては Java版の順番のまま、**10階ごとのかたまり**に当てた
/// （Java版は 13階ごとに切り替えていたが、この遊びは10階ごとに区切られているため）。
/// </summary>
public static class JavaMusic
{
    /// <summary>Java版の曲を使うか（作者の指定）。</summary>
    public const bool Enabled = true;

    /// <summary>かたまりごとの曲。1〜10階 / 11〜20 / 21〜30 / 31〜40 / 41〜50。</summary>
    static readonly string[] Blocks =
        { "Music/Block1", "Music/Block2", "Music/Block3", "Music/Block4", "Music/Block5" };

    /// <summary>戦いの曲（Java版 attackMonster が切り替える 20＝A_046）。</summary>
    public const string Boss = "Music/Boss";

    /// <summary>
    /// 戦いの曲にする相手。Java版は**どの戦いでも**曲を変えるが、
    /// 作者の指定で**ボスだけ**にしてある（10階の金の骸骨騎士・20階のヴァンパイア・
    /// 40階と32階の金騎士・49/50階のゼノ。3x3の大物2体も入れてある）。
    /// </summary>
    public static readonly HashSet<string> BossEnemies = new HashSet<string>
    {
        "Enemy_BossSkeleton",      // 10階
        "Enemy_ID95",              // 20階（ヴァンパイア）
        "Boss_GiantOctopus",       // 15階
        "Enemy_Worrior",           // 32階・40階（金騎士）
        "Boss_Dragon",             // 35階
        "Boss_Zeno",               // 49・50階
    };

    /// <summary>その階で流す曲。</summary>
    public static string ForFloor(int floor)
    {
        int i = Mathf.Clamp((Mathf.Max(1, floor) - 1) / 10, 0, Blocks.Length - 1);
        return Blocks[i];
    }

    /// <summary>その相手はボスか（戦いの曲に切り替えるか）。</summary>
    public static bool IsBoss(string enemy)
    {
        return !string.IsNullOrEmpty(enemy) && BossEnemies.Contains(enemy);
    }
}
