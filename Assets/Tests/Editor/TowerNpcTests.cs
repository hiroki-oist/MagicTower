using NUnit.Framework;

/// <summary>商人・祭壇・妖精とのやりとりが原典どおりかを確かめる。</summary>
public class TowerNpcTests
{
    PlayerState player;

    [SetUp]
    public void SetUp()
    {
        player = new PlayerState { Hp = 1000, Attack = 20, Defense = 20, Gold = 5000, Floor = 6 };
    }

    [Test]
    public void 盤面の人を見分けられる()
    {
        Assert.IsTrue(TowerNpc.IsMerchant("Merchant"));
        Assert.IsTrue(TowerNpc.IsAltar("Shop_Altar"));
        Assert.IsTrue(TowerNpc.IsAltar("Vendor"), "祭壇は3マスで1つの絵");
        Assert.IsTrue(TowerNpc.IsMaster("Master"), "仙人");
        Assert.IsTrue(TowerNpc.IsThief("Mikata"), "盗賊");
    }

    [Test]
    public void 六階の商人は紫の鍵を50ゴールドで売る()
    {
        var offers = TowerNpc.OffersAt(6);
        Assert.AreEqual(1, offers.Count);
        Assert.AreEqual(50, offers[0].Price);
        StringAssert.Contains("紫の鍵を大特価の５０ＧＯＬＤで売る",
                              TowerNpc.Pitch(offers[0], player.HighestFloor));

        Assert.AreEqual("まいどーっ！", TowerNpc.Buy(player, offers[0]));
        Assert.AreEqual(1, player.PurpleKeys);
        Assert.AreEqual(4950, player.Gold);
    }

    [Test]
    public void 金が足りなければ買えない()
    {
        player.Gold = 10;
        var offers = TowerNpc.OffersAt(6);
        Assert.AreEqual("お金がたりないよ！", TowerNpc.Buy(player, offers[0]));
        Assert.AreEqual(0, player.PurpleKeys);
        Assert.AreEqual(10, player.Gold, "金は減らない");
    }

    [Test]
    public void 値段が変動する鍵屋は到達最高階で決まる()
    {
        player.HighestFloor = 12;
        var offers = TowerNpc.OffersAt(12, 9);
        Assert.AreEqual(1, offers.Count);
        Assert.AreEqual("buy_var", offers[0].Mode);
        StringAssert.Contains("２４０ＧＯＬＤ", TowerNpc.Pitch(offers[0], player.HighestFloor));

        TowerNpc.Buy(player, offers[0]);
        Assert.AreEqual(1, player.YellowKeys);
        Assert.AreEqual(5000 - 240, player.Gold);
    }

    [Test]
    public void 二十八階では鍵を売って金になる()
    {
        player.Floor = 28; player.YellowKeys = 3; player.Gold = 0;
        var offers = TowerNpc.OffersAt(28);
        Assert.AreEqual("sell", offers[0].Mode);
        TowerNpc.Buy(player, offers[0]);
        Assert.AreEqual(2, player.YellowKeys);
        Assert.AreEqual(100, player.Gold, "1本100ＧＯＬＤ");

        player.YellowKeys = 0;
        Assert.AreEqual("鍵を持ってないよ！", TowerNpc.Buy(player, offers[0]));
    }

    [Test]
    public void 祭壇の寄進額は回数で上がる()
    {
        player.Floor = 4; player.Gold = 10000;
        Assert.AreEqual(20, TowerNpc.AltarCost(player));
        TowerNpc.Donate(player, TowerNpc.Wish.Defense);
        Assert.AreEqual(40, TowerNpc.AltarCost(player), "2回目は40");
        Assert.AreEqual(24, player.Defense, "4階はブロック1なので防御+4");

        TowerNpc.Donate(player, TowerNpc.Wish.Attack);
        Assert.AreEqual(22, player.Attack, "攻撃は+2（防御の半分）");
    }

    [Test]
    public void 金が足らなければ寄進できない()
    {
        player.Floor = 4; player.Gold = 5;
        Assert.AreEqual("金が足らん！", TowerNpc.Donate(player, TowerNpc.Wish.Hp));
        Assert.AreEqual(0, player.DonationCount);
    }

    [Test]
    public void 妖精は決まった階にしかいない()
    {
        player.Floor = 11;
        Assert.IsFalse(TowerNpc.AskFairy(player).Valid);
        player.Floor = 10;
        Assert.IsTrue(TowerNpc.AskFairy(player).Valid);
    }

    [Test]
    public void 妖精の採点と称号()
    {
        player.Floor = 10; player.Hp = 400; player.Attack = 29; player.Defense = 35;
        player.YellowKeys = 0; player.PurpleKeys = 0;
        var r = TowerNpc.AskFairy(player);
        Assert.AreEqual(60, r.Score, "基準値ちょうどで60点");
        Assert.IsNull(r.Rank, "80点未満では称号は出ない");

        player.Attack = 50;   // 21×3 = +63点
        r = TowerNpc.AskFairy(player);
        Assert.GreaterOrEqual(r.Score, 80);
        Assert.IsNotNull(r.Rank, "80点以上で称号がもらえる");
        CollectionAssert.Contains(Fairy.Ranks, r.Rank);
        Assert.AreEqual("戦士", r.Rank, "1人目で80点以上なら戦士");
    }

    [Test]
    public void 称号は80点以上を取った妖精の人数で上がる()
    {
        // 原典は妖精が出た直後に消えるので1階に1回。こちらは盤面に残るので
        // 同じ階を二度数えない（AuthorStyle.FairyWaitsToBeAsked）
        player.Hp = 9990; player.Attack = 990; player.Defense = 990;
        player.YellowKeys = 10; player.PurpleKeys = 10;

        player.Floor = 10;
        Assert.AreEqual("戦士", TowerNpc.AskFairy(player).Rank);
        Assert.AreEqual("戦士", TowerNpc.AskFairy(player).Rank, "同じ妖精は二度数えない");
        Assert.AreEqual(1, player.FairyCount);

        player.Floor = 20;
        Assert.AreEqual("勇者", TowerNpc.AskFairy(player).Rank);
        player.Floor = 25;
        Assert.AreEqual("英雄", TowerNpc.AskFairy(player).Rank);
        player.Floor = 40;
        Assert.AreEqual("剣帝", TowerNpc.AskFairy(player).Rank);
        player.Floor = 49;
        Assert.AreEqual("闘神", TowerNpc.AskFairy(player).Rank);
        Assert.AreEqual(5, player.FairyCount);
    }

    [Test]
    public void 点数が足りなければ称号は数えない()
    {
        player.Floor = 10; player.Hp = 400; player.Attack = 29; player.Defense = 35;
        player.YellowKeys = 0; player.PurpleKeys = 0;
        Assert.IsNull(TowerNpc.AskFairy(player).Rank);
        Assert.AreEqual(0, player.FairyCount);
    }

    [Test]
    public void 締めが違うのは25階だけ()
    {
        // 原典 0x77bb は 25階だけを分けて「ここまで、おつかれさまでした。」にする
        player.Floor = 25;
        Assert.IsTrue(TowerNpc.AskFairy(player).Farewell);
        player.Floor = 49;
        Assert.IsFalse(TowerNpc.AskFairy(player).Farewell);
    }

    // ---- 商人の3段構え（原典 MTE.EXE 0x0ea6） ----

    [Test]
    public void 商人は買うまで売り口上をくり返す()
    {
        var o = TowerNpc.OffersAt(6)[0];
        var a = TowerNpc.TalkToMerchant(player, o);
        Assert.AreEqual(TowerNpc.MerchantStage.Offer, a.Stage);
        Assert.IsTrue(a.AskYesNo, "◎はい／◎いいえ を聞く");

        var b = TowerNpc.TalkToMerchant(player, o);
        Assert.AreEqual(TowerNpc.MerchantStage.Offer, b.Stage, "断ったら何度でも同じ");
    }

    [Test]
    public void 買った次に話すと重要な情報が聞ける()
    {
        var o = TowerNpc.OffersAt(6)[0];
        Assert.AreEqual("まいどーっ！", TowerNpc.Buy(player, o));
        Assert.IsTrue(player.EventFlags.Contains(51), "買ったフラグ（原典 21 + 0x1e）");

        var hint = TowerNpc.TalkToMerchant(player, o);
        Assert.AreEqual(TowerNpc.MerchantStage.Hint, hint.Stage);
        Assert.AreEqual(GameText.Instance.Dialogue(12).Value.Body, hint.Text);
        Assert.IsTrue(player.EventFlags.Contains(52), "聞いたフラグ");

        var done = TowerNpc.TalkToMerchant(player, o);
        Assert.AreEqual(TowerNpc.MerchantStage.Done, done.Stage);
        Assert.AreEqual("もう、話す事はないよ。", done.Text);
    }

    [Test]
    public void 何度でも買える商人はずっと売り口上のまま()
    {
        var keyShop = TowerNpc.OffersAt(12, 9)[0];
        player.HighestFloor = 12;
        TowerNpc.Buy(player, keyShop);
        Assert.AreEqual(TowerNpc.MerchantStage.Offer,
                        TowerNpc.TalkToMerchant(player, keyShop).Stage, "12階の鍵屋");

        var buyer = TowerNpc.OffersAt(28)[0];
        player.YellowKeys = 3;
        TowerNpc.Buy(player, buyer);
        Assert.AreEqual(TowerNpc.MerchantStage.Offer,
                        TowerNpc.TalkToMerchant(player, buyer).Stage, "28階の買い取り");
    }

    [Test]
    public void 二階の商人は選ばせずに千ゴールドくれる()
    {
        var o = TowerNpc.OffersAt(2)[0];
        player.Gold = 0;
        var a = TowerNpc.TalkToMerchant(player, o);
        Assert.IsFalse(a.AskYesNo, "はい／いいえ は出さない");
        Assert.AreEqual(1000, player.Gold);
        Assert.IsTrue(player.EventFlags.Contains(50), "原典は 0x32 の直書き");

        var b = TowerNpc.TalkToMerchant(player, o);
        Assert.AreEqual("有効につかってね。", b.Text);
        Assert.AreEqual(1000, player.Gold, "もらえるのは一度きり");
    }

    [Test]
    public void 商人のフラグは五十から六十八まででアイテムと仙人の間に並ぶ()
    {
        var used = new System.Collections.Generic.SortedSet<int>();
        foreach (var o in Shop.Instance.ForFloor(-1)) { }
        for (int f = 1; f <= 50; f++)
            foreach (var o in Shop.Instance.ForFloor(f))
            {
                if (o.BuyFlag >= 0) Assert.IsTrue(used.Add(o.BuyFlag), $"{o.BuyFlag} が重複");
                if (o.HintFlag >= 0) Assert.IsTrue(used.Add(o.HintFlag), $"{o.HintFlag} が重複");
            }
        Assert.AreEqual(19, used.Count);
        Assert.AreEqual(50, used.Min);
        Assert.AreEqual(68, used.Max, "69以降は仙人が使う");
    }
}
