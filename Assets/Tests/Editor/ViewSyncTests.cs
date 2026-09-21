using NUnit.Framework;
using UnityEngine;

/// <summary>
/// **盤面の中身と画面の中身が食い違わない**ことを見る検査。
///
/// 手で遊んでもらって見つかった不具合がこれだった:
///   序章で会話を送るとゼノに奪われて2階の牢へ移るのに、**画面は3階のまま**で
///   主人公も動かなかった。本線（`TowerGame`）の側はちゃんと移っていたので、
///   見た目を作り直していなかったのが原因。
///
/// 会話を送っただけ・メニューを触っただけでも階は変わる（序章のゼノ／飛翔のオーブ／
/// 終幕の駆け上がり）ので、そこを押さえる。
/// </summary>
public class ViewSyncTests
{
    GameObject host;
    TowerController controller;
    FloorLoader loader;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("ViewSyncHost");
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

    bool HasObjectAt(string prefix, Vector2Int at)
    {
        var suffix = $"_{at.x}_{at.y}";
        foreach (Transform c in loader.Container)
            if (c.name.StartsWith(prefix) && c.name.EndsWith(suffix)) return true;
        return false;
    }

    static TowerGame Game(int floor, Vector2Int at)
    {
        return new TowerGame(new PlayerState
        {
            Hp = 5000, Attack = 500, Defense = 500,
            YellowKeys = 9, PurpleKeys = 9, RedKeys = 9,
            Floor = floor, HighestFloor = 50, Position = at,
        });
    }

    /// <summary>倒した敵・開けた扉は、階を作り直しても戻ってこない。</summary>
    [Test]
    public void 作り直しても済んだことは元に戻らない()
    {
        var g = Game(1, new Vector2Int(0, -5));
        SetGame(g);

        // 初期配置で敵と扉がある所を探す
        Vector2Int enemy = Vector2Int.zero, door = Vector2Int.zero;
        bool foundEnemy = false, foundDoor = false;
        for (int y = FloorData.MaxY; y >= FloorData.MinY && !(foundEnemy && foundDoor); y--)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
            {
                var p = new Vector2Int(x, y);
                var w = g.Floor.At(p);
                if (!foundEnemy && ObjectIds.Instance.IsEnemy(w)) { enemy = p; foundEnemy = true; }
                if (!foundDoor && w == "y") { door = p; foundDoor = true; }
            }
        Assert.IsTrue(foundEnemy && foundDoor, "1階に敵と黄色い扉がある");

        controller.RebuildView();
        Assert.IsTrue(HasObjectAt("Enemy_", enemy), "はじめは敵が居る");
        Assert.IsTrue(HasObjectAt("LockYellow", door), "はじめは扉がある");

        g.Floor.Clear(enemy);          // 倒した
        g.Floor.Clear(door);           // 開けた
        controller.RebuildView();      // 階を出入りしたつもりで作り直す

        Assert.IsFalse(HasObjectAt("Enemy_", enemy), "倒した敵は戻ってこない");
        Assert.IsFalse(HasObjectAt("LockYellow", door), "開けた扉も戻ってこない");
    }

    /// <summary>序章: 会話を送りきると2階の牢に居て、牢の出口も開いて見える。</summary>
    [Test]
    public void 序章を送りきると画面も二階の牢になる()
    {
        var g = new TowerGame(PlayerState.NewGame());
        SetGame(g);
        g.EnterFloor(GameStart.ZenoEncounterFloor, new Vector2Int(-1, -2));
        g.Floor.Clear(new Vector2Int(-1, -3));
        g.Step(Vector2Int.down);
        Assert.AreEqual("ゼノとの初対面", g.PendingScene);

        for (int i = 0; i < 40 && g.CurrentKind.HasValue; i++) g.NextPage();
        Assert.AreEqual(2, g.Player.Floor, "本線は2階へ移っている");

        controller.RebuildView();
        Assert.IsFalse(HasObjectAt("Wall", new Vector2Int(-4, -1)), "牢の出口が開いて見える");
        Assert.IsFalse(HasObjectAt("Wall", new Vector2Int(-3, -1)));
        Assert.IsTrue(HasObjectAt("Mikata", new Vector2Int(-3, -1))
                   || g.Floor.At(new Vector2Int(-3, -1)) == FloorState.Floor,
                      "盗賊が居るか、居なくなったマスは床");
    }

    /// <summary>3x3のボスは作り直しても1体のまま（9体に増えない）。</summary>
    [Test]
    public void 作り直しても大きなボスは一体のまま()
    {
        var g = Game(35, new Vector2Int(0, -5));
        SetGame(g);
        controller.RebuildView();
        int dragons = 0;
        foreach (Transform c in loader.Container)
            if (c.name.StartsWith("Boss_Dragon")) dragons++;
        Assert.AreEqual(1, dragons);
    }
}
