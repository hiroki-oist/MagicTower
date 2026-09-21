using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 塔の中の人（商人・祭壇・妖精）とのやりとり。
///
/// 盤面に置かれている物体の対応（原典 INITIAL.DAT より）:
/// <code>
///   Merchant(ID19)    … 商人。2/6/7/12/15/28/31/38/39/45/47階
///   Vendor(ID15) / Shop_Altar(ID16) / Shop_Altar_R(ID17) … 祭壇。4/12/32/46階。
///   3マスで1つの絵だが**マスごとに絵が違う**ので、1マスずつ別の絵を置く。
///   Master(ID25)      … 仙人。2/3/4/6/16/18/21/27/31/33/36/37/39/42/45/46/48階（16階は2人）
///   Mikata(ID21)      … 盗賊。2/15/29階（物語に出てくる味方）
///   妖精              … 盤面には無く、ブロックをクリアすると現れる
/// </code>
/// </summary>
public static class TowerNpc
{
    public static bool IsMerchant(string name) { return name == "Merchant"; }
    public static bool IsAltar(string name)
    {
        return name == "Shop_Altar" || name == "Shop_Altar_R" || name == "Vendor";
    }
    public static bool IsMaster(string name) { return name == "Master"; }
    public static bool IsThief(string name)  { return name == "Mikata"; }
    public static bool IsDoll(string name)   { return name == "Princess"; }
    /// <summary>妖精(ID23)。盤面には最初から居らず、関門の解錠で現れる。</summary>
    public static bool IsFairy(string name)  { return name == "Fairy"; }

    /// <summary>
    /// 触れても上に乗らず、その場で用が起きる物体か。
    /// 原典の属性表では種別7（人や物）にあたり、ディスパッチャ（0x23ee）が
    /// それぞれの関数へ飛ばすだけでプレーヤーは動かない。
    /// </summary>
    public static bool IsPerson(string name)
    {
        return IsMerchant(name) || IsAltar(name) || IsMaster(name)
            || IsThief(name) || IsDoll(name) || IsFairy(name);
    }

    // ---- 商店 --------------------------------------------------------

    /// <summary>その階の商人の品揃え。同じ階に2人いる場合は X で選ぶ。</summary>
    public static List<Shop.Offer> OffersAt(int floor, int x = int.MinValue)
    {
        var list = new List<Shop.Offer>();
        foreach (var o in Shop.Instance.ForFloor(floor, x)) list.Add(o);
        return list;
    }

    /// <summary>売り口上。原典の MT.GTX のレコードをそのまま出す。</summary>
    public static string Pitch(Shop.Offer offer, int highestFloor)
    {
        if (offer.Mode == "buy_var")
        {
            // 値段が変動する鍵屋。1本 = 20 × 到達最高階
            return GameText.Instance.Message("shop_key_any", Shop.VariableKeyPrice(highestFloor));
        }
        var line = GameText.Instance.Dialogue(offer.OfferId);
        return line.HasValue ? line.Value.Body : "";
    }

    /// <summary>買う。金が足りなければ「お金がたりないよ！」。</summary>
    public static string Buy(PlayerState p, Shop.Offer offer)
    {
        if (p == null) return null;
        int price = offer.Mode == "buy_var"
            ? Shop.VariableKeyPrice(p.HighestFloor) : offer.Price;

        if (offer.Mode == "sell")
        {
            if (p.YellowKeys <= 0) return GameText.Instance.Message("shop_no_key");
            p.YellowKeys--;
            p.Gold += offer.Price;
            return GameText.Instance.Message("shop_thanks");
        }
        if (offer.Mode == "gift")
        {
            p.Gold += offer.Price;
            return GameText.Instance.Message("shop_thanks");
        }
        if (p.Gold < price) return GameText.Instance.Message("shop_no_money");

        p.Gold -= price;
        ApplyGoods(p, offer.Goods);
        p.ClampStats();
        if (offer.BuyFlag >= 0) p.EventFlags.Add(offer.BuyFlag);
        return GameText.Instance.Message("shop_thanks");
    }

    /// <summary>商人に話しかけたときの段階。</summary>
    public enum MerchantStage
    {
        /// <summary>売り口上。「◎はい／◎いいえ」を聞く。</summary>
        Offer,
        /// <summary>買ったあとにもう一度話すと聞ける「重要な情報」。</summary>
        Hint,
        /// <summary>「もう、話す事はないよ。」</summary>
        Done,
    }

    public struct MerchantTalk
    {
        public MerchantStage Stage;
        public string Text;
        /// <summary>「◎はい／◎いいえ」を出すか。</summary>
        public bool AskYesNo;
    }

    /// <summary>
    /// 商人に話しかける。原典（MTE.EXE 0x0ea6）の3段構え:
    ///   1度目 … 売り口上 → はい/いいえ → 買えば「まいどーっ！」で買ったフラグが立つ
    ///   2度目 … 「重要な情報」を話して聞いたフラグが立つ
    ///   3度目 … 「もう、話す事はないよ。」
    /// 何度でも買える商人（28階の買い取り、12階の鍵屋）はいつでも1度目のまま。
    /// </summary>
    public static MerchantTalk TalkToMerchant(PlayerState p, Shop.Offer offer)
    {
        var t = GameText.Instance;
        var r = new MerchantTalk();

        // 2階の商人は選ばせずにその場でくれる（原典 0x14e4）
        if (offer.Mode == "gift")
        {
            if (offer.BuyFlag >= 0 && p.EventFlags.Contains(offer.BuyFlag))
            {
                r.Stage = MerchantStage.Done;
                r.Text = t.Message("shop_use_well");
                return r;
            }
            if (offer.BuyFlag >= 0) p.EventFlags.Add(offer.BuyFlag);
            ApplyGoods(p, offer.Goods);
            p.ClampStats();
            r.Stage = MerchantStage.Offer;
            r.Text = Pitch(offer, p.HighestFloor);
            return r;
        }

        bool bought = offer.BuyFlag >= 0 && p.EventFlags.Contains(offer.BuyFlag);
        if (!bought)
        {
            r.Stage = MerchantStage.Offer;
            r.Text = Pitch(offer, p.HighestFloor);
            r.AskYesNo = true;
            return r;
        }

        if (offer.HintFlag >= 0 && !p.EventFlags.Contains(offer.HintFlag))
        {
            p.EventFlags.Add(offer.HintFlag);
            r.Stage = MerchantStage.Hint;
            var line = GameText.Instance.Dialogue(offer.HintId);
            r.Text = line.HasValue ? line.Value.Body : "";
            return r;
        }

        r.Stage = MerchantStage.Done;
        r.Text = t.Message("shop_done");
        return r;
    }

    /// <summary>"yellow:5 purple:1" のような書き方を解いて渡す。</summary>
    static void ApplyGoods(PlayerState p, string goods)
    {
        if (string.IsNullOrEmpty(goods)) return;
        foreach (var part in goods.Split(' '))
        {
            var kv = part.Split(':');
            if (kv.Length != 2) continue;
            int n;
            if (!int.TryParse(kv[1], out n)) continue;
            switch (kv[0])
            {
                case "yellow": p.YellowKeys += n; break;
                case "purple": p.PurpleKeys += n; break;
                case "red":    p.RedKeys += n; break;
                case "hp":     p.Heal(n); break;
                case "gold":   p.Gold += n; break;
                case "matock_super": p.Give(SpecialItems.Kind.SuperMatock); break;
            }
        }
    }

    // ---- 仙人 --------------------------------------------------------

    /// <summary>
    /// 仙人に話しかける。出す文字列を返す。
    ///
    /// 原典（MTE.EXE 0x0000）の順番どおり:
    ///   表引き → 済みなら「もう、話は終わりじゃ。」→ セリフ → フラグ → ごほうび。
    /// </summary>
    public static string TalkToMaster(PlayerState p)
    {
        if (p == null) return null;
        var hit = MasterHints.Instance.For(p.Floor, p.Position.x);
        if (!hit.HasValue) return null;          // 原典の「ｼﾞｲｻﾝ ﾊﾞﾝｺﾞｳ ｶﾞﾐﾂｶﾘﾏｾﾝ」
        var h = hit.Value;

        if (p.EventFlags.Contains(h.FlagId)) return GameText.Instance.Message("master_done");

        var line = GameText.Instance.Dialogue(h.Record);
        p.EventFlags.Add(h.FlagId);
        GiveReward(p, h);
        return line.HasValue ? line.Value.Body : "";
    }

    /// <summary>仙人のごほうび。原典 0x01ce〜0x027f にある3件だけ。</summary>
    static void GiveReward(PlayerState p, MasterHints.Hint h)
    {
        if (h.Floor == 3)  p.Give(SpecialItems.Kind.HeroOrb);
        if (h.Floor == 16 && h.MinX != int.MinValue) p.Give(SpecialItems.Kind.Elixir);
        if (h.Floor == 2)
        {
            // 「そなたの攻撃力と防御力を３パーセントだけ上げてやろう」。原典は整数除算。
            p.Attack  += p.Attack  * 3 / 100;
            p.Defense += p.Defense * 3 / 100;
            p.ClampStats();
        }
    }

    // ---- 祭壇 --------------------------------------------------------

    public enum Wish { Hp, Attack, Defense, Cancel }

    /// <summary>今回求められる寄進額。</summary>
    public static int AltarCost(PlayerState p) { return Altar.Cost(p.DonationCount); }

    /// <summary>祭壇の3つの選択肢に出る数値。</summary>
    public static string AltarMenu(PlayerState p)
    {
        var t = GameText.Instance;
        return t.Message("altar_which") + "\n"
             + t.Message("altar_hp",  Altar.HealAmount(p.DonationCount)) + "\n"
             + t.Message("altar_atk", Altar.AttackGain(p.Floor)) + "\n"
             + t.Message("altar_def", Altar.DefenseGain(p.Floor)) + "\n"
             + t.Message("altar_cancel");
    }

    /// <summary>寄進する。金が足りなければ「金が足らん！」。</summary>
    public static string Donate(PlayerState p, Wish wish)
    {
        if (p == null || wish == Wish.Cancel) return null;
        int cost = AltarCost(p);
        if (p.Gold < cost) return GameText.Instance.Message("altar_poor");

        p.Gold -= cost;
        switch (wish)
        {
            case Wish.Hp:      p.Heal(Altar.HealAmount(p.DonationCount)); break;
            case Wish.Attack:  p.Attack  += Altar.AttackGain(p.Floor); break;
            case Wish.Defense: p.Defense += Altar.DefenseGain(p.Floor); break;
        }
        p.DonationCount++;
        p.ClampStats();
        return null;
    }

    // ---- 妖精 --------------------------------------------------------

    public struct FairyResult
    {
        public bool Valid;
        public int Score;
        public string EvaluationKey;
        /// <summary>80点以上でもらえる称号。もらえなければ null。</summary>
        public string Rank;
    }

    /// <summary>妖精の採点。10/20/25/40/49階にしかいない。</summary>
    public static FairyResult AskFairy(PlayerState p)
    {
        var r = new FairyResult();
        if (p == null || !Fairy.IsFairyFloor(p.Floor)) return r;

        r.Valid = true;
        r.Score = Fairy.Score(p.Floor, p.Hp, p.Attack, p.Defense, p.YellowKeys, p.PurpleKeys);
        r.EvaluationKey = Fairy.EvaluationKey(r.Score);
        if (r.Score >= Fairy.RankThreshold)
        {
            // 点数が高いほど上の称号。原典の並びは 無称号/戦士/勇者/英雄/剣帝/闘神
            int i = Mathf.Clamp((r.Score - Fairy.RankThreshold) / 10 + 1, 1, Fairy.Ranks.Length - 1);
            r.Rank = Fairy.Ranks[i];
        }
        return r;
    }
}
