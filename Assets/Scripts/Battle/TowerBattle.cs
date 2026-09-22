using UnityEngine;

/// <summary>
/// 敵との戦闘を決着させる。原典 PC98版の戦闘ルーチン（MTE.EXE 0x6082）に従う。
///
/// <code>
///   1撃ダメージ = max(0, 攻撃力 - 防御力)      ; 0 なら「相手の防御力が高すぎて攻撃できない。」
///   被ダメージ  = max(0, 敵攻撃力 - 自防御力) × (必要撃数 - 1)
///                                              ; 最後の一撃で倒すので反撃は1回少ない
///   十字架          → ID85 ゾンビ / ID87 ゾンビナイト / ID95 バンパイア に攻撃力2倍
///   ドラゴンスレイヤー → ID122 ドラゴンにのみ攻撃力2倍
///   幸運の金貨      → 得られる金が2倍
/// </code>
///
/// 勝つと**そのマスは床になる**。門の解錠判定はその状態を見ているので、
/// 盤面の書き換えは <see cref="GameRules.OnEnemyDefeated"/> 側で行う。
/// </summary>
public static class TowerBattle
{
    public struct Result
    {
        public bool Fought;         // そもそも戦闘になったか
        public bool Won;
        /// <summary>
        /// 生命力が尽きたか。原典 0x64cd は**相手の一撃ごとに**
        /// `生命力 > その一撃` を調べ、そうでなければ生命力を0にして
        /// ゲームオーバー（0x68b:0x2efc）へ行く。
        /// 受ける合計と比べるのと同じことなので、ここでは合計で判定する。
        /// </summary>
        public bool Died;
        public int DamageTaken;
        public int GoldGained;
        public int Hits;
        /// <summary>倒せないときに出す文言のキー。</summary>
        public string MessageKey;

        // 戦いに使った数字そのもの（封印で下がったゼノの数値もこちら）。
        // 作者の旧版の見せ方（BattleBand）が帯に出すのに要る。
        public int EnemyHp, EnemyAttack, EnemyDefense;
        /// <summary>自分の一撃で相手が減る量。</summary>
        public int PlayerDamage;
        /// <summary>相手の一撃で自分が減る量。</summary>
        public int EnemyDamage;
    }

    /// <summary>
    /// プレハブ名から原典の数値を読む。Resources/Battlers/&lt;名前&gt;.asset。
    ///
    /// 盤面（.floor）は3x3のボスと魔導師ゼノを <c>Boss_</c> で呼ぶが、
    /// 数値アセットは <c>Enemy_</c> で作られている。名前が食い違うと
    /// **大ダコ・ドラゴン・ゼノの戦闘が成立しない**ので、ここで読み替える。
    /// </summary>
    public static BattlerBase LoadEnemy(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return null;
        var e = Resources.Load<BattlerBase>("Battlers/" + prefabName);
        if (e == null && prefabName.StartsWith("Boss_"))
            e = Resources.Load<BattlerBase>("Battlers/Enemy_" + prefabName.Substring(5));
        return e;
    }

    /// <summary>所持品から戦闘の補正を作る。</summary>
    public static BattleRules.Charms CharmsOf(PlayerState player)
    {
        var c = BattleRules.Charms.None;
        if (player == null) return c;
        if (player.Has(SpecialItems.Kind.Cross))        c |= BattleRules.Charms.Cross;
        if (player.Has(SpecialItems.Kind.DragonSlayer)) c |= BattleRules.Charms.DragonSlayer;
        if (player.Has(SpecialItems.Kind.LuckyCoin))    c |= BattleRules.Charms.LuckyCoin;
        return c;
    }

    /// <summary>
    /// 戦って決着をつける。勝てば生命力と所持金に反映する。
    /// 倒せない相手のときは何もせず <c>MessageKey = "cannot_attack"</c> を返す。
    /// </summary>
    public static Result Resolve(PlayerState player, string enemyPrefab)
    {
        var r = new Result();
        var e = LoadEnemy(enemyPrefab);
        if (player == null || e == null) return r;

        var kind = BattleRules.KindFromTag(enemyPrefab);
        var charms = CharmsOf(player);

        // サイコロの５の目で封じられていればゼノの数値が10分の1になる
        int ehp = e.MaxHP, eatk = e.Attack, edef = e.Defense;
        MagicSeal.Adjust(player, enemyPrefab, ref ehp, ref eatk, ref edef);

        var p = BattleRules.Predict(player.Attack, player.Defense,
                                    ehp, eatk, edef, e.Coin, kind, charms);

        r.Fought = true;
        r.EnemyHp = ehp; r.EnemyAttack = eatk; r.EnemyDefense = edef;
        r.PlayerDamage = BattleRules.DamagePerHit(
            BattleRules.EffectiveAttack(player.Attack, kind, charms), edef);
        r.EnemyDamage = BattleRules.DamagePerHit(eatk, player.Defense);
        if (!p.CanWin)
        {
            r.MessageKey = "cannot_attack";   // 「相手の防御力が高すぎて攻撃できない。」
            return r;
        }

        r.Hits = p.PlayerHits;
        r.DamageTaken = p.DamageTaken;

        // 原典 0x64cd: 相手の一撃で生命力が尽きるなら、そこで終わり。
        // 倒す前に死ぬので**相手は残り、金も入らない**（0x6327 まで届かない）。
        if (p.DamageTaken >= player.Hp)
        {
            r.Died = true;
            // **倒れるまでに入れた斬りの数**だけ見せる。原典 0x64cd は一撃ごとに
            // 生命力を見ていて、尽きたらそこで終わり。最後まで斬り合ってから
            // 死ぬように見えていた（作者の報告）。
            r.Hits = r.EnemyDamage > 0
                   ? Mathf.Min(p.PlayerHits,
                               Mathf.CeilToInt(player.Hp / (float)r.EnemyDamage))
                   : 1;
            r.DamageTaken = player.Hp;
            player.Hp = 0;
            return r;
        }

        r.Won = true;
        r.GoldGained = p.GoldGained;
        player.Hp -= p.DamageTaken;
        player.Gold += p.GoldGained;
        return r;
    }
}
