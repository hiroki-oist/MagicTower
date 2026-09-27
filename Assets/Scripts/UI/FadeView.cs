using System.Collections;
using UnityEngine;

/// <summary>
/// ★**歩きながらの暗転**を出す役（作者の指定。原典に無い）。
///
/// 終幕で、主人公がゼノに向かって1歩ぶんゆっくり歩き出し、その間に画面が
/// 真っ黒になる。暗くなりきってから塔の1枚絵に入る。
/// 原典（0xc4d8）はセリフ170のあと**そのまま**塔の絵へ切り替わる。
///
/// 黒地は**暗転しきったあとも消さない**。次に出るのは全画面の1枚絵なので、
/// そちらが出てから消さないと、その隙に盤面が一瞬見えてしまう。
/// </summary>
public class FadeView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    /// <summary>画面いっぱいの黒地。ふだんは消してある。</summary>
    [SerializeField] UnityEngine.UI.Image black;

    /// <summary>暗くしている最中か。場面はこれが終わるまで次へ進まない。</summary>
    public bool IsPlaying { get; private set; }

    void LateUpdate()
    {
        if (controller == null || controller.Game == null || black == null) return;

        foreach (var f in controller.Game.ConsumeFades())
            if (isActiveAndEnabled) StartCoroutine(Play(f));

        // 全画面の1枚絵が出たら黒地は要らない（そちらが画面を覆う）。
        if (!IsPlaying && black.enabled && FullScreenShowing()) Clear();
    }

    /// <summary>全画面の1枚絵（序章・題名・終幕）が出ているか。</summary>
    bool FullScreenShowing()
    {
        var page = controller.Game.CurrentKind;
        if (!page.HasValue) return false;
        switch (page.Value)
        {
            case TowerGame.PageKind.Prologue:
            case TowerGame.PageKind.Title:
            case TowerGame.PageKind.EndingTower:
            case TowerGame.PageKind.EndingText:
            case TowerGame.PageKind.FinalScreen:
            case TowerGame.PageKind.GameOver:
                return true;
            default:
                return false;
        }
    }

    IEnumerator Play(TowerGame.FadeWalk f)
    {
        IsPlaying = true;
        float dur = Mathf.Max(0.1f, f.Frames * MonsterAnimation.FrameSeconds);

        // 歩くのと暗くするのを**同時に**進める（歩き終わってから暗転ではない）。
        if (controller != null) StartCoroutine(controller.SlidePlayerViewTo(f.To, dur));

        black.enabled = true;
        black.transform.SetAsLastSibling();      // 何より前に出す
        var c = black.color; c.a = 0f; black.color = c;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / dur);
            black.color = c;
            yield return null;
        }
        c.a = 1f; black.color = c;
        IsPlaying = false;                        // 黒いまま次の頁へ渡す
    }

    /// <summary>黒地をしまう。</summary>
    public void Clear()
    {
        if (black == null) return;
        var c = black.color; c.a = 0f; black.color = c;
        black.enabled = false;
    }
}
