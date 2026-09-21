using UnityEngine;

/// <summary>
/// **調べもの用の細工**。原典の再現ではないので、見終わったら必ず false に戻すこと。
///
/// 作者からの頼み（2026-09-21）:
/// 「本来はゲーム開始時（3Fのイベント後）は攻守ともに10なんだろうけど、
///   デバッグ用にいまは一旦1000にしておいてくれる？
///   そしたらイベント側の不具合とかがないか見れるから」
///
/// <see cref="AuthorStyle"/> と同じで、**原典から外れる所はここ1か所にまとめる**。
/// </summary>
public static class DebugPlay
{
    /// <summary>
    /// ゼノに奪われたあとの攻撃力・防御力を <see cref="StrongStat"/> にする。
    /// **false が原典どおり**（攻10 / 防10、原典 0x06f1b〜0x06f49）。
    /// </summary>
    public static readonly bool StrongAfterZeno = true;

    /// <summary>
    /// そのときの攻撃力・防御力。頼まれたのは 1000 だが、**原典の上限が 990**
    /// （<see cref="PlayerState.MaxAttack"/> / <see cref="PlayerState.MaxDefense"/>、
    /// 原典は描き直すたびに頭を押さえる）なので、そこで止まる 990 にしてある。
    /// </summary>
    public const int StrongStat = PlayerState.MaxAttack;

    /// <summary>
    /// 生命力は原典のまま 400 にしてある。**魔法攻撃は防御力を素通りする**
    /// （魔術士Ａ=200・Ｂ=100・魔導師は半分、原典 0x1b75）ので、
    /// 塔を上まで見て回るなら合わせて上げる必要が出るかもしれない。
    /// そのときはここを true にする。
    /// </summary>
    public static readonly bool StrongHpAfterZeno = false;

    /// <summary>そのときの生命力。</summary>
    public const int StrongHp = 9990;
}
