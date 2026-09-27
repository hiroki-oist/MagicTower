using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// ★**評価のお願い**（作者の指定。原典に無い）。実体は
/// <c>Assets/Plugins/iOS/MagicTowerStore.swift</c> の <c>MTRequestReview</c>。
///
/// 出るのは **iOS の決まった窓**（SKStoreReviewController）で、
/// **文面・星・ボタンはすべて Apple のもの**。こちらからは変えられないし、
/// 似せた窓を自前で作ることも App Review ガイドラインで禁じられている。
/// 出すかどうかも OS が決める（同じ人には年3回まで。出ないこともある）。
/// **TestFlight のビルドでは何も出ない**。
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

    /// <summary>もう頼んだか（同じ回で何度も呼ばないため）。</summary>
    public static bool Asked { get; private set; }

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

    /// <summary>次の遊びのために忘れる（題名へ戻ったとき）。</summary>
    public static void Forget() { Asked = false; }
}
