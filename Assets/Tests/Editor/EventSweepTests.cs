using NUnit.Framework;
using UnityEngine;

/// <summary>
/// **立ち入りイベントを全部ひとつずつ踏んでみる**検査（原典 0x1588 の表）。
///
/// `StoryWalkTests` は物語の筋をたどるだけなので、表に載っていて筋から外れたものは
/// 誰も踏んでいなかった。ここでは `PositionEvents.txt` の全件について
/// 「条件を満たして立てば発火し、積まれたページを最後まで送れて、
///   書き換えた地図が登録簿にある物になっている」ことを見る。
/// </summary>
public class EventSweepTests
{
    static TowerGame Strong(int floor, Vector2Int at)
    {
        return new TowerGame(new PlayerState
        {
            Hp = PlayerState.MaxHp, Attack = PlayerState.MaxAttack,
            Defense = PlayerState.MaxDefense, Gold = 1000,
            YellowKeys = 9, PurpleKeys = 9, RedKeys = 9, SwordRank = 5, ShieldRank = 5,
            Floor = floor, HighestFloor = 50, Position = at,
        });
    }

    [Test]
    public void 立ち入りイベントは全部発火して最後まで送れる()
    {
        var reg = Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry");
        Assert.IsNotNull(reg, "登録簿が読めない");
        int fired = 0;
        foreach (var e in PositionEvents.Instance.All)
        {
            var at = e.Spots[0];
            var g = Strong(e.Floor, at);
            Assert.IsNotNull(g.Floor, $"F{e.Floor} のデータが読めない");

            // 追加条件を満たしてやる
            if (e.NeedsFlag > 0) g.Player.EventFlags.Add(e.NeedsFlag);
            if (e.NeedsCleared.HasValue) g.Floor.Clear(e.NeedsCleared.Value);
            if (e.NeedsGate.HasValue) g.Floor.Put(e.NeedsGate.Value, "s");
            if (e.NeedsItem != SpecialItems.Kind.None) g.Player.Give(e.NeedsItem);

            var hit = PositionEvents.Instance.At(e.Floor, at, g.Player.EventFlags,
                                                g.Floor.IsCleared, p => g.Floor.At(p) == "s",
                                                k => g.Player.Has(k));
            Assert.IsTrue(hit.HasValue, $"F{e.Floor} {at} の旗{e.FlagId} が発火しない");
            fired++;

            // 本線に流して、積まれたページを最後まで送れること
            g.Player.Position = at;
            Assert.IsTrue(g.CheckPositionEvent(), $"F{e.Floor} {at} が本線で発火しない");
            for (int i = 0; i < 400 && g.IsShowingPage; i++) g.NextPage();
            Assert.IsFalse(g.IsShowingPage, $"F{e.Floor} {at} のページが送りきれない");

            // 書き換えた地図の中身は、見た目の側が出せるものでなければならない
            foreach (var c in e.Changes)
            {
                var sym = FloorState.SymbolOfKind(c.Kind);
                var key = FloorState.ViewKeyOf(sym);
                if (string.IsNullOrEmpty(key)) continue;      // 床は何も置かない
                Assert.IsNotNull(reg.Get(key),
                    $"F{c.Floor} {c.Pos} に置く '{c.Kind}'（{key}）が登録簿に無い");
            }
        }
        Assert.AreEqual(PositionEvents.Instance.Count, fired, "全部踏めた");
        Assert.Greater(fired, 0);
    }

    [Test]
    public void 階段で着いたときのできごとも全部当てられる()
    {
        var reg = Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry");
        int n = 0;
        foreach (var e in ArrivalEvents.Instance.All)
        {
            var g = Strong(e.Floor, new Vector2Int(0, 0));
            Assert.IsTrue(g.ApplyArrival(e.Floor, e.Descending),
                          $"F{e.Floor} の到着のできごとが起きない");
            foreach (var c in e.Changes)
            {
                var key = FloorState.ViewKeyOf(FloorState.SymbolOfKind(c.What));
                if (string.IsNullOrEmpty(key)) continue;
                Assert.IsNotNull(reg.Get(key),
                    $"F{c.Floor} {c.Pos} に置く '{c.What}'（{key}）が登録簿に無い");
            }
            n++;
        }
        Assert.AreEqual(ArrivalEvents.Instance.Count, n);
    }
}
