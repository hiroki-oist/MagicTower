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
/// ★**3つの判定は順に全部通る**（作者の報告で直した）。原典は
/// 0x1b75（魔導師）→ 0x1c48〜0x1d0e（魔術士Ａの4マス）→ 0x1e29〜0x1eef（Ｂの4マス）
/// と続けて書いてあり、**途中で抜けない**。だから魔導師に挟まれながら
/// 魔術士Ａと魔術士Ｂの両方に隣接していれば、半減＋200＋100 を一度に受ける。
///
/// ただし**同じ種類が何体いても1回**。Ａの4マスの比較はどれも同じ
/// 0x1d21（盾を見て 200 引く所）へ飛ぶので、Ａが2体いても 400 にはならない。
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

    /// <summary>その種類が上下左右のどれかに居るか。</summary>
    public static bool IsNextTo(Neighbors n, int id)
    {
        return n.Up == id || n.Down == id || n.Left == id || n.Right == id;
    }

    /// <summary>
    /// 隣接している魔術士から受けるダメージの合計。
    /// ⚠ **ＡとＢは重なる**（原典は別々の枝で、Ａを引いたあとＢの判定に進む）。
    /// 同じ種類が2体いても1回ぶんなのは原典どおり。
    /// </summary>
    public static int WizardDamage(Neighbors n)
    {
        int dmg = 0;
        if (IsNextTo(n, WizardAId)) dmg += WizardADamage;
        if (IsNextTo(n, WizardBId)) dmg += WizardBDamage;
        return dmg;
    }

    /// <summary>
    /// 移動後の生命力を返す。<paramref name="messageKeys"/> には出すべき
    /// Messages.txt のキーが**受けた順に**入る（受けていなければ空）。
    ///
    /// 原典の並びのまま、魔導師の半減 → 魔術士Ａの200 → 魔術士Ｂの100 と
    /// **続けて**当てる。途中で抜けない。
    /// </summary>
    public static int Apply(int hp, int shieldRank, Neighbors n,
                            System.Collections.Generic.List<string> messageKeys)
    {
        if (messageKeys != null) messageKeys.Clear();
        if (Immune(shieldRank)) return hp;

        if (IsPincered(n))
        {
            // 原典 0x1bfe は**生命力が1のときだけ半分にしない**（cmp 1 → je）
            if (hp != 1) hp = hp / 2;
            if (messageKeys != null) messageKeys.Add("mage_halve");
        }
        if (IsNextTo(n, WizardAId))
        {
            hp -= WizardADamage;
            if (messageKeys != null) messageKeys.Add("wizard_200");
        }
        if (IsNextTo(n, WizardBId))
        {
            hp -= WizardBDamage;
            if (messageKeys != null) messageKeys.Add("wizard_100");
        }
        return Mathf.Max(0, hp);
    }

    /// <summary>1つだけ受け取る古い口（試験と、1件しか出さない所のため）。</summary>
    public static int Apply(int hp, int shieldRank, Neighbors n, out string messageKey)
    {
        var keys = new System.Collections.Generic.List<string>();
        int after = Apply(hp, shieldRank, n, keys);
        messageKey = keys.Count > 0 ? keys[0] : null;
        return after;
    }
}
