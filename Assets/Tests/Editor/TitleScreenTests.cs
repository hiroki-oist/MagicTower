using NUnit.Framework;

/// <summary>題名画面が原典（MTE.EXE 0x6db2）どおりかを確かめる。</summary>
public class TitleScreenTests
{
    [Test]
    public void 題名の文字が原典の文字列と一致する()
    {
        Assert.AreEqual("Ｔｏｗｅｒ　ｏｆ　ｔｈｅ　Ｓｏｒｃｅｒｅｒ", TitleScreen.Title);
        Assert.AreEqual("　　　　　　　　　　（Ｃ）１９９６　Ｎ．Ｗ", TitleScreen.Copyright);
        Assert.AreEqual(21, TitleScreen.Title.Length, "全角21文字");
        Assert.AreEqual(21, TitleScreen.Copyright.Length);
    }

    [Test]
    public void 一九九六年の表記は動画の題名画面と同じ()
    {
        StringAssert.Contains("１９９６", TitleScreen.Copyright);
        StringAssert.Contains("Ｎ．Ｗ", TitleScreen.Copyright);
    }

    [Test]
    public void 題名は序章の百十一と百十二の間に入る()
    {
        Assert.AreEqual(111, TitleScreen.AfterRecord);
        Assert.AreEqual(112, TitleScreen.NextRecord);
        CollectionAssert.Contains(Prologue.BeforeFall, TitleScreen.AfterRecord);
        CollectionAssert.Contains(Prologue.AfterFall, TitleScreen.NextRecord);
    }

    [Test]
    public void 場面の表にも題名が入っている()
    {
        var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Cutscenes");
        Assert.IsNotNull(asset);
        foreach (var raw in asset.text.Split('\n'))
        {
            var line = raw.Trim();
            if (!line.StartsWith("ゼノとの初対面")) continue;
            var seq = line.Split('|')[2];
            StringAssert.Contains("111 ; 閃光 ; 題名 ; 序章の絵 ; 奪われる ; 112", seq.Replace("  ", " "));
            return;
        }
        Assert.Fail("ゼノとの初対面の行が見つからない");
    }

    [Test]
    public void 待ち時間が原典の値と一致する()
    {
        CollectionAssert.AreEqual(new[] { 60, 120, 60 }, TitleScreen.Waits);
    }
}
