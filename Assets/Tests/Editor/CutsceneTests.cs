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
            // 点滅の回数と待ちは原典のまま（ゼノ50回→30待ち、魔導師15回→10待ち）
            new[] { "地図 (-1,-1)=ID91 点滅50,30", "110",
                    "地図 (-1,-2)=ID93 点滅15,10", "地図 (-1,-4)=ID93 点滅15,10",
                    "地図 (-2,-3)=ID93 点滅15,10", "地図 (+0,-3)=ID93 点滅15,10",
                    "111", "閃光", "題名", "序章の絵",
                    // 終わりに同じ5マスを床へ戻す（原典 0x6ebd〜0x6f0f）。
                    // これが抜けていて「3階に戻るとまだ魔導師がいる」と報告された。
                    "地図 (-1,-1)=床", "地図 (-1,-2)=床", "地図 (-1,-4)=床",
                    "地図 (-2,-3)=床", "地図 (+0,-3)=床",
                    "奪われる",
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

        // 原典 0x6ae0 は盤面にゼノ1体と魔導師4体を**順に**書き込む。
        // ここが抜けていて「ゼノも魔導師も現れない」と報告された。
        var mages = new[] { new Vector2Int(-1, -2), new Vector2Int(-1, -4),
                            new Vector2Int(-2, -3), new Vector2Int(0, -3) };
        Assert.AreEqual("Boss_Zeno", g.Floor.At(new Vector2Int(-1, -1)),
                        "セリフ110の前にゼノが出る");
        foreach (var at in mages)
            Assert.AreNotEqual("Enemy_ID93", g.Floor.At(at),
                               $"魔導師はまだ出ていない {at}");

        g.NextPage();                                   // 110 を読み終える
        foreach (var at in mages)
            Assert.AreEqual("Enemy_ID93", g.Floor.At(at), $"魔導師 {at}");
        // 魔導師に囲まれても、場面の最中は魔法攻撃を受けない（原典は場面が画面を持つ）
        Assert.AreEqual(9990, g.Player.Hp, "場面の最中に魔法攻撃を受けない");

        // 原典 0x6ebd〜0x6f0f は奪われる直前に同じ5マスを床へ戻す。
        // これが抜けていて「3階に戻るとまだ魔導師がいて先に進めない」と報告された。
        var f3 = g.FloorAt(3);
        for (int i = 0; i < 200 && g.PendingPages > 0; i++) g.NextPage();
        Assert.AreEqual(2, g.Player.Floor, "2階の牢に移っている");
        Assert.AreEqual(".", f3.At(new Vector2Int(-1, -1)), "ゼノは片付いている");
        foreach (var at in mages)
            Assert.AreEqual(".", f3.At(at), $"魔導師が片付いている {at}");
    }

    [Test]
    public void 金騎士の突撃は部下が一体ずつ突撃してくる()
    {
        var g = Game(40, new Vector2Int(0, -1));
        Assert.IsTrue(g.PlayScene("金騎士の突撃"));

        // 原典 0x325e は**部下が1体ずつ歩いてきて、そのたびに戦う**。
        // 12体＋金騎士の13戦で、3体ごとにセリフが入る。
        var want = new[] { 135, 136, 137, 138, 139, 140 };
        int seen = 0, walks = 0, battles = 0;
        int hpBefore = g.Player.Hp;
        for (int i = 0; i < 400 && g.PendingPages > 0; i++)
        {
            if (g.CurrentKind == TowerGame.PageKind.Effect)
            {
                var before = g.LastBattle;
                g.Pump(false);
                walks += g.ConsumeWalks().Count;
                if (g.LastBattle != null && g.LastBattle != before) battles++;
            }
            else
            {
                Assert.AreEqual(want[seen++], g.Current.Value.Record);
                g.NextPage();
            }
        }
        Assert.AreEqual(6, seen, "セリフは6つ");
        Assert.AreEqual(14, walks, "12体＋金騎士の突撃13本＋引き上げ1本");
        Assert.AreEqual(13, battles, "引き上げ以外は戦闘が入る");
        Assert.AreEqual(FloorState.Floor, g.Floor.At(new Vector2Int(0, 0)), "中央は空く");
        Assert.AreEqual(FloorState.Floor, g.Floor.At(new Vector2Int(0, -2)), "退路の門が開く");
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

        // 駆け上がる → セリフ161〜170 → 塔の絵 → 終わりの言葉 → 最後の画面
        // （終わりの言葉は原典なら段落4つ、Java版（作者の指定）なら1枚絵）
        Assert.AreEqual(10, kinds.FindAll(k => k == TowerGame.PageKind.Dialogue).Count,
                        "ゼノとの最後のやりとりは10ページ");
        Assert.AreEqual(1, kinds.FindAll(k => k == TowerGame.PageKind.EndingTower).Count,
                        "塔の絵は1枚");
        Assert.AreEqual(Ending.WordsPages,
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

    /// <summary>
    /// ★**序章と終幕の間は、触った合図の音を鳴らさない**（作者の指定）。
    /// 原典に無い音なので、一枚絵と終わりの言葉の間合いを壊さないため。
    /// **その中の会話も含める**（送るたびに鳴っては見せ場にならない）。
    /// 見分けは <see cref="TowerGame.InStoryScene"/> 1か所にまとめてある。
    /// </summary>
    [Test]
    public void 序章と終幕は会話も含めて物語の場面とみなす()
    {
        var g = Game(3, new Vector2Int(-1, -3));
        Assert.IsFalse(g.InStoryScene, "何も出ていなければ物語の場面ではない");

        Assert.IsTrue(g.PlayScene(Cutscenes.PrologueScene));
        int guard = 0, seen = 0;
        while (g.Current.HasValue && guard++ < 200)
        {
            Assert.IsTrue(g.InStoryScene, $"序章の{guard}枚目（{g.Current.Value.Kind}）");
            seen++;
            g.NextPage();
        }
        Assert.GreaterOrEqual(seen, 10, "序章は会話も一枚絵も通る");
        Assert.IsFalse(g.InStoryScene, "送り終われば元どおり");

        var e = Game(50, new Vector2Int(0, -1));
        Assert.IsTrue(e.PlayScene(Cutscenes.EndingScene));
        guard = 0;
        while (e.Current.HasValue && guard++ < 200)
        {
            Assert.IsTrue(e.InStoryScene, $"終幕の{guard}枚目（{e.Current.Value.Kind}）");
            e.NextPage();
        }

        // 道中の場面は物語の場面ではない（音は鳴る）
        var m = Game(20, new Vector2Int(0, -2));
        Assert.IsTrue(m.PlayScene("大魔導師の口上"));
        Assert.IsFalse(m.InStoryScene, "道中の場面では鳴る");
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
