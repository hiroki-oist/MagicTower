using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 知らせ（「鍵を手に入れた！」など）を**画面の中央に出して、少し置いてから
/// 薄くして消す**役。**作者の指定**（旧版がこの見せ方だった）で、原典は
/// 右下の欄に書きっぱなしにする（<see cref="TowerHud.InfoBox"/>）。
///
/// 出すのは本線が積んだ知らせ（<see cref="TowerGame.Log"/>）の最後の1本。
/// 会話の窓が出ている間は出さない（窓の中で同じ文が読めるため）。
/// </summary>
public class MessageToastView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] RectTransform panel;
    [SerializeField] Image background;
    [SerializeField] TextMeshProUGUI text;

    int seen;
    float age = -1f;

    /// <summary>いま出ているか（テストから見る）。</summary>
    public bool IsShowing { get { return age >= 0f; } }

    void LateUpdate()
    {
        if (controller == null || controller.Game == null || panel == null) return;
        var log = controller.Game.Log;

        if (log.Count != seen)
        {
            // 新しい知らせが積まれた。窓が出ているときは窓の中で読めるので出さない。
            bool quiet = !controller.Game.IsShowingPage && log.Count > 0;
            seen = log.Count;
            if (quiet)
            {
                if (text != null) text.text = log[log.Count - 1];
                age = 0f;
            }
        }

        if (age < 0f) { Show(false, 0f); return; }

        age += Time.deltaTime;
        float a = age <= PhoneScreen.ToastHold
                ? 1f
                : 1f - (age - PhoneScreen.ToastHold) / PhoneScreen.ToastFade;
        if (a <= 0f) { age = -1f; Show(false, 0f); return; }
        Show(true, a);
    }

    void Show(bool on, float alpha)
    {
        if (panel.gameObject.activeSelf != on) panel.gameObject.SetActive(on);
        if (!on) return;
        if (text != null)
        {
            var c = text.color; c.a = alpha; text.color = c;
        }
        if (background != null)
        {
            var c = background.color; c.a = alpha * 0.72f; background.color = c;
        }
    }
}
