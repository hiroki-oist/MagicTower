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
            // 原典 0x6ae0 は ゼノ(ID91) を出す → セリフ110 → 魔導師(ID93)4体を出す
            // → セリフ111 → 閃光 → 題名 の順。魔導師は主人公 (-1,-3) の上下左右。
            new[] { "地図 (-1,-1)=ID91", "110",
                    "地図 (-1,-2)=ID93", "地図 (-1,-4)=ID93",
                    "地図 (-2,-3)=ID93", "地図 (+0,-3)=ID93",
                    "111", "閃光", "題名", "序章の絵", "奪われる",
                    "112", "113", "114", "115", "牢が開く" },
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

        // 110 111 閃光 題名 序章の絵×3 奪われる 112 113 114 115 牢が開く
        // → 会話5 ＋ 閃光1 ＋ 題名1 ＋ 序章の絵3 ＝ 13（奪われる・牢が開くはページを作らない）
        Assert.AreEqual(13, g.PendingPages);

        StringAssert.Contains("１００人目の侵入者", g.CurrentPage);
        Assert.AreEqual(110, g.Current.Value.Record);
        Assert.AreEqual(TowerGame.PageKind.Dialogue, g.Current.Value.Kind);

        g.NextPage();
        StringAssert.Contains("うわぁーっ", g.CurrentPage);

        g.NextPage();
        Assert.AreEqual(TowerGame.PageKind.Title, g.Current.Value.Kind, "ここで題名");
        StringAssert.Contains("Ｔｏｗｅｒ", g.CurrentPage);
        StringAssert.Contains("１９９６", g.CurrentPage);

        // 題名のあとは序章の1枚絵が3枚（行が1つずつ現れる）
        for (int shown = 1; shown <= Cutscenes.PrologueLines; shown++)
        {
            g.NextPage();
            Assert.AreEqual(TowerGame.PageKind.Prologue, g.Current.Value.Kind, $"{shown}枚目");
            Assert.AreEqual(shown, g.Current.Value.Record, "何行目まで出すか");
        }

        g.NextPage();          // 序章の絵 → 奪われるが自動で起きて 112 が出る
        Assert.AreEqual(112, g.Current.Value.Record, "序章と奪われるのあとは112");
        Assert.AreEqual(2, g.Player.Floor, "奪われて2階にいる");
    }

    [Test]
    public void 三階に立つと初対面の場面が流れ出す()
    {
        var g = Game(3, new Vector2Int(-1, -2));
        Assert.AreEqual(0, g.PendingPages);

        g.Step(Vector2Int.down);
        Assert.AreEqual("ゼノとの初対面", g.PendingScene);
        Assert.AreEqual(13, g.PendingPages, "場面がそのまま積まれる（序章の絵3枚を含む）");
        StringAssert.Contains("ようこそ", g.CurrentPage);

        // 原典 0x6ae0 は盤面にゼノ1体と魔導師4体を書き込む。
        // ここが抜けていて「ゼノも魔導師も現れない」と報告された。
        Assert.AreEqual("Boss_Zeno", g.Floor.At(new Vector2Int(-1, -1)), "ゼノ");
        foreach (var at in new[] { new Vector2Int(-1, -2), new Vector2Int(-1, -4),
                                   new Vector2Int(-2, -3), new Vector2Int(0, -3) })
            Assert.AreEqual("Enemy_ID93", g.Floor.At(at), $"魔導師 {at}");
        // 魔導師に囲まれても、場面の最中は魔法攻撃を受けない（原典は場面が画面を持つ）
        Assert.AreEqual(9990, g.Player.Hp, "場面の最中に魔法攻撃を受けない");
    }

    [Test]
    public void 金騎士の突撃は六つのセリフが順に出る()
    {
        var g = Game(40, new Vector2Int(0, -1));
        Assert.IsTrue(g.PlayScene("金騎士の突撃"));
        Assert.AreEqual(7, g.PendingPages, "セリフ6つ＋斬りかかる動き");

        var want = new[] { 135, 136, 137, 138, 139, 140 };
        foreach (var id in want)
        {
            Assert.AreEqual(id, g.Current.Value.Record);
            g.NextPage();
        }
        Assert.IsNull(g.CurrentPage, "セリフのあとは動きが残るだけ");
        Assert.IsNotNull(g.LastKnightCharge, "斬りかかる道順が積まれる");
        Assert.AreEqual(KnightCharge.Instance.StepCount, g.LastKnightCharge.Count);
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

/// <summary>
/// 場面を最後まで送り切れるか。段を足したときに途中で止まらないかを見る。
/// </summary>
public class ScenePlaythroughTests
{
    static TowerGame Game(int floor, Vector2Int at)
    {
        return new TowerGame(new PlayerState
        {
            Hp = 9990, Attack = 999, Defense = 999, Floor = floor, Position = at
        });
    }

    /// <summary>その場面を最後まで送って、出たページの種類を並べて返す。</summary>
    static System.Collections.Generic.List<TowerGame.PageKind> Drain(TowerGame g, string scene)
    {
        Assert.IsTrue(g.PlayScene(scene), $"{scene} が流れない");
        var kinds = new System.Collections.Generic.List<TowerGame.PageKind>();
        int guard = 0;
        while (g.Current.HasValue && guard++ < 200)
        {
            kinds.Add(g.Current.Value.Kind);
            g.NextPage();
        }
        Assert.Less(guard, 200, "終わらない");
        return kinds;
    }

    [Test]
    public void 終幕を最後まで送れる()
    {
        var g = Game(50, new Vector2Int(0, -1));
        var kinds = Drain(g, "終幕");

        // 駆け上がる → セリフ161〜170 → 塔の絵 → 終わりの言葉×4 → 最後の画面
        Assert.AreEqual(10, kinds.FindAll(k => k == TowerGame.PageKind.Dialogue).Count,
                        "ゼノとの最後のやりとりは10ページ");
        Assert.AreEqual(1, kinds.FindAll(k => k == TowerGame.PageKind.EndingTower).Count,
                        "塔の絵は1枚");
        Assert.AreEqual(Cutscenes.EndingTextParagraphs,
                        kinds.FindAll(k => k == TowerGame.PageKind.EndingText).Count,
                        "終わりの言葉は段落の数だけ");
        Assert.AreEqual(TowerGame.PageKind.FinalScreen, kinds[kinds.Count - 1],
                        "いちばん最後は最後の画面");
    }

    [Test]
    public void 序章を最後まで送れる()
    {
        var g = Game(3, new Vector2Int(-1, -3));
        var kinds = Drain(g, "ゼノとの初対面");

        Assert.AreEqual(1, kinds.FindAll(k => k == TowerGame.PageKind.Title).Count,
                        "題名は1枚");
        Assert.AreEqual(Cutscenes.PrologueLines,
                        kinds.FindAll(k => k == TowerGame.PageKind.Prologue).Count,
                        "序章の絵は行の数だけ");
        Assert.AreEqual(2, g.Player.Floor, "送り終わると2階にいる");
    }

    [Test]
    public void どの場面も最後まで送れる()
    {
        foreach (var name in Cutscenes.Instance.Names)
        {
            var g = Game(50, new Vector2Int(0, -1));
            var kinds = Drain(g, name);
            Assert.Greater(kinds.Count, 0, $"{name} でページが出ない");
        }
    }
}
