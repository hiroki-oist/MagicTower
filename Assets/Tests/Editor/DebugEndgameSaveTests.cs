using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>調べもの用のセーブ（作者の頼み。49階ボス〜終幕だけ見たい）。</summary>
public class DebugEndgameSaveTests
{
    static void Build(out PlayerState p, out Dictionary<int, FloorState> f)
    {
        DebugEndgameSave.Build(DebugEndgameSave.BeforeZeno, out p, out f);
    }

    static void Gate(out PlayerState p, out Dictionary<int, FloorState> f)
    {
        DebugEndgameSave.Build(DebugEndgameSave.BeforeGate, out p, out f);
    }

    [Test]
    public void 能力値はカンストで鍵は三十本ずつ()
    {
        PlayerState p; Dictionary<int, FloorState> f;
        Build(out p, out f);
        Assert.AreEqual(PlayerState.MaxHp, p.Hp);
        Assert.AreEqual(PlayerState.MaxAttack, p.Attack);
        Assert.AreEqual(PlayerState.MaxDefense, p.Defense);
        Assert.AreEqual(30, p.YellowKeys);
        Assert.AreEqual(30, p.PurpleKeys);
        Assert.AreEqual(30, p.RedKeys);
        Assert.AreEqual(5, p.SwordRank, "神剣");
        Assert.AreEqual(5, p.ShieldRank, "神盾");
        Assert.AreEqual(49, p.Floor);
        Assert.AreEqual(new Vector2Int(-5, -5), p.Position, "49階の下り階段");
    }

    [Test]
    public void 持ち物はドラゴンスレイヤー以外の十四種()
    {
        PlayerState p; Dictionary<int, FloorState> f;
        Build(out p, out f);
        int n = 0;
        foreach (SpecialItems.Kind k in System.Enum.GetValues(typeof(SpecialItems.Kind)))
        {
            if (k == SpecialItems.Kind.None) continue;
            if (k == SpecialItems.Kind.DragonSlayer)
                Assert.IsFalse(p.Has(k), "ドラゴンスレイヤーだけ持っていない");
            else { Assert.IsTrue(p.Has(k), k.ToString()); n++; }
        }
        Assert.AreEqual(14, n);
    }

    [Test]
    public void 四十八階までは敵が残っていない()
    {
        PlayerState p; Dictionary<int, FloorState> f;
        Build(out p, out f);
        var ids = ObjectIds.Instance;
        for (int n = 1; n <= DebugEndgameSave.LastCleared; n++)
        {
            if (n == 24 || n == 26) continue;
            for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
                for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                {
                    var c = f[n].At(new Vector2Int(x, y));
                    if (n == DebugEndgameSave.DragonFloor
                        && c == DebugEndgameSave.DragonPrefab) continue;
                    Assert.IsFalse(c != null && (c.StartsWith("Enemy_") || c.StartsWith("Boss_")),
                                   $"F{n} ({x},{y}) に {c} が残っている");
                }
        }
    }

    [Test]
    public void ドラゴンと二十四階と二十六階と四十九階は手付かず()
    {
        PlayerState p; Dictionary<int, FloorState> f;
        Build(out p, out f);
        Assert.AreEqual(DebugEndgameSave.DragonPrefab,
                        f[35].At(new Vector2Int(0, -1)), "35階のドラゴンは残す");
        foreach (var n in new[] { 24, 26, 49 })
        {
            var data = FloorData.Load(n);
            Assert.AreEqual(0, f[n].Diff(data).Count, $"F{n} は元のまま");
        }
        Assert.AreEqual("Princess", f[26].At(new Vector2Int(0, 0)), "人形はまだ居る");
        Assert.AreEqual(".", f[24].At(SpacetimeGate.GatePos), "時空の門はまだ出ていない");
    }

    /// <summary>49階のボスの引き金と、そのあとの終幕の引き金がどちらも生きているか。</summary>
    [Test]
    public void 四十九階のボスと終幕の引き金がまだ立っていない()
    {
        PlayerState p; Dictionary<int, FloorState> f;
        Build(out p, out f);
        foreach (var e in PositionEvents.Instance.All)
        {
            if (e.Floor == 49) Assert.IsFalse(p.EventFlags.Contains(e.FlagId), "49階のボス");
            if (e.Floor == 24) Assert.AreEqual(-1, e.FlagId, "24階は印を持たない");
        }
    }

    /// <summary>48階までの扉と門は開いている（登ってきた人のセーブと同じ形）。</summary>
    [Test]
    public void 四十八階までの扉と門は開いている()
    {
        PlayerState p; Dictionary<int, FloorState> f;
        Build(out p, out f);
        for (int n = 1; n <= DebugEndgameSave.LastCleared; n++)
        {
            if (n == 24 || n == 26) continue;
            for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
                for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                {
                    var c = f[n].At(new Vector2Int(x, y));
                    CollectionAssert.DoesNotContain(new[] { "y", "b", "r", "s", "x" }, c,
                                                    $"F{n} ({x},{y})");
                }
        }
    }

    /// <summary>
    /// 時空の門の1歩手前から始める版（作者の頼み「時空の扉を通る寸前で用意して」）。
    /// 24階の (0,-2) に立ち、上へ1歩で終幕が始まる。
    /// </summary>
    [Test]
    public void 門の手前版は一歩で終幕が始まる()
    {
        PlayerState p; Dictionary<int, FloorState> f;
        Gate(out p, out f);
        Assert.AreEqual(24, p.Floor);
        Assert.AreEqual(new Vector2Int(0, -2), p.Position);
        Assert.AreEqual("s", f[24].At(SpacetimeGate.GatePos), "時空の門が出ている");
        Assert.AreEqual(".", f[24].At(new Vector2Int(0, -1)), "門の真下は空いている");
        Assert.IsTrue(p.Has(SpecialItems.Kind.DragonSlayer), "ゼノを倒したあとなので持っている");
        Assert.IsTrue(p.EventFlags.Contains(MagicSeal.FlagId), "五の目も済んでいる");

        // 実際に1歩で場面が始まるか
        var g = new TowerGame(p);
        g.EnterFloor(24, p.Position);
        foreach (var kv in f[24].Diff(FloorData.Load(24))) g.Floor.Put(kv.Key, kv.Value);
        g.Step(Vector2Int.up);
        Assert.AreEqual("終幕", g.PendingScene);
    }

    /// <summary>書き出して読み直しても同じ形になるか。</summary>
    [Test]
    public void 書き出して読み直せる()
    {
        var dir = System.IO.Path.Combine(Application.temporaryCachePath, "endgame_test");
        var keep = TowerSave.Directory;
        TowerSave.Directory = dir;
        try
        {
            TowerSave.Delete(1);
            DebugEndgameSave.WriteTo(1, DebugEndgameSave.BeforeZeno);
            var snap = TowerSave.Read(1);
            Assert.IsNotNull(snap);
            Assert.AreEqual(PlayerState.MaxHp, snap.Player.Hp);
            Assert.AreEqual(49, snap.Player.Floor);
            Assert.IsTrue(snap.Player.Has(SpecialItems.Kind.WisdomOrb));
            Assert.IsFalse(snap.Player.Has(SpecialItems.Kind.DragonSlayer));
            Assert.IsFalse(snap.Floors.ContainsKey(24), "24階は元のままなので差が無い");
            Assert.IsFalse(snap.Floors.ContainsKey(26), "26階も");
            Assert.IsTrue(snap.Floors.ContainsKey(1), "1階には差がある");
        }
        finally { TowerSave.Directory = keep; }
    }
}
