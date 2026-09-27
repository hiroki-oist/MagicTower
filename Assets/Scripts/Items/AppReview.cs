using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// ★**評価のお願い**（作者の指定。原典に無い）。実体は
/// <c>Assets/Plugins/iOS/MagicTowerStore.swift</c> の <c>MTRequestReview</c>。
///
/// 出るのは **iOS の決まった窓**（SKStoreReviewController）で、
/// **文面・星・ボタンはすべて Apple のもの**。こちらからは変えられないし、
/// 似せた窓を自前で作ることも App Review ガイドラインで禁じられている。
/// **TestFlight のビルドでは何も出ない。**
///
/// ⚠ **「断られたか」「もう評価したか」はアプリからは分からない。**
/// この窓は結果を返さない（Apple がそう決めている）。分かるのは
/// 「こちらからお願いした」ことだけ。だから
///   ・何回目のクリアでお願いするか … こちらで決める（<see cref="AskOnClears"/>）
///   ・実際に出すかどうか           … OS が決める
/// という分担になる。OS の側にも歯止めがあり、**同じ人には年に3回まで**、
/// **すでに評価した人には出さない**。つまり「断った人・評価済みの人に
/// 出さない」は、こちらが何もしなくても OS が面倒を見てくれる。
///
/// 文面を自分で書きたいときは、この窓ではなく
/// <see cref="WriteReviewUrl"/> を開く形にする（そちらは普通のリンク）。
/// </summary>
public static class AppReview
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void MTRequestReview();
#endif

    /// <summary>
    /// App Store の「レビューを書く」画面を直接開く行き先。
    /// こちらは自分の文面で誘える（ボタンや会話から開いてよい）。
    /// 数字はこのアプリの Apple ID。
    /// </summary>
    public const string WriteReviewUrl =
        "https://apps.apple.com/app/id6815624226?action=write-review";

    /// <summary>
    /// **何回目のクリアでお願いするか**（作者の指定）。
    /// 1回目で断られても次は3回目、それも駄目なら6回目、最後に10回目。
    /// 断られたかどうかは分からないので、**この回数で機械的に**お願いし、
    /// 出す出さないは OS の判断に任せる。
    /// </summary>
    public static readonly int[] AskOnClears = { 1, 3, 6, 10 };

    /// <summary>クリアした回数の覚え書き（端末に残る）。</summary>
    public const string ClearsKey = "mt_clears";

    /// <summary>その回数のときにお願いするか。</summary>
    public static bool ShouldAsk(int clears)
    {
        foreach (var n in AskOnClears) if (n == clears) return true;
        return false;
    }

    /// <summary>これまでにクリアした回数。</summary>
    public static int Clears
    {
        get { return PlayerPrefs.GetInt(ClearsKey, 0); }
        set { PlayerPrefs.SetInt(ClearsKey, value); PlayerPrefs.Save(); }
    }

    /// <summary>もう頼んだか（同じ起ち上げで何度も呼ばないため）。</summary>
    public static bool Asked { get; private set; }

    /// <summary>
    /// **クリアして題名へ戻ったところ**で呼ぶ（作者の指定）。
    /// クリアの回数を1つ増やし、その回数が頃合いならお願いする。
    /// </summary>
    public static void AfterClear()
    {
        int clears = Clears + 1;
        Clears = clears;
        if (ShouldAsk(clears)) Request();
    }

    /// <summary>
    /// 評価の窓をお願いする。区切りのよい所で1回だけ呼ぶ
    /// （HIG: 押したら出るボタンにはしない）。
    /// </summary>
    public static void Request()
    {
        if (Asked) return;
        Asked = true;
#if UNITY_IOS && !UNITY_EDITOR
        MTRequestReview();
#else
        Debug.Log("AppReview: 実機でだけ出る（エディタでは何も起きない）");
#endif
    }

    /// <summary>試験用に覚えを消す。</summary>
    public static void Forget()
    {
        Asked = false;
        PlayerPrefs.DeleteKey(ClearsKey);
    }
}
