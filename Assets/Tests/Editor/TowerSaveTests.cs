using NUnit.Framework;
using UnityEngine;

/// <summary>セーブとロードが原典の並びどおりに往復するかを確かめる。</summary>
public class TowerSaveTests
{
    string dir;

    [SetUp]
    public void SetUp()
    {
        dir = System.IO.Path.Combine(Application.temporaryCachePath, "mt_save_test");
        if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        TowerSave.Directory = dir;
    }

    [TearDown]
    public void TearDown()
    {
        TowerSave.Directory = null;
        if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
    }

    [Test]
    public void スロットは原典と同じ8つ()
    {
        Assert.AreEqual(8, TowerSave.SlotCount);
    }

    [Test]
    public void 保存して読み戻せる()
    {
        var p = new PlayerState
        {
            Hp = 1234, Attack = 56, Defense = 78, Gold = 900,
            Floor = 17, HighestFloor = 20, Position = new Vector2Int(-3, 4),
            YellowKeys = 5, RedKeys = 1, PurpleKeys = 2,
            SwordRank = 3, ShieldRank = 4, DonationCount = 6,
        };
        p.Give(SpecialItems.Kind.Cross);
        p.Give(SpecialItems.Kind.FlightOrb);

        TowerSave.Save(3, p);
        Assert.IsTrue(TowerSave.Exists(3));

        var q = TowerSave.Load(3);
        Assert.IsNotNull(q);
        Assert.AreEqual(1234, q.Hp);
        Assert.AreEqual(56, q.Attack);
        Assert.AreEqual(78, q.Defense);
        Assert.AreEqual(900, q.Gold);
        Assert.AreEqual(17, q.Floor);
        Assert.AreEqual(20, q.HighestFloor);
        Assert.AreEqual(new Vector2Int(-3, 4), q.Position);
        Assert.AreEqual(5, q.YellowKeys);
        Assert.AreEqual(2, q.PurpleKeys);
        Assert.AreEqual(3, q.SwordRank);
        Assert.AreEqual(4, q.ShieldRank);
        Assert.AreEqual(6, q.DonationCount);
        Assert.IsTrue(q.Has(SpecialItems.Kind.Cross));
        Assert.IsTrue(q.Has(SpecialItems.Kind.FlightOrb));
        Assert.IsFalse(q.Has(SpecialItems.Kind.LuckyCoin));
    }

    [Test]
    public void 空きスロットは読めない()
    {
        Assert.IsFalse(TowerSave.Exists(1));
        Assert.IsNull(TowerSave.Load(1));
        Assert.AreEqual("セーブデータがありません。",
                        GameText.Instance.Message(TowerSave.EmptyMessageKey));
    }

    [Test]
    public void 消せる()
    {
        TowerSave.Save(2, PlayerState.NewGame());
        Assert.IsTrue(TowerSave.Exists(2));
        TowerSave.Delete(2);
        Assert.IsFalse(TowerSave.Exists(2));
    }
}
