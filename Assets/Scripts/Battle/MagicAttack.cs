using UnityEngine;

/// <summary>
/// 隣接マスから受ける魔法攻撃。原典 MTE.EXE の移動後処理（0x1ab0〜0x1f80）より。
///
/// 原典は、プレーヤーが動いたあとに上下左右4マスを調べる。マスの位置は
/// <c>bx = (124×階 + Y×11 + X)×2</c> で求め、変位 +2/−2/+22/−22 で右/左/下/上を見る。
///
/// <code>
///   魔導師(ID93)   : 左右の両方、または上下の両方が魔導師 → 生命力を半分にする
///   魔術士Ａ(ID151): 上下左右のどれかに隣接 → 200ポイントダメージ
///   魔術士Ｂ(ID153): 上下左右のどれかに隣接 → 100ポイントダメージ
/// </code>
///
/// どの判定にも直前に「装備中の盾の番号が5（＝神盾）か」の確認があり、
/// **神盾を装備していれば魔法攻撃を受けない**。
/// 原典ヒント「神盾は、魔法使いたちの魔法攻撃を防ぐ力があるらしい」に対応する。
/// </summary>
public static class MagicAttack
{
    /// <summary>原典の物体ID。</summary>
    public const int SorcererId = 93;    // 魔導師（挟み撃ち）
    public const int WizardAId  = 151;   // 魔術士Ａ
    public const int WizardBId  = 153;   // 魔術士Ｂ

    /// <summary>神盾の装備番号。盾は 1=鉄 2=銀 3=騎士 4=聖 5=神。</summary>
    public const int DivineShieldRank = 5;

    public const int WizardADamage = 200;
    public const int WizardBDamage = 100;

    public struct Neighbors
    {
        public int Up, Down, Left, Right;   // 隣接4マスの物体ID（無ければ 0）
    }

    /// <summary>神盾を装備しているか。装備していれば魔法攻撃はすべて無効。</summary>
    public static bool Immune(int shieldRank) { return shieldRank >= DivineShieldRank; }

    /// <summary>魔導師に挟まれているか（左右の両方、または上下の両方）。</summary>
    public static bool IsPincered(Neighbors n)
    {
        return (n.Left == SorcererId && n.Right == SorcererId)
            || (n.Up == SorcererId && n.Down == SorcererId);
    }

    /// <summary>隣接している魔術士から受けるダメージ。隣接していなければ0。</summary>
    public static int WizardDamage(Neighbors n)
    {
        if (n.Up == WizardAId || n.Down == WizardAId || n.Left == WizardAId || n.Right == WizardAId)
            return WizardADamage;
        if (n.Up == WizardBId || n.Down == WizardBId || n.Left == WizardBId || n.Right == WizardBId)
            return WizardBDamage;
        return 0;
    }

    /// <summary>
    /// 移動後の生命力を返す。<paramref name="messageKey"/> には出すべき
    /// Messages.txt のキー（無ければ null）が入る。
    /// </summary>
    public static int Apply(int hp, int shieldRank, Neighbors n, out string messageKey)
    {
        messageKey = null;
        if (Immune(shieldRank)) return hp;

        if (IsPincered(n))
        {
            messageKey = "mage_halve";
            return hp / 2;                      // 原典は shr [生命力],1
        }
        int dmg = WizardDamage(n);
        if (dmg > 0)
        {
            messageKey = dmg == WizardADamage ? "wizard_200" : "wizard_100";
            return Mathf.Max(0, hp - dmg);
        }
        return hp;
    }
}
