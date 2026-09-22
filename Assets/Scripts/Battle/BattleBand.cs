using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戦闘中の黒い帯に出す数字の移り変わり。**作者の旧版の見せ方**（<see cref="AuthorStyle"/>）。
///
/// 旧版 `BattleSystem.RunTurn`（commit 2b383ea^）はこう回していた:
///
/// <code>
///   プレーヤーが殴る → 0.1秒待つ → 相手の欄を書き直す
///   相手がまだ生きていれば → 0.1秒待つ → 相手が殴る → 自分の欄を書き直す
/// </code>
///
/// つまり**1ターンごとにHPが減っていく**。数の減り方そのものは原典の戦闘規則
/// （<see cref="BattleRules"/>）と同じなので、ここは並べ直すだけ。
/// 間合いは原典の待ち（<see cref="BattlePresentation"/>）に乗せるので、
/// 音との合わせ方は今までどおりになる。
/// </summary>
public static class BattleBand
{
    /// <summary>
    /// ひとつの戦いの数字ひとそろい。戦う前に <see cref="TowerBattle.Resolve"/> が
    /// 見ていた値をそのまま持つ（封印で下がったゼノの数値もここに入る）。
    /// </summary>
    public struct Numbers
    {
        /// <summary>相手のプレハブ名。</summary>
        public string Enemy;
        public int PlayerHp, PlayerAttack, PlayerDefense;
        public int EnemyHp, EnemyAttack, EnemyDefense;
        /// <summary>自分の一撃で相手が減る量。</summary>
        public int PlayerDamage;
        /// <summary>相手の一撃で自分が減る量。</summary>
        public int EnemyDamage;
        /// <summary>自分が殴る回数。</summary>
        public int Hits;
        /// <summary>勝って手に入る金。</summary>
        public int Gold;
    }

    /// <summary>ある時点の欄の中身。</summary>
    public struct Tick
    {
        /// <summary>「第 N ターン」の N（1から）。</summary>
        public int Turn;
        public int PlayerHp;
        public int EnemyHp;
    }

    /// <summary>
    /// 自分が <paramref name="hitsDone"/> 回、相手が <paramref name="countersDone"/> 回
    /// 殴り終わった時点の欄の中身。
    /// </summary>
    public static Tick At(int playerHp, int enemyHp, int playerDamage, int enemyDamage,
                          int hitsDone, int countersDone)
    {
        if (hitsDone < 0) hitsDone = 0;
        if (countersDone < 0) countersDone = 0;
        return new Tick
        {
            Turn      = hitsDone < 1 ? 1 : hitsDone,
            PlayerHp  = Mathf.Max(0, playerHp - enemyDamage * countersDone),
            EnemyHp   = Mathf.Max(0, enemyHp - playerDamage * hitsDone),
        };
    }

    /// <summary>
    /// 戦いのはじめから終わりまでの移り変わり。旧版の順（自分→相手→自分→…）で、
    /// 最後の一撃で倒すので相手の反撃は1回少ない。
    /// </summary>
    public static List<Tick> Trace(int playerHp, int enemyHp,
                                   int playerDamage, int enemyDamage, int hits)
    {
        var list = new List<Tick>();
        list.Add(At(playerHp, enemyHp, playerDamage, enemyDamage, 0, 0));
        for (int i = 0; i < hits; i++)
        {
            list.Add(At(playerHp, enemyHp, playerDamage, enemyDamage, i + 1, i));
            if (i == hits - 1) break;                  // 倒したので反撃は無い
            list.Add(At(playerHp, enemyHp, playerDamage, enemyDamage, i + 1, i + 1));
        }
        return list;
    }

    /// <summary>
    /// 勝ったあとの金の一言（旧版 EarnedCoinDialogue）。
    /// **帯には出さない** — 中央の知らせ（`battle_gold`）と二重になるため（作者の指摘）。
    /// 文面だけ残してある。
    /// </summary>
    public static string GoldLine(int gold)
    {
        return GameText.Num(gold) + " ゴールド 手に入れた";
    }
}
