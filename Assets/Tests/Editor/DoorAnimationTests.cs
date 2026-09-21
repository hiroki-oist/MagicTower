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
    public void 門は後ろの間が長い()
    {
        // 門は別の関数（原典 0x07a68）だが 9コマ・音11・貼り方は同じで、
        // 後ろの間だけ 0x9288(0x14) と長い。鍵の扉は 0x9288(8)。
        Assert.AreEqual(8, DoorAnimation.TailOf("y"));
        Assert.AreEqual(8, DoorAnimation.TailOf("b"));
        Assert.AreEqual(8, DoorAnimation.TailOf("r"));
        Assert.AreEqual(0x14, DoorAnimation.TailOf("s"));
    }

    [Test]
    public void 門が開くと動きが積まれる()
    {
        // 2階の檻（特殊扉6枚）と同じ仕組み。解錠で門が床になったマスを拾う。
        var p = new PlayerState { Hp = 1000, Attack = 100, Defense = 100, Floor = 1,
                                  Position = new Vector2Int(0, 0) };
        var g = new TowerGame(p);
        var at = new Vector2Int(0, 1);
        g.Floor.Put(at, "s");
        g.ConsumeDoorOpens();
        // 門を直に床へ変えても積まれない（解錠が起きたときだけ）
        g.Floor.Clear(at);
        Assert.AreEqual(0, g.ConsumeDoorOpens().Count);
    }

    [Test]
    public void ボスが現れる十コマも書き出してある()
    {
        // LCG #6（480x192）は 96x96（3x3マス）が横5x縦2 の10コマ。原典 0x03044。
        for (int i = 0; i < 10; i++)
            Assert.IsTrue(System.IO.File.Exists($"Assets/Sprites/Effects/BossAppear_{i}.png"),
                          $"BossAppear_{i} が無い");
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

/// <summary>
/// 20階のボスが現れる10コマ。原典 PC98版 MTE.EXE の `0x03044`（0x3119〜0x3194）。
/// </summary>
public class BossAppearTests
{
    [Test]
    public void 原典の回数と待ちと大きさ()
    {
        // di: 0 → 0x1e00（縦2段）、si: 0,0xc,0x18,0x24,0x30（横5列）= 10コマ
        int n = 0;
        for (int di = 0; di < 0x3c00; di += 0x1e00)
            for (int si = 0; si < 0x3c; si += 0xc) n++;
        Assert.AreEqual(10, n);
        Assert.AreEqual(10, BossAppear.Frames);
        Assert.AreEqual(0x19, BossAppear.FrameWait, "1コマ 0x19 待つ");
        Assert.AreEqual(25f / 60f, BossAppear.FrameSeconds, 1e-5f);
        Assert.AreEqual(250f / 60f, BossAppear.Duration, 1e-4f);
        Assert.AreEqual(96, BossAppear.Size, "3x3マスぶん");
    }

    [Test]
    public void 貼る場所は盤面の中心()
    {
        // 原典の貼り先 0x2fa2 = y*80 + xバイト → y=152, x=34バイト=272ドット
        int vram = 0x2fa2;
        int y = vram / 80, xByte = vram % 80;
        Assert.AreEqual(152, y);
        Assert.AreEqual(34, xByte);
        // 盤面の式（x*32+144 / y*32+24）で解くと左上のマスは (-1,+1)
        int cellX = (xByte * 8 - 144) / 32, cellY = (y - 24) / 32;
        Assert.AreEqual(-1, cellX - 5);
        Assert.AreEqual(1, 5 - cellY);
        // その3x3の中心は (0,0)
        Assert.AreEqual(new Vector2Int(0, 0), BossAppear.Center);
    }

    [Test]
    public void 場面の表で門のあと三つ組の前に入る()
    {
        var steps = new System.Collections.Generic.List<string>(
            Cutscenes.Instance.Steps("ボス登場"));
        int i = steps.IndexOf(Cutscenes.BossAppearStep);
        Assert.Greater(i, 0, "ボスが現れる段が無い");
        StringAssert.Contains("=門", steps[i - 1], "退路の門を置いたあと");
        StringAssert.StartsWith(Cutscenes.MapStepPrefix, steps[i + 1], "三つ組の前");
    }

    [Test]
    public void 本線は場所を積むだけで描かない()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerGame.cs");
        StringAssert.Contains("ConsumeBossAppears", src);
        StringAssert.Contains("bossAppears.Add(BossAppear.Center)", src);
    }
}
