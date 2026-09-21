using UnityEngine;

/// <summary>
/// 原典（PC98版 魔塔）の戦闘規則。純粋な計算のみを持つ。
///
/// 公式マニュアルより:
///   1回の攻撃でモンスターに与えられるダメージ = プレーヤーの攻撃力 − モンスターの防御力
///   1回の攻撃でプレーヤーが受けるダメージ     = モンスターの攻撃力 − プレーヤーの防御力
///   戦闘はプレーヤー、モンスター、…の順で交互に攻撃し、先に生命力が0になった方の負け。
///   自分の攻撃力以上の防御力を持った敵は攻撃できない（＝倒せない）。
///   乱数要素は一切なし。
///
/// 特殊アイテムによる補正（セリフ原文より）:
///   十字架            … バンパイア・ゾンビに対する攻撃力が2倍
///   ドラゴンスレーヤー … ドラゴンに対する攻撃力が2倍
///   幸運の金貨        … 敵が持っているGOLDが2倍
///
/// 「バンパイアは十字架が無いと倒せない」は規則ではなく忠告。
/// 機械的には倒せるが被ダメージが桁違いになる（下の CanDefeat のコメント参照）。
/// </summary>
public static class BattleRules
{
    [System.Flags]
    public enum Charms
    {
        None = 0,
        Cross = 1 << 0,        // 十字架
        DragonSlayer = 1 << 1, // ドラゴンスレーヤー
        LuckyCoin = 1 << 2,    // 幸運の金貨
    }

    /// <summary>補正対象を判定するための敵の種別。</summary>
    public enum EnemyKind { Normal, Zombie, Vampire, Dragon }

    /// <summary>
    /// プレハブのタグから補正対象の種別を判定する。
    ///
    /// 対象は**原典の戦闘ルーチン（MTE.EXE 0x6101 / 0x6142）が名指ししているIDだけ**:
    ///   十字架         → ID85 ゾンビ / ID87 ゾンビナイト / ID95 バンパイア
    ///   ドラゴンスレイヤー → ID122 ドラゴン のみ
    ///
    /// ⚠ 以前は tag.Contains("Zombie") で判定していたが、これは
    ///    Enemy_GreatZombieSoldier(ID139 = 原典名「兵士」) と
    ///    Enemy_GreatZombie(ID141 = 原典名「死霊兵士」) まで拾ってしまい、
    ///    原典では効かない相手に十字架の2倍が乗っていた。名指しに改めた。
    /// </summary>
    public static EnemyKind KindFromTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return EnemyKind.Normal;
        if (tag == "Enemy_Dragon") return EnemyKind.Dragon;             // ID122
        // バンパイア(ID95)はマップに配置されずイベントで登場する。
        // リメイク側の旧プレハブ Enemy_Vampire も同一視する。
        if (tag == "Enemy_Vampire" || tag == "Enemy_ID95") return EnemyKind.Vampire;
        if (tag == "Enemy_Zombie"          // ID85 ゾンビ
         || tag == "Enemy_ZombieWorrior")  // ID87 ゾンビナイト
            return EnemyKind.Zombie;
        return EnemyKind.Normal;
    }

    /// <summary>十字架・ドラゴンスレーヤーを加味した実効攻撃力。</summary>
    public static int EffectiveAttack(int attack, EnemyKind kind, Charms charms)
    {
        bool crossWorks = (charms & Charms.Cross) != 0
                          && (kind == EnemyKind.Zombie || kind == EnemyKind.Vampire);
        bool slayerWorks = (charms & Charms.DragonSlayer) != 0 && kind == EnemyKind.Dragon;
        return (crossWorks || slayerWorks) ? attack * 2 : attack;
    }

    /// <summary>1回の攻撃で与えるダメージ。防御力が上回るなら0。</summary>
    public static int DamagePerHit(int attack, int defense)
    {
        int d = attack - defense;
        return d > 0 ? d : 0;
    }

    /// <summary>
    /// その敵に攻撃できるか。原典の規則は
    /// 「自分の攻撃力以上の防御力を持った敵は攻撃できない」の一点のみ。
    ///
    /// ※ バンパイアについて原典のヒントに
    ///   「特にバンパイアは十字架を持っていないと倒せないだろう」とあるが、
    ///   これは規則ではなく忠告。実際には攻撃力が相手の防御力(66)を超えていれば
    ///   倒せるが、被ダメージが桁違いになるため実質的に死ぬ、という意味。
    ///   例: 自攻80/自防80 なら 十字架なし 32撃で被3689、十字架あり 5撃で被476。
    ///   よってここで禁止はしない。十字架の効果は「ダメージ2倍」だけ。
    /// </summary>
    public static bool CanDefeat(int playerAttack, int enemyDefense,
                                 EnemyKind kind, Charms charms)
    {
        return DamagePerHit(EffectiveAttack(playerAttack, kind, charms), enemyDefense) > 0;
    }

    /// <summary>敵を倒すのに必要な攻撃回数。倒せない場合は -1。</summary>
    public static int HitsToDefeat(int enemyHP, int damagePerHit)
    {
        if (damagePerHit <= 0) return -1;
        return (enemyHP + damagePerHit - 1) / damagePerHit;   // 切り上げ
    }

    public struct Prediction
    {
        public bool CanWin;
        public int DamageTaken;   // 勝つまでに失う生命力
        public int GoldGained;
        public int PlayerHits;    // プレーヤーの攻撃回数
    }

    /// <summary>
    /// 戦闘結果を事前計算する。勇者のオーブが表示する内容そのもの。
    /// 先手はプレーヤーなので、プレーヤーの最後の一撃の後に敵は反撃しない。
    /// </summary>
    public static Prediction Predict(int playerAttack, int playerDefense,
                                     int enemyHP, int enemyAttack, int enemyDefense,
                                     int enemyGold, EnemyKind kind, Charms charms)
    {
        var r = new Prediction();
        if (!CanDefeat(playerAttack, enemyDefense, kind, charms)) return r;

        int myDmg = DamagePerHit(EffectiveAttack(playerAttack, kind, charms), enemyDefense);
        int hits = HitsToDefeat(enemyHP, myDmg);
        int theirDmg = DamagePerHit(enemyAttack, playerDefense);

        r.CanWin = true;
        r.PlayerHits = hits;
        r.DamageTaken = theirDmg * (hits - 1);   // 最後の一撃で倒すので反撃は hits-1 回
        r.GoldGained = (charms & Charms.LuckyCoin) != 0 ? enemyGold * 2 : enemyGold;
        return r;
    }
}
