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
    public void 塔の絵は流し終わってから進む()
    {
        float t = Ending.AutoSeconds(TowerGame.PageKind.EndingTower, 0);
        Assert.Greater(t, Ending.TowerDuration, "13秒の流しが終わる前に進んではいけない");
        Assert.AreEqual(Ending.TowerDuration + Ending.TowerHoldSeconds, t, 0.001f);
    }

    [Test]
    public void 終わりの言葉は一枚ずつひとりでに進む()
    {
        for (int i = 0; i < Ending.WordsSeconds.Length; i++)
            Assert.Greater(Ending.AutoSeconds(TowerGame.PageKind.EndingText, i), 0f, $"{i}枚目");
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
}
