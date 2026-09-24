using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 13階のマグマを雪の結晶で溶かす仕掛け（原典 MTE.EXE 0x1fd5〜0x2094）。
///
/// <code>
///   if (現在階==13 && X==5 && Y==9 && 所持:雪の結晶) {
///       「雪の結晶を使った！」; 所持=0; 持ち物欄を描き直す;
///       for (Y=5; Y<9; Y++) { マス(5,Y)=床; 描く; 音6; 0x1e 待つ }
///   }
/// </code>
/// 盤面に直すと、(0,-4) に立つと (0,0)(0,-1)(0,-2)(0,-3) の4マスが床になり、
/// (0,+1) の神剣（ID55）へ道が通る。**一度きりの印は無く**、
/// 二度目が起きないのは持ち物を使い切るから。
/// </summary>
public class SnowCrystalTests
{
    static TowerGame Game(bool withCrystal)
    {
        var p = new PlayerState
        {
            Hp = 5000, Attack = 500, Defense = 500,
            Floor = 13, HighestFloor = 50, Position = new Vector2Int(0, -5),
        };
        if (withCrystal) p.Give(SpecialItems.Kind.SnowCrystal);
        var g = new TowerGame(p);
        g.EnterFloor(13, new Vector2Int(0, -5));
        return g;
    }

    static readonly Vector2Int[] Melted =
    {
        new Vector2Int(0, 0), new Vector2Int(0, -1),
        new Vector2Int(0, -2), new Vector2Int(0, -3),
    };

    [Test]
    public void 十三階の道はもともとマグマで塞がっている()
    {
        var g = Game(false);
        foreach (var p in Melted) Assert.AreEqual("~", g.Floor.At(p), $"{p}");
    }

    [Test]
    public void 結晶を持って立つと四マス溶ける()
    {
        var g = Game(true);
        g.Player.Position = new Vector2Int(0, -4);
        g.ConsumeSounds();
        Assert.IsTrue(g.CheckPositionEvent(), "発火する");
        foreach (var p in Melted) Assert.AreEqual(FloorState.Floor, g.Floor.At(p), $"{p}");
        Assert.IsFalse(g.Player.Has(SpecialItems.Kind.SnowCrystal), "使い切る");
        int six = 0;
        foreach (var n in g.Sounds) if (n == 6) six++;
        Assert.AreEqual(4, six, "1マスずつ音6（原典 0x2079）");
    }

    [Test]
    public void 結晶が無ければ何も起きない()
    {
        var g = Game(false);
        g.Player.Position = new Vector2Int(0, -4);
        Assert.IsFalse(g.CheckPositionEvent(), "発火しない");
        foreach (var p in Melted) Assert.AreEqual("~", g.Floor.At(p), $"{p}");
    }

    [Test]
    public void 一度きりの印は使わない()
    {
        var ev = PositionEvents.Instance.At(13, new Vector2Int(0, -4), null,
                                            p => false, null,
                                            k => k == SpecialItems.Kind.SnowCrystal);
        Assert.IsTrue(ev.HasValue, "持ち物があれば当たる");
        Assert.AreEqual(-1, ev.Value.FlagId, "フラグ番号は持たない");
        Assert.AreEqual(SpecialItems.Kind.SnowCrystal, ev.Value.NeedsItem);
    }
}

/// <summary>
/// 逃げる相手（原典 0x217b の43階と 0x209a の47階）。どちらも一度きりの印を持たず、
/// 「そのマスにまだ居るか」を見て発火する。
/// </summary>
public class FleeingEnemyTests
{
    static TowerGame Game(int floor, Vector2Int at)
    {
        var p = new PlayerState
        {
            Hp = 5000, Attack = 500, Defense = 500,
            YellowKeys = 9, PurpleKeys = 9, RedKeys = 9,
            Floor = floor, HighestFloor = 50, Position = at,
        };
        var g = new TowerGame(p);
        g.EnterFloor(floor, at);
        return g;
    }

    [Test]
    public void 四十三階は魔導師が奥へ移って退路に壁が出る()
    {
        var g = Game(43, new Vector2Int(2, 5));
        Assert.AreEqual("Enemy_ID93", g.Floor.At(new Vector2Int(3, 5)), "はじめは隣に居る");
        g.ConsumeSounds();
        Assert.IsTrue(g.CheckPositionEvent(), "発火する");

        Assert.AreEqual(FloorState.Floor, g.Floor.At(new Vector2Int(3, 5)), "居なくなる");
        Assert.AreEqual("Enemy_ID93", g.Floor.At(new Vector2Int(5, 5)), "奥へ移る");
        Assert.AreEqual("#", g.Floor.At(new Vector2Int(1, 5)), "退路に壁");
        Assert.AreEqual("#", g.Floor.At(new Vector2Int(4, 5)), "行く手にも壁");
        var sounds = new System.Collections.Generic.List<int>(g.Sounds);
        CollectionAssert.AreEqual(new[] { 6, 11 }, sounds, "音6のあと音11（原典 0x2203 / 0x226b）");

        Assert.IsFalse(g.CheckPositionEvent(), "二度目は起きない");
    }

    [Test]
    public void 四十七階は上の相手が一マス上がる()
    {
        var g = Game(47, new Vector2Int(-5, -4));
        Assert.AreEqual("Enemy_ID151", g.Floor.At(new Vector2Int(-5, -3)), "はじめは真上");
        g.ConsumeSounds();
        Assert.IsTrue(g.CheckPositionEvent(), "発火する");
        Settle(g);                                   // ★下がるのはひと呼吸あと
        Assert.AreEqual(FloorState.Floor, g.Floor.At(new Vector2Int(-5, -3)));
        Assert.AreEqual("Enemy_ID151", g.Floor.At(new Vector2Int(-5, -2)), "もう1マス上へ");
        Assert.Contains(6, new System.Collections.Generic.List<int>(g.Sounds), "音6");

        Assert.IsFalse(g.CheckPositionEvent(), "同じ所ではもう起きない");
    }

    /// <summary>
    /// 相手が下がるのは**ひと呼吸あと**（作者の指定）。積んだ段を最後まで流す。
    /// </summary>
    static void Settle(TowerGame g)
    {
        for (int i = 0; i < 30 && g.PendingPages > 0; i++)
        {
            if (g.CurrentKind == TowerGame.PageKind.Effect) g.Pump(false);
            else g.NextPage();
        }
        g.ConsumePauseFrames();
    }

    [Test]
    public void 四十七階は上の列でしか起きない()
    {
        var g = Game(47, new Vector2Int(-4, -4));
        g.Floor.Put(new Vector2Int(-4, -3), "Enemy_ID151");
        Assert.IsFalse(g.CheckPositionEvent(), "左端の列(-5)でなければ起きない");
    }

    [Test]
    public void 四十七階は追いつめると逃げ場が無くなる()
    {
        var g = Game(47, new Vector2Int(-5, -4));
        // 下から順に追っていくと、y=+2 で発火しなくなる（原典の条件は Y>2 ＝ y<=+2）
        for (int y = -4; y <= 2; y++)
        {
            g.Player.Position = new Vector2Int(-5, y);
            Assert.IsTrue(g.CheckPositionEvent(), $"y={y} では逃げる");
            Settle(g);
        }
        Assert.AreEqual("Enemy_ID151", g.Floor.At(new Vector2Int(-5, 4)), "最後は (-5,+4)");
        g.Player.Position = new Vector2Int(-5, 3);
        Assert.IsFalse(g.CheckPositionEvent(), "y=+3 では逃げない（原典 Y>2 の外）");
    }
}
