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
