using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 1マス移動の規則が原典どおりかを確かめる。
/// 原典の扉の規則は「鍵の扉だけが鍵1本で開く。門と特殊扉は触れても開かない」。
/// </summary>
public class TowerPlayerTests
{
    FloorState floor;
    PlayerState player;

    [SetUp]
    public void SetUp()
    {
        floor = new FloorState(FloorData.Load(17));
        player = new PlayerState { Position = new Vector2Int(0, 0) };
        // 足元と周りを床にしておく
        for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                floor.Clear(new Vector2Int(x, y));
    }

    static Vector2Int Up    = new Vector2Int(0, 1);
    static Vector2Int Right = new Vector2Int(1, 0);

    [Test]
    public void 床なら進める()
    {
        var r = TowerPlayer.Step(floor, player, Up);
        Assert.AreEqual(TowerPlayer.Outcome.Moved, r.Outcome);
        Assert.AreEqual(new Vector2Int(0, 1), player.Position);
    }

    [Test]
    public void 壁は通れない()
    {
        floor.Put(new Vector2Int(0, 1), "#");
        var r = TowerPlayer.Step(floor, player, Up);
        Assert.AreEqual(TowerPlayer.Outcome.Blocked, r.Outcome);
        Assert.AreEqual(new Vector2Int(0, 0), player.Position, "動いていない");
    }

    [Test]
    public void 門は触れても開かない()
    {
        floor.Put(new Vector2Int(0, 1), "s");
        var r = TowerPlayer.Step(floor, player, Up);
        Assert.AreEqual(TowerPlayer.Outcome.Blocked, r.Outcome);
        Assert.AreEqual("s", floor.At(new Vector2Int(0, 1)), "門は残ったまま");
    }

    [Test]
    public void 鍵が無ければ扉は開かない()
    {
        floor.Put(new Vector2Int(0, 1), "y");
        player.YellowKeys = 0;
        var r = TowerPlayer.Step(floor, player, Up);
        Assert.AreEqual(TowerPlayer.Outcome.Blocked, r.Outcome);
        Assert.AreEqual("y", floor.At(new Vector2Int(0, 1)), "扉は残る");
    }

    [Test]
    public void 鍵があれば扉が開いて1本減る()
    {
        floor.Put(new Vector2Int(0, 1), "y");
        player.YellowKeys = 2;
        var r = TowerPlayer.Step(floor, player, Up);
        Assert.AreEqual(TowerPlayer.Outcome.OpenedDoor, r.Outcome);
        Assert.AreEqual(1, player.YellowKeys, "鍵が1本減る");
        Assert.AreEqual(FloorState.Floor, floor.At(new Vector2Int(0, 1)), "扉が床になる");
        Assert.AreEqual(new Vector2Int(0, 0), player.Position, "開けた手では進まない");
    }

    [Test]
    public void 鍵の色ごとに別々に消費する()
    {
        floor.Put(new Vector2Int(0, 1), "b");
        player.YellowKeys = 5; player.PurpleKeys = 1;
        TowerPlayer.Step(floor, player, Up);
        Assert.AreEqual(5, player.YellowKeys, "黄の鍵は減らない");
        Assert.AreEqual(0, player.PurpleKeys, "紫の鍵が減る");
    }

    [Test]
    public void 敵に当たると戦闘になる()
    {
        floor.Put(new Vector2Int(0, 1), "Enemy_SlimeG");
        var r = TowerPlayer.Step(floor, player, Up);
        Assert.AreEqual(TowerPlayer.Outcome.Encounter, r.Outcome);
        Assert.AreEqual(new Vector2Int(0, 0), player.Position, "戦闘中は動かない");
    }

    [Test]
    public void 物を拾う手では進まない()
    {
        floor.Put(new Vector2Int(1, 0), "Key_Yellow");
        var r = TowerPlayer.Step(floor, player, Right);
        Assert.AreEqual(TowerPlayer.Outcome.TookItem, r.Outcome);
        Assert.AreEqual(new Vector2Int(0, 0), player.Position,
                        "原典は行き先が床(6)のときだけ動く（0x00a6fb）");
    }

    [Test]
    public void 隠し壁は壊せて次の手で入れる()
    {
        floor.Put(new Vector2Int(1, 0), "%");
        var r = TowerPlayer.Step(floor, player, Right);
        Assert.AreEqual(TowerPlayer.Outcome.BrokeWall, r.Outcome);
        Assert.AreEqual(new Vector2Int(0, 0), player.Position, "壊した手では進まない");
        Assert.AreEqual(FloorState.Floor, floor.At(new Vector2Int(1, 0)), "壊したマスは床になる");

        var r2 = TowerPlayer.Step(floor, player, Right);
        Assert.AreEqual(TowerPlayer.Outcome.Moved, r2.Outcome);
        Assert.AreEqual(new Vector2Int(1, 0), player.Position);
    }

    [Test]
    public void 階段を踏むと分かる()
    {
        floor.Put(new Vector2Int(0, 1), ">");
        var r = TowerPlayer.Step(floor, player, Up);
        Assert.AreEqual(TowerPlayer.Outcome.Stairs, r.Outcome);
    }

    [Test]
    public void 盤面の外には出られない()
    {
        player.Position = new Vector2Int(0, 5);
        var r = TowerPlayer.Step(floor, player, Up);
        Assert.AreEqual(TowerPlayer.Outcome.Blocked, r.Outcome);
    }
}
