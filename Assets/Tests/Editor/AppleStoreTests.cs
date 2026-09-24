using NUnit.Framework;

/// <summary>
/// Apple In-App Purchase（StoreKit 2）の橋渡し。
/// 実体は Swift（Assets/Plugins/iOS/MagicTowerStore.swift）なので、
/// ここでは**受け取った様子を読む所**と、Apple の決まりに関わる形だけを見る。
/// </summary>
public class AppleStoreTests
{
    [Test]
    public void 品目は三つで非消耗型の番号をそろえてある()
    {
        Assert.AreEqual(GoldShop.Count, AppleStore.ProductIds.Length);
        foreach (var id in AppleStore.ProductIds)
            StringAssert.StartsWith("magicTowerGold", id);
        // 並びは安い順（PhoneScreen.ShopGold と同じ）。
        // ★App Store Connect に入れた字と**1文字でも違うと読めない**。
        CollectionAssert.AreEqual(
            new[] { "magicTowerGoldFourHundred",
                    "magicTowerGoldOneThousand",
                    "magicTowerGoldFourThousand" }, AppleStore.ProductIds);
    }

    [Test]
    public void 受け取った様子を読める()
    {
        AppleStore.Parse("{\"ready\":true,\"canPay\":true,\"busy\":false,\"msg\":\"\","
                       + "\"items\":["
                       + "{\"id\":\"magicTowerGoldFourHundred\",\"price\":\"¥160\",\"owned\":true},"
                       + "{\"id\":\"magicTowerGoldOneThousand\",\"price\":\"¥320\",\"owned\":false}"
                       + "]}");
        Assert.IsTrue(AppleStore.Ready);
        Assert.IsTrue(AppleStore.CanPay);
        Assert.IsFalse(AppleStore.Busy);
        Assert.AreEqual(2, AppleStore.Items.Length);
        Assert.AreEqual("¥160", AppleStore.PriceOf(0));
        Assert.IsTrue(AppleStore.Owned(0));
        Assert.IsFalse(AppleStore.Owned(1));
        Assert.AreEqual("", AppleStore.PriceOf(2), "読めていない品目は空");
    }

    [Test]
    public void 知らせの文も読める()
    {
        AppleStore.Parse("{\"ready\":false,\"canPay\":false,\"busy\":true,"
                       + "\"msg\":\"購入できませんでした。\",\"items\":[]}");
        Assert.AreEqual("購入できませんでした。", AppleStore.Message);
        Assert.IsTrue(AppleStore.Busy);
        Assert.IsFalse(AppleStore.CanPay);
        Assert.AreEqual(0, AppleStore.Items.Length);
    }

    /// <summary>
    /// 支払いができない端末では商店を出さない（Apple の HIG
    /// 「Display your store only when people can make payments」）。
    /// </summary>
    [Test]
    public void 支払えない端末では商店を出さない()
    {
        // エディタでは StoreKit を呼べない＝見本の道なので、いつでも出せる
        Assert.IsFalse(AppleStore.Live);
        Assert.IsTrue(GoldShop.Available);
        StringAssert.Contains("購入ができない", GoldShop.Unavailable);
    }

    /// <summary>実機では値段を App Store の現地表記から出す（円を焼き込まない）。</summary>
    [Test]
    public void 値段はストアの表記から出す()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/Items/GoldShop.cs");
        StringAssert.Contains("AppleStore.PriceOf(i)", src);
        StringAssert.Contains("displayPrice", src, "そのことが書いてある");
        var swift = System.IO.File.ReadAllText("Assets/Plugins/iOS/MagicTowerStore.swift");
        StringAssert.Contains("p.displayPrice", swift);
        StringAssert.Contains("AppStore.canMakePayments", swift);
        StringAssert.Contains("AppStore.sync()", swift, "購入の復元（審査指針 3.1.1）");
        StringAssert.Contains("Transaction.currentEntitlements", swift, "非消耗型の権利を読む");
        StringAssert.Contains("product.purchase()", swift, "確認のシートは Apple に任せる");
    }

    /// <summary>
    /// **広い窓は目一杯の幅で固定する**。本文に合わせて詰めると、値段が現地の表記
    /// （`$0.99` / `¥160` / `R$ 29,90`）で長さが変わるたびに足りなくなって折り返す
    /// （作者の報告: 「$0.9」で切れて次の行に「9」が落ちていた）。
    /// </summary>
    [Test]
    public void 課金の窓は目一杯の幅で固定する()
    {
        int wide = AuthorStyle.WidthFor(GoldShop.Text());
        // 値段が短かろうと長かろうと同じ幅
        AppleStore.Parse("{\"ready\":true,\"canPay\":true,\"busy\":false,\"msg\":\"\","
                       + "\"items\":[{\"id\":\"magicTowerGoldFourHundred\","
                       + "\"price\":\"R$ 29,90\",\"owned\":false}]}");
        Assert.AreEqual(wide, AuthorStyle.WidthFor(GoldShop.Text()));
        // ふつうの窓より広い
        Assert.Greater(wide, AuthorStyle.WidthFor("ふつうのセリフ"));
    }

    /// <summary>
    /// 値段の頭をそろえる。⚠ TMP の `&lt;pos&gt;` の札は解釈されず字として出てしまったので、
    /// **飾りの札を使わず全角空白で伸ばす**（作者の報告）。
    /// </summary>
    [Test]
    public void 値段の頭がそろっている()
    {
        var text = GoldShop.Text();
        StringAssert.DoesNotContain("<pos", text, "位置を指す札は使わない");
        int at = -1;
        foreach (var line in text.Split('\n'))
        {
            if (!line.Contains("ゴールドの引換券")) continue;
            int i = line.IndexOf("券") + 1;
            while (i < line.Length && line[i] == '　') i++;
            if (at < 0) at = i;
            Assert.AreEqual(at, i, "値段の頭が同じ位置: " + line);
        }
        Assert.Greater(at, 0, "品目の行がある");
    }

    /// <summary>買えたことが分かってから渡す（シートを出した時点では渡さない）。</summary>
    [Test]
    public void 買えたことが分かってから渡す()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerGame.cs");
        StringAssert.Contains("pendingBuy", src);
        StringAssert.Contains("public void ClaimBought()", src);
        var ctrl = System.IO.File.ReadAllText("Assets/Scripts/TowerController.cs");
        StringAssert.Contains("AppleStore.Poll();", ctrl);
        StringAssert.Contains("Game.ClaimBought();", ctrl);
    }
}
