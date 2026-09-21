using NUnit.Framework;
using UnityEngine;

/// <summary>
/// セーブは**地図も一緒に**保存する。原典のセーブ（MTE.EXE 0x9b4e）は
/// `DS:0x1ae8` から 0x1930 ワード＝52ブロックぶんの地図をまるごと書き出していて、
/// 倒した敵・開けた扉・拾った物がセーブに含まれる。
/// </summary>
public class SaveMapTests
{
    string dir;

    [SetUp]
    public void SetUp()
    {
        dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mt_save_" + System.Guid.NewGuid());
        TowerSave.Directory = dir;
    }

    [TearDown]
    public void TearDown()
    {
        TowerSave.Directory = null;
        if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
    }

    static TowerGame NewGame()
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100, Floor = 1,
            Position = new Vector2Int(0, 0),
        };
        return new TowerGame(p);
    }

    [Test]
    public void 倒した敵と開けた扉と拾った物が戻ってくる()
    {
        var g = NewGame();
        var f1 = g.FloorAt(1);
        var at = new Vector2Int(0, 1);
        f1.Put(at, "Enemy_SlimeG");
        g.EnterFloor(1, new Vector2Int(0, 0));
        f1.Clear(at);                                  // 倒した
        var f2 = g.FloorAt(2);
        var door = new Vector2Int(1, 1);
        f2.Clear(door);                                // 開けた

        g.OpenMenu();
        g.Player.Gold = 123;
        TowerSave.Save(1, g.Player, Floors(g));

        // まっさらから読み直す
        var h = NewGame();
        var snap = TowerSave.Read(1);
        Assert.IsNotNull(snap);
        Assert.AreEqual(123, snap.Player.Gold);
        foreach (var kv in snap.Floors)
        {
            var st = h.FloorAt(kv.Key);
            Assert.IsNotNull(st);
            st.Apply(kv.Value);
        }
        Assert.AreEqual(".", h.FloorAt(1).At(at), "倒した敵は戻ってこない");
        Assert.AreEqual(".", h.FloorAt(2).At(door), "開けた扉は開いたまま");
    }

    [Test]
    public void 変わっていない階は書き出さない()
    {
        var g = NewGame();
        g.FloorAt(1);
        g.FloorAt(2);
        TowerSave.Save(1, g.Player, Floors(g));
        var snap = TowerSave.Read(1);
        Assert.AreEqual(0, snap.Floors.Count, "差が無ければ何も残らない");
    }

    [Test]
    public void 地図の無い古いセーブも読める()
    {
        var g = NewGame();
        TowerSave.Save(1, g.Player);                    // 地図を渡さない古い形
        var text = System.IO.File.ReadAllText(
            System.IO.Path.Combine(dir, "save1.txt"));
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "save1.txt"),
            string.Join("|", text.Split('|')[0], text.Split('|')[1], text.Split('|')[2]));
        var snap = TowerSave.Read(1);
        Assert.IsNotNull(snap);
        Assert.AreEqual(0, snap.Floors.Count);
        Assert.IsNotNull(TowerSave.Load(1), "今までの読み口もそのまま使える");
    }

    static System.Collections.Generic.Dictionary<int, FloorState> Floors(TowerGame g)
    {
        // TowerGame の中の辞書はしまってあるので、テストからは作り直して渡す
        var d = new System.Collections.Generic.Dictionary<int, FloorState>();
        for (int n = 0; n <= 50; n++)
        {
            var st = g.FloorAt(n);
            if (st != null) d[n] = st;
        }
        return d;
    }
}
