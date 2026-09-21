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
