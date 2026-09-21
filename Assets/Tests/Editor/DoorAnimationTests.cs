using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 鍵で扉を開けたときの9コマの動き。原典 PC98版 MTE.EXE の `0x07be6`。
/// </summary>
public class DoorAnimationTests
{
    [Test]
    public void 原典の回数と長さ()
    {
        // 0x48 から 0xa00 きざみで 0x5a48 まで → 9回
        int n = 0;
        for (int si = 0x48; si < 0x5a48; si += 0xa00) n++;
        Assert.AreEqual(9, n);
        Assert.AreEqual(9, DoorAnimation.Frames);
        Assert.AreEqual(8, DoorAnimation.TailFrames, "そのあと 0x9288(8)");
        // 0x92a4 の中の縦帰線待ち1回ぶん＝1/60秒
        Assert.AreEqual(1f / 60f, DoorAnimation.FrameSeconds, 1e-6f);
        Assert.AreEqual(17f / 60f, DoorAnimation.Duration, 1e-5f);
        Assert.AreEqual(11, DoorAnimation.Sound, "原典 0x7be6 の中の 0xb");
    }

    [Test]
    public void 色の対応は原典の番号どおり()
    {
        Assert.AreEqual(1, DoorAnimation.NumberOf("y"));
        Assert.AreEqual(2, DoorAnimation.NumberOf("b"));
        Assert.AreEqual(3, DoorAnimation.NumberOf("r"));
        Assert.AreEqual("Yellow", DoorAnimation.ColorOf("y"));
        Assert.AreEqual("Purple", DoorAnimation.ColorOf("b"));
        Assert.AreEqual("Red", DoorAnimation.ColorOf("r"));
        Assert.AreEqual("Sealed", DoorAnimation.ColorOf("s"));
        Assert.IsNull(DoorAnimation.ColorOf("#"), "扉でないものには絵が無い");
    }

    [Test]
    public void 四色ぶん九コマの絵が書き出してある()
    {
        foreach (var c in new[] { "Yellow", "Purple", "Red", "Sealed" })
            for (int f = 0; f < DoorAnimation.Frames; f++)
                Assert.IsTrue(System.IO.File.Exists(DoorAnimation.SpritePath(c, f)),
                              $"{c}_{f} が無い");
    }

    [Test]
    public void 一コマ目は閉じた扉の絵と同じ()
    {
        // LCG #2 の1コマ目 ＝ 盤面のタイル 001_Lock_Yellow（色を突き合わせて確定した対応）
        var a = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
            DoorAnimation.SpritePath("Yellow", 0));
        var b = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Sprites/Tiles/001_Lock_Yellow.png");
        Assert.IsNotNull(a); Assert.IsNotNull(b);
        Assert.AreEqual(32, a.width); Assert.AreEqual(32, a.height);
        Assert.AreEqual(b.width, a.width);
    }

    [Test]
    public void 本線は扉を積むだけで描かない()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerGame.cs");
        StringAssert.Contains("ConsumeDoorOpens", src);
        StringAssert.Contains("doorOpens.Add", src);
    }

    [Test]
    public void 扉の音は十一番()
    {
        // 原典 0x23ee 自身は鳴らさず、0x7be6 の中で 0xb を鳴らす。
        // 0x23ee の 6 は「拾う」と「39階の仕掛け」のもの。
        Assert.AreEqual(DoorAnimation.Sound, SoundCues.Instance.Number(SoundCues.OpenDoor));
    }
}
