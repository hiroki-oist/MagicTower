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

    [Test]
    public void たどると盤面の中に収まる()
    {
        // 40階の上のほう（(0,+4)）から始めると、下へ降りて最後に少し戻る形になる
        var trace = KnightCharge.Instance.Trace(new Vector2Int(0, 4));
        Assert.AreEqual(KnightCharge.Instance.StepCount, trace.Count);
        int lowest = int.MaxValue;
        foreach (var p in trace) lowest = Mathf.Min(lowest, p.y);
        Assert.Less(lowest, 4, "下へ進む");
        Assert.Greater(trace[trace.Count - 1].y, lowest, "最後は少し上へ戻る");
    }
}
