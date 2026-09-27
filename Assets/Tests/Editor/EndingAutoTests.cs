using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 終幕は**キーを待たずにひとりでに進む**（原典 0xc4d8 の後半）。
/// キー待ち（`lcall 0x107e,0x4fa`）が出てくるのは 0xd274 のいちばん最後だけで、
/// そこから先が「Ｈｉｔ　ａｎｙ　ｋｅｙ！」の最後の画面。
/// </summary>
public class EndingAutoTests
{
    [Test]
    public void 塔の絵は動きが終わってから進む()
    {
        float t = Ending.AutoSeconds(TowerGame.PageKind.EndingTower, 0);
        if (AuthorStyle.JavaEnding)
        {
            // Java版は15枚のモザイク送り。全部出し終える前に進んではいけない
            Assert.Greater(t, Ending.JavaFrames * Ending.JavaFrameSeconds);
            Assert.AreEqual(Ending.JavaMovieSeconds, t, 0.001f);
        }
        else
        {
            Assert.Greater(t, Ending.TowerDuration, "13秒の流しが終わる前に進んではいけない");
            Assert.AreEqual(Ending.TowerDuration + Ending.TowerHoldSeconds, t, 0.001f);
        }
    }

    [Test]
    public void 終わりの言葉は一枚ずつひとりでに進む()
    {
        for (int i = 0; i < Ending.WordsPages; i++)
            Assert.Greater(Ending.AutoSeconds(TowerGame.PageKind.EndingText, i), 0f, $"{i}枚目");
        if (AuthorStyle.JavaEnding)
            Assert.AreEqual(1, Ending.WordsPages, "Java版の終わりの言葉は1枚絵");
        else
            // 最後の1枚はいちばん長く見せる（4段落そろったところ）
            Assert.AreEqual(Ending.WordsSeconds[Ending.WordsSeconds.Length - 1],
                            Max(Ending.WordsSeconds));
    }

    static float Max(float[] v)
    {
        float m = v[0];
        foreach (var x in v) if (x > m) m = x;
        return m;
    }

    [Test]
    public void 最後の画面だけは触るまで待つ()
    {
        Assert.AreEqual(0f, Ending.AutoSeconds(TowerGame.PageKind.FinalScreen, 0),
                        "原典 0xd274 のキー待ちがここ");
        Assert.AreEqual(0f, Ending.AutoSeconds(TowerGame.PageKind.Dialogue, 0), "会話は触って送る");
        Assert.AreEqual(0f, Ending.AutoSeconds(TowerGame.PageKind.GameOver, 0));
    }

    /// <summary>
    /// ★**ゼノの最後のセリフから「Ｆｉｎ」までは触っても飛ばせない**（作者の指定）。
    /// </summary>
    [Test]
    public void ゼノの最後のセリフはひとりでに進む()
    {
        Assert.AreEqual(170, Ending.LastZenoRecord, "終幕の最後のセリフは170");
        Assert.Greater(Ending.AutoSeconds(TowerGame.PageKind.Dialogue, Ending.LastZenoRecord), 0f,
                       "ここから先は触っても飛ばせない");
        Assert.AreEqual(0f, Ending.AutoSeconds(TowerGame.PageKind.Dialogue, 169),
                        "その手前までは触って送る");
    }

    /// <summary>そこから「Ｆｉｎ」まで、間に触って送る画面が1つも無い。</summary>
    [Test]
    public void 最後のセリフからＦｉｎまで一本で流れる()
    {
        var steps = Cutscenes.Instance.Steps(Cutscenes.EndingScene);
        Assert.IsNotNull(steps);
        int from = steps.IndexOf(Ending.LastZenoRecord.ToString());
        Assert.Greater(from, 0, "終幕の台本に170がある");
        Assert.AreEqual(steps.Count - 1, steps.IndexOf(Cutscenes.FinalScreenStep),
                        "最後の画面が台本の終わり");
        for (int i = from + 1; i < steps.Count - 1; i++)
        {
            int id;
            Assert.IsFalse(int.TryParse(steps[i], out id),
                           $"170 と Ｆｉｎ の間にセリフ（{steps[i]}）があると、そこで止まる");
        }
    }

    [Test]
    public void Ｆｉｎは二秒たってから触れる()
    {
        // 終幕が流れ切ったところで指が当たって飛んでしまうのを防ぐ間（作者の指定）
        Assert.AreEqual(2f, Ending.FinalTapDelaySeconds);
        Assert.IsTrue(AuthorStyle.EndingRunsByItself);
    }
}
