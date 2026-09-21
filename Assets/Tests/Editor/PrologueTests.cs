using NUnit.Framework;
using UnityEngine;

/// <summary>冒頭の演出が原典の流れどおりかを確かめる。</summary>
public class PrologueTests
{
    [Test]
    public void ゼノに会うのは3階()
    {
        Assert.AreEqual(3, Prologue.ZenoFloor);
        Assert.AreEqual(3, GameStart.ZenoEncounterFloor);
    }

    [Test]
    public void 冒頭のセリフが原典のものになっている()
    {
        var before = new System.Collections.Generic.List<string>(Prologue.Lines(Prologue.BeforeFall));
        Assert.AreEqual(2, before.Count);
        StringAssert.Contains("１００人目の侵入者", before[0]);
        StringAssert.Contains("うわぁーっ", before[1]);

        var after = new System.Collections.Generic.List<string>(Prologue.Lines(Prologue.AfterFall));
        Assert.AreEqual(5, after.Count);
        StringAssert.Contains("牢屋", after[3]);
        StringAssert.Contains("剣と盾", after[4]);
        StringAssert.Contains("５階に剣、", after[4]);
    }

    [Test]
    public void 冒頭を終えると丸腰で2階から始まる()
    {
        var p = Prologue.Finish(PlayerState.NewGame());
        Assert.AreEqual(2, p.Floor, "2階の牢");
        Assert.AreEqual(GameStart.AfterZeno.Hp, p.Hp);
        Assert.AreEqual(GameStart.AfterZeno.Attack, p.Attack);
        Assert.AreEqual(GameStart.AfterZeno.Defense, p.Defense);
        // 原典は 400 / 10 / 10。今は調べもの用に DebugPlay で攻守を差し替えている。
        if (!DebugPlay.StrongAfterZeno)
        {
            Assert.AreEqual(10, p.Attack, "原典の攻撃力");
            Assert.AreEqual(10, p.Defense, "原典の防御力");
        }
        if (!DebugPlay.StrongHpAfterZeno) Assert.AreEqual(400, p.Hp, "原典の生命力");
        Assert.AreEqual(0, p.SwordRank, "神剣を奪われている");
        Assert.AreEqual(0, p.ShieldRank, "神盾を奪われている");
    }

    // ---- 通しで動かして見つかった取りこぼしの回帰試験 ----

    [Test]
    public void ゼノに奪われたあとは盗賊の真下に立つ()
    {
        // 原典 0x0701e / 0x07029 が X=2, Y=7 を書く → 盤面の (-3,-2)
        Assert.AreEqual(new Vector2Int(-3, -2), GameStart.AfterZeno.Position);
        Assert.AreEqual(2, GameStart.AfterZeno.Floor);

        var a = Resources.Load<TextAsset>("Floors/Floor_2");
        StringAssert.Contains("-3 -1  Mikata", a.text, "真上が盗賊");
    }

    [Test]
    public void 場面の途中で本当に奪われる()
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100,
            SwordRank = 5, ShieldRank = 5,
            Floor = 3, Position = new Vector2Int(-1, -2)
        };
        var g = new TowerGame(p);
        g.Step(Vector2Int.down);                 // 3階(-1,-3) で発火

        Assert.AreEqual(3, g.Player.Floor, "まだ3階");
        while (g.IsShowingPage) g.NextPage();

        Assert.AreEqual(2, g.Player.Floor, "2階に移る");
        Assert.AreEqual(GameStart.AfterZeno.Hp, g.Player.Hp);
        Assert.AreEqual(GameStart.AfterZeno.Attack, g.Player.Attack);
        Assert.AreEqual(GameStart.AfterZeno.Defense, g.Player.Defense);
        Assert.AreEqual(0, g.Player.SwordRank, "神剣を奪われる");
        Assert.AreEqual(0, g.Player.ShieldRank, "神盾を奪われる");
        Assert.AreEqual(new Vector2Int(-3, -2), g.Player.Position);
    }

    [Test]
    public void 奪われるのは題名のあと百十二の前()
    {
        var steps = Cutscenes.Instance.Steps("ゼノとの初対面");
        int title = steps.IndexOf(Cutscenes.TitleStep);
        int take = steps.IndexOf(Cutscenes.ZenoTakesStep);
        int next = steps.IndexOf("112");
        Assert.Greater(take, title, "題名のあと");
        Assert.Less(take, next, "112の前");
    }

    [Test]
    public void 牢が開いて左の通路へ出られる()
    {
        // 原典 0x070c8 / 0x070d7 が 2階の (-4,-1) と (-3,-1) に床を書く
        CollectionAssert.AreEqual(
            new[] { new Vector2Int(-4, -1), new Vector2Int(-3, -1) }, Prologue.PrisonOpens);

        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100,
            SwordRank = 5, ShieldRank = 5,
            Floor = 3, Position = new Vector2Int(-1, -2)
        };
        var g = new TowerGame(p);
        g.Step(Vector2Int.down);
        while (g.IsShowingPage) g.NextPage();

        Assert.AreEqual(2, g.Player.Floor);
        foreach (var c in Prologue.PrisonOpens)
            Assert.IsTrue(g.Floor.IsCleared(c), $"{c} が床になる");
    }

    [Test]
    public void 牢から上り階段まで歩いて行ける()
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100,
            SwordRank = 5, ShieldRank = 5,
            Floor = 3, Position = new Vector2Int(-1, -2)
        };
        var g = new TowerGame(p);
        g.Step(Vector2Int.down);
        while (g.IsShowingPage) g.NextPage();

        // (-3,-2) → (-3,-1) → (-4,-1) → (-5,-1) → 左端を下って (-5,-5) の上り階段。
        // ただし (-5,-2) には**歩いていった盗賊が立っている**（原典 0x07153）ので、
        // まず話しかける。話すとセリフ116 が出てそのマスが床になる
        // （ThiefEvents.txt の1行目 `2 | フラグ94=0 | 116 | - | 2:(-5,-2)=床`）。
        foreach (var c in new[]
                 {
                     new Vector2Int(-3, -1), new Vector2Int(-4, -1), new Vector2Int(-5, -1),
                 })
            Assert.IsTrue(g.Floor.IsCleared(c), $"{c} を通れる");
        Assert.AreEqual(Prologue.ThiefPrefab, g.Floor.At(Prologue.ThiefGoal), "盗賊が立っている");

        g.Player.Position = new Vector2Int(-5, -1);
        g.Step(Vector2Int.down);                       // 話しかける
        while (g.IsShowingPage) g.NextPage();

        foreach (var c in new[]
                 {
                     new Vector2Int(-5, -2), new Vector2Int(-5, -3), new Vector2Int(-5, -4),
                 })
            Assert.IsTrue(g.Floor.IsCleared(c), $"{c} を通れる");
        Assert.AreEqual(">", g.Floor.At(new Vector2Int(-5, -5)), "その先が上り階段");
    }

    [Test]
    public void 牢が開くのは百十五のあと()
    {
        var steps = Cutscenes.Instance.Steps("ゼノとの初対面");
        Assert.AreEqual(Cutscenes.PrisonOpensStep, steps[steps.Count - 1], "場面の最後");
        Assert.AreEqual("115", steps[steps.Count - 2]);
    }
}
