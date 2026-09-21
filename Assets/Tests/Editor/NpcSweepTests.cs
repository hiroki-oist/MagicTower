using NUnit.Framework;
using UnityEngine;

/// <summary>
/// **商人と仙人を全員ひとりずつ相手にしてみる**検査。
///
/// 表（`ShopOffers.txt` / `MasterHints.txt`）に載っているのに、
/// 筋からは外れていて誰も触っていないものがあり得るので、全件を通す。
/// 見るのは「話が出る・買える・値段と品物が表どおり・覚え書きが立つ」まで。
/// </summary>
public class NpcSweepTests
{
    static PlayerState Rich(int floor)
    {
        return new PlayerState
        {
            Hp = PlayerState.MaxHp, Attack = 100, Defense = 100,
            Gold = 5000, Floor = floor, HighestFloor = 50,   // 上限で頭を押さえられない額
            YellowKeys = 9, PurpleKeys = 9, RedKeys = 9,
        };
    }

    [Test]
    public void 商人は全員から買えて覚え書きが立つ()
    {
        int n = 0;
        foreach (var o in Shop.Instance.ForFloor(-1))
            Assert.Fail("ForFloor(-1) は空のはず");     // 使い方の確認

        for (int floor = 0; floor <= 50; floor++)
            foreach (var o in Shop.Instance.ForFloor(floor))
            {
                n++;
                var p = Rich(floor);
                int goldBefore = p.Gold, keysBefore = p.YellowKeys;

                var said = TowerNpc.Buy(p, o);
                Assert.IsFalse(string.IsNullOrEmpty(said), $"F{floor} {o.Goods} で何も言わない");

                if (o.Mode == "buy" || o.Mode == "buy_var")
                {
                    int price = o.Mode == "buy_var"
                        ? Shop.VariableKeyPrice(p.HighestFloor) : o.Price;
                    Assert.AreEqual(goldBefore - price, p.Gold, $"F{floor} の代金");
                    Assert.Greater(price, 0, $"F{floor} の値段");
                }
                else if (o.Mode == "sell")
                {
                    Assert.AreEqual(keysBefore - 1, p.YellowKeys, "売ると鍵が減る");
                    Assert.AreEqual(goldBefore + o.Price, p.Gold, "売ると金が増える");
                }
                else if (o.Mode == "gift")
                {
                    Assert.AreEqual(goldBefore + o.Price, p.Gold, "もらうと金が増える");
                }

                if (o.BuyFlag >= 0)
                    Assert.IsTrue(p.EventFlags.Contains(o.BuyFlag),
                                  $"F{floor} の買った覚え書き {o.BuyFlag} が立たない");
                if (o.OfferId >= 0)
                    Assert.IsTrue(GameText.Instance.Dialogue(o.OfferId).HasValue,
                                  $"F{floor} の売り口上 {o.OfferId} が無い");
                if (o.HintId >= 0)
                    Assert.IsTrue(GameText.Instance.Dialogue(o.HintId).HasValue,
                                  $"F{floor} のヒント {o.HintId} が無い");
            }
        Assert.AreEqual(Shop.Instance.Count, n, "表の全件を通せた");
        Assert.Greater(n, 0);
    }

    [Test]
    public void 金が足りなければ買えない()
    {
        foreach (var o in Shop.Instance.ForFloor(6))      // 6階の紫の鍵（50GOLD）
        {
            if (o.Mode != "buy") continue;
            var p = Rich(6);
            p.Gold = o.Price - 1;
            var said = TowerNpc.Buy(p, o);
            Assert.AreEqual(GameText.Instance.Message("shop_no_money"), said);
            Assert.AreEqual(o.Price - 1, p.Gold, "金は減らない");
            Assert.IsFalse(p.EventFlags.Contains(o.BuyFlag), "覚え書きも立たない");
        }
    }

    [Test]
    public void 鍵屋の値段は到達最高階で決まる()
    {
        // 原典 0x14 * es:[0x96] ＝ 20 x 到達最高階
        Assert.AreEqual(20, Shop.VariableKeyUnitPrice);
        foreach (var f in new[] { 12, 30, 41, 50 })
            Assert.AreEqual(20 * f, Shop.VariableKeyPrice(f), $"到達最高階 {f}");
    }

    [Test]
    public void 仙人のヒントは全員ぶん読める()
    {
        int n = 0;
        foreach (var h in MasterHints.Instance.All)
        {
            n++;
            Assert.IsTrue(GameText.Instance.Dialogue(h.Record).HasValue,
                          $"F{h.Floor} の仙人のヒント {h.Record} が無い");
            Assert.GreaterOrEqual(h.FlagId, 0, $"F{h.Floor} の覚え書きが無い");
        }
        Assert.Greater(n, 0);
    }

    [Test]
    public void 拾える物は全部その場で効く()
    {
        // 盤面に置かれている物を全部、拾ってみる
        var power = ItemPowerTable.Instance;
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int floor = 0; floor <= 50; floor++)
        {
            var data = FloorData.Load(floor);
            if (data == null) continue;
            foreach (var o in data.Objects)
            {
                if (!seen.Add(o.Prefab)) continue;
                var p = Rich(floor);
                p.Gold = 0; p.YellowKeys = p.PurpleKeys = p.RedKeys = 0;
                p.Hp = 100;
                var got = ItemPickup.Apply(p, o.Prefab, power);
                if (!got.Taken) continue;                    // 敵や人や地形
                Assert.IsFalse(string.IsNullOrEmpty(got.MessageKey),
                               $"{o.Prefab} に文言が無い");
                var text = GameText.Instance.Message(got.MessageKey, got.Value);
                Assert.IsFalse(string.IsNullOrEmpty(text),
                               $"{o.Prefab} の文言 '{got.MessageKey}' が Messages.txt に無い");
            }
        }
        Assert.Greater(seen.Count, 30, "見た物の種類");
    }
}
