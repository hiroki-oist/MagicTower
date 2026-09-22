using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 10階「骸骨に囲まれる」場面（原典 MTE.EXE 0x03e0a）と、
/// そのあとの「盗賊との再会」（0x003e0 の10階の枝）を確かめる。
/// </summary>
public class AmbushTests
{
    static TowerGame Game(int floor, Vector2Int at)
    {
        var p = new PlayerState
        {
            Hp = 9990, Attack = 999, Defense = 999, Floor = floor, Position = at
        };
        return new TowerGame(p);
    }

    /// <summary>場面を最後まで送る。1手ずつしか進まないので繰り返し押す。</summary>
    static void Finish(TowerGame g)
    {
        for (int i = 0; i < 500 && g.PendingPages > 0; i++)
        {
            if (g.CurrentKind == TowerGame.PageKind.Effect) g.Pump(false);
            else g.NextPage();
            g.ConsumePlayerSlide();
        }
    }

    static List<string> Script { get { return Cutscenes.Script(Cutscenes.AmbushScript); } }

    [Test]
    public void 台本は金騎士と骸骨八体の道順を持っている()
    {
        var walks = Script.FindAll(s => s.StartsWith(Cutscenes.WalkPrefix));
        Assert.AreEqual(9, walks.Count, "金騎士1体＋骸骨8体");

        // 金騎士は (0,+2) から上へ3歩（原典 0x3f6b〜0x3fbd）
        string prefab; Vector2Int from; List<Vector2Int> path;
        Assert.IsTrue(Cutscenes.TryParseWalk(walks[0], out prefab, out from, out path));
        Assert.AreEqual("Enemy_BossSkeleton", prefab);
        Assert.AreEqual(new Vector2Int(0, 2), from);
        Assert.AreEqual(new Vector2Int(0, 5), path[path.Count - 1]);

        // 骸骨戦士(79)が2体、骸骨(77)が6体の順（原典は 0x4109 が 0x4f、0x4225 が 0x4d）
        var names = new List<string>();
        var goals = new List<Vector2Int>();
        for (int i = 1; i < walks.Count; i++)
        {
            Assert.IsTrue(Cutscenes.TryParseWalk(walks[i], out prefab, out from, out path));
            names.Add(prefab);
            goals.Add(path[path.Count - 1]);
        }
        CollectionAssert.AreEqual(
            new[] { "Enemy_SkeletonWorrior", "Enemy_SkeletonWorrior",
                    "Enemy_Skeleton", "Enemy_Skeleton", "Enemy_Skeleton",
                    "Enemy_Skeleton", "Enemy_Skeleton", "Enemy_Skeleton" },
            names);

        // 行き先は主人公が押し上げられる (0,+1) の周り8マス
        var around = new List<Vector2Int>();
        for (int y = 0; y <= 2; y++)
            for (int x = -1; x <= 1; x++)
                if (!(x == 0 && y == 1)) around.Add(new Vector2Int(x, y));
        CollectionAssert.AreEquivalent(around, goals);
    }

    [Test]
    public void 骸骨は出発のマスに最初から立っている()
    {
        var data = FloorData.Load(10);
        foreach (var line in Script)
        {
            string prefab; Vector2Int from; List<Vector2Int> path;
            if (!Cutscenes.TryParseWalk(line, out prefab, out from, out path)) continue;
            var where = from;
            Assert.IsTrue(data.Objects.Exists(o => o.Pos == where && o.Prefab == prefab),
                          $"{from} に {prefab} が居るはず");
        }
    }

    [Test]
    public void 中央に立つと押し上げられて八体に囲まれる()
    {
        var g = Game(10, new Vector2Int(0, -1));
        g.Step(Vector2Int.up);                       // (0,0) に踏み込んで場面が始まる

        // 原典 0x3ebc は主人公を上へ1マス滑らせる
        Assert.AreEqual(new Vector2Int(0, 1), g.Player.Position);
        Assert.IsNotNull(g.PlayerSlide, "滑らせる1歩を見た目へ渡す");
        Finish(g);

        // 周り8マスが骸骨で埋まる
        Assert.AreEqual("Enemy_SkeletonWorrior", g.Floor.At(new Vector2Int(0, 0)));
        Assert.AreEqual("Enemy_SkeletonWorrior", g.Floor.At(new Vector2Int(0, 2)));
        foreach (var p in new[] { new Vector2Int(-1, 0), new Vector2Int(1, 0),
                                  new Vector2Int(-1, 1), new Vector2Int(1, 1),
                                  new Vector2Int(-1, 2), new Vector2Int(1, 2) })
            Assert.AreEqual("Enemy_Skeleton", g.Floor.At(p), $"{p}");

        // 出発のマスは空になる
        Assert.IsTrue(g.Floor.IsCleared(new Vector2Int(-5, 3)));
        Assert.IsTrue(g.Floor.IsCleared(new Vector2Int(5, 3)));

        // 金騎士は奥へ下がり、前後の門が閉まる（原典 0x3fd7〜0x4016）
        Assert.AreEqual("Enemy_BossSkeleton", g.Floor.At(new Vector2Int(0, 5)));
        Assert.AreEqual("s", g.Floor.At(new Vector2Int(0, 3)));
        Assert.AreEqual("s", g.Floor.At(new Vector2Int(0, -1)));
        // 左右の封印扉は開いて骸骨を通したあと、また閉まる（0x424a〜0x428a）
        Assert.AreEqual("s", g.Floor.At(new Vector2Int(-2, 2)));
        Assert.AreEqual("s", g.Floor.At(new Vector2Int(2, 2)));
    }

    [Test]
    public void 扉は閉まる向きで渡される()
    {
        var g = Game(10, new Vector2Int(0, -1));
        g.Step(Vector2Int.up);
        int closing = 0, opening = 0;
        for (int i = 0; i < 500 && g.PendingPages > 0; i++)
        {
            if (g.CurrentKind == TowerGame.PageKind.Effect) g.Pump(false);
            else g.NextPage();
            g.ConsumePlayerSlide();
            foreach (var d in g.ConsumeDoorOpens())
                if (d.Closing) closing++; else opening++;
        }
        Assert.AreEqual(2, opening, "左右の封印扉が開く");
        Assert.AreEqual(4, closing, "奥・退路・左右の4つが閉まる");
    }

    /// <summary>
    /// ボスの曲は**退路の門が閉まった時点**から（作者の指定）。
    /// 10階は奥の門 (0,+3) が1枚目で、そこで切り替わる。
    /// </summary>
    [Test]
    public void 門が閉まった時点でボスの曲になる()
    {
        var g = Game(10, new Vector2Int(0, -1));
        Assert.IsFalse(g.BossMusic, "場面に入る前はその階の曲");
        g.Step(Vector2Int.up);
        bool closed = false;
        for (int i = 0; i < 500 && g.PendingPages > 0; i++)
        {
            if (g.CurrentKind == TowerGame.PageKind.Effect) g.Pump(false);
            else g.NextPage();
            g.ConsumePlayerSlide();
            foreach (var d in g.ConsumeDoorOpens()) if (d.Closing) closed = true;
            // 1枚目が閉まるまでは切り替わらず、閉まったあとは（扉が開いても）続く
            Assert.AreEqual(closed, g.BossMusic, $"{i}手目");
        }
        Assert.IsTrue(closed, "門が閉まる場面である");
        Assert.IsTrue(g.BossMusic, "場面が終わってもボスの曲のまま");

        // 金の骸骨を倒すとその階の曲へ戻る
        g.Player.Position = new Vector2Int(0, 4);
        g.Floor.Clear(new Vector2Int(0, 3));
        g.Step(Vector2Int.up);
        Assert.AreEqual("Enemy_BossSkeleton", g.LastBattleEnemy);
        Assert.IsFalse(g.BossMusic, "倒したら戻す");
    }

    [Test]
    public void 一度に一体ずつしか歩かない()
    {
        var g = Game(10, new Vector2Int(0, -1));
        g.Step(Vector2Int.up);
        int most = 0;
        for (int i = 0; i < 500 && g.PendingPages > 0; i++)
        {
            if (g.CurrentKind == TowerGame.PageKind.Effect) g.Pump(false);
            else g.NextPage();
            g.ConsumePlayerSlide();
            most = Mathf.Max(most, g.ConsumeWalks().Count);
        }
        Assert.AreEqual(1, most, "原典 0x4096 の輪は1体ずつ寄せてくる");
    }

    [Test]
    public void 盗賊は九階から上がってきて話し終えると消える()
    {
        var steps = Cutscenes.Instance.Steps("盗賊との再会");
        Assert.AreEqual(3, steps.Count);

        string prefab; Vector2Int from; List<Vector2Int> path;
        Assert.IsTrue(Cutscenes.TryParseWalk(steps[0], out prefab, out from, out path));
        Assert.AreEqual("Mikata", prefab);
        // 原典 0x662 は X=0,Y=9（盤面 (-5,-4)）から歩かせる。下り階段 (-5,-5) の隣。
        Assert.AreEqual(new Vector2Int(-5, -4), from);
        Assert.AreEqual(11, path.Count, "原典の向きの表は11歩");
        // 着く先は主人公 (0,-3) の1つ下
        Assert.AreEqual(new Vector2Int(0, -4), path[path.Count - 1]);

        Assert.AreEqual("107", steps[1]);
        Vector2Int at;
        Assert.IsTrue(Cutscenes.TryParseCell(steps[2], Cutscenes.VanishPrefix, out at));
        Assert.AreEqual(new Vector2Int(0, -4), at, "原典 0x71d はこのマスを描き直す");
    }

    [Test]
    public void 盗賊は盤面に載らないので立ち絵のまま残る()
    {
        var g = Game(10, new Vector2Int(0, -2));
        g.Floor.Clear(new Vector2Int(0, 0));                   // 囲まれる場面は済んだことに
        g.Floor.Clear(new Vector2Int(0, -3));                  // 赤い扉は開けてある
        g.Player.EventFlags.Add(91);
        g.Step(Vector2Int.down);                               // (0,-3) で盗賊が来る
        Assert.AreEqual(new Vector2Int(0, -3), g.Player.Position);

        List<TowerGame.NpcWalk> walks = null;
        for (int i = 0; i < 50 && (walks == null || walks.Count == 0); i++)
        {
            g.Pump(false);
            walks = g.ConsumeWalks();
        }
        Assert.AreEqual(1, walks.Count);
        Assert.IsTrue(walks[0].Stay, "盤面に居ないので着いたあとも立たせておく");
    }
}
