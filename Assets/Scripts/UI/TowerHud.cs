using UnityEngine;

/// <summary>
/// 画面に出す文字を組み立てる。原典 PC98版の画面構成に合わせる。
///
/// <code>
///   左側   「塔　Ｎ階」／ハート＝生命力／剣＝攻撃力／盾＝防御力／コイン＝所持金
///          その下に所持している特殊アイテムのアイコン
///   右上   WEAPON 欄（武器名）／SHIELD 欄（盾名）／鍵3種のアイコン
///   右下   モンスター情報（名前・命・攻・防）とメッセージ欄を兼ねる1つの枠
/// </code>
///
/// 数字は原典どおり**全角**で出す（<see cref="GameText.Num"/>）。
/// 装備名は原典が持っている6文字幅の短縮名をそのまま使う
/// （`panel_sword_N` / `panel_shield_N`）。
/// </summary>
public static class TowerHud
{
    /// <summary>
    /// 「塔　Ｎ階」。原典は先に **「塔　　階」** を書いてから（0xbaf5）、
    /// 真ん中の2マスに階を**右詰め**で重ねる（0xb8d6、x=6バイト・枠2つ）。
    /// なので2桁の階では全角空白が消えて「塔５０階」になる。
    /// </summary>
    public const int FloorDigits = 2;

    public static string FloorLabel(int floor)
    {
        return GameText.Instance.Message("floor_label",
                                         GameText.NumRight(floor, FloorDigits));
    }

    /// <summary>左側に並ぶ4つの数値。全角。</summary>
    public static string Hp(PlayerState p)      { return GameText.Num(p.Hp); }
    public static string Attack(PlayerState p)  { return GameText.Num(p.Attack); }
    public static string Defense(PlayerState p) { return GameText.Num(p.Defense); }
    public static string Gold(PlayerState p)    { return GameText.Num(p.Gold); }

    /// <summary>WEAPON 欄に出す名前。持っていなければ空。</summary>
    public static string WeaponName(PlayerState p)
    {
        return p.SwordRank >= 1 && p.SwordRank <= 5
            ? GameText.Instance.Message("panel_sword_" + p.SwordRank) : "";
    }

    /// <summary>SHIELD 欄に出す名前。</summary>
    public static string ShieldName(PlayerState p)
    {
        return p.ShieldRank >= 1 && p.ShieldRank <= 5
            ? GameText.Instance.Message("panel_shield_" + p.ShieldRank) : "";
    }

    /// <summary>右下の枠に出すモンスター情報。原典は 名前／命／攻／防 の並び。</summary>
    public struct MonsterInfo
    {
        public bool Valid;
        public string Name;
        public string Hp, Attack, Defense;
        /// <summary>勇者のオーブを持っているときだけ出る予想ダメージ。</summary>
        public string ExpectedDamage;
    }

    public static MonsterInfo Inspect(PlayerState player, string enemyPrefab)
    {
        var info = new MonsterInfo();
        var e = TowerBattle.LoadEnemy(enemyPrefab);
        if (e == null) return info;

        info.Valid = true;
        info.Name = e.Name;
        info.Hp = GameText.Num(e.MaxHP);
        info.Attack = GameText.Num(e.Attack);
        info.Defense = GameText.Num(e.Defense);

        // 勇者のオーブを持っていれば、受けるダメージまで出してくれる
        if (player != null && player.Has(SpecialItems.Kind.HeroOrb))
        {
            var kind = BattleRules.KindFromTag(enemyPrefab);
            var charms = TowerBattle.CharmsOf(player);
            var p = BattleRules.Predict(player.Attack, player.Defense,
                                        e.MaxHP, e.Attack, e.Defense, e.Coin, kind, charms);
            info.ExpectedDamage = p.CanWin
                ? GameText.Num(p.DamageTaken)
                : GameText.Instance.Message("orb_no_attack");   // 「　　　　　攻撃不可能」
        }
        return info;
    }

    /// <summary>所持している鍵の本数。原典は本数ぶんアイコンを並べる。</summary>
    public static int YellowKeys(PlayerState p) { return p.YellowKeys; }
    public static int PurpleKeys(PlayerState p) { return p.PurpleKeys; }
    public static int RedKeys(PlayerState p)    { return p.RedKeys; }
}
