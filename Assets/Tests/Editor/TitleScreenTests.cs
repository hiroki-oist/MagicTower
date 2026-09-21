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
            // 地図の書き換えは途中に挟まるので、それを除いた並びで見る
            var seq = new System.Collections.Generic.List<string>();
            foreach (var s in line.Split('|')[2].Split(';'))
            {
                var t = s.Trim();
                if (t.Length > 0 && !t.StartsWith("地図 ")) seq.Add(t);
            }
            int i = seq.IndexOf("111");
            CollectionAssert.AreEqual(
                new[] { "111", "閃光", "題名", "序章の絵", "奪われる", "112" },
                seq.GetRange(i, 6));
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
