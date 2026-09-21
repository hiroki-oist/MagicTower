using NUnit.Framework;
using UnityEngine;

/// <summary>終幕が原典（MTE.EXE 0xc4d8 / 0xd284 / 0xd45c）どおりかを確かめる。</summary>
public class EndingTests
{
    [Test]
    public void 最後の画面の文字が原典と一致する()
    {
        Assert.AreEqual("迷宮ＲＰＧ『魔法の塔』", Ending.TitleLine);
        Assert.AreEqual("Ｈｉｔ　ａｎｙ　ｋｅｙ！", Ending.AnyKeyLine);
        Assert.AreEqual(new Vector2Int(0x1d, 0xaa), Ending.TitlePos);
        Assert.AreEqual(new Vector2Int(0x1c, 0xd2), Ending.AnyKeyPos);
    }

    [Test]
    public void 決着のセリフは百六十一から百七十まで連続()
    {
        Assert.AreEqual(10, Ending.FinaleRecords.Length);
        for (int i = 0; i < Ending.FinaleRecords.Length; i++)
            Assert.AreEqual(161 + i, Ending.FinaleRecords[i]);
    }

    [Test]
    public void 終幕のセリフは全部原典にある()
    {
        foreach (var r in Ending.Records())
            Assert.IsTrue(GameText.Instance.Dialogue(r).HasValue, $"セリフ{r}");
    }

    [Test]
    public void 大魔導師の口上は四行()
    {
        var body = GameText.Instance.Dialogue(Ending.ArchmageRecord).Value.Body;
        Assert.AreEqual(4, DialogueBox.WindowLines(body), "原典の窓も4行");
        StringAssert.Contains("よくぞ、ここまで", body);
    }

    [Test]
    public void 使われないセリフは実在するが出番が無い()
    {
        foreach (var r in Ending.UnusedRecords)
            Assert.IsTrue(GameText.Instance.Dialogue(r).HasValue,
                          $"セリフ{r} はデータにはある");
        CollectionAssert.DoesNotContain(Ending.UnusedRecords, 155);
        CollectionAssert.DoesNotContain(Ending.UnusedRecords, 170);
    }

    [Test]
    public void 終幕で使う絵が揃っている()
    {
        Assert.AreEqual(4, Ending.Images.Length);
        foreach (var n in Ending.Images)
            Assert.IsTrue(System.IO.File.Exists($"Assets/Sprites/Screens/{n}.png"), n);
    }

    [Test]
    public void 語りのセリフ百八十はこの版では出ない()
    {
        // 「ながい戦いは終わりを告げ、虚栄の塔は崩れ去る。」
        var body = GameText.Instance.Dialogue(180).Value.Body;
        StringAssert.Contains("ながい戦いは終わりを告げ", body);
        CollectionAssert.Contains(Ending.UnusedRecords, 180);
    }
}
