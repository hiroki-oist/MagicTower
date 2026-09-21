using NUnit.Framework;
using UnityEngine;

/// <summary>サイコロの５の目による封印（原典 MTE.EXE 0x5cdb）を確かめる。</summary>
public class MagicSealTests
{
    static TowerGame Game()
    {
        var p = new PlayerState
        {
            Hp = 9990, Attack = 1000, Defense = 1000,
            Floor = MagicSeal.Floor, Position = new Vector2Int(0, 2)
        };
        return new TowerGame(p);
    }

    /// <summary>四隅に敵を置き、間を空ける。</summary>
    static void MakeFormation(TowerGame g)
    {
        foreach (var e in MagicSeal.Edges) g.Floor.Clear(e);
        foreach (var c in MagicSeal.Corners) g.Floor.Put(c, "Enemy_ID151");
    }

    [Test]
    public void 封印の形は中央と四隅と間の四マスでできている()
    {
        Assert.AreEqual(49, MagicSeal.Floor);
        Assert.AreEqual(new Vector2Int(0, 3), MagicSeal.Center);
        Assert.AreEqual(4, MagicSeal.Corners.Length);
        Assert.AreEqual(4, MagicSeal.Edges.Length);
        // 中央から見て四隅は斜め、間は上下左右
        foreach (var c in MagicSeal.Corners)
        {
            var d = c - MagicSeal.Center;
            Assert.AreEqual(1, Mathf.Abs(d.x));
            Assert.AreEqual(1, Mathf.Abs(d.y));
        }
        foreach (var e in MagicSeal.Edges)
        {
            var d = e - MagicSeal.Center;
            Assert.AreEqual(1, Mathf.Abs(d.x) + Mathf.Abs(d.y));
        }
    }

    [Test]
    public void 中央に立つと封印されてゼノが十分の一になる()
    {
        var g = Game();
        MakeFormation(g);
        Assert.IsFalse(g.Player.EventFlags.Contains(MagicSeal.FlagId));

        g.Step(Vector2Int.up);                       // (0,+2) → (0,+3)
        Assert.AreEqual(MagicSeal.Center, g.Player.Position);
        Assert.IsTrue(g.Player.EventFlags.Contains(MagicSeal.FlagId));

        var said = string.Join("\n", g.Log);
        StringAssert.Contains("この陣形は", said, "セリフ156");
    }

    [Test]
    public void 四隅のどれかが空いていると封印されない()
    {
        var g = Game();
        MakeFormation(g);
        g.Floor.Clear(MagicSeal.Corners[0]);         // 1体倒してしまった

        g.Step(Vector2Int.up);
        Assert.IsFalse(g.Player.EventFlags.Contains(MagicSeal.FlagId));
    }

    [Test]
    public void 間のマスがふさがっていると封印されない()
    {
        var g = Game();
        MakeFormation(g);
        g.Floor.Put(MagicSeal.Edges[0], "Enemy_ID151");

        g.Step(Vector2Int.up);
        Assert.IsFalse(g.Player.EventFlags.Contains(MagicSeal.FlagId));
    }

    [Test]
    public void 封印されるまではゼノの数値は原典のまま()
    {
        var e = TowerBattle.LoadEnemy(MagicSeal.BossPrefab);
        Assert.IsNotNull(e, "Resources/Battlers/Boss_Zeno.asset");
        Assert.AreEqual(8000, e.MaxHP);
        Assert.AreEqual(5000, e.Attack);
        Assert.AreEqual(1000, e.Defense);

        int hp = e.MaxHP, atk = e.Attack, def = e.Defense;
        var p = new PlayerState();
        MagicSeal.Adjust(p, MagicSeal.BossPrefab, ref hp, ref atk, ref def);
        Assert.AreEqual(8000, hp, "封印前は書き換えない");
    }

    [Test]
    public void 封印すると数値がちょうど十分の一になる()
    {
        var p = new PlayerState();
        p.EventFlags.Add(MagicSeal.FlagId);

        int hp = 8000, atk = 5000, def = 1000;
        MagicSeal.Adjust(p, MagicSeal.BossPrefab, ref hp, ref atk, ref def);
        Assert.AreEqual(800, hp);
        Assert.AreEqual(500, atk);
        Assert.AreEqual(100, def);
    }

    [Test]
    public void 封印はゼノ以外には効かない()
    {
        var p = new PlayerState();
        p.EventFlags.Add(MagicSeal.FlagId);

        int hp = 230, atk = 450, def = 100;
        MagicSeal.Adjust(p, "Enemy_ID93", ref hp, ref atk, ref def);
        Assert.AreEqual(230, hp);
    }

    [Test]
    public void 解錠表の四十九階の行が否定条件を持っている()
    {
        foreach (var u in SealedDoors.Instance.ForFloor(49))
        {
            if (u.FlagId != MagicSeal.FlagId) continue;
            Assert.AreEqual(4, u.Guards.Count, "空いていることを求めるマス");
            Assert.AreEqual(4, u.AntiGuards.Count, "敵がいることを求めるマス");
            CollectionAssert.AreEquivalent(MagicSeal.Edges, u.Guards);
            CollectionAssert.AreEquivalent(MagicSeal.Corners, u.AntiGuards);
            return;
        }
        Assert.Fail("49階のフラグ131の行が見つからない");
    }
}
