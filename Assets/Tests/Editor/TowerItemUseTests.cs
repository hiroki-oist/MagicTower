using NUnit.Framework;
using UnityEngine;

/// <summary>特殊アイテムを使ったときの効果が原典どおりかを確かめる。</summary>
public class TowerItemUseTests
{
    TowerGame NewGame(int floor)
    {
        var p = new PlayerState { Hp = 5000, Attack = 80, Defense = 98, Floor = floor,
                                  Position = new Vector2Int(0, 0) };
        return new TowerGame(p);
    }

    [Test]
    public void 持っていないものは使えない()
    {
        var g = NewGame(1);
        Assert.IsFalse(g.UseItem(SpecialItems.Kind.Matock));
    }

    [Test]
    public void エリクサーは攻撃と防御に応じて回復する()
    {
        var g = NewGame(1);
        g.Player.Give(SpecialItems.Kind.Elixir);
        g.Player.Hp = 100;
        Assert.IsTrue(g.UseItem(SpecialItems.Kind.Elixir));
        Assert.AreEqual(100 + 5 * (80 * 2 + 98), g.Player.Hp, "5×(攻撃×2+防御)");
        Assert.IsFalse(g.Player.Has(SpecialItems.Kind.Elixir), "使い切り");
    }

    [Test]
    public void マトックは四方の壁を崩す()
    {
        var g = NewGame(1);
        g.Player.Give(SpecialItems.Kind.Matock);
        g.Floor.Clear(new Vector2Int(0, 0));
        g.Floor.Put(new Vector2Int(0, 1), "#");
        g.Floor.Put(new Vector2Int(1, 0), "#");
        g.Floor.Put(new Vector2Int(0, -1), "Enemy_SlimeG");   // 壁ではないので残る

        Assert.IsTrue(g.UseItem(SpecialItems.Kind.Matock));
        Assert.AreEqual(FloorState.Floor, g.Floor.At(new Vector2Int(0, 1)));
        Assert.AreEqual(FloorState.Floor, g.Floor.At(new Vector2Int(1, 0)));
        Assert.AreEqual("Enemy_SlimeG", g.Floor.At(new Vector2Int(0, -1)), "敵は崩せない");
    }

    [Test]
    public void スーパーマトックは階の壁を全部崩す()
    {
        var g = NewGame(1);
        g.Player.Give(SpecialItems.Kind.SuperMatock);
        Assert.IsTrue(g.UseItem(SpecialItems.Kind.SuperMatock));
        int walls = 0;
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                if (g.Floor.At(new Vector2Int(x, y)) == "#") walls++;
        Assert.AreEqual(0, walls, "壁が1つも残らない");
    }

    [Test]
    public void 魔法の鍵は階の黄色い扉を全部開ける()
    {
        var g = NewGame(1);
        g.Player.Give(SpecialItems.Kind.MagicKey);
        Assert.IsTrue(g.UseItem(SpecialItems.Kind.MagicKey));
        int doors = 0;
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                if (g.Floor.At(new Vector2Int(x, y)) == "y") doors++;
        Assert.AreEqual(0, doors, "黄色い扉が残らない");
    }

    [Test]
    public void 破壊の玉は四方の敵を消すがボスには効かない()
    {
        var g = NewGame(1);
        g.Player.Give(SpecialItems.Kind.DestructionOrb);
        g.Floor.Clear(new Vector2Int(0, 0));
        g.Floor.Put(new Vector2Int(0, 1),  "Enemy_SlimeG");    // 消える
        g.Floor.Put(new Vector2Int(1, 0),  "Boss_Dragon");     // ID122 は対象外
        g.Floor.Put(new Vector2Int(-1, 0), "#");               // 壁は消えない

        Assert.IsTrue(g.UseItem(SpecialItems.Kind.DestructionOrb));
        Assert.AreEqual(FloorState.Floor, g.Floor.At(new Vector2Int(0, 1)));
        Assert.AreEqual("Boss_Dragon", g.Floor.At(new Vector2Int(1, 0)), "ドラゴンは消せない");
        Assert.AreEqual("#", g.Floor.At(new Vector2Int(-1, 0)));
    }

    [Test]
    public void 空間転移の秘宝は生命力を払う()
    {
        var g = NewGame(1);
        g.Player.Give(SpecialItems.Kind.TeleportRelic);
        g.Player.Hp = 2000;
        // 飛び先（中心について点対称のマス）が床でないと断られるので、空けておく
        g.Floor.Clear(SpecialItemEffects.TeleportTarget(g.Player.Position));
        Assert.IsTrue(g.UseItem(SpecialItems.Kind.TeleportRelic));
        Assert.AreEqual(1200, g.Player.Hp, "生命力−800");
    }

    [Test]
    public void オーブは使うものではない()
    {
        var g = NewGame(1);
        g.Player.Give(SpecialItems.Kind.HeroOrb);
        Assert.IsFalse(g.UseItem(SpecialItems.Kind.HeroOrb));
        Assert.IsTrue(g.Player.Has(SpecialItems.Kind.HeroOrb), "持ったまま");
    }
}
