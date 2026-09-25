using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 解錠（原典 0x4762）の**一度きりの印はセーブに残る**。
///
/// 原典のセーブは 0x9b38 で `0x1229:0x1e` から 60 ワード（＝旗の配列）を書き出し、
/// そのあと 0x9b4e で地図を書き出している。つまり**旗と地図は必ず一緒**に残る。
///
/// ⚠ ここが抜けていた（作者の報告「通常プレイしてたら、いつのまにか取得したはずの
/// 10Fのボス戦闘後の報酬と妖精が復活してた」）。印を階の器の中だけに置いていたので、
/// セーブから読み直すと消え、守護マスは床のままなので解錠がもう一度走っていた。
/// </summary>
public class UnlockFlagSaveTests
{
    string dir;

    [SetUp]
    public void SetUp()
    {
        dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                                     "mt_unlock_" + System.Guid.NewGuid());
        TowerSave.Directory = dir;
    }

    [TearDown]
    public void TearDown()
    {
        TowerSave.Directory = null;
        if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
    }

    static TowerGame At(int floor, Vector2Int pos)
    {
        return new TowerGame(new PlayerState
        {
            Hp = 9990, Attack = 999, Defense = 999,
            Floor = floor, HighestFloor = floor, Position = pos,
        });
    }

    /// <summary>10階のボスを倒した形にする（守護マスを全部床にする）。</summary>
    static void BeatBoss(FloorState f)
    {
        // 0x5c の守護マス＝骸骨が居た3x3、0x5d の守護マス＝(0,+5)
        for (int y = 0; y <= 2; y++)
            for (int x = -1; x <= 1; x++)
                f.Clear(new Vector2Int(x, y));
        f.Clear(new Vector2Int(0, 5));
    }

    [Test]
    public void 十階の褒美と妖精はセーブから読み直しても生き返らない()
    {
        var fairy = new Vector2Int(0, 3);      // 0x5d が置く妖精(ID23)
        var gem   = new Vector2Int(-5, 3);     // 同じく宝(ID34)
        var stair = new Vector2Int(0, -5);     // 同じく上り階段

        var g = At(10, new Vector2Int(-5, -5));
        BeatBoss(g.FloorAt(10));
        g.EnterFloor(10, new Vector2Int(-5, -5));       // ここで解錠が走る

        Assert.AreNotEqual(".", g.Floor.At(fairy), "妖精が出る");
        Assert.AreNotEqual(".", g.Floor.At(gem), "褒美が出る");
        Assert.AreNotEqual(".", g.Floor.At(stair), "上り階段が出る");

        g.Floor.Clear(fairy);                           // 妖精と話した
        g.Floor.Clear(gem);                             // 宝を拾った
        g.WriteResume();                                // ふだんと同じ書き出し

        var h = At(1, new Vector2Int(0, -5));
        Assert.IsTrue(h.LoadSlot(TowerSave.AutoSlot), "読める");
        Assert.AreEqual(10, h.Player.Floor);
        Assert.AreEqual(".", h.Floor.At(fairy), "妖精は生き返らない");
        Assert.AreEqual(".", h.Floor.At(gem), "褒美は生き返らない");
        Assert.AreNotEqual(".", h.Floor.At(stair), "階段は出たまま");
    }

    /// <summary>印が遊び手の旗に載る（原典と同じ1本の配列）。</summary>
    [Test]
    public void 解錠の印は遊び手の旗に控える()
    {
        var g = At(10, new Vector2Int(-5, -5));
        Assert.IsFalse(g.Player.EventFlags.Contains(0x5c));
        BeatBoss(g.FloorAt(10));
        g.EnterFloor(10, new Vector2Int(-5, -5));
        Assert.IsTrue(g.Player.EventFlags.Contains(0x5c), "3x3 が床になった印");
        Assert.IsTrue(g.Player.EventFlags.Contains(0x5d), "褒美を出した印");
    }

    /// <summary>
    /// 40階だけは、前提（突撃の 0x79）と自分の印（0xc8）が別の番号になっている。
    /// 同じにすると「前提が立った瞬間に自分も立っている」ことになって永久に出ない。
    /// </summary>
    [Test]
    public void 四十階の褒美は突撃のあとに一度だけ出る()
    {
        var reward = new Vector2Int(0, 0);              // 妖精(ID23)
        var stair  = new Vector2Int(0, 5);              // 上り階段

        var g = At(40, new Vector2Int(0, -5));
        g.FloorAt(40).Clear(reward);                    // 突撃のあと (0,0) は床
        g.EnterFloor(40, new Vector2Int(0, -5));
        Assert.AreEqual(".", g.Floor.At(reward), "突撃の印が無いうちは出ない");

        g.Player.EventFlags.Add(0x79);                  // 突撃（位置イベント121）が済んだ
        g.EnterFloor(40, new Vector2Int(0, -5));
        Assert.AreNotEqual(".", g.Floor.At(reward), "妖精が出る");
        Assert.AreNotEqual(".", g.Floor.At(stair), "上り階段が出る");

        g.Floor.Clear(reward);
        g.WriteResume();

        var h = At(1, new Vector2Int(0, -5));
        Assert.IsTrue(h.LoadSlot(TowerSave.AutoSlot));
        Assert.AreEqual(".", h.Floor.At(reward), "読み直しても生き返らない");
    }
}
