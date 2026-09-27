using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 物語を頭から終わりまで、引き金を順に踏んで通す検査。
///
/// 一手ずつ遊ぶのではなく、**原典が物語を進める場所に立って**その場面が起きるかを見る。
/// 階ごとの行き来（通しで遊べるか）は `TowerPlaythrough` の総当たりが受け持つので、
/// ここは「序章から終幕まで筋が繋がっているか」だけを確かめる。
/// </summary>
public class StoryWalkTests
{
    static TowerGame Strong(int floor, Vector2Int at)
    {
        return new TowerGame(new PlayerState
        {
            Hp = PlayerState.MaxHp, Attack = 500, Defense = 500, Gold = 1000,
            YellowKeys = 9, PurpleKeys = 9, RedKeys = 9, SwordRank = 5, ShieldRank = 5,
            Floor = floor, HighestFloor = 50, Position = at,
        });
    }

    /// <summary>その場所へ隣から踏み込む。</summary>
    static void StepInto(TowerGame g, Vector2Int at)
    {
        var from = at + Vector2Int.down;
        if (from.y < FloorData.MinY) from = at + Vector2Int.up;
        g.EnterFloor(g.Player.Floor, from);
        g.Floor.Clear(at);
        g.Step(at - from);
    }

    /// <summary>ページを全部送る。</summary>
    static System.Collections.Generic.List<TowerGame.PageKind> Drain(TowerGame g, int limit = 60)
    {
        var kinds = new System.Collections.Generic.List<TowerGame.PageKind>();
        for (int i = 0; i < limit && g.CurrentKind.HasValue; i++)
        {
            kinds.Add(g.CurrentKind.Value);
            g.NextPage();
        }
        return kinds;
    }

    /// <summary>序章。3階の (-1,-3) でゼノに会い、2階の牢で目を覚ます。</summary>
    [Test]
    public void 序章はゼノに会って二階の牢から始まる()
    {
        var g = new TowerGame(PlayerState.NewGame());
        Assert.AreEqual(1, g.Player.Floor);
        Assert.AreEqual(5, g.Player.SwordRank, "はじめは神剣を持っている");

        g.EnterFloor(GameStart.ZenoEncounterFloor, new Vector2Int(-1, -2));
        g.Floor.Clear(new Vector2Int(-1, -3));
        g.Step(Vector2Int.down);
        Assert.AreEqual("ゼノとの初対面", g.PendingScene);

        var kinds = Drain(g);
        CollectionAssert.Contains(kinds, TowerGame.PageKind.Title, "題名が入る");
        Assert.AreEqual(2, g.Player.Floor, "2階の牢へ");
        Assert.AreEqual(new Vector2Int(-3, -2), g.Player.Position, "盗賊の真下");
        Assert.AreEqual(0, g.Player.SwordRank, "神剣は奪われる");
        Assert.AreEqual(400, g.Player.Hp);
        Assert.IsTrue(g.Floor.IsCleared(new Vector2Int(-4, -1)), "牢が開く");
        Assert.IsTrue(g.Floor.IsCleared(new Vector2Int(-3, -1)));
    }

    /// <summary>10階・20階・25階の関門はそれぞれの場所で始まる。</summary>
    [Test]
    public void 関門の場面はそれぞれの場所で始まる()
    {
        var g10 = Strong(10, new Vector2Int(0, -1));
        g10.Floor.Clear(new Vector2Int(0, -1));
        StepInto(g10, new Vector2Int(0, 0));
        Assert.AreEqual("ボススケルトン登場", g10.PendingScene);

        var g20 = Strong(20, new Vector2Int(0, -3));
        g20.Floor.Clear(new Vector2Int(0, -3));       // 赤い扉を開けて上がってきたところ
        StepInto(g20, new Vector2Int(0, -2));
        Assert.AreEqual("ボス登場", g20.PendingScene);
        Drain(g20);
        Assert.AreEqual("Enemy_ID95", g20.Floor.At(new Vector2Int(0, 0)), "中央にボスが出る");
        Assert.AreEqual("s", g20.Floor.At(new Vector2Int(0, -3)), "退路が門になる");

        var g25 = Strong(25, new Vector2Int(0, -2));
        StepInto(g25, new Vector2Int(0, -3));
        Assert.AreEqual("s", g25.Floor.At(new Vector2Int(0, -4)),
                        "真下の赤い扉が門に変わって閉じ込められる（原典の罠）");
    }

    /// <summary>49階の中央で大魔導師が口上を述べる。</summary>
    [Test]
    public void 四十九階の中央で大魔導師の口上()
    {
        var g = Strong(49, new Vector2Int(0, -1));
        g.Floor.Clear(new Vector2Int(0, -1));
        StepInto(g, new Vector2Int(0, 0));
        Assert.AreEqual("大魔導師の口上", g.PendingScene);
        var line = GameText.Instance.Dialogue(Ending.ArchmageRecord);
        StringAssert.Contains(line.Value.Body.Substring(0, 6), g.CurrentPage);
    }

    /// <summary>24階の時空の門の下で終幕。塔を駆け上がって最後の1枚絵で終わる。</summary>
    [Test]
    public void 二十四階の門の下で終幕まで通る()
    {
        var g = Strong(24, new Vector2Int(0, -2));
        g.Floor.Put(new Vector2Int(0, 0), "s");          // 時空の門
        StepInto(g, new Vector2Int(0, -1));
        Assert.AreEqual("終幕", g.PendingScene);

        var kinds = Drain(g);
        Assert.AreEqual(TowerGame.PageKind.FinalScreen, kinds[kinds.Count - 1], "最後は1枚絵");
        Assert.AreEqual(50, g.Player.Floor, "塔を駆け上がる");
        Assert.AreEqual(Ending.LastStep, g.Player.Position, "最後にゼノへ1歩踏み出す");
        Assert.IsNotNull(g.LastClimb);
        Assert.AreEqual(26, g.LastClimb.Count, "25階から50階まで");
    }

    /// <summary>序章から終幕まで、通しでページが積まれて途切れない。</summary>
    [Test]
    public void 物語の引き金は全部場面に繋がっている()
    {
        foreach (var ev in PositionEvents.Instance.All)
        {
            if (string.IsNullOrEmpty(ev.Scene)) continue;
            Assert.IsNotNull(Cutscenes.Instance.Steps(ev.Scene),
                             $"{ev.Floor}階の「{ev.Scene}」が場面の表に無い");
        }
        // 階段のできごと（29階の抜け穴）も繋がっている
        Assert.AreEqual(1, ArrivalEvents.Instance.Count);
    }
}
