using NUnit.Framework;
using UnityEngine;

/// <summary>盗賊のできごとが原典（MTE.EXE 0x03e0）どおりかを確かめる。</summary>
public class ThiefEventsTests
{
    [Test]
    public void 表は十件で原典の分岐の並びどおり()
    {
        var t = ThiefEvents.Instance;
        Assert.AreEqual(10, t.Count);

        var seq = new System.Collections.Generic.List<string>();
        foreach (var e in t.All) seq.Add($"{e.Floor}:{e.Record}");
        CollectionAssert.AreEqual(
            new[] { "2:116", "15:120", "10:107", "2:141", "2:142",
                    "35:143", "35:144", "29:126", "29:124", "29:125" }, seq);
    }

    [Test]
    public void 盤面に最初からいる盗賊は二階と十五階と二十九階だけ()
    {
        var placed = new System.Collections.Generic.List<int>();
        for (int f = 1; f <= 50; f++)
        {
            var a = Resources.Load<TextAsset>($"Floors/Floor_{f}");
            if (a != null && a.text.Contains("Mikata")) placed.Add(f);
        }
        CollectionAssert.AreEqual(new[] { 2, 15, 29 }, placed,
            "10階と35階の盗賊は物語の途中で現れる");
    }

    static TowerGame Game(int floor, Vector2Int at)
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100,
            Floor = floor, Position = at
        };
        return new TowerGame(p);
    }

    [Test]
    public void 二階の一度目は抜け穴を開ける()
    {
        var g = Game(2, new Vector2Int(-3, -1));
        var text = g.TalkToThief();
        StringAssert.Contains("先に行かせてもらう", text, "セリフ116");
        Assert.IsTrue(g.FloorAt(2).IsCleared(new Vector2Int(-5, -2)), "2階(-5,-2)が床になる");
    }

    [Test]
    public void 二階の二度目は三十五階の抜け穴を開ける()
    {
        var g = Game(2, new Vector2Int(5, -5));
        g.Player.EventFlags.Add(94);                     // 序章を済ませた印

        var text = g.TalkToThief();
        StringAssert.Contains("ドジッて", text, "セリフ141");
        Assert.IsTrue(g.Player.EventFlags.Contains(88));
        // 地図の書き換えは**セリフを読み終えてから**（原典 0x003e0 も同じ順）
        while (g.IsShowingPage) g.NextPage();
        Assert.IsTrue(g.FloorAt(35).IsCleared(new Vector2Int(-2, -3)),
                      "35階(-2,-3)＝動画で通路になっていた唯一のマス");

        StringAssert.Contains("後でちゃんと行く", g.TalkToThief(), "2度目以降はセリフ142");
    }

    [Test]
    public void 三十五階で会うと二階の盗賊がいなくなる()
    {
        var g = Game(35, new Vector2Int(-2, -3));
        StringAssert.Contains("抜け穴を", g.TalkToThief());
        Assert.IsTrue(g.Player.EventFlags.Contains(118));
        Assert.IsTrue(g.FloorAt(2).IsCleared(new Vector2Int(5, -5)), "牢から消える");

        StringAssert.Contains("金騎士", g.TalkToThief(), "2度目はセリフ144");
    }

    [Test]
    public void 十五階の盗賊は大ダコを避ける抜け穴を掘って消える()
    {
        var g = Game(15, new Vector2Int(3, 5));
        StringAssert.Contains("大\nダコ", g.TalkToThief(), "セリフ120（原文の改行位置のまま）");
        while (g.IsShowingPage) g.NextPage();            // 書き換えはセリフのあと
        Assert.IsTrue(g.FloorAt(15).IsCleared(new Vector2Int(2, 5)));
        Assert.IsTrue(g.FloorAt(15).IsCleared(new Vector2Int(3, 5)), "盗賊自身も消える");
    }

    [Test]
    public void 二十九階は立っている場所でセリフが変わる()
    {
        var below = Game(29, new Vector2Int(0, 3));      // 原典 Y>=2
        StringAssert.Contains("まだ抜け穴、完成してなかっ", below.TalkToThief());
        Assert.IsFalse(below.Player.EventFlags.Contains(107), "フラグは立たない");

        var above = Game(29, new Vector2Int(0, 4));      // 原典 Y<2
        StringAssert.Contains("やっと抜け穴が完成した", above.TalkToThief());
        Assert.IsTrue(above.Player.EventFlags.Contains(107));
        StringAssert.Contains("プリンセス", above.TalkToThief(), "2度目はセリフ125");
    }

    [Test]
    public void 十階の盗賊はブロックのボスを倒した事をねぎらう()
    {
        var g = Game(10, new Vector2Int(0, 0));
        StringAssert.Contains("ここのブロックのボスを倒すと", g.TalkToThief());
    }

    /// <summary>
    /// 32階で金騎士を追い払うと、盗賊が29階から**2階の奥の牢**へ移る（原典 0x1801）。
    /// そこで話しかけると 35階(-2,-3) に抜け穴が空いて、**ドラゴンを避けて通れる**
    /// （未使用レコード220「３５階のドラゴンの手前まで来たら、一度、２階まで戻って
    /// みてくれ。俺が道をつくってあげよう。」）。
    /// </summary>
    [Test]
    public void 三十二階の場面で盗賊が二階の奥の牢へ移る()
    {
        var g = Game(32, new Vector2Int(0, -5));
        g.Player.Hp = 99999; g.Player.Attack = 999; g.Player.Defense = 999;
        var back = new Vector2Int(5, -5);
        Assert.IsTrue(g.FloorAt(2).IsCleared(back), "はじめは誰も居ない");

        g.Player.Position = new Vector2Int(0, -5);   // 下り階段のマス
        g.Step(Vector2Int.up);                       // (0,-4) へ踏み込んで場面が始まる
        Assert.IsTrue(g.Player.EventFlags.Contains(111));
        Assert.AreEqual("Mikata", g.FloorAt(2).At(back), "2階の奥の牢に移る");
        Assert.IsTrue(g.FloorAt(29).IsCleared(new Vector2Int(1, 4)), "29階からは消える");
    }

    [Test]
    public void 二階の奥の盗賊が三十五階の抜け穴を掘る()
    {
        var g = Game(2, new Vector2Int(5, -5));
        g.Player.EventFlags.Add(94);                 // 10階で再会済み
        g.FloorAt(2).Put(new Vector2Int(5, -5), "Mikata");

        var hole = new Vector2Int(-2, -3);
        Assert.AreEqual("#", g.FloorAt(35).At(hole), "掘る前は壁");
        StringAssert.Contains("奥の牢に閉じ込められ", g.TalkToThief());
        Assert.IsTrue(g.Player.EventFlags.Contains(88));
        // 地図の書き換えは**セリフを読み終えてから**効く（原典 0x4c9〜0x520）
        for (int i = 0; i < 20 && g.PendingPages > 0; i++)
        {
            if (g.CurrentKind == TowerGame.PageKind.Effect) g.Pump(false);
            else g.NextPage();
        }
        Assert.IsTrue(g.FloorAt(35).IsCleared(hole), "35階に抜け穴が空く");
        // ★**本人も35階へ移る**（原典 0x7aa が ID21 を書き、0x7bb が穴を開ける）。
        //   これが抜けていたので、35階で会えず＝2階の盗賊が消えないままだった
        //   （作者の報告「35階で穴あけてくれたのにまだ2階にもいます」）。
        Assert.AreEqual("Mikata", g.FloorAt(35).At(new Vector2Int(-1, -4)),
                        "35階に本人が現れる");
    }

    /// <summary>
    /// 35階で会うと、2階の奥の牢からは居なくなる（原典の表の 2:(+5,-5)=床）。
    /// </summary>
    [Test]
    public void 三十五階で会うと二階からは消える()
    {
        var g = Game(35, new Vector2Int(-1, -5));
        g.Player.EventFlags.Add(94);
        g.Player.EventFlags.Add(88);                 // 2階で2度目の話を済ませた
        var back = new Vector2Int(5, -5);
        g.FloorAt(2).Put(back, "Mikata");
        g.FloorAt(35).Put(new Vector2Int(-1, -4), "Mikata");

        Assert.AreEqual("Mikata", g.FloorAt(2).At(back), "まだ2階にいる");
        g.TalkToThief();
        for (int i = 0; i < 20 && g.PendingPages > 0; i++)
        {
            if (g.CurrentKind == TowerGame.PageKind.Effect) g.Pump(false);
            else g.NextPage();
        }
        Assert.IsTrue(g.Player.EventFlags.Contains(118), "35階で会った印");
        Assert.IsTrue(g.FloorAt(2).IsCleared(back), "2階からは消える");
    }

    /// <summary>
    /// 抜け穴が空くと、ドラゴン(3x3)の部屋を通らずに上の階段まで行ける。
    /// 左の隠し壁の縦穴2本（x=-3 と x=-5）が y=-5 と y=+4 で繋がっていて、
    /// (-2,-3) の1マスだけが INITIAL.DAT では壁＝盗賊が掘るところ。
    /// </summary>
    [Test]
    public void 抜け穴を掘るとドラゴンを通らずに登れる()
    {
        var data = FloorData.Load(35);
        var st = new FloorState(data);
        st.Clear(new Vector2Int(-2, -3));            // 盗賊が掘ったあと

        // 幅優先で、ドラゴンのマスを**通らずに**下り階段から上り階段へ行けるか
        var start = new Vector2Int(0, -5);
        var goal = new Vector2Int(5, 5);
        var seen = new System.Collections.Generic.HashSet<Vector2Int> { start };
        var q = new System.Collections.Generic.Queue<Vector2Int>();
        q.Enqueue(start);
        var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (q.Count > 0)
        {
            var at = q.Dequeue();
            foreach (var d in dirs)
            {
                var to = at + d;
                if (to.x < FloorData.MinX || to.x > FloorData.MaxX
                    || to.y < FloorData.MinY || to.y > FloorData.MaxY) continue;
                if (!seen.Add(to)) continue;
                var cell = st.At(to);
                // 床・隠し壁（壊せる）・階段だけ通る。ドラゴンと本物の壁と封印扉は通らない
                if (cell != FloorState.Floor && cell != "%" && cell != ">" && cell != "<")
                    continue;
                q.Enqueue(to);
            }
        }
        Assert.IsTrue(seen.Contains(goal), "ドラゴンを避けて上り階段まで行ける");

        // 掘る前は行けない
        var before = new FloorState(FloorData.Load(35));
        Assert.AreEqual("#", before.At(new Vector2Int(-2, -3)), "掘る前は壁");
    }

    [Test]
    public void 階の盤面は入り直しても元に戻らない()
    {
        var g = Game(2, new Vector2Int(-3, -1));
        g.TalkToThief();
        g.EnterFloor(1, new Vector2Int(0, -5));
        g.EnterFloor(2, new Vector2Int(-3, -1));
        Assert.IsTrue(g.Floor.IsCleared(new Vector2Int(-5, -2)),
                      "原典は50階ぶんを1本の配列で持っている");
    }
}
