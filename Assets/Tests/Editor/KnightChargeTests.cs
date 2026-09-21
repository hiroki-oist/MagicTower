using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 金騎士の突撃の道順。原典 MTE.EXE 0x325e の向きの表そのもの。
/// </summary>
public class KnightChargeTests
{
    [Test]
    public void 道順は八十九歩の十四まとまり()
    {
        var k = KnightCharge.Instance;
        Assert.AreEqual(89 - 14, k.StepCount, "89バイトのうち14個は区切り");
        Assert.AreEqual(14, k.RunCount);
    }

    /// <summary>原典の Y は下へ増えるので、盤面では向きが逆になる。</summary>
    [Test]
    public void 向きの読み替え()
    {
        Assert.AreEqual(new Vector2Int(1, 0), KnightCharge.DirOf(1), "X+1 は右");
        Assert.AreEqual(new Vector2Int(-1, 0), KnightCharge.DirOf(2), "X-1 は左");
        Assert.AreEqual(new Vector2Int(0, -1), KnightCharge.DirOf(3), "原典の Y+1 は盤面の下");
        Assert.AreEqual(new Vector2Int(0, 1), KnightCharge.DirOf(4), "原典の Y-1 は盤面の上");
        Assert.AreEqual(Vector2Int.zero, KnightCharge.DirOf(5), "5 は区切り");
    }

    /// <summary>ジグザグに降りていって、最後にまっすぐ下りてから上へ戻る。</summary>
    [Test]
    public void ジグザグに降りて最後に上へ戻る()
    {
        var runs = new System.Collections.Generic.List<KnightCharge.Run>(KnightCharge.Instance.Runs);
        var first = runs[0].Steps;
        Assert.AreEqual(new Vector2Int(0, -1), first[0], "はじめは下へ");

        var last = runs[runs.Count - 1].Steps;
        Assert.AreEqual(5, last.Count, "最後のまとまりは5歩");
        foreach (var d in last) Assert.AreEqual(new Vector2Int(0, 1), d, "最後は上へ戻る");

        var beforeLast = runs[runs.Count - 2].Steps;
        Assert.AreEqual(4, beforeLast.Count);
        foreach (var d in beforeLast) Assert.AreEqual(new Vector2Int(0, -1), d, "その前はまっすぐ下へ");
    }

    /// <summary>
    /// 振るたびに始まりへ戻る（原典 0x35e0〜0x35e7）ので、14回とも盤面に収まる。
    /// 1振り目だけは門のマス (0,-2) から、2振り目からは中央 (0,0) から。
    /// </summary>
    [Test]
    public void どの振りも盤面の中に収まる()
    {
        var k = KnightCharge.Instance;
        Assert.AreEqual(new Vector2Int(0, -2), KnightCharge.FirstStart);
        Assert.AreEqual(new Vector2Int(0, 0), KnightCharge.RunStart);

        var all = k.Trace();
        Assert.AreEqual(k.StepCount, all.Count);
        foreach (var p in all)
        {
            Assert.IsTrue(p.x >= FloorData.MinX && p.x <= FloorData.MaxX, $"x={p.x}");
            Assert.IsTrue(p.y >= FloorData.MinY && p.y <= FloorData.MaxY, $"y={p.y}");
        }

        var first = k.TraceRun(0);
        Assert.AreEqual(new Vector2Int(0, -3), first[0], "門から一歩下へ");
        var last = k.TraceRun(k.RunCount - 1);
        Assert.AreEqual(new Vector2Int(0, 5), last[last.Count - 1], "最後は中央から上へ5歩");
    }
}
