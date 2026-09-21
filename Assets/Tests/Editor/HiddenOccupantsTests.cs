using NUnit.Framework;
using UnityEngine;

/// <summary>隠れている者（原典 地形ID10・関数 0x02c6）が原典どおりかを確かめる。</summary>
public class HiddenOccupantsTests
{
    [Test]
    public void 隠れている者は四人で原典の階とマスどおり()
    {
        var h = HiddenOccupants.Instance;
        Assert.AreEqual(4, h.Count);

        var seq = new System.Collections.Generic.List<string>();
        foreach (var e in h.All) seq.Add($"{e.Floor}:{e.Pos.x},{e.Pos.y}:{e.Id}");
        CollectionAssert.AreEqual(
            new[] { "12:5,5:19", "16:5,-5:25", "19:0,3:39", "41:4,4:151" }, seq);
    }

    [Test]
    public void 表のマスは全部盤面のID10と一致する()
    {
        foreach (var e in HiddenOccupants.Instance.All)
        {
            var a = Resources.Load<TextAsset>($"Floors/Floor_{e.Floor}");
            Assert.IsNotNull(a);
            var want = $"{e.Pos.x:+#;-#;+0} {e.Pos.y:+#;-#;+0}  Marker_ID10";
            StringAssert.Contains(want, a.text, $"F{e.Floor} {e.Pos}");
        }
    }

    static TowerGame Game(int floor, Vector2Int at)
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100, Floor = floor, Position = at
        };
        return new TowerGame(p);
    }

    [Test]
    public void 十六階の隠し壁を壊すと仙人が現れる()
    {
        var g = Game(16, new Vector2Int(4, -5));
        var r = g.Step(Vector2Int.right);
        Assert.AreEqual(TowerPlayer.Outcome.Blocked, r, "壁と同じで進めない");
        Assert.AreEqual(new Vector2Int(4, -5), g.Player.Position, "手前に立ったまま");
        Assert.AreEqual("Master", g.Floor.At(new Vector2Int(5, -5)));
    }

    [Test]
    public void 現れた仙人はエリクサーをくれる方()
    {
        var g = Game(16, new Vector2Int(4, -5));
        g.Step(Vector2Int.right);
        StringAssert.Contains("エリクサー", TowerNpc.TalkToMaster(g.Player),
                              "原典の X=9 > 8");
        Assert.IsTrue(g.Player.Has(SpecialItems.Kind.Elixir));
    }

    [Test]
    public void 十二階の隠し壁を壊すと鍵屋が現れる()
    {
        var g = Game(12, new Vector2Int(4, 5));
        g.Step(Vector2Int.right);
        Assert.AreEqual("Merchant", g.Floor.At(new Vector2Int(5, 5)));

        g.Player.HighestFloor = 12;
        var offers = TowerNpc.OffersAt(12, 9);
        Assert.AreEqual(1, offers.Count);
        Assert.AreEqual("buy_var", offers[0].Mode, "値段が変動する鍵屋");
    }

    [Test]
    public void 十九階の隠し壁を壊すと十字架が現れる()
    {
        var g = Game(19, new Vector2Int(0, 2));
        g.Step(Vector2Int.up);
        Assert.AreEqual("Cross", g.Floor.At(new Vector2Int(0, 3)),
                        "原典ヒント210「十字架は１９階にある」");
    }

    [Test]
    public void 四十一階は左上を倒してからでないと現れない()
    {
        var g = Game(41, new Vector2Int(3, 4));
        g.Step(Vector2Int.right);
        Assert.AreEqual("Marker_ID10", g.Floor.At(new Vector2Int(4, 4)),
                        "左上が残っているうちは現れない");

        g.Floor.Clear(new Vector2Int(-4, 4));          // 左上の魔術士Ａを倒した
        g.Step(Vector2Int.right);
        Assert.AreEqual("Enemy_ID151", g.Floor.At(new Vector2Int(4, 4)),
                        "原典ヒント216「左上の奴を倒さないとだめだな」");
    }

    [Test]
    public void 四十一階の隠れた敵を倒すと降臨の翼が出る()
    {
        var g = Game(41, new Vector2Int(3, 4));
        g.Floor.Clear(new Vector2Int(-4, 4));
        g.Step(Vector2Int.right);
        g.Floor.Clear(new Vector2Int(4, 4));           // 現れた敵を倒した
        g.Floor.ApplyUnlocks();
        Assert.AreEqual("Wing_Down", g.Floor.At(new Vector2Int(0, 1)),
                        "降臨の翼。原典は物体ID45なので名前に直して置く");
    }

    // ---- 26階の人形（原典 物体ID27） ----

    [Test]
    public void 人形に触れると二十四階の中央に門が現れる()
    {
        var g = Game(26, new Vector2Int(0, 1));
        Assert.AreEqual(".", g.FloorAt(24).At(SpacetimeGate.GatePos), "はじめは空いている");

        var text = g.TouchDoll();
        StringAssert.Contains("ジクウノモンヲクグリテ", text);
        StringAssert.Contains("人形だっ", text);
        Assert.AreEqual("s", g.FloorAt(24).At(SpacetimeGate.GatePos), "24階(0,0)に門");
    }

    [Test]
    public void 人形は二十六階の中央にしかいない()
    {
        var placed = new System.Collections.Generic.List<int>();
        for (int f = 1; f <= 50; f++)
        {
            var a = Resources.Load<TextAsset>($"Floors/Floor_{f}");
            if (a != null && a.text.Contains("Princess")) placed.Add(f);
        }
        CollectionAssert.AreEqual(new[] { 26 }, placed);
    }
}
