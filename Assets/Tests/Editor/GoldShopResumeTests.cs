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
        StringAssert.Contains("一度購入すれば", t);
        StringAssert.Contains("無料", t);
        foreach (var line in PhoneScreen.ShopIntro)
            Assert.LessOrEqual(line.Length, DialogueBox.CharsPerLine, line);
    }

    [Test]
    public void 選べる行は売り物の数とやめるで四つ()
    {
        var p = new PlayerState { Hp = 100, Floor = 1 };
        var g = new TowerGame(p);
        Assert.IsTrue(g.AskGoldShop());
        Assert.IsTrue(g.IsChoosing);
        Assert.AreEqual(GoldShop.Count + 1, g.ChoiceCount);
        // 窓の本文にも売り物の行が並ぶ
        var text = GoldShop.Text();
        for (int i = 0; i < GoldShop.Count; i++)
            StringAssert.Contains(GoldShop.Wide(GoldShop.GoldOf(i)) + "ＧＯＬＤ", text);
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
