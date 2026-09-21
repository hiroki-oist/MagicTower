using NUnit.Framework;
using UnityEngine;

/// <summary>メニューと3つのオーブの機能が原典どおりかを確かめる。</summary>
public class TowerMenuTests
{
    PlayerState player;

    [SetUp]
    public void SetUp()
    {
        player = new PlayerState { Hp = 1000, Attack = 20, Defense = 20, Floor = 5 };
    }

    [Test]
    public void 持っていないオーブは並ばない()
    {
        var e = TowerMenu.Entries(player);
        CollectionAssert.DoesNotContain(e, TowerMenu.Entry.HeroOrb);
        CollectionAssert.Contains(e, TowerMenu.Entry.SpecialItems);
        CollectionAssert.Contains(e, TowerMenu.Entry.Save);
        Assert.AreEqual(5, e.Count, "オーブ無しなら5項目");
    }

    [Test]
    public void 三つ揃うと原典と同じ8項目になる()
    {
        player.Give(SpecialItems.Kind.HeroOrb);
        player.Give(SpecialItems.Kind.FlightOrb);
        player.Give(SpecialItems.Kind.WisdomOrb);
        var e = TowerMenu.Entries(player);
        Assert.AreEqual(8, e.Count);
        Assert.AreEqual(TowerMenu.Entry.HeroOrb, e[0], "並びは 勇者→飛翔→知恵");
        Assert.AreEqual(TowerMenu.Entry.FlightOrb, e[1]);
        Assert.AreEqual(TowerMenu.Entry.WisdomOrb, e[2]);
    }

    [Test]
    public void 項目の文言が原典のものになる()
    {
        Assert.AreEqual("メニューの選択をしてください。", TowerMenu.Title());
        Assert.AreEqual("　　勇者のオーブを使う", TowerMenu.Label(TowerMenu.Entry.HeroOrb));
        Assert.AreEqual("　　スペシャル・アイテムを使う", TowerMenu.Label(TowerMenu.Entry.SpecialItems));
        Assert.AreEqual("　　ゲーム終了", TowerMenu.Label(TowerMenu.Entry.Quit));
    }

    [Test]
    public void 飛翔のオーブは階段の隣でしか使えない()
    {
        var floor = new FloorState(FloorData.Load(2));
        Vector2Int stair = default(Vector2Int);
        foreach (var p in FloorData.Load(2).Find(FloorData.Terrain.StairsUp)) { stair = p; break; }

        Assert.IsTrue(TowerMenu.NextToStairs(floor, stair + new Vector2Int(0, -1)));
        Assert.IsFalse(TowerMenu.NextToStairs(floor, stair + new Vector2Int(0, -5)));
        Assert.AreEqual("階段の隣でなければ使えません。", TowerMenu.FlightBlockedMessage());
    }

    [Test]
    /// <summary>
    /// 原典（0x91b4〜0x921d）は**1階から到達最高階まで**を上下するだけで、
    /// 訪れたかどうかは見ていない。44階（幻の階）だけ名指しで飛ばす。
    /// </summary>
    public void 飛べるのは1階から到達最高階まで()
    {
        Assert.IsEmpty(TowerMenu.FlightTargets(player), "到達最高階が0なら何も出ない");
        player.MarkVisited(1);
        player.MarkVisited(5);
        player.MarkVisited(3);
        Assert.AreEqual(5, player.HighestFloor, "到達最高階が更新される");
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, TowerMenu.FlightTargets(player),
                                  "飛び越した階も選べる");

        player.MarkVisited(46);
        CollectionAssert.DoesNotContain(TowerMenu.FlightTargets(player),
                                        FloorTransition.PhantomFloor, "幻の階は出ない");
        CollectionAssert.Contains(TowerMenu.FlightTargets(player), 45);
    }

    [Test]
    public void 知恵のオーブは持っている物の説明を並べる()
    {
        Assert.IsEmpty(TowerMenu.ItemKnowledge(player));
        player.Give(SpecialItems.Kind.Cross);
        player.Give(SpecialItems.Kind.LuckyCoin);
        var k = TowerMenu.ItemKnowledge(player);
        Assert.AreEqual(2, k.Count);
    }

    [Test]
    public void 訪問記録もセーブされる()
    {
        var dir = System.IO.Path.Combine(Application.temporaryCachePath, "mt_visit_test");
        if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        TowerSave.Directory = dir;
        try
        {
            player.MarkVisited(1); player.MarkVisited(7);
            TowerSave.Save(1, player);
            var q = TowerSave.Load(1);
            Assert.IsNotNull(q);
            CollectionAssert.AreEqual(new[] { 1, 7 }, new System.Collections.Generic.List<int>(q.Visited));
            Assert.AreEqual(7, q.HighestFloor, "到達最高階も残る（飛翔のオーブが見るのはこちら）");
        }
        finally
        {
            TowerSave.Directory = null;
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        }
    }

    // ---- 窓（原典 0x0d1e）----

    [Test]
    public void メニューの窓は十一行で上端五十()
    {
        Assert.AreEqual(11, TowerMenu.WindowLines, "原典 0xb");
        Assert.AreEqual(50, TowerMenu.TopEdge, "原典 0x32");
        Assert.AreEqual(82, TowerMenu.CursorX);
        Assert.AreEqual(23, TowerMenu.CursorY);
    }

    [Test]
    public void 窓は会話と同じ形で画面に収まる()
    {
        var w = TowerMenu.Window;
        Assert.AreEqual(160, w.xMin, "会話の窓と同じ左端");
        Assert.AreEqual(320, w.width);
        Assert.AreEqual(50, w.yMin);
        Assert.AreEqual(DialogueWindow.Height(11), w.height);
        Assert.IsTrue(DialogueWindow.Fits(w));
    }

    [Test]
    public void 窓の大きさはオーブの有無で変わらない()
    {
        // 原典は行数を 0xb 固定で渡している
        var none = new PlayerState();
        var all = new PlayerState();
        all.Give(SpecialItems.Kind.HeroOrb);
        all.Give(SpecialItems.Kind.FlightOrb);
        all.Give(SpecialItems.Kind.WisdomOrb);
        Assert.AreEqual(5, TowerMenu.Entries(none).Count);
        Assert.AreEqual(8, TowerMenu.Entries(all).Count);
        Assert.AreEqual(TowerMenu.Window, TowerMenu.Window, "窓は同じ");
    }

    [Test]
    public void 文字の組み立てが原典の連結と同じ()
    {
        var p = new PlayerState();
        p.Give(SpecialItems.Kind.HeroOrb);
        var text = TowerMenu.Text(p);
        var lines = text.Split('\n');

        Assert.AreEqual("メニューの選択をしてください。", lines[0]);
        Assert.AreEqual("　　勇者のオーブを使う", lines[1]);
        Assert.AreEqual("　　スペシャル・アイテムを使う", lines[2], "持っていないオーブは飛ばす");
        Assert.AreEqual("　　データ・ロード", lines[3]);
        Assert.AreEqual("　　データ・セーブ", lines[4]);
        Assert.AreEqual("　　ゲームに戻る", lines[5]);
        Assert.AreEqual("　　ゲーム終了", lines[6]);
        Assert.AreEqual(7, lines.Length);
    }

    [Test]
    public void 並びは勇者飛翔知恵の順()
    {
        var p = new PlayerState();
        p.Give(SpecialItems.Kind.WisdomOrb);
        p.Give(SpecialItems.Kind.FlightOrb);
        p.Give(SpecialItems.Kind.HeroOrb);
        var e = TowerMenu.Entries(p);
        Assert.AreEqual(TowerMenu.Entry.HeroOrb, e[0]);
        Assert.AreEqual(TowerMenu.Entry.FlightOrb, e[1], "原典のフラグ順 0x23→0x25→0x24");
        Assert.AreEqual(TowerMenu.Entry.WisdomOrb, e[2]);
    }

    [Test]
    public void メニューを開く鍵は二つ()
    {
        CollectionAssert.AreEqual(new[] { 0x1c, 0x34 }, TowerMenu.OpenKeyCodes,
                                  "原典 0xa8e2 / 0xa8eb");
    }

    [Test]
    public void メニューは会話中は開けない()
    {
        var p = new PlayerState
        {
            Hp = 9990, Attack = 999, Defense = 999,
            Floor = 3, Position = new Vector2Int(-1, -2)
        };
        var g = new TowerGame(p);
        g.Step(Vector2Int.down);               // 場面が積まれる
        Assert.IsTrue(g.IsShowingPage);
        Assert.IsFalse(g.OpenMenu(), "会話中は開かない");

        while (g.IsShowingPage) g.NextPage();
        Assert.IsTrue(g.OpenMenu());
        Assert.IsTrue(g.IsMenuOpen);
    }

    [Test]
    public void メニューが開いている間は動けない()
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100,
            Floor = 1, Position = new Vector2Int(0, -5)
        };
        var g = new TowerGame(p);
        g.OpenMenu();
        var at = g.Player.Position;
        Assert.AreEqual(TowerPlayer.Outcome.Blocked, g.TryStep(Vector2Int.up));
        Assert.AreEqual(at, g.Player.Position);

        g.CloseMenu();
        g.TryStep(Vector2Int.up);
        Assert.AreNotEqual(at, g.Player.Position);
    }

    [Test]
    public void メニューの文章が本線から取れる()
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100,
            Floor = 1, Position = new Vector2Int(0, -5)
        };
        p.Give(SpecialItems.Kind.HeroOrb);
        var g = new TowerGame(p);
        StringAssert.StartsWith("メニューの選択をしてください。", g.MenuText);
        StringAssert.Contains("　　勇者のオーブを使う", g.MenuText);
    }

    static TowerGame Open(params SpecialItems.Kind[] orbs)
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100,
            Floor = 1, Position = new Vector2Int(0, -5)
        };
        foreach (var o in orbs) p.Give(o);
        var g = new TowerGame(p);
        g.OpenMenu();
        return g;
    }

    [Test]
    public void 選ぶ場所は上下に動いて端で止まる()
    {
        var g = Open();
        Assert.AreEqual(0, g.MenuIndex);
        g.MenuMove(-1);
        Assert.AreEqual(0, g.MenuIndex, "一番上で止まる");

        for (int i = 0; i < 10; i++) g.MenuMove(1);
        Assert.AreEqual(g.MenuEntries.Count - 1, g.MenuIndex, "一番下で止まる");
    }

    [Test]
    public void ゲームに戻るを選ぶと閉じる()
    {
        var g = Open();
        int i = g.MenuEntries.IndexOf(TowerMenu.Entry.Resume);
        g.MenuMove(i);
        Assert.AreEqual(TowerMenu.Entry.Resume, g.MenuCurrent);
        g.MenuConfirm();
        Assert.IsFalse(g.IsMenuOpen);
    }

    [Test]
    public void 印は選んでいる行に付く()
    {
        var g = Open(SpecialItems.Kind.HeroOrb);
        var lines = MenuWindowView.Compose(g).Split('\n');
        Assert.AreEqual("メニューの選択をしてください。", lines[0], "見出しには付かない");
        StringAssert.StartsWith(MenuWindowView.Cursor, lines[1]);
        StringAssert.Contains("勇者のオーブを使う", lines[1]);

        g.MenuMove(1);
        lines = MenuWindowView.Compose(g).Split('\n');
        StringAssert.StartsWith("　　", lines[1], "元に戻る");
        StringAssert.StartsWith(MenuWindowView.Cursor, lines[2]);
    }

    [Test]
    public void 印を付けても行数は変わらない()
    {
        var g = Open(SpecialItems.Kind.HeroOrb, SpecialItems.Kind.WisdomOrb);
        var before = g.MenuText.Split('\n').Length;
        var after = MenuWindowView.Compose(g).Split('\n').Length;
        Assert.AreEqual(before, after);
        Assert.LessOrEqual(after, TowerMenu.WindowLines, "窓の11行に収まる");
    }
}
