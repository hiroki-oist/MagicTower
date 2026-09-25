using NUnit.Framework;
using UnityEngine;

/// <summary>
/// **「取ったはずのものがセーブから読み直すと生き返る」を総当たりで潰す。**
///
/// 作者の報告（2026-09-25）「10階のボス戦闘後の報酬と妖精が復活してた」「14階の
/// 赤い鍵も復活しちゃわない？　それ系のやつ全部調べて」。
///
/// 原典のセーブは
///   0x97ec … 能力値
///   0x9b38 … **旗の配列**（`0x1229:0x1e` から 60 ワード）
///   0x9b4e … 地図（`0x1234:0x1ae8` から 0x1930 ワード＝52ブロック）
/// の3つを書き出している。つまり原典では「一度きりの印」と「地図」は必ず一緒に残る。
/// こちらも同じで、残るのは <see cref="PlayerState.EventFlags"/> と元データとの差だけ。
/// **その2つに載らない覚えを持った瞬間に、読み直しで生き返る**ことになる。
/// </summary>
public class ResurrectionSweepTests
{
    string dir;

    [SetUp]
    public void SetUp()
    {
        dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                                     "mt_sweep_" + System.Guid.NewGuid());
        TowerSave.Directory = dir;
    }

    [TearDown]
    public void TearDown()
    {
        TowerSave.Directory = null;
        if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
    }

    /// <summary>どの階でも引き金の上に乗らない立ち位置（立ち入りのできごとを起こさない）。</summary>
    static readonly Vector2Int Corner = new Vector2Int(5, -5);

    static TowerGame Strong(int floor)
    {
        return new TowerGame(new PlayerState
        {
            Hp = 9990, Attack = 999, Defense = 999,
            Floor = floor, HighestFloor = 50, Position = Corner,
        });
    }

    /// <summary>地図を書き換える結果だけ（「表駆動」＝モンスター表の書き換えは除く）。</summary>
    static bool ChangesMap(SealedDoors.Result r)
    {
        return r.Kind != "表駆動" && r.Kind != "封印";
    }

    /// <summary>
    /// **解錠（原典 0x4762）の全件**。守護マスを空けて発火させ、出たものを取り去り、
    /// 書き出して読み直しても**もう一度出てこない**ことを確かめる。
    ///
    /// 14階の赤い鍵（0x66。左上のゾンビナイト3体が守護マス）もこの中に入っている。
    /// </summary>
    [Test]
    public void どの階の解錠も読み直しで生き返らない()
    {
        int tried = 0;
        for (int floor = 1; floor <= 50; floor++)
        {
            foreach (var u in SealedDoors.Instance.ForFloor(floor))
            {
                var g = Strong(floor);
                var f = g.FloorAt(floor);
                foreach (var guard in u.Guards) f.Clear(guard);          // 守護者を倒した
                foreach (var anti in u.AntiGuards) f.Put(anti, "Enemy_SlimeG");
                if (u.RequiresFlag >= 0) g.Player.EventFlags.Add(u.RequiresFlag);
                g.EnterFloor(floor, Corner);                             // ここで解錠が走る

                // 発火したかは**階の器の側**で見る（旗に載っているかは、読み直した
                // あとで確かめたいことそのものなので、ここで使ってはいけない）。
                if (!f.HasFired(u.FlagId)) continue;     // 条件を作れない行は飛ばす
                tried++;

                // 出たものを取り去る（拾った・話した・倒した）
                var taken = new System.Collections.Generic.List<Vector2Int>();
                foreach (var r in u.Results)
                {
                    if (!ChangesMap(r)) continue;
                    g.Floor.Clear(r.Pos);
                    taken.Add(r.Pos);
                }
                g.WriteResume();

                var h = Strong(1);
                Assert.IsTrue(h.LoadSlot(TowerSave.AutoSlot), $"F{floor} 0x{u.FlagId:x} を読める");
                Assert.IsTrue(h.Player.EventFlags.Contains(u.FlagId),
                              $"F{floor} 0x{u.FlagId:x} の印が残っている");
                foreach (var at in taken)
                    Assert.AreEqual(".", h.FloorAt(floor).At(at),
                                    $"F{floor} 0x{u.FlagId:x} の {at} が生き返った");
            }
        }
        Assert.AreEqual(SealedDoors.Instance.Count, tried,
                        "解錠の表の**全行**を実際に発火させて確かめている");
    }

    /// <summary>
    /// **立ち入りのできごと（原典 0x1588）の印**も旗に載る。
    /// 印を持たない行（13階の雪の結晶・43階・47階・24階の門）は原典もそうで、
    /// 二度目を止めているのは**地図そのもの**（持ち物を使い切る／相手が動いてしまう）。
    /// </summary>
    [Test]
    public void 立ち入りの印は旗に載っていて読み直しても残る()
    {
        var g = Strong(1);
        int n = 0;
        foreach (var e in PositionEvents.Instance.All)
        {
            if (e.FlagId < 0) continue;
            g.Player.EventFlags.Add(e.FlagId);
            n++;
        }
        Assert.GreaterOrEqual(n, 14, "印を持つ行がある");
        g.WriteResume();

        var h = Strong(1);
        Assert.IsTrue(h.LoadSlot(TowerSave.AutoSlot));
        foreach (var e in PositionEvents.Instance.All)
            if (e.FlagId >= 0)
                Assert.IsTrue(h.Player.EventFlags.Contains(e.FlagId), $"印 {e.FlagId}");
    }

    /// <summary>
    /// 隠れている者（原典 0x2c6）は、現れたら**地図そのものが変わる**ので
    /// 二度目は起きない。取ったあと（床になったあと）も同じ。
    /// </summary>
    [Test]
    public void 隠れている者は現れたあと読み直しても戻らない()
    {
        foreach (var h0 in HiddenOccupants.Instance.All)
        {
            var g = Strong(h0.Floor);
            var f = g.FloorAt(h0.Floor);
            if (h0.RequiresCleared.HasValue) f.Clear(h0.RequiresCleared.Value);
            var before = f.At(h0.Pos);
            // 壁を壊して中身を出す（**本線と同じ口**＝手前に立って踏み込む）
            var from = h0.Pos + (h0.Pos.x >= 0 ? Vector2Int.left : Vector2Int.right);
            var dir = h0.Pos - from;
            g.EnterFloor(h0.Floor, from);
            f.Clear(from);                               // 手前に立てるようにしておく
            g.Step(dir);
            Assert.AreNotEqual(before, g.Floor.At(h0.Pos), $"F{h0.Floor} で現れる");
            g.Floor.Clear(h0.Pos);                       // 話した・拾った・倒した
            g.WriteResume();

            var k = Strong(1);
            Assert.IsTrue(k.LoadSlot(TowerSave.AutoSlot));
            Assert.AreEqual(".", k.FloorAt(h0.Floor).At(h0.Pos),
                            $"F{h0.Floor} の {h0.Pos} が生き返った");
        }
    }

    /// <summary>
    /// **印を持たない立ち入りのできごと**のうち、ものを増やしうるのは43階だけ
    /// （魔導師が奥へ逃げる ＝ (+3,+5) を消して (+5,+5) に書く）。二度目を止めて
    /// いるのは**地図そのもの**で、逃げたあとは (+3,+5) に魔導師が居ないので
    /// 条件が成り立たない。読み直しても地図の差は残るので同じ。
    ///
    /// ほかの3件は 13階（雪の結晶でマグマを溶かす＝同じ床をもう一度床にするだけ）、
    /// 47階（相手が1マス上へ逃げる＝原典も繰り返す）、24階（時空の門＝原典にも
    /// 一度きりの印が無い）で、どれも増えない。
    /// </summary>
    [Test]
    public void 印を持たない四十三階の魔導師は読み直しても増えない()
    {
        var mage = new Vector2Int(3, 5);
        var to   = new Vector2Int(5, 5);
        var g = Strong(43);
        var f = g.FloorAt(43);
        Assert.AreEqual("Enemy_ID93", f.At(mage), "はじめは (+3,+5) に居る");

        f.Clear(new Vector2Int(2, 5));
        g.EnterFloor(43, new Vector2Int(1, 5));
        g.Step(Vector2Int.right);                       // (+2,+5) に踏み込むと逃げる
        Assert.AreEqual(".", g.Floor.At(mage), "居なくなる");
        Assert.AreEqual("Enemy_ID93", g.Floor.At(to), "奥へ移る");

        g.Floor.Clear(to);                              // 追いかけて倒した
        g.WriteResume();

        var h = Strong(1);
        Assert.IsTrue(h.LoadSlot(TowerSave.AutoSlot));
        Assert.AreEqual(".", h.FloorAt(43).At(mage), "元の場所に戻っていない");
        Assert.AreEqual(".", h.FloorAt(43).At(to), "奥にも湧いていない");
    }

    /// <summary>
    /// 盗賊・階段の到着・五の目・商人・仙人・持ち物の印も、
    /// どれも同じ1本の旗（原典 0x1229:0x1e の配列）に載っている。
    /// </summary>
    [Test]
    public void その他の一度きりの印も全部旗に載る()
    {
        var g = Strong(1);
        var want = new System.Collections.Generic.List<int>();
        foreach (var t in ThiefEvents.Instance.All) if (t.SetsFlag >= 0) want.Add(t.SetsFlag);
        foreach (var a in ArrivalEvents.Instance.All) if (a.FlagId >= 0) want.Add(a.FlagId);
        want.Add(MagicSeal.FlagId);
        Assert.GreaterOrEqual(want.Count, 4);
        foreach (var v in want) g.Player.EventFlags.Add(v);

        // 商人・仙人・持ち物の印（原典は同じ配列の 35〜86 番）
        g.Player.Give(SpecialItems.Kind.Cross);
        g.Player.Rank = "闘神";
        g.Player.DonationCount = 3;
        g.WriteResume();

        var h = Strong(50);
        Assert.IsTrue(h.LoadSlot(TowerSave.AutoSlot));
        foreach (var v in want) Assert.IsTrue(h.Player.EventFlags.Contains(v), $"印 {v}");
        Assert.IsTrue(h.Player.Has(SpecialItems.Kind.Cross), "持ち物");
        Assert.AreEqual("闘神", h.Player.Rank, "称号");
        Assert.AreEqual(3, h.Player.DonationCount, "寄進の回数");
    }
}
