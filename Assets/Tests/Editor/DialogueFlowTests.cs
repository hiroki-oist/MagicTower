using NUnit.Framework;
using UnityEngine;

/// <summary>会話が本線（TowerGame）からページ単位で出てくるかを確かめる。</summary>
public class DialogueFlowTests
{
    static TowerGame Game(int floor, Vector2Int at)
    {
        var p = new PlayerState
        {
            Hp = 9990, Attack = 999, Defense = 999, Floor = floor, Position = at
        };
        return new TowerGame(p);
    }

    [Test]
    public void 何も起きていなければページは無い()
    {
        var g = Game(1, new Vector2Int(0, -5));
        Assert.AreEqual(0, g.PendingPages);
        Assert.IsNull(g.CurrentPage);
    }

    [Test]
    public void 盗賊に話すとページが積まれる()
    {
        var g = Game(2, new Vector2Int(-3, -1));
        g.TalkToThief();
        // セリフ116（1ページ）＋ そのあとの「盗賊が去る」（できごとの頁）
        Assert.AreEqual(2, g.PendingPages, "セリフ1ページと、去っていく動き");
        StringAssert.Contains("先に行かせてもらう", g.CurrentPage);

        Assert.IsFalse(g.NextPage(), "読み終えたら空になる（去る動きはその場で起きる）");
        Assert.IsNull(g.CurrentPage);
    }

    [Test]
    public void 原典の改行位置がページの中でそのまま残る()
    {
        var g = Game(2, new Vector2Int(-3, -1));
        g.TalkToThief();
        var page = g.CurrentPage;
        var body = GameText.Instance.Dialogue(116).Value.Body;
        Assert.AreEqual(body.Replace("\r", ""), page, "折り返さずそのまま");
    }

    [Test]
    public void 一番長いセリフでも一ページに収まる()
    {
        // 原典145本の最長は10行。LinesPerPage が10なので必ず1ページ
        int worst = 0, worstId = -1;
        for (int i = 1; i <= 238; i++)
        {
            var l = GameText.Instance.Dialogue(i);
            if (!l.HasValue) continue;
            int n = DialogueBox.Wrap(l.Value.Body).Count;
            if (n > worst) { worst = n; worstId = i; }
        }
        Assert.AreEqual(11, worst, $"最長はセリフ{worstId}（60/105/180 が11行）");
        Assert.AreEqual(1, DialogueBox.Paginate(
            GameText.Instance.Dialogue(worstId).Value.Body).Count, "分割しない");
    }

    [Test]
    public void 人形のセリフもページになる()
    {
        var g = Game(26, new Vector2Int(0, 1));
        g.TouchDoll();
        Assert.AreEqual(1, g.PendingPages);
        StringAssert.Contains("人形だっ", g.CurrentPage);
    }

    [Test]
    public void 原典にページ送りは無いので分割しない()
    {
        var many = string.Join("\n", System.Linq.Enumerable.Repeat("あいうえお", 25));
        var pages = DialogueBox.Paginate(many);
        Assert.AreEqual(1, pages.Count, "窓は文章に合わせて開く");
        Assert.AreEqual(25, DialogueBox.WindowLines(many));
    }

    [Test]
    public void 窓の行数は原典の指定以下におさまる()
    {
        // 原典 0x94ba の第1引数（docs/original/dialogue_windows.txt より）
        var want = new System.Collections.Generic.Dictionary<int, int>
        {
            {116,8},{120,6},{107,9},{141,8},{142,2},{143,4},{144,4},{125,3},
            {19,4},{29,4},{159,2},{160,6},{121,5},{135,3},{136,2},{137,2},
            {138,2},{139,3},{140,5},{100,5},{101,3},{106,6},{122,6},{156,1},
        };
        // 原典の窓はセリフの行数ちょうどか、それより大きい（小さいことは無い）
        foreach (var kv in want)
        {
            var body = GameText.Instance.Dialogue(kv.Key).Value.Body;
            Assert.LessOrEqual(DialogueBox.WindowLines(body), kv.Value, $"セリフ{kv.Key}");
        }
    }

    [Test]
    public void 姿の見えない相手の声だけ窓が高い位置に開く()
    {
        Assert.AreEqual(120, DialogueBox.TopEdge);
        Assert.AreEqual(50, DialogueBox.HighTopEdge);
    }

    [Test]
    public void 原典のどのセリフも一ページに収まる()
    {
        for (int i = 1; i <= 238; i++)
        {
            var l = GameText.Instance.Dialogue(i);
            if (!l.HasValue) continue;
            Assert.AreEqual(1, DialogueBox.Paginate(l.Value.Body).Count, $"セリフ{i}");
        }
    }
}
