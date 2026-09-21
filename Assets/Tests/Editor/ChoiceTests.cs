using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 「◎はい／◎いいえ」と祭壇の望みを**選べる**こと。原典 `0x9e78`
/// （上下で印を動かし、リターン 0x1c かスペース 0x34 で決める）。
///
/// ここが繋がっていなくて、**商人から何も買えず、祭壇にも寄進できなかった**。
/// 売り口上は出るのに何も起きない、という形だったので遊んでも気づきにくい。
/// </summary>
public class ChoiceTests
{
    static TowerGame Shopper(int floor, Vector2Int at, int gold)
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100, Gold = gold,
            Floor = floor, HighestFloor = floor, Position = at,
        };
        return new TowerGame(p);
    }

    /// <summary>その階の商人に触れる。</summary>
    static TowerGame TalkToMerchantOn(int floor)
    {
        var data = FloorData.Load(floor);
        Assert.IsNotNull(data);
        foreach (var o in data.Objects)
        {
            if (o.Prefab != "Merchant") continue;
            var g = Shopper(floor, o.Pos + Vector2Int.down, 5000);
            g.Floor.Clear(o.Pos + Vector2Int.down);
            g.Step(Vector2Int.up);                     // 触れる（上には乗らない）
            return g;
        }
        Assert.Fail($"F{floor} に商人が居ない");
        return null;
    }

    [Test]
    public void 商人に触れると選ばされる()
    {
        var g = TalkToMerchantOn(7);                   // 7階＝黄の鍵5本 50GOLD
        Assert.IsTrue(g.IsShowingPage, "売り口上が出る");
        Assert.IsTrue(g.IsChoosing, "選ばせている");
        Assert.AreEqual(2, g.ChoiceCount, "はい／いいえ");
        Assert.AreEqual(0, g.ChoiceIndex);
        g.MoveChoice(1);
        Assert.AreEqual(1, g.ChoiceIndex, "下へ動く");
        g.MoveChoice(-5);
        Assert.AreEqual(0, g.ChoiceIndex, "上で止まる");
    }

    [Test]
    public void はいを選ぶと買えて金が減る()
    {
        var g = TalkToMerchantOn(7);
        int gold = g.Player.Gold, keys = g.Player.YellowKeys;
        g.ConfirmChoice();
        Assert.IsFalse(g.IsChoosing);
        Assert.AreEqual(keys + 5, g.Player.YellowKeys, "黄の鍵が5本増える");
        Assert.AreEqual(gold - 50, g.Player.Gold, "50GOLD 払う");
    }

    [Test]
    public void いいえを選ぶと何も起きない()
    {
        var g = TalkToMerchantOn(7);
        int gold = g.Player.Gold, keys = g.Player.YellowKeys;
        g.MoveChoice(1);
        g.ConfirmChoice();
        Assert.AreEqual(keys, g.Player.YellowKeys);
        Assert.AreEqual(gold, g.Player.Gold);
    }

    [Test]
    public void 金が足りなければ買えない()
    {
        var data = FloorData.Load(7);
        foreach (var o in data.Objects)
        {
            if (o.Prefab != "Merchant") continue;
            var g = Shopper(7, o.Pos + Vector2Int.down, 10);
            g.Floor.Clear(o.Pos + Vector2Int.down);
            g.Step(Vector2Int.up);
            g.ConfirmChoice();
            Assert.AreEqual(10, g.Player.Gold, "金は減らない");
            Assert.AreEqual(0, g.Player.YellowKeys, "鍵も増えない");
            return;
        }
        Assert.Fail();
    }

    [Test]
    public void 祭壇は望みを四つから選ばせる()
    {
        var data = FloorData.Load(4);                  // 4階の祭壇
        foreach (var o in data.Objects)
        {
            if (o.Prefab != "Shop_Altar") continue;
            var g = Shopper(4, o.Pos + Vector2Int.down, 5000);
            g.Floor.Clear(o.Pos + Vector2Int.down);
            g.Step(Vector2Int.up);
            Assert.IsTrue(g.IsChoosing, "選ばせている");
            Assert.AreEqual(4, g.ChoiceCount, "生命力／攻撃力／防御力／やめる");

            int atk = g.Player.Attack, gold = g.Player.Gold;
            g.MoveChoice(1);                           // 攻撃力
            g.ConfirmChoice();
            Assert.Greater(g.Player.Attack, atk, "攻撃力が上がる");
            Assert.Less(g.Player.Gold, gold, "寄進した");
            return;
        }
        Assert.Fail("4階に祭壇が無い");
    }

    [Test]
    public void やめるを選ぶと何も起きない()
    {
        var data = FloorData.Load(4);
        foreach (var o in data.Objects)
        {
            if (o.Prefab != "Shop_Altar") continue;
            var g = Shopper(4, o.Pos + Vector2Int.down, 5000);
            g.Floor.Clear(o.Pos + Vector2Int.down);
            g.Step(Vector2Int.up);
            int atk = g.Player.Attack, gold = g.Player.Gold;
            g.MoveChoice(3);
            g.ConfirmChoice();
            Assert.AreEqual(atk, g.Player.Attack);
            Assert.AreEqual(gold, g.Player.Gold);
            return;
        }
        Assert.Fail();
    }

    [Test]
    public void 入力の側が選ばせに繋がっている()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerController.cs");
        StringAssert.Contains("Game.IsChoosing", src);
        StringAssert.Contains("Game.MoveChoice", src);
        StringAssert.Contains("Game.ConfirmChoice", src);
    }
}
