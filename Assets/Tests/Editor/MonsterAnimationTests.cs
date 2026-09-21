using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 敵と人の2コマの動き（原典 0x92a4）。
/// </summary>
public class MonsterAnimationTests
{
    [Test]
    public void 数は原典どおり()
    {
        Assert.AreEqual(121, MonsterAnimation.Cells, "11×11、原典は 0x79 で戻す");
        Assert.AreEqual(5, MonsterAnimation.CellsPerTick, "1回で5マス（[bp-6]=5）");
        Assert.AreEqual(6, MonsterAnimation.TicksPerStep, "1歩で6回（0xa276 の中に6か所）");
    }

    [Test]
    public void マスの番号と座標が行優先で対応する()
    {
        Assert.AreEqual(new Vector2Int(-5, 5), MonsterAnimation.CellOf(0));
        Assert.AreEqual(new Vector2Int(5, 5), MonsterAnimation.CellOf(10));
        Assert.AreEqual(new Vector2Int(-5, 4), MonsterAnimation.CellOf(11));
        Assert.AreEqual(new Vector2Int(5, -5), MonsterAnimation.CellOf(120));
        for (int i = 0; i < MonsterAnimation.Cells; i++)
            Assert.AreEqual(i, MonsterAnimation.IndexOf(MonsterAnimation.CellOf(i)), $"{i}");
    }

    [Test]
    public void ひと押しで五マス進む()
    {
        var a = new MonsterAnimation();
        var t = a.Tick();
        Assert.AreEqual(5, t.Count);
        Assert.AreEqual(5, a.Cursor);
    }

    [Test]
    public void 百二十一マスなめ終わるとコマが入れ替わる()
    {
        var a = new MonsterAnimation();
        Assert.IsTrue(a.FirstFrame);
        // 121マスを越えるまで押す
        int ticks = 0;
        while (a.FirstFrame && ticks < 100) { a.Tick(); ticks++; }
        Assert.AreEqual(25, ticks, "5マスずつなので25回目で121を越える");
        Assert.IsFalse(a.FirstFrame);
    }

    [Test]
    public void およそ四歩に一度入れ替わる()
    {
        var a = new MonsterAnimation();
        int steps = 0;
        while (a.FirstFrame && steps < 20) { a.Step(); steps++; }
        Assert.AreEqual(5, steps, "1歩30マスなので5歩目で121を越える");
    }

    [Test]
    public void 切り替えは一斉ではなく舐めるように進む()
    {
        var a = new MonsterAnimation();
        // 121を越えて2コマ目に入った直後は、まだ1コマ目のままのマスが残っている
        for (int i = 0; i < 25; i++) a.Tick();
        Assert.IsFalse(a.FirstFrame);
        var first = MonsterAnimation.CellOf(0);
        var last = MonsterAnimation.CellOf(120);
        Assert.IsFalse(a.FrameAt(first), "なめ直したところは2コマ目");
        Assert.IsTrue(a.FrameAt(last), "まだ来ていないところは1コマ目のまま");
    }

    [Test]
    public void 二コマ目の絵が書き出してある()
    {
        foreach (var n in new[] { "062_Enemy_SlimeG_b", "020_Merchant_b" })
        {
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                $"Assets/Sprites/Tiles/{n}.png");
            Assert.IsNotNull(s, $"{n} が無い");
        }
    }
}
