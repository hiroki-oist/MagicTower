using NUnit.Framework;
using UnityEngine;

/// <summary>場面（演出）が原典の並びどおりに流れるかを確かめる。</summary>
public class CutsceneTests
{
    [Test]
    public void 場面は九つで原典の関数の場所を控えている()
    {
        var c = Cutscenes.Instance;
        Assert.AreEqual(9, c.Count, "七つ＋物語の終わりの2つ");
        Assert.AreEqual("0x6ae0", c.Source("ゼノとの初対面"));
        Assert.AreEqual("0x325e", c.Source("金騎士の突撃"));
        Assert.AreEqual("0xd284", c.Source("大魔導師の口上"));
        Assert.AreEqual("0xc4d8", c.Source("終幕"));
    }

    [Test]
    public void ゼノとの初対面は題名をはさむ()
    {
        var steps = Cutscenes.Instance.Steps("ゼノとの初対面");
        CollectionAssert.AreEqual(
            new[] { "110", "111", "閃光", "題名", "奪われる", "112", "113", "114", "115", "牢が開く" },
            steps);
    }

    [Test]
    public void 知らない場面は流れない()
    {
        Assert.IsNull(Cutscenes.Instance.Steps("そんな場面は無い"));
    }

    static TowerGame Game(int floor, Vector2Int at)
    {
        var p = new PlayerState
        {
            Hp = 9990, Attack = 999, Defense = 999, Floor = floor, Position = at
        };
        return new TowerGame(p);
    }

    [Test]
    public void 場面を流すとページが順に積まれる()
    {
        var g = Game(1, new Vector2Int(0, -5));
        Assert.IsTrue(g.PlayScene("ゼノとの初対面"));

        // 110 111 閃光 題名 奪われる 112 113 114 115 牢が開く → 10 
        Assert.AreEqual(10, g.PendingPages);

        StringAssert.Contains("１００人目の侵入者", g.CurrentPage);
        Assert.AreEqual(110, g.Current.Value.Record);
        Assert.AreEqual(TowerGame.PageKind.Dialogue, g.Current.Value.Kind);

        g.NextPage();
        StringAssert.Contains("うわぁーっ", g.CurrentPage);

        g.NextPage();
        Assert.AreEqual(TowerGame.PageKind.Title, g.Current.Value.Kind, "ここで題名");
        StringAssert.Contains("Ｔｏｗｅｒ", g.CurrentPage);
        StringAssert.Contains("１９９６", g.CurrentPage);

        g.NextPage();          // 題名 → 奪われるが自動で起きて 112 が出る
        Assert.AreEqual(112, g.Current.Value.Record, "題名と奪われるのあとは112");
        Assert.AreEqual(2, g.Player.Floor, "奪われて2階にいる");
    }

    [Test]
    public void 三階に立つと初対面の場面が流れ出す()
    {
        var g = Game(3, new Vector2Int(-1, -2));
        Assert.AreEqual(0, g.PendingPages);

        g.Step(Vector2Int.down);
        Assert.AreEqual("ゼノとの初対面", g.PendingScene);
        Assert.AreEqual(10, g.PendingPages, "場面がそのまま積まれる");
        StringAssert.Contains("ようこそ", g.CurrentPage);
    }

    [Test]
    public void 金騎士の突撃は六つのセリフが順に出る()
    {
        var g = Game(40, new Vector2Int(0, -1));
        Assert.IsTrue(g.PlayScene("金騎士の突撃"));
        Assert.AreEqual(6, g.PendingPages);

        var want = new[] { 135, 136, 137, 138, 139, 140 };
        foreach (var id in want)
        {
            Assert.AreEqual(id, g.Current.Value.Record);
            g.NextPage();
        }
        Assert.IsNull(g.CurrentPage);
    }

    [Test]
    public void 場面の名前は全部位置イベントから呼ばれる()
    {
        foreach (var name in Cutscenes.Instance.Names)
        {
            bool used = false;
            foreach (var ev in PositionEvents.Instance.All)
                if (ev.Scene == name) used = true;
            Assert.IsTrue(used, name);
        }
    }

    [Test]
    public void 会話が出ている間は動けず送るだけになる()
    {
        var g = Game(3, new Vector2Int(-1, -2));
        g.Step(Vector2Int.down);                      // ここで場面が積まれる
        Assert.IsTrue(g.IsShowingPage);

        var at = g.Player.Position;
        int pages = g.PendingPages;

        var r = g.TryStep(Vector2Int.up);
        Assert.AreEqual(TowerPlayer.Outcome.Blocked, r, "動かない");
        Assert.AreEqual(at, g.Player.Position);
        Assert.AreEqual(pages - 1, g.PendingPages, "1つ送られる");
    }

    [Test]
    public void 送り終えたら動けるようになる()
    {
        var g = Game(3, new Vector2Int(-1, -2));
        g.Step(Vector2Int.down);
        var at = g.Player.Position;

        while (g.IsShowingPage) g.TryStep(Vector2Int.up);
        Assert.AreEqual(0, g.PendingPages);

        g.TryStep(Vector2Int.up);
        Assert.AreNotEqual(at, g.Player.Position, "送り終えたら進める");
    }

    [Test]
    public void 何も出ていなければそのまま進む()
    {
        var g = Game(1, new Vector2Int(0, -5));
        Assert.IsFalse(g.IsShowingPage);
        var before = g.Player.Position;
        g.TryStep(Vector2Int.up);
        Assert.AreNotEqual(before, g.Player.Position);
    }
}
