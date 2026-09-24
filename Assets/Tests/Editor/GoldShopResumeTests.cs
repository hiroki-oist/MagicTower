using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 課金の窓と中断データ（どちらも作者の指定。原典にも Java版にも無い）。
/// </summary>
public class GoldShopResumeTests
{
    [Test]
    public void 売り物は三つで値段は原典ではなく作者の指定()
    {
        Assert.AreEqual(3, GoldShop.Count);
        CollectionAssert.AreEqual(new[] { 400, 1000, 4000 }, PhoneScreen.ShopGold);
        CollectionAssert.AreEqual(new[] { 160, 320, 640 }, PhoneScreen.ShopYen);
    }

    [Test]
    public void 窓には一度買えば次から無料と書いてある()
    {
        var t = string.Join("", PhoneScreen.ShopIntro);
        StringAssert.Contains("一度購入", t);
        StringAssert.Contains("無料", t);
        StringAssert.Contains("買い切り", t, "非消耗型であることを言い切る");
        StringAssert.Contains("復元", t, "復元できることを言い切る");
        // 絵を入れる窓なので広いほう（DialogueBox.WideCharsPerLine）に収める
        foreach (var line in PhoneScreen.ShopIntro)
            Assert.LessOrEqual(line.Length, DialogueBox.WideCharsPerLine, line);
        foreach (var line in DialogueBox.Wrap(GoldShop.Text()))
            Assert.LessOrEqual(DialogueBox.Visible(line).Length,
                               DialogueBox.WideCharsPerLine, line);
    }

    [Test]
    public void 選べる行は売り物と復元とやめる()
    {
        var p = new PlayerState { Hp = 100, Floor = 1 };
        var g = new TowerGame(p);
        Assert.IsTrue(g.AskGoldShop());
        Assert.IsTrue(g.IsChoosing);
        Assert.AreEqual(GoldShop.Count + 2, g.ChoiceCount, "品目＋復元＋やめる");
        StringAssert.Contains(PhoneScreen.ShopRestore, GoldShop.Text(),
                              "Apple の審査指針 3.1.1 で復元の口が要る");
        // 窓の本文にも売り物の行が並ぶ
        var text = GoldShop.Text();
        for (int i = 0; i < GoldShop.Count; i++)
            StringAssert.Contains(GoldShop.Wide(GoldShop.GoldOf(i)) + "ゴールドの引換券", text);
        // 品目の行は全角2つで字下げしてある（選ばせている行の決まり＋金貨を置く場所）
        int items = 0;
        foreach (var line in text.Split('\n'))
            if (line.StartsWith(PhoneScreen.ShopIndent)
                && line.Contains("ゴールドの引換券")) items++;
        Assert.AreEqual(GoldShop.Count, items, "品目の行が字下げしてある");
    }

    [Test]
    public void 買うとゴールドが増えて上限で止まる()
    {
        var p = new PlayerState { Gold = 0 };
        GoldShop.Buy(p, 0);
        Assert.AreEqual(400, p.Gold);
        p.Gold = PlayerState.MaxGold;
        GoldShop.Buy(p, 2);
        Assert.AreEqual(PlayerState.MaxGold, p.Gold, "原典の上限で頭を押さえる");
    }

    [Test]
    public void 数は全角で書く()
    {
        Assert.AreEqual("１０００", GoldShop.Wide(1000));
        Assert.AreEqual("１６０", GoldShop.Wide(160));
    }

    /// <summary>
    /// 課金の窓は**選択肢を枠で囲い、矢印を出さず、選んでいない行を灰色に沈める**
    /// （作者の指定）。窓の幅も広いほうを使う。
    /// </summary>
    [Test]
    public void 課金の窓は枠で囲って選んでいない行を沈める()
    {
        var view = System.IO.File.ReadAllText("Assets/Scripts/UI/DialogueWindowView.cs");
        StringAssert.Contains("ChoiceKind.GoldShop", view, "その窓だけの扱い");
        StringAssert.Contains("optionEdges", view);
        StringAssert.Contains("optionCoins", view, "金貨は普通の絵で置く");
        StringAssert.Contains("static string Dim(", view, "選んでいない行を沈める");
        StringAssert.Contains("!Boxed", view, "枠の窓では矢印を出さない");

        // 窓の幅は広いほう（絵を入れるため）
        var text = GoldShop.Text();
        Assert.AreEqual(DialogueBox.WideCharsPerLine, DialogueBox.LimitFor(text));
        Assert.AreEqual(DialogueBox.CharsPerLine, DialogueBox.LimitFor("ふつうのセリフ"));
        Assert.Greater(AuthorStyle.WidthFor(text),
                       AuthorStyle.WidthFor("ふつうの窓はこれまでどおり"), "課金の窓は広い");
    }

    /// <summary>飾りの札は幅に数えない（絵の札だけ1文字ぶん）。</summary>
    [Test]
    public void 絵の札は一文字ぶんとして数える()
    {
        Assert.AreEqual("あ", DialogueBox.Visible("<nobr>あ"));
        Assert.AreEqual("あ", DialogueBox.Visible("<color=#777777>あ</color>"));
        Assert.AreEqual("ふつう", DialogueBox.Visible("ふつう"));
    }

    /// <summary>中断データ。書いて読み直すと同じ状態になる。</summary>
    [Test]
    public void 中断データは書いて読み直せる()
    {
        var dir = System.IO.Path.Combine(Application.temporaryCachePath, "resume_test");
        var keep = TowerSave.Directory;
        TowerSave.Directory = dir;
        try
        {
            TowerSave.Delete(TowerSave.AutoSlot);
            Assert.IsFalse(TowerSave.Exists(TowerSave.AutoSlot), "はじめは無い");

            var p = new PlayerState { Hp = 1234, Gold = 55, Floor = 7,
                                      HighestFloor = 7, Position = new Vector2Int(1, -2) };
            var g = new TowerGame(p);
            g.EnterFloor(7, new Vector2Int(1, -2));
            g.WriteResume();
            Assert.IsTrue(TowerSave.Exists(TowerSave.AutoSlot), "書ける");

            var back = TowerSave.Read(TowerSave.AutoSlot);
            Assert.IsNotNull(back);
            Assert.AreEqual(1234, back.Player.Hp);
            Assert.AreEqual(7, back.Player.Floor);
            Assert.AreEqual(new Vector2Int(1, -2), back.Player.Position);

            TowerGame.DropResume();
            Assert.IsFalse(TowerSave.Exists(TowerSave.AutoSlot), "捨てられる");
        }
        finally { TowerSave.Directory = keep; }
    }

    /// <summary>中断データは手で使う8つのスロットとは別のファイル。</summary>
    [Test]
    public void 中断データは手のセーブと別枠()
    {
        Assert.AreEqual(0, TowerSave.AutoSlot);
        Assert.AreNotEqual(TowerSave.AutoSlot, PhoneScreen.QuickSlot);
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerSave.cs");
        StringAssert.Contains("resume.txt", src, "別のファイル名にしてある");
    }
}
