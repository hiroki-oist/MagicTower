using NUnit.Framework;
using UnityEngine;

/// <summary>会話ウィンドウの寸法が原典（MTE.EXE 0x94ba）どおりかを確かめる。</summary>
public class DialogueWindowTests
{
    [Test]
    public void 窓は画面中央寄りの横320ドット()
    {
        Assert.AreEqual(640, DialogueWindow.ScreenWidth);
        Assert.AreEqual(160, DialogueWindow.Left, "原典 0x14 バイト = 160 ドット");
        Assert.AreEqual(320, DialogueWindow.Width, "原典 0x14 ワード = 320 ドット");
        Assert.AreEqual(DialogueWindow.Left + DialogueWindow.Width,
                        DialogueWindow.ScreenWidth - DialogueWindow.Left, "左右の余白が同じ");
    }

    [Test]
    public void 高さは行数の二十倍足す三十二()
    {
        // 本体の塗りは「16行ぶんを4行ずつずらして 5*行数+1 回」なので、
        // 届く下端は 32 + 20*行数。帯（上端から32行）も含めた大きさ。
        Assert.AreEqual(52, DialogueWindow.Height(1));
        Assert.AreEqual(72, DialogueWindow.Height(2));
        Assert.AreEqual(152, DialogueWindow.Height(6));
        Assert.AreEqual(192, DialogueWindow.Height(8));
        Assert.AreEqual(212, DialogueWindow.Height(9));
    }

    [Test]
    public void 文字は窓の左上から十六と十二の内側に出る()
    {
        // 原典 0x9563〜0x9571: x=0x16 バイト＝176（左端160の16ドット内側）、y=上端+0xc
        Assert.AreEqual(16, DialogueWindow.TextLeft);
        Assert.AreEqual(12, DialogueWindow.TextTop);
        Assert.AreEqual(176, DialogueWindow.Left + DialogueWindow.TextLeft);
    }

    [Test]
    public void 行送りは二十ドット()
    {
        // lcall 0x107e,0 の第1引数が 0x14（0x66ed / 0xc47e）
        Assert.AreEqual(20, ScreenLayout.LineAdvance);
        // 文字は16ドットなので4ドットの空きが付く
        Assert.AreEqual(4, ScreenLayout.LineAdvance - ScreenLayout.TextCharWidth);
    }

    [Test]
    public void 一番下の行は窓の中に収まる()
    {
        for (int n = 1; n <= 11; n++)
        {
            int bottom = DialogueWindow.TextTop
                       + ScreenLayout.LineAdvance * (n - 1) + ScreenLayout.TextCharWidth;
            Assert.LessOrEqual(bottom, DialogueWindow.Height(n), $"{n}行");
        }
    }

    [Test]
    public void 原典で使われる行数はどれも画面に収まる()
    {
        for (int n = 1; n <= 11; n++)
        {
            Assert.IsTrue(DialogueWindow.Fits(DialogueWindow.For(n)), $"{n}行");
            Assert.IsTrue(DialogueWindow.Fits(DialogueWindow.For(n, true)), $"{n}行（高い位置）");
        }
    }

    [Test]
    public void 姿の見えない声は上端五十に開く()
    {
        Assert.AreEqual(120, DialogueWindow.For(6).yMin);
        Assert.AreEqual(50, DialogueWindow.For(6, true).yMin);
    }

    [Test]
    public void 十階の盗賊は九行の窓を高い位置に開く()
    {
        var body = GameText.Instance.Dialogue(107).Value.Body;
        var r = DialogueWindow.ForBody(body, high: true);
        Assert.AreEqual(9, DialogueBox.WindowLines(body));
        Assert.AreEqual(new RectInt(160, 50, 320, 212), r);
        Assert.IsTrue(DialogueWindow.Fits(r));
    }

    [Test]
    public void 一行のセリフは一番小さい窓()
    {
        var body = GameText.Instance.Dialogue(156).Value.Body;
        Assert.AreEqual(1, DialogueBox.WindowLines(body), "「うっ……この陣形は……まずいっ！」");
        Assert.AreEqual(new RectInt(160, 50, 320, 52),
                        DialogueWindow.ForBody(body, high: true));
    }

    [Test]
    public void 帯の大きさがLcg1と一致する()
    {
        Assert.AreEqual(320, DialogueWindow.Width);
        Assert.AreEqual(32, DialogueWindow.StripHeight);
        var tex = Resources.Load<Texture2D>("Screens/Lcg_1");
        if (tex != null)   // Resources 外に置いてある場合は飛ばす
        {
            Assert.AreEqual(320, tex.width);
            Assert.AreEqual(32, tex.height);
        }
    }

    // ---- Canvas への置き換え ----

    [Test]
    public void 原典の画面とCanvasの倍率が一定()
    {
        Assert.AreEqual(1.8f, DialogueWindow.CanvasScale, 1e-6f);
        Assert.AreEqual(1152f, DialogueWindow.CanvasWidth, 1e-3f);
        Assert.AreEqual(720f, DialogueWindow.CanvasHeight, 1e-3f);
    }

    [Test]
    public void 原典の座標がCanvasの位置に直る()
    {
        var r = DialogueWindow.For(6);                 // (160,120,320,124)
        Assert.AreEqual(new Vector2(288f, -216f), DialogueWindow.ToCanvas(r));
        Assert.AreEqual(new Vector2(576f, 273.6f), DialogueWindow.SizeOnCanvas(r));
    }

    [Test]
    public void 高い位置に開くセリフは原典の表どおり十件()
    {
        // 出どころは docs/original/dialogue_windows.txt（原典の呼び出しを全部拾った表）。
        // 場面の中から呼ばれるもの（110・130・131・150・151・155）が抜けていた。
        CollectionAssert.AreEquivalent(
            new[] { 53, 107, 110, 130, 131, 150, 151, 155, 156, 159 },
            DialogueWindowView.HighRecords);
        foreach (var id in DialogueWindowView.HighRecords)
            Assert.IsTrue(DialogueWindowView.HighFor(
                GameText.Instance.Dialogue(id).Value.Body), $"セリフ{id}");
        Assert.IsFalse(DialogueWindowView.HighFor(
            GameText.Instance.Dialogue(116).Value.Body), "普通のセリフは120");
    }

    [Test]
    public void 窓の色は原典のパレットから取る()
    {
        Assert.AreEqual(new Color32(0x44, 0x44, 0x44, 0xff), (Color32)DialogueWindowView.Fill);
        Assert.AreEqual(new Color32(0xff, 0xff, 0xff, 0xff), (Color32)DialogueWindowView.Text);
    }
}
