using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 文字を出す位置。原典の描画（linear 0x10883）は `VRAM位置 = y × 80 + x` で書くので、
/// **x はバイト（＝8ドット）、y はドット**。
/// </summary>
public class ScreenTextTests
{
    [Test]
    public void 位置の単位はバイトとドット()
    {
        Assert.AreEqual(new Vector2Int(144, 160), ScreenLayout.FromTextPos(0x12, 0xa0));
        Assert.AreEqual(16, ScreenLayout.TextCharWidth, "全角1文字で2バイト進む");
        // 1行は80バイト＝640ドット
        Assert.AreEqual(ScreenLayout.ScreenWidth, ScreenLayout.FromTextPos(80, 0).x);
    }

    /// <summary>原典 0x6db2 / 0x6dcd が渡す値そのもの。</summary>
    [Test]
    public void 題名の位置は原典の値()
    {
        Assert.AreEqual(0x12, TitleScreen.TitleX, "x はバイト");
        Assert.AreEqual(0xa0, TitleScreen.TitleY, "y はドット");
        Assert.AreEqual(0x12, TitleScreen.CopyrightX);
        Assert.AreEqual(0xbe, TitleScreen.CopyrightY);
        Assert.AreEqual(new Vector2Int(144, 160), TitleScreen.TitleDot);
        Assert.AreEqual(new Vector2Int(144, 190), TitleScreen.CopyrightDot);
        Assert.AreEqual(30, TitleScreen.CopyrightDot.y - TitleScreen.TitleDot.y,
                        "2行の間は30ドット");
    }

    /// <summary>題名も終幕の文字も、画面のほぼ中央に来る。</summary>
    [Test]
    public void 文字は画面の中央あたりに来る()
    {
        int mid = ScreenLayout.ScreenWidth / 2;
        int titleCenter = TitleScreen.TitleDot.x
                        + TitleScreen.Title.Length * ScreenLayout.TextCharWidth / 2;
        Assert.AreEqual(mid, titleCenter, 16, "題名");

        int endCenter = Ending.TitleDot.x
                      + Ending.TitleLine.Length * ScreenLayout.TextCharWidth / 2;
        Assert.AreEqual(mid, endCenter, 16, "終幕の題名");

        int keyCenter = Ending.AnyKeyDot.x
                      + Ending.AnyKeyLine.Length * ScreenLayout.TextCharWidth / 2;
        Assert.AreEqual(mid, keyCenter, 16, "Hit any key");
    }

    [Test]
    public void 終幕の位置は原典の値()
    {
        Assert.AreEqual(new Vector2Int(0x1d, 0xaa), Ending.TitlePos);
        Assert.AreEqual(new Vector2Int(0x1c, 0xd2), Ending.AnyKeyPos);
        Assert.AreEqual(new Vector2Int(232, 170), Ending.TitleDot);
        Assert.AreEqual(new Vector2Int(224, 210), Ending.AnyKeyDot);
    }

    /// <summary>組み立てたシーンに題名と終幕の画面があり、文字が原典の位置にある。</summary>
    [Test]
    public void 場面に題名と終幕の画面がある()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            RectTransform title = null, ending = null, titleLine = null, endLine = null;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var rt in go.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt.name == "TitleScreen") title = rt;
                    if (rt.name == "EndingScreen") ending = rt;
                    if (rt.name == "TitleScreen_Line1") titleLine = rt;
                    if (rt.name == "EndingScreen_Line1") endLine = rt;
                }

            Assert.IsNotNull(title, "題名の画面が無い");
            Assert.IsNotNull(ending, "終幕の画面が無い");
            Assert.IsFalse(title.gameObject.activeSelf, "ふだんは隠れている");
            Assert.IsFalse(ending.gameObject.activeSelf, "ふだんは隠れている");

            Assert.IsNotNull(titleLine);
            Assert.AreEqual(ScreenLayout.ToCanvas(
                                new RectInt(TitleScreen.TitleDot.x, TitleScreen.TitleDot.y, 0, 0)),
                            titleLine.anchoredPosition, "題名の文字の位置");
            Assert.IsNotNull(endLine);
            Assert.AreEqual(ScreenLayout.ToCanvas(
                                new RectInt(Ending.TitleDot.x, Ending.TitleDot.y, 0, 0)),
                            endLine.anchoredPosition, "終幕の文字の位置");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }

    /// <summary>画面の外にはみ出さない。</summary>
    [Test]
    public void 文字は画面に収まる()
    {
        foreach (var t in new[]
        {
            new { Pos = TitleScreen.TitleDot, Text = TitleScreen.Title },
            new { Pos = TitleScreen.CopyrightDot, Text = TitleScreen.Copyright },
            new { Pos = Ending.TitleDot, Text = Ending.TitleLine },
            new { Pos = Ending.AnyKeyDot, Text = Ending.AnyKeyLine },
        })
        {
            int right = t.Pos.x + t.Text.Length * ScreenLayout.TextCharWidth;
            Assert.LessOrEqual(right, ScreenLayout.ScreenWidth, t.Text);
            Assert.LessOrEqual(t.Pos.y, ScreenLayout.ScreenHeight, t.Text);
        }
    }
}
