using NUnit.Framework;
using UnityEngine;

/// <summary>
/// **場面の中で階が変わる**ところを数え上げる。
///
/// 見た目の作り直しを「この一手で階が変わったか」で決めていたため、
/// 場面の中で階が変わっても誰も気付かず、前の階の盤面が残っていた
/// （作者の報告「24階で時空の扉を通ってからスクリーンに変化が無い」）。
/// いまは <c>TowerController.shownFloor</c> と比べるようにしてある。
/// ここでは**本線の側**が確かに階を変えていることを押さえておく。
/// </summary>
public class FloorChangeInSceneTests
{
    static void Drain(TowerGame g, int limit = 400)
    {
        for (int i = 0; i < limit && g.CurrentKind.HasValue; i++) g.NextPage();
    }

    /// <summary>終幕。24階の門をくぐると50階へ（原典 0x2386〜0x23cd）。</summary>
    [Test]
    public void 終幕は場面の中で二十四階から五十階へ移る()
    {
        var p = new PlayerState
        {
            Hp = 9990, Attack = 990, Defense = 990,
            Floor = 24, HighestFloor = 49, Position = new Vector2Int(0, -2),
        };
        var g = new TowerGame(p);
        g.EnterFloor(24, new Vector2Int(0, -2));
        g.Floor.Put(SpacetimeGate.GatePos, "s");        // 人形に触れて出した門

        int floorAtTrigger = g.Player.Floor;
        g.Step(Vector2Int.up);                          // (0,-1) へ立つ
        Assert.AreEqual("終幕", g.PendingScene, "場面が始まる");
        Assert.AreEqual(24, floorAtTrigger, "**踏み込んだ時点ではまだ24階**");

        Drain(g);
        Assert.AreEqual(Ending.LastFloor, g.Player.Floor, "場面の中で50階へ移る");
        Assert.AreEqual(Ending.LastStep, g.Player.Position, "最後にゼノへ1歩踏み出す");
        Assert.AreEqual(50, g.Floor.FloorNumber, "盤面も50階に入れ替わっている");
        Assert.IsNotNull(g.LastClimb, "駆け上がる段が渡されている");
    }

    /// <summary>序章。3階でゼノに会うと、場面の中で2階の牢へ移る（原典 0x06f1b）。</summary>
    [Test]
    public void 序章は場面の中で三階から二階へ移る()
    {
        var g = new TowerGame(PlayerState.NewGame());
        g.EnterFloor(GameStart.ZenoEncounterFloor, new Vector2Int(-1, -2));
        g.Floor.Clear(new Vector2Int(-1, -3));
        g.Step(Vector2Int.down);
        Assert.AreEqual(3, g.Player.Floor, "踏み込んだ時点ではまだ3階");

        Drain(g);
        Assert.AreEqual(2, g.Player.Floor, "場面の中で2階の牢へ移る");
        Assert.AreEqual(2, g.Floor.FloorNumber);
    }
}
