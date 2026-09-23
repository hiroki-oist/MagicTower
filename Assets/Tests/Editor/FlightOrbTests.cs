using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 飛翔のオーブ。原典 MTE.EXE 0x8fda の作りをそのまま写した。
///   ・上下左右のどれかが階段(11/12)でなければ使えない
///   ・1階から到達最高階までを上下して選ぶ。**44階（幻の階）は飛ばす**
///   ・決めると階段の隣に着く（上へ行くなら下から上がったときの位置、下なら逆）
/// </summary>
public class FlightOrbTests
{
    static TowerGame Game(int floor, int highest)
    {
        var p = new PlayerState
        {
            Hp = 5000, Attack = 500, Defense = 500,
            Floor = floor, HighestFloor = highest, Position = new Vector2Int(0, 0),
        };
        p.Give(SpecialItems.Kind.FlightOrb);
        return new TowerGame(p);
    }

    /// <summary>
    /// ★作者の指定でメニューからは外し、持ち物の帯を触ると直に開く。
    /// **階段の隣でなければ断るだけで、階選びの画面は開かない。**
    /// </summary>
    static bool OpenFlight(TowerGame g) { return g.TapItem(SpecialItems.Kind.FlightOrb); }

    /// <summary>階段の隣に立たせる。</summary>
    static void StandByStairs(TowerGame g)
    {
        g.Floor.Put(g.Player.Position + Vector2Int.up, ">");
    }

    [Test]
    public void 階段の隣でなければ断られる()
    {
        var g = Game(5, 10);
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                g.Floor.Clear(new Vector2Int(x, y));
        Assert.IsFalse(OpenFlight(g), "何も開かない");
        Assert.AreEqual(TowerGame.MenuStage.Root, g.Stage, "階選びに入らない");
        Assert.IsFalse(g.IsMenuOpen, "メニューも出さない");
        StringAssert.Contains("階段の隣", g.Log[g.Log.Count - 1]);
    }

    [Test]
    public void 階段の隣なら階選びに入る()
    {
        var g = Game(5, 10);
        StandByStairs(g);
        OpenFlight(g);
        Assert.AreEqual(TowerGame.MenuStage.FlightFloors, g.Stage);
        Assert.AreEqual(5, g.FlightFloor, "今いる階から選び始める");
    }

    [Test]
    public void 選べるのは1階から到達最高階まで()
    {
        var g = Game(5, 7);
        StandByStairs(g);
        OpenFlight(g);
        for (int i = 0; i < 10; i++) g.FlightMove(1);
        Assert.AreEqual(7, g.FlightFloor, "到達最高階で止まる");
        for (int i = 0; i < 20; i++) g.FlightMove(-1);
        Assert.AreEqual(1, g.FlightFloor, "1階で止まる");
    }

    [Test]
    public void 幻の階は飛ばす()
    {
        var g = Game(43, 46);
        StandByStairs(g);
        OpenFlight(g);
        g.FlightMove(1);
        Assert.AreEqual(45, g.FlightFloor, "44階（幻の階）は飛ばす");
        g.FlightMove(-1);
        Assert.AreEqual(43, g.FlightFloor, "戻るときも飛ばす");
    }

    [Test]
    public void 決めるとその階の階段の隣に着く()
    {
        var g = Game(5, 12);
        StandByStairs(g);
        OpenFlight(g);
        for (int i = 0; i < 3; i++) g.FlightMove(1);
        Assert.AreEqual(8, g.FlightFloor);
        Assert.IsTrue(g.ConfirmFlight());
        Assert.AreEqual(8, g.Player.Floor);
        var want = FloorConnections.Instance.Get(8).ArriveFromBelow;
        Assert.AreEqual(want.Value, g.Player.Position, "下から上がってきたときの位置");
        Assert.IsFalse(g.IsChoosingFloor, "階選びは閉じる");
    }

    [Test]
    public void 下の階へ飛ぶと上から降りたときの位置に着く()
    {
        var g = Game(8, 12);
        StandByStairs(g);
        OpenFlight(g);
        for (int i = 0; i < 3; i++) g.FlightMove(-1);
        Assert.AreEqual(5, g.FlightFloor);
        Assert.IsTrue(g.ConfirmFlight());
        var want = FloorConnections.Instance.Get(5).ArriveFromAbove;
        Assert.AreEqual(want.Value, g.Player.Position);
    }

    [Test]
    public void 同じ階を選ぶと何も起きない()
    {
        var g = Game(5, 12);
        StandByStairs(g);
        OpenFlight(g);
        Assert.IsFalse(g.ConfirmFlight());
        Assert.AreEqual(5, g.Player.Floor);
    }
}
