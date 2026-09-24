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

    /// <summary>
    /// 本文はこちらで行に分けてあるので、**TMP に重ねて折り返させない**。
    /// 折り返させると値段だけが次の行へ落ちる（作者の報告「.99が二行目に」）。
    /// 実際の組まれ方は `TowerScreenshot.CaptureShop` で写しを撮って確かめる。
    /// </summary>
    [Test]
    public void 課金の窓はTMPに折り返させない()
    {
        var view = System.IO.File.ReadAllText("Assets/Scripts/UI/DialogueWindowView.cs");
        StringAssert.Contains("body.enableWordWrapping = !Boxed;", view);
        StringAssert.Contains("PreviewBoxed", view, "写しの道具が同じ道を通れる");
        var shot = System.IO.File.ReadAllText("Assets/Editor/TowerScreenshot.cs");
        StringAssert.Contains("CaptureShop", shot);
        StringAssert.Contains("MT_SHOP_PRICES", shot, "実機と同じ値段の字で組ませる");

        // 本文の行数
        //   見出し ＋ 空行1 ＋ 品目（1行＋空行 ShopRowLines-1）×3 ＋ 復元 ＋ 空行1 ＋ やめる
        var lines = GoldShop.Text().Split('\n');
        Assert.AreEqual(PhoneScreen.ShopIntro.Length + 1
                        + GoldShop.Count * PhoneScreen.ShopRowLines
                        + PhoneScreen.ShopSmallRowLines + 1, lines.Length);
        // ★課金の窓は**原典の画面（400ドット）より高くてよい**（作者の指定）。
        //   スマホは縦に余裕があるので、上下に少しはみ出しても構わない。
        //   はみ出すときは上下に均等に（真ん中に置く）。
        var r = AuthorStyle.DialogueFor(GoldShop.Text());
        Assert.Greater(r.height, DialogueWindow.ScreenHeight, "品目を大きく見せるので高い");
        Assert.AreEqual((DialogueWindow.ScreenHeight - r.height) / 2, r.y, "真ん中に置く");
        // 出過ぎないこと（上下それぞれ1品目ぶんまで、という作者の目安）
        int over = (r.height - DialogueWindow.ScreenHeight) / 2;
        Assert.LessOrEqual(over,
            AuthorStyle.DialogueLineAdvance * PhoneScreen.ShopRowLines, "はみ出しは1品目ぶんまで");
    }

    /// <summary>
    /// ★**本線が通る道（TowerGame.Say → DialogueBox.Paginate）で1行に収まるか**。
    ///
    /// ここが本命の見張り。窓へ渡る前に `Paginate` が 18文字で切っていたせいで、
    /// 値段の「.99」だけが次の行に落ちていた（作者の報告「私の端末では2行のまま」）。
    /// 写しの道具も同じ道を通すようにしてある。
    /// </summary>
    [Test]
    public void 本線が通る道で品目が一行に収まる()
    {
        // 実機でいちばん長くなる表記（米国ストア）で確かめる
        AppleStore.PreviewLive = true;
        try
        {
            AppleStore.Parse("{\"ready\":true,\"canPay\":true,\"busy\":false,\"msg\":\"\","
                + "\"items\":["
                + "{\"id\":\"magicTowerGoldFourHundred\",\"price\":\"$0.99\",\"owned\":false},"
                + "{\"id\":\"magicTowerGoldOneThousand\",\"price\":\"$1.99\",\"owned\":false},"
                + "{\"id\":\"magicTowerGoldFourThousand\",\"price\":\"$5.99\",\"owned\":false}]}");

            var body = GoldShop.Text();
            var pages = DialogueBox.Paginate(body);
            Assert.AreEqual(1, pages.Count, "1ページに収まる");
            Assert.AreEqual(body.Split('\n').Length, pages[0].Split('\n').Length,
                            "行が増えていない＝どこも折り返されていない");
            foreach (var line in pages[0].Split('\n'))
                Assert.LessOrEqual(DialogueBox.Visible(line).Length,
                                   DialogueBox.WideCharsPerLine, line);
            // 値段だけが落ちた行ができていないこと
            foreach (var line in pages[0].Split('\n'))
                Assert.IsFalse(line.StartsWith(".") || line.StartsWith("99"), line);
        }
        finally { AppleStore.PreviewLive = false; }
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
