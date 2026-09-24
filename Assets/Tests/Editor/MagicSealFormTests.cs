using NUnit.Framework;
using UnityEngine;

/// <summary>
/// サイコロの５の目（原典 0x5cdb〜0x5e16）。
/// 上下左右の魔導師4体を倒し、**斜めの4体とゼノは残す**とゼノが10分の1になる。
/// 原典は主人公の立ち位置を見ていない（中央にはゼノが居るので立てない）。
/// </summary>
public class MagicSealFormTests
{
    static TowerGame OnNine()
    {
        var p = new PlayerState
        {
            Hp = 9999, Attack = 900, Defense = 900,
            Floor = 49, HighestFloor = 49, Position = new Vector2Int(0, 5),
        };
        var g = new TowerGame(p);
        // 大魔導師の口上のあとの形（ゼノと魔導師8体）を作る
        g.Floor.Put(MagicSeal.Center, MagicSeal.BossPrefab);
        foreach (var c in MagicSeal.Corners) g.Floor.Put(c, "Enemy_ID93");
        foreach (var e in MagicSeal.Edges) g.Floor.Put(e, "Enemy_ID93");
        return g;
    }

    [Test]
    public void 五の目は四隅の魔導師と中央のゼノ()
    {
        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(-1, 4), new Vector2Int(1, 4),
                    new Vector2Int(-1, 2), new Vector2Int(1, 2) }, MagicSeal.Corners);
        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(0, 4), new Vector2Int(-1, 3),
                    new Vector2Int(1, 3), new Vector2Int(0, 2) }, MagicSeal.Edges);
        Assert.AreEqual(new Vector2Int(0, 3), MagicSeal.Center);
    }

    [Test]
    public void 上下左右を倒すと封印が成立する()
    {
        var g = OnNine();
        Assert.IsFalse(g.CheckMagicSeal(), "まだ形になっていない");

        foreach (var e in MagicSeal.Edges) g.Floor.Clear(e);
        Assert.IsTrue(g.CheckMagicSeal(), "**立ち位置に関わらず**成立する");
        Assert.IsTrue(g.Player.EventFlags.Contains(MagicSeal.FlagId));
        StringAssert.Contains("この陣形は", string.Join("\n", g.Log));
        Assert.IsFalse(g.CheckMagicSeal(), "一度きり");
    }

    [Test]
    public void 斜めまで倒すと成立しない()
    {
        var g = OnNine();
        foreach (var e in MagicSeal.Edges) g.Floor.Clear(e);
        g.Floor.Clear(MagicSeal.Corners[0]);          // 斜めを1体倒してしまった
        Assert.IsFalse(g.CheckMagicSeal());
        Assert.IsFalse(g.Player.EventFlags.Contains(MagicSeal.FlagId));
    }

    [Test]
    public void 封印するとゼノが十分の一になる()
    {
        var g = OnNine();
        foreach (var e in MagicSeal.Edges) g.Floor.Clear(e);

        var before = TowerBattle.LoadEnemy(MagicSeal.BossPrefab);
        Assert.AreEqual(8000, before.MaxHP);
        Assert.AreEqual(5000, before.Attack);
        Assert.AreEqual(1000, before.Defense);

        g.CheckMagicSeal();
        int hp = before.MaxHP, atk = before.Attack, def = before.Defense;
        MagicSeal.Adjust(g.Player, MagicSeal.BossPrefab, ref hp, ref atk, ref def);
        Assert.AreEqual(800, hp);
        Assert.AreEqual(500, atk);
        Assert.AreEqual(100, def);
    }

    /// <summary>倒れずに勝てるのは封印したときだけ（実際に戦って確かめる）。</summary>
    [Test]
    public void 封印してから戦うと勝てる()
    {
        var g = OnNine();
        g.Player.Attack = 1100; g.Player.Defense = 1050; g.Player.Hp = 3000;

        var plain = TowerBattle.Resolve(new PlayerState
        {
            Hp = 3000, Attack = 1100, Defense = 1050
        }, MagicSeal.BossPrefab);
        Assert.IsFalse(plain.Won, "封印しないと勝てない");

        foreach (var e in MagicSeal.Edges) g.Floor.Clear(e);
        g.CheckMagicSeal();
        var sealed_ = TowerBattle.Resolve(g.Player, MagicSeal.BossPrefab);
        Assert.IsTrue(sealed_.Won, "封印すれば勝てる");
    }
}

/// <summary>
/// 台本の「待つ」（作者の指定）。絵を出してからセリフを出すまでの間を空ける。
/// 20階はバンパイアを地図に書いてから1秒おいてセリフ121を出す。
/// </summary>
public class ScenePauseTests
{
    [Test]
    public void 二十階はボスを出してから待ってセリフを出す()
    {
        var steps = Cutscenes.Instance.Steps("ボス登場");
        int boss = steps.FindIndex(t => t == Cutscenes.BossAppearStep);
        int put = steps.FindIndex(t => t.Contains("(+0,+0)=ID95"));
        int wait = steps.FindIndex(t => t.StartsWith(Cutscenes.WaitPrefix));
        int line = steps.FindIndex(t => t.Trim() == "121");

        Assert.Greater(boss, -1); Assert.Greater(put, -1);
        Assert.Greater(wait, -1, "待つ段がある");
        Assert.Greater(line, -1);
        Assert.Less(boss, put, "合体の動きのあとに盤面へ書く");
        Assert.Less(put, wait, "**書いてから**待つ（ここが逆だと絵と同時に喋る）");
        Assert.Less(wait, line, "待ってからセリフ");
        Assert.AreEqual("待つ 60", steps[wait], "1秒");
    }

    [Test]
    public void 待つ段は本線が受け取って下ろす()
    {
        var p = new PlayerState { Hp = 1000, Attack = 10, Defense = 10,
                                  Floor = 1, Position = new Vector2Int(0, -5) };
        var g = new TowerGame(p);
        Assert.AreEqual(0, g.ConsumePauseFrames());
    }
}
