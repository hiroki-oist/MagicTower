using NUnit.Framework;
using UnityEngine;

/// <summary>作者の報告（2026-09-22 その3）で直した点を固定する。</summary>
public class FeedbackFixTests
{
    static TowerGame NewGameOn(int floor, Vector2Int at)
    {
        var p = PlayerState.NewGame();
        p.Floor = floor; p.Position = at;
        return new TowerGame(p);
    }

    [Test]
    public void 倒れるときは倒れるまでの斬りしか出さない()
    {
        var p = PlayerState.NewGame();
        p.Hp = 50; p.Attack = 10; p.Defense = 0;
        var before = p.Hp;
        var r = TowerBattle.Resolve(p, "Enemy_SlimeR");
        if (!r.Died) Assert.Ignore("この相手では死なない");
        Assert.AreEqual(0, p.Hp);
        Assert.Greater(r.Hits, 0);
        // 反撃1回ぶんずつ減って、生命力が尽きるところで止まる
        Assert.LessOrEqual((r.Hits - 1) * r.EnemyDamage, before);
        Assert.GreaterOrEqual(r.Hits * r.EnemyDamage, before);
    }

    [Test]
    public void 倒れたらゲームオーバーの画面が出る()
    {
        var g = NewGameOn(1, new Vector2Int(0, 0));
        g.Player.Hp = 10; g.Player.Attack = 10; g.Player.Defense = 0;
        g.Floor.Clear(new Vector2Int(0, 0));
        g.Floor.Put(new Vector2Int(0, 1), "Enemy_SlimeR");
        g.Step(Vector2Int.up);
        Assert.IsTrue(g.IsGameOver, "生命力が尽きたらゲームオーバー");
        Assert.AreEqual("Enemy_SlimeR", g.Floor.At(new Vector2Int(0, 1)),
                        "倒す前に死ぬので相手は残る（原典 0x64cd）");
    }

    [Test]
    public void 戦いの後始末は絵が動いている間は当たらない()
    {
        var g = NewGameOn(1, new Vector2Int(0, 0));
        g.Floor.Clear(new Vector2Int(0, 0));
        g.Floor.Put(new Vector2Int(0, 1), "Enemy_SlimeR");
        g.Step(Vector2Int.up);
        g.Pump(true);                                  // 絵が動いている間
        Assert.AreEqual("Enemy_SlimeR", g.Floor.At(new Vector2Int(0, 1)));
        g.Pump(false);                                 // 動き終わった
        Assert.AreEqual(FloorState.Floor, g.Floor.At(new Vector2Int(0, 1)));
        Assert.AreEqual(new Vector2Int(0, 1), g.Player.Position);
    }

    [Test]
    public void 妖精の称号を覚えて右上に出せる()
    {
        var g = NewGameOn(10, new Vector2Int(0, 2));
        g.Player.Hp = 99999; g.Player.Attack = 999; g.Player.Defense = 999;
        g.Player.YellowKeys = 99; g.Player.PurpleKeys = 99;
        Assert.AreEqual("", g.Player.Rank, "まだ何ももらっていない");
        g.Floor.Put(new Vector2Int(0, 3), "Fairy");
        g.Step(Vector2Int.up);                         // 妖精に話しかける
        CollectionAssert.Contains(Fairy.Ranks, g.Player.Rank, "もらった称号を覚える");
    }

    [Test]
    public void 渡したばかりの動きがある間は手を止める()
    {
        var g = NewGameOn(10, new Vector2Int(0, -1));
        g.Step(Vector2Int.up);                         // 骸骨に囲まれる場面
        Assert.IsTrue(g.HasPendingView, "押し上げの1歩を渡したところ");
        g.ConsumePlayerSlide();
        Assert.IsFalse(g.HasPendingView);
    }

    [Test]
    public void 本文は一秒に十文字ずつ出す()
    {
        Assert.AreEqual(10f, AuthorStyle.TypeCharsPerSecond,
                        "作者の指定（出し終わる前に決定で全部出し、もう一度で送る）");
    }
}
