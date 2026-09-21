using NUnit.Framework;
using TMPro;
using UnityEngine;

/// <summary>
/// 文字の大きさが原典の欄に収まっているか、TextMeshPro に実際に組ませて測る。
/// 原典の全角は 16x16 ドットで、欄の大きさはすべてその倍数（情報欄 96x128 ＝ 6文字×8行）。
/// </summary>
public class TextMetricsTests
{
    static TMP_FontAsset Font()
    {
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:TMP_FontAsset"))
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("PixelMplus"))
                return UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }
        return null;
    }

    static Vector2 Measure(string text, float lineSpacing)
    {
        var go = new GameObject("measure", typeof(RectTransform));
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = Font();
        t.fontSize = ScreenLayout.TextSizeOnCanvas;
        t.lineSpacing = lineSpacing;
        t.enableWordWrapping = false;
        t.text = text;
        var size = t.GetPreferredValues();
        Object.DestroyImmediate(go);
        return size;
    }

    [Test]
    public void 全角4文字が能力値の欄に収まる()
    {
        var box = ScreenLayout.SizeOnCanvas(ScreenLayout.HpBox);
        var size = Measure("９９９９", TowerHud.InfoLineSpacing);
        Assert.LessOrEqual(size.x, box.x + 1f, $"横幅 {size.x} が欄 {box.x} を超える");
    }

    [Test]
    public void 情報欄の六文字が欄の幅に収まる()
    {
        var box = ScreenLayout.SizeOnCanvas(ScreenLayout.MessageBox);
        var size = Measure("あいうえおか", TowerHud.InfoLineSpacing);
        Assert.LessOrEqual(size.x, box.x + 1f, $"横幅 {size.x} が欄 {box.x} を超える");
    }

    [Test]
    public void 行送りが原典の二十ドットになっている()
    {
        // 1行と2行の高さの差＝行送り。原典は20ドット＝Canvas で 36。
        var one = Measure("あ", TowerHud.InfoLineSpacing);
        var two = Measure("あ\nあ", TowerHud.InfoLineSpacing);
        var advance = ScreenLayout.LineAdvance * DialogueWindow.CanvasScale;
        Assert.AreEqual(advance, two.y - one.y, 0.5f,
                        $"行送りが {two.y - one.y}（{advance} のはず）");
    }
}

/// <summary>
/// 情報欄の折り返し（原典 0x9d80）。6桁×8行、メッセージは1つだけ。
/// </summary>
public class InfoBoxTests
{
    [Test]
    public void 六文字で折り返す()
    {
        // 「長剣を手に入れた！」は9文字 → 6 + 3
        Assert.AreEqual("長剣を手に入\nれた！", TowerHud.InfoBox("長剣を手に入れた！"));
    }

    [Test]
    public void 改行が来たらその行はそこで終わる()
    {
        Assert.AreEqual("あい\nうえお", TowerHud.InfoBox("あい\nうえお"));
    }

    [Test]
    public void 半角の空白も一桁を使う()
    {
        // あ,空,い,空,う,空 で6桁ぶん。7つ目の「え」から次の行
        Assert.AreEqual("あ い う \nえ", TowerHud.InfoBox("あ い う え"));
    }

    [Test]
    public void タブは桁を使わずに読み飛ばす()
    {
        Assert.AreEqual("あいうえおか\nき", TowerHud.InfoBox("あい\tうえおかき"));
    }

    [Test]
    public void 八行までしか出さない()
    {
        var src = new string('あ', 6 * 10);
        var wrapped = TowerHud.InfoBox(src);
        Assert.AreEqual(TowerHud.InfoLines, wrapped.Split('\n').Length);
    }

    [Test]
    public void 空でも落ちない()
    {
        Assert.AreEqual("", TowerHud.InfoBox(null));
        Assert.AreEqual("", TowerHud.InfoBox(""));
    }
}
