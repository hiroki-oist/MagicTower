using NUnit.Framework;
using UnityEngine;

/// <summary>階段で着いたときのできごと（原典 MTE.EXE 0x2982）を確かめる。</summary>
public class ArrivalEventTests
{
    [Test]
    public void できごとは一件だけ()
    {
        Assert.AreEqual(1, ArrivalEvents.Instance.Count,
                        "EXE 全体を調べてもこの形は28階の1件だけだった");
        foreach (var e in ArrivalEvents.Instance.All)
        {
            Assert.AreEqual(28, e.Floor);
            Assert.IsTrue(e.Descending, "下りで着いたときだけ");
            Assert.AreEqual(108, e.FlagId);
            Assert.AreEqual(3, e.Changes.Count);
        }
    }

    [Test]
    public void 二十九階は最初は上り階段まで行けない()
    {
        var a = Resources.Load<TextAsset>("Floors/Floor_29");
        Assert.IsNotNull(a);
        // 原典データでは (0,+3) と (0,+2) が壁と盗賊で、左半分と縦穴が繋がっていない
        StringAssert.Contains("+0 +2  Mikata", a.text);
    }

    static TowerGame Game(int floor)
    {
        var p = new PlayerState
        {
            Hp = 9990, Attack = 990, Defense = 990,
            YellowKeys = 99, PurpleKeys = 99, RedKeys = 99,
            Floor = floor, Position = new Vector2Int(0, 0)
        };
        return new TowerGame(p);
    }

    [Test]
    public void 二十八階に降りると二十九階の抜け穴が開く()
    {
        var g = Game(29);
        Assert.IsFalse(g.FloorAt(29).IsCleared(new Vector2Int(0, 3)), "はじめは壁");
        Assert.AreEqual("Mikata", g.FloorAt(29).At(new Vector2Int(0, 2)), "はじめは盗賊");

        Assert.IsTrue(g.ApplyArrival(28, descending: true));

        Assert.IsTrue(g.FloorAt(29).IsCleared(new Vector2Int(0, 3)), "抜け穴が開く");
        Assert.IsTrue(g.FloorAt(29).IsCleared(new Vector2Int(0, 2)));
        Assert.AreEqual("Mikata", g.FloorAt(29).At(new Vector2Int(1, 4)), "盗賊が移る");
        Assert.IsTrue(g.Player.EventFlags.Contains(108));
    }

    [Test]
    public void 一度きりで上りでは起きない()
    {
        var g = Game(29);
        Assert.IsFalse(g.ApplyArrival(28, descending: false), "上りでは起きない");
        Assert.IsTrue(g.ApplyArrival(28, descending: true));
        Assert.IsFalse(g.ApplyArrival(28, descending: true), "二度目は起きない");
    }

    [Test]
    public void 抜け穴が開くと上り階段まで繋がる()
    {
        var g = Game(29);
        g.ApplyArrival(28, descending: true);
        var f = g.FloorAt(29);
        // 左端の列 → 上の段 → 抜け穴 → 縦穴 → 上り階段
        foreach (var c in new[]
        {
            new Vector2Int(-5, -4), new Vector2Int(-5, 5), new Vector2Int(-1, 5),
            new Vector2Int(-1, 4), new Vector2Int(0, 4), new Vector2Int(0, 3),
            new Vector2Int(0, 2), new Vector2Int(0, -4),
        })
            Assert.IsTrue(f.IsCleared(c), $"{c} を通れる");
        Assert.AreEqual(">", f.At(new Vector2Int(0, -5)), "その先が上り階段");
    }
}
