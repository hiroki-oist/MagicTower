using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 2階の牢の格子（特殊扉 6枚）が、序章を抜けたあとも盤面と画面の両方に残っているか。
///
/// 原典（動画 Part1 t=50）の2階は、盤面の (-1,+1)(+3,+1)(-1,-2)(+3,-2)(-1,-5)(+3,-5)
/// に白い格子が並び、主人公は (-3,-2) の牢の中に居る。
/// この6枚は解錠 0x57（守護マス (0,+4) と (+2,+4) の大守衛2体）で初めて開くので、
/// **序章直後は必ず6枚とも閉じている**。
/// </summary>
public class PrisonBarsTests
{
    static readonly Vector2Int[] Bars =
    {
        new Vector2Int(-1, 1), new Vector2Int(3, 1),
        new Vector2Int(-1, -2), new Vector2Int(3, -2),
        new Vector2Int(-1, -5), new Vector2Int(3, -5),
    };

    GameObject host;
    TowerController controller;
    FloorLoader loader;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("PrisonBarsHost");
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

    bool HasObjectAt(string prefix, Vector2Int at)
    {
        var suffix = $"_{at.x}_{at.y}";
        foreach (Transform c in loader.Container)
            if (c.name.StartsWith(prefix) && c.name.EndsWith(suffix)) return true;
        return false;
    }

    [Test]
    public void 階データの時点で格子が六枚ある()
    {
        var data = FloorData.Load(2);
        int n = 0;
        foreach (var b in Bars)
            if (data.At(b.x, b.y) == FloorData.Terrain.LockSpecial) n++;
        Assert.AreEqual(6, n, "Floor_2.txt の特殊扉");
    }

    [Test]
    public void 序章を抜けても盤面に格子が残っている()
    {
        var g = new TowerGame(PlayerState.NewGame());
        g.EnterFloor(GameStart.ZenoEncounterFloor, new Vector2Int(-1, -2));
        g.Floor.Clear(new Vector2Int(-1, -3));
        g.Step(Vector2Int.down);
        for (int i = 0; i < 60 && g.CurrentKind.HasValue; i++) g.NextPage();
        Assert.AreEqual(2, g.Player.Floor, "2階の牢に居る");
        foreach (var b in Bars)
            Assert.AreEqual("x", g.Floor.At(b), $"格子 {b}");
    }

    [Test]
    public void 序章を抜けた画面にも格子が出ている()
    {
        var g = new TowerGame(PlayerState.NewGame());
        typeof(TowerController).GetProperty("Game").SetValue(controller, g);
        g.EnterFloor(GameStart.ZenoEncounterFloor, new Vector2Int(-1, -2));
        g.Floor.Clear(new Vector2Int(-1, -3));
        g.Step(Vector2Int.down);
        for (int i = 0; i < 60 && g.CurrentKind.HasValue; i++) g.NextPage();
        controller.RebuildView();
        foreach (var b in Bars)
            Assert.IsTrue(HasObjectAt("LockSpecial", b), $"画面の格子 {b}");
    }
}

/// <summary>
/// 序章の終わりに盗賊が牢を出て歩いていくところ（原典 MTE.EXE 0x070c8〜0x07153）。
/// 動画 Part1 t=52.2〜53.1 でも、左・左・下と1マスずつ滑って (-5,-2) に立つのが見える。
/// </summary>
public class ThiefWalkTests
{
    static TowerGame AfterPrologue()
    {
        var g = new TowerGame(PlayerState.NewGame());
        g.EnterFloor(GameStart.ZenoEncounterFloor, new Vector2Int(-1, -2));
        g.Floor.Clear(new Vector2Int(-1, -3));
        g.Step(Vector2Int.down);
        for (int i = 0; i < 60 && g.CurrentKind.HasValue; i++) g.NextPage();
        return g;
    }

    [Test]
    public void 道順は左左下で原典の向きの表どおり()
    {
        Assert.AreEqual(3, Prologue.ThiefWalk.Length, "表 {2,2,3} の3歩");
        Assert.AreEqual(new Vector2Int(-4, -1), Prologue.ThiefWalk[0], "2 = X-1");
        Assert.AreEqual(new Vector2Int(-5, -1), Prologue.ThiefWalk[1], "2 = X-1");
        Assert.AreEqual(new Vector2Int(-5, -2), Prologue.ThiefWalk[2], "3 = 画面の下");
        Assert.AreEqual(new Vector2Int(-5, -2), Prologue.ThiefGoal);
    }

    [Test]
    public void 歩いたあとも盗賊は盤面に残る()
    {
        var g = AfterPrologue();
        Assert.AreEqual(2, g.Player.Floor);
        Assert.AreEqual(FloorState.Floor, g.Floor.At(Prologue.ThiefStart), "元の所には居ない");
        Assert.AreEqual(Prologue.ThiefPrefab, g.Floor.At(Prologue.ThiefGoal),
                        "歩いた先に立っている（原典 0x07153 が ID21 を書く）");
    }

    [Test]
    public void 歩きが見た目の層へ渡る()
    {
        var g = AfterPrologue();
        var walks = g.ConsumeWalks();
        Assert.AreEqual(1, walks.Count, "歩きは1件");
        Assert.AreEqual(Prologue.ThiefPrefab, walks[0].Prefab);
        Assert.AreEqual(Prologue.ThiefStart, walks[0].From);
        CollectionAssert.AreEqual(Prologue.ThiefWalk, walks[0].Path);
        Assert.AreEqual(0, g.ConsumeWalks().Count, "取り出したら空になる");
    }

    [Test]
    public void 立ち止まった所で話すとセリフ百十六が出る()
    {
        var g = AfterPrologue();
        // (-5,-1) から下へ踏み込むと盗賊に話しかける形になる
        g.Player.Position = new Vector2Int(-5, -1);
        g.Step(Vector2Int.down);
        StringAssert.Contains("５階に剣", g.CurrentPage ?? "");
    }
}

/// <summary>
/// 3階のゼノとの初対面で鳴る音（原典 0x6ae0 の中の `lcall 0x10da,0x20b` は1か所だけ）。
/// 「ゼノの出るマスに入ると扉が開く音がする」という報告を受けて入れた検査。
/// </summary>
public class ZenoSceneSoundTests
{
    [Test]
    public void 踏み込んだだけでは音が鳴らない()
    {
        var g = new TowerGame(PlayerState.NewGame());
        g.EnterFloor(GameStart.ZenoEncounterFloor, new Vector2Int(-1, -2));
        g.Floor.Clear(new Vector2Int(-1, -3));
        g.ConsumeSounds();
        g.Step(Vector2Int.down);
        CollectionAssert.DoesNotContain(g.Sounds, 11, "扉の音11は鳴らない");
        CollectionAssert.DoesNotContain(g.Sounds, 6, "拾う音6も鳴らない（原典に無い）");
    }

    [Test]
    public void 場面の中で鳴るのは閃光の音七だけ()
    {
        var g = new TowerGame(PlayerState.NewGame());
        g.EnterFloor(GameStart.ZenoEncounterFloor, new Vector2Int(-1, -2));
        g.Floor.Clear(new Vector2Int(-1, -3));
        g.ConsumeSounds();
        g.Step(Vector2Int.down);
        for (int i = 0; i < 60 && g.CurrentKind.HasValue; i++) g.NextPage();
        var sounds = new System.Collections.Generic.List<int>(g.Sounds);
        CollectionAssert.Contains(sounds, 7, "閃光の音7（原典 0x6d09）");
        // 題名の曲(2)と本編の曲(1)は場面の中で切り替わる。効果音はそれ以外に無い。
        foreach (var n in sounds)
            Assert.IsTrue(n == 7 || n == 1 || n == 2, $"余分な音 {n}");
    }
}
