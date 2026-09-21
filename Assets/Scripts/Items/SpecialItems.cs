using System;
using UnityEngine;

/// <summary>
/// 原典の特殊アイテム15種の所持状態。
/// 一覧はセリフ原文（docs/original/items_and_hints.txt）より。
///
/// 効果が原典で明記されているもの:
///   十字架            … バンパイア・ゾンビへの攻撃力2倍（原典の「十字架が無いと倒せない」は忠告であり、規則ではない）
///   ドラゴンスレーヤー … ドラゴンへの攻撃力2倍
///   幸運の金貨        … 敵が持っているGOLDが2倍
///   勇者のオーブ      … 戦闘結果をコンピュータが計算して表示する
///   昇華の翼／降臨の翼 … 1つ上／下の階へ強制的に移動する（幻の階へはこれでしか行けない）
///   空間転移の秘宝    … 階を選んで移動する
/// </summary>
[Serializable]
public class SpecialItems
{
    [Flags]
    public enum Kind
    {
        None            = 0,
        HeroOrb         = 1 << 0,   // 勇者のオーブ
        FlightOrb       = 1 << 1,   // 飛翔のオーブ
        WisdomOrb       = 1 << 2,   // 知恵のオーブ
        Cross           = 1 << 3,   // 十字架
        Elixir          = 1 << 4,   // エリクサー
        Matock          = 1 << 5,   // マトック
        SuperMatock     = 1 << 6,   // スーパーマトック
        DestructionOrb  = 1 << 7,   // 破壊の玉
        TeleportRelic   = 1 << 8,   // 空間転移の秘宝
        WingUp          = 1 << 9,   // 昇華の翼
        WingDown        = 1 << 10,  // 降臨の翼
        DragonSlayer    = 1 << 11,  // ドラゴンスレーヤー
        SnowCrystal     = 1 << 12,  // 雪の結晶
        MagicKey        = 1 << 13,  // 魔法の鍵
        LuckyCoin       = 1 << 14,  // 幸運の金貨
    }

    [SerializeField] Kind owned = Kind.None;

    public Kind Owned { get { return owned; } }

    public bool Has(Kind k) { return (owned & k) == k; }

    /// <summary>取得する。原典のメッセージを返す。</summary>
    public string Add(Kind k)
    {
        owned |= k;
        return MessageOf(k);
    }

    /// <summary>原典のセリフ原文。</summary>
    public static string MessageOf(Kind k)
    {
        switch (k)
        {
            case Kind.HeroOrb:        return "勇者のオーブを手に入れた！";
            case Kind.FlightOrb:      return "飛翔のオーブを手に入れた！";
            case Kind.WisdomOrb:      return "知恵のオーブを手に入れた！";
            case Kind.Cross:          return "十字架を手に入れた！バンパイア、ゾンビに対する攻撃力が２倍になった！";
            case Kind.Elixir:         return "エリクサーを手に入れた！";
            case Kind.Matock:         return "マトックを手に入れた！";
            case Kind.SuperMatock:    return "スーパーマトックを手に入れた！";
            case Kind.DestructionOrb: return "破壊の玉を手に入れた！";
            case Kind.TeleportRelic:  return "空間転移の秘宝を手に入れた！";
            case Kind.WingUp:         return "昇華の翼を手に入れた！";
            case Kind.WingDown:       return "降臨の翼を手に入れた！";
            case Kind.DragonSlayer:   return "ドラゴンスレーヤーを手に入れた！ドラゴンに対する攻撃力が２倍になった！";
            case Kind.SnowCrystal:    return "雪の結晶を手に入れた！";
            case Kind.MagicKey:       return "魔法の鍵を手に入れた！";
            case Kind.LuckyCoin:      return "幸運の金貨を手に入れた！敵が持っているＧＯＬＤ数が２倍になる。";
            default:                  return "";
        }
    }

    /// <summary>戦闘計算に渡す補正。</summary>
    public BattleRules.Charms ToCharms()
    {
        BattleRules.Charms c = BattleRules.Charms.None;
        if (Has(Kind.Cross))        c |= BattleRules.Charms.Cross;
        if (Has(Kind.DragonSlayer)) c |= BattleRules.Charms.DragonSlayer;
        if (Has(Kind.LuckyCoin))    c |= BattleRules.Charms.LuckyCoin;
        return c;
    }

    /// <summary>
    /// `.floor` データのプレハブ名から種別を引く。
    ///
    /// 物体IDと中身は**原典 MTE.EXE のアイテム取得分岐（0x2cfc 付近）から確定**した。
    ///   36 勇者のオーブ / 37 知恵のオーブ / 38 飛翔のオーブ / 39 十字架 / 40 エリクサー /
    ///   41 マトック / 42 破壊の玉 / 43 空間転移の秘宝 / 44 昇華の翼 / 45 降臨の翼 /
    ///   46 ドラゴンスレイヤー / 47 雪の結晶 / 48 魔法の鍵 / 49 スーパーマトック / 50 幸運の金貨
    /// 所持フラグの番号は「物体ID − 1」。
    /// 初期配置にあるのは 37/38/42/44/48/50 の6つだけで、残りはイベントで手に入る。
    /// </summary>
    public static Kind FromPrefabName(string name)
    {
        // 初期配置にあるもの
        if (name.StartsWith("Orb_Wisdom"))      return Kind.WisdomOrb;      // ID37 F5
        if (name.StartsWith("Orb_Flight"))      return Kind.FlightOrb;      // ID38 F1
        if (name.StartsWith("Ball_Destruction"))return Kind.DestructionOrb; // ID42 F37
        if (name.StartsWith("Wing_Up"))         return Kind.WingUp;         // ID44 F45
        if (name.StartsWith("Key_Magic"))       return Kind.MagicKey;       // ID48 F46
        if (name.StartsWith("Coin_Lucky"))      return Kind.LuckyCoin;      // ID50 F0
        // 初期配置には無く、できごとで盤面に出るもの。
        // （ID39 十字架＝19階の隠れマス / ID41 マトック＝15階の解錠 /
        //   ID45 降臨の翼＝41階の解錠 / 残りは商人や仙人から直に受け取る）
        if (name.StartsWith("Orb_Hero"))        return Kind.HeroOrb;        // ID36
        if (name.StartsWith("Cross"))           return Kind.Cross;          // ID39 F19
        if (name.StartsWith("Elixir"))          return Kind.Elixir;         // ID40
        if (name.StartsWith("Matock_Super"))    return Kind.SuperMatock;    // ID49
        if (name.StartsWith("Matock"))          return Kind.Matock;         // ID41 F15
        if (name.StartsWith("Relic_Teleport"))  return Kind.TeleportRelic;  // ID43
        if (name.StartsWith("Wing_Down"))       return Kind.WingDown;       // ID45 F41
        if (name.StartsWith("Sword_Dragon"))    return Kind.DragonSlayer;   // ID46
        if (name.StartsWith("Crystal_Snow"))    return Kind.SnowCrystal;    // ID47
        return Kind.None;
    }
}
