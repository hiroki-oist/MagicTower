using UnityEngine;

/// <summary>
/// 画面いっぱいの1枚絵と、その上に載る文字。題名と終幕がこれを使う。
///
/// 原典はどちらも「絵を出して、決まった位置に文字を出して、待つ」だけなので、
/// 見た目の側もその形にしておく。位置は原典の値（x はバイト＝8ドット、y はドット）で、
/// 画布へは <see cref="ScreenLayout.ToCanvas"/> が 1.8倍して置く。
/// </summary>
public class FullScreenView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    /// <summary>この種類のページが出ているときに自分を出す。</summary>
    [SerializeField] TowerGame.PageKind showOn = TowerGame.PageKind.Title;
    [SerializeField] GameObject panel;
    [SerializeField] UnityEngine.UI.Image art;
    [SerializeField] TMPro.TextMeshProUGUI line1;
    [SerializeField] TMPro.TextMeshProUGUI line2;

    public bool IsShowing { get { return panel != null && panel.activeSelf; } }

    /// <summary>絵と2行の文字を出す。文字は null なら消す。</summary>
    public void Show(Sprite image, string first, string second)
    {
        if (panel == null) return;
        if (art != null && image != null) art.sprite = image;
        if (line1 != null) line1.text = first ?? "";
        if (line2 != null) line2.text = second ?? "";
        panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>
    /// 本線が出しているページの種類を見て、自分の出番なら出る。
    /// 原典も題名（0x6db2）と最後の画面（0xd45c）はこの形で、
    /// 絵と文字を出したあとキー入力を待つだけ。
    /// </summary>
    void LateUpdate()
    {
        if (controller == null || controller.Game == null) return;
        bool mine = controller.Game.CurrentKind == showOn;
        if (panel != null && panel.activeSelf != mine) panel.SetActive(mine);
    }
}
