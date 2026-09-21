using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// **どんな一手のあとでも、画面の中身が盤面の中身と一致している**ことを見る検査。
///
/// 手で遊んでもらって出た不具合（会話で階が変わったのに画面が付いてこない、
/// 階を作り直すと倒した敵が復活する）は、どれも「盤面は正しいのに画面が違う」だった。
/// 個々の場面ごとに検査を足すのではなく、**毎手つき合わせる**ことで同じ類を丸ごと防ぐ。
/// </summary>
public class ViewMatchesBoardTests
{
    static readonly BindingFlags Any =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    GameObject host;
    TowerController controller;
    FloorLoader loader;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("ViewMatchHost");
        loader = host.AddComponent<FloorLoader>();
        loader.Configure(Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry"),
                         host.transform);
        controller = host.AddComponent<TowerController>();
        var so = new UnityEditor.SerializedObject(controller);
        so.FindProperty("loader").objectReferenceValue = loader;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [TearDown]
    public void TearDown()
    {
        if (loader != null) loader.Clear();
        if (host != null) Object.DestroyImmediate(host);
    }

    void SetGame(TowerGame g)
    {
        typeof(TowerController).GetProperty("Game").SetValue(controller, g);
    }

    /// <summary>`TowerController` が一手ごとにやる後始末。入力の代わりに直に呼ぶ。</summary>
    void AfterAction(int floorBefore)
    {
        typeof(TowerController).GetMethod("AfterAction", Any)
            .Invoke(controller, new object[] { floorBefore });
    }

    /// <summary>画面に出ている物の名前（マスごと）。床の下地は数えない。</summary>
    Dictionary<Vector2Int, string> ViewCells()
    {
        var map = new Dictionary<Vector2Int, string>();
        foreach (Transform c in loader.Container)
        {
            var parts = c.name.Split('_');
            if (parts.Length < 3) continue;
            int x, y;
            if (!int.TryParse(parts[parts.Length - 2], out x)) continue;
            if (!int.TryParse(parts[parts.Length - 1], out y)) continue;
            var key = string.Join("_", parts, 0, parts.Length - 2);
            if (key == "Floor") continue;                 // 下地はどのマスにもある
            map[new Vector2Int(x, y)] = key;
        }
        return map;
    }

    /// <summary>盤面と画面がマスごとに合っているか。</summary>
    void AssertMatches(TowerGame g, string where)
    {
        var view = ViewCells();
        var bad = new List<string>();
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
            {
                var p = new Vector2Int(x, y);
                var want = FloorState.ViewKeyOf(g.Floor.At(p));
                string got;
                view.TryGetValue(p, out got);

                // 3x3のボスは中心の1体だけ出す約束なので、周りの8マスは見逃す
                if (want != null && want.StartsWith("Boss_")) continue;
                if (want == got) continue;
                bad.Add($"({x},{y}) 盤面={want ?? "床"} 画面={got ?? "なし"}");
            }
        Assert.IsEmpty(bad, $"{where}: " + string.Join(" / ", bad.ToArray()));
    }

    static TowerGame Game(int floor, Vector2Int at)
    {
        return new TowerGame(new PlayerState
        {
            Hp = 5000, Attack = 500, Defense = 500, Gold = 500,
            YellowKeys = 9, PurpleKeys = 9, RedKeys = 9,
            Floor = floor, HighestFloor = 50, Position = at,
        });
    }

    /// <summary>拾う・開ける・倒す・階段。ひと手ごとに突き合わせる。</summary>
    [Test]
    public void 動き回っても画面と盤面が食い違わない()
    {
        var g = Game(1, new Vector2Int(0, -5));
        SetGame(g);
        controller.RebuildView();
        AssertMatches(g, "はじめ");

        var dirs = new[] { Vector2Int.up, Vector2Int.left, Vector2Int.up,
                           Vector2Int.right, Vector2Int.up, Vector2Int.up,
                           Vector2Int.left, Vector2Int.down, Vector2Int.up };
        for (int i = 0; i < dirs.Length; i++)
        {
            int before = g.Player.Floor;
            g.TryStep(dirs[i]);
            while (g.IsShowingPage) g.NextPage();
            AfterAction(before);
            AssertMatches(g, $"{i + 1}手目");
        }
    }

    /// <summary>序章。会話を送るだけで階が変わるところ。</summary>
    [Test]
    public void 序章を送っても画面と盤面が食い違わない()
    {
        var g = new TowerGame(PlayerState.NewGame());
        SetGame(g);
        g.EnterFloor(GameStart.ZenoEncounterFloor, new Vector2Int(-1, -2));
        controller.RebuildView();

        g.Floor.Clear(new Vector2Int(-1, -3));
        int before = g.Player.Floor;
        g.Step(Vector2Int.down);
        AfterAction(before);

        for (int i = 0; i < 40 && g.CurrentKind.HasValue; i++)
        {
            before = g.Player.Floor;
            g.NextPage();
            AfterAction(before);
            AssertMatches(g, $"{i + 1}ページ目");
        }
        Assert.AreEqual(2, g.Player.Floor, "2階の牢に移っている");
    }

    /// <summary>飛翔のオーブ。メニューから階が飛ぶところ。</summary>
    [Test]
    public void 飛んでも画面と盤面が食い違わない()
    {
        var g = Game(5, new Vector2Int(0, 0));
        g.Player.Give(SpecialItems.Kind.FlightOrb);
        SetGame(g);
        g.Floor.Put(g.Player.Position + Vector2Int.up, ">");
        controller.RebuildView();

        g.OpenMenu();
        while (g.MenuCurrent != TowerMenu.Entry.FlightOrb) g.MenuMove(1);
        g.MenuConfirm();
        for (int i = 0; i < 3; i++) g.FlightMove(1);

        int before = g.Player.Floor;
        Assert.IsTrue(g.ConfirmFlight());
        AfterAction(before);
        Assert.AreEqual(8, g.Player.Floor);
        AssertMatches(g, "飛んだあと");
    }
}
