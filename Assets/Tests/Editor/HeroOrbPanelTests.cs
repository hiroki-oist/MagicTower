using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 勇者のオーブの一覧の見せ方（作者の指定。中身は原典 0x8c2a のまま）。
/// </summary>
public class HeroOrbPanelTests
{
    static PlayerState Player(int floor, int attack, int defense)
    {
        return new PlayerState
        {
            Hp = 1000, Attack = attack, Defense = defense,
            Floor = floor, HighestFloor = 50, Position = new Vector2Int(0, 0),
        };
    }

    [Test]
    public void 一行に名前と金と手数とダメージが並ぶ()
    {
        var g = new TowerGame(Player(3, 100, 100));
        var rows = HeroOrb.Rows(g.Floor, g.Player);
        Assert.Greater(rows.Count, 0, "3階には相手が居る");
        foreach (var r in rows)
        {
            Assert.IsNotEmpty(r.Name, "名前が入っている");
            Assert.GreaterOrEqual(r.Gold, 0);
            if (r.CanAttack) Assert.Greater(r.Turns, 0, $"{r.Name} の手数");
        }
    }

    [Test]
    public void 欄の文字は見せてもらった画面と同じ並び()
    {
        Assert.AreEqual("５ 金", HeroOrbPanel.GoldLine(5));
        Assert.AreEqual("５ ターン", HeroOrbPanel.TurnLine(5));
        Assert.AreEqual("生命 ６０　攻撃 ３２　防御 ８", HeroOrbPanel.StatLine(60, 32, 8));
        StringAssert.Contains("８８", HeroOrbPanel.DamageLine(true, 88));
        Assert.AreEqual("攻撃不可能", HeroOrbPanel.DamageLine(false, 0));
    }

    [Test]
    public void メニューから選ぶと専用の頁が出る()
    {
        var g = new TowerGame(Player(3, 100, 100));
        g.Player.Give(SpecialItems.Kind.HeroOrb);
        g.OpenMenu();
        while (g.MenuCurrent != TowerMenu.Entry.HeroOrb) g.MenuMove(1);
        g.MenuConfirm();
        Assert.IsTrue(g.IsShowingPage, "中身が出る");
        Assert.AreEqual(TowerGame.PageKind.HeroOrb, g.CurrentKind, "会話の窓ではない");
        g.NextPage();
        Assert.IsFalse(g.IsShowingPage, "1枚で終わる");
    }

    [Test]
    public void 九行ぶんの枠が画面に収まる()
    {
        Assert.AreEqual(9, HeroOrb.MaxRows);
        Assert.AreEqual(1f, HeroOrbPanel.RowHeight * HeroOrb.MaxRows, 1e-4f, "9行で窓いっぱい");
        Assert.Less(HeroOrbPanel.NameLeft, HeroOrbPanel.MiddleLeft, "名前は左、能力は右");
        Assert.Less(HeroOrbPanel.MiddleLeft, HeroOrbPanel.RightEdge);
    }
}
