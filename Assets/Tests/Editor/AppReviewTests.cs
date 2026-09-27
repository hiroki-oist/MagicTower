using NUnit.Framework;

/// <summary>
/// ★評価のお願い（作者の指定。原典に無い）。
///
/// ⚠ **出したかどうか・断られたかどうかはアプリからは分からない**。
/// iOS の窓は結果を返さないので、こちらで決められるのは
/// 「何回目のクリアで頼むか」だけ。実際に出すかどうかは OS が決める
/// （すでに評価した人・年3回を超える人には出ない）。
/// </summary>
public class AppReviewTests
{
    [TearDown]
    public void TearDown() { AppReview.Forget(); }

    [Test]
    public void 頼むのは一回目と三回目と六回目と十回目のクリア()
    {
        CollectionAssert.AreEqual(new[] { 1, 3, 6, 10 }, AppReview.AskOnClears);
        for (int n = 0; n <= 12; n++)
        {
            bool want = n == 1 || n == 3 || n == 6 || n == 10;
            Assert.AreEqual(want, AppReview.ShouldAsk(n), $"{n}回目");
        }
    }

    [Test]
    public void クリアの回数は端末に残る()
    {
        AppReview.Forget();
        Assert.AreEqual(0, AppReview.Clears, "はじめは0");
        AppReview.AfterClear();
        Assert.AreEqual(1, AppReview.Clears, "クリアするたびに増える");
        AppReview.AfterClear();
        Assert.AreEqual(2, AppReview.Clears);
    }

    [Test]
    public void 同じ起ち上げでは一度しか頼まない()
    {
        AppReview.Forget();
        Assert.IsFalse(AppReview.Asked);
        AppReview.Request();
        Assert.IsTrue(AppReview.Asked);
        AppReview.Request();                       // 二度目は何もしない
        Assert.IsTrue(AppReview.Asked);
    }

    /// <summary>自分の文面で誘いたいときの行き先も持っている。</summary>
    [Test]
    public void レビュー画面の行き先を持っている()
    {
        StringAssert.Contains("action=write-review", AppReview.WriteReviewUrl);
        StringAssert.Contains("6815624226", AppReview.WriteReviewUrl);
    }
}
