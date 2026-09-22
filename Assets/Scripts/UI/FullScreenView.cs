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
    // 入れ物（panel）は**消さない**。この役がその入れ物に付いているので、
    // 消すと自分の LateUpdate ごと止まってしまう。出し入れは絵と文字の側で行う。
    /// <summary>1枚絵の後ろに敷く黒地（画面いっぱい）。出している間だけ出す。</summary>
    [SerializeField] GameObject backdrop;
    [SerializeField] UnityEngine.UI.Image art;
    [SerializeField] TMPro.TextMeshProUGUI line1;
    [SerializeField] TMPro.TextMeshProUGUI line2;

    public bool IsShowing { get { return shown; } }

    bool shown;

    /// <summary>絵と2行の文字を出す。文字は null なら消す。</summary>
    public void Show(Sprite image, string first, string second)
    {
        if (art != null && image != null) art.sprite = image;
        if (line1 != null) line1.text = first ?? "";
        if (line2 != null) line2.text = second ?? "";
        SetVisible(true);
    }

    public void Hide() { SetVisible(false); }

    /// <summary>
    /// 出す・消すは**絵と文字の側**で行う。
    ///
    /// ⚠ この役は1枚絵の入れ物そのものに付いているので、
    /// **入れ物を SetActive(false) にすると自分の LateUpdate も止まり、
    /// 二度と出せなくなる**。題名も序章の絵もゲームオーバーの画面も
    /// 出なかったのはこれが原因だった（起動して最初の1コマで自分を消していた）。
    /// </summary>
    void SetVisible(bool on)
    {
        shown = on;
        if (art != null) art.enabled = on;
        if (line1 != null) line1.enabled = on;
        if (line2 != null) line2.enabled = on;
        if (whiteVeil != null) whiteVeil.enabled = on && scrollTower;
    }

    /// <summary>
    /// 本線が出しているページの種類を見て、自分の出番なら出る。
    /// 原典も題名（0x6db2）と最後の画面（0xd45c）はこの形で、
    /// 絵と文字を出したあとキー入力を待つだけ。
    /// </summary>
    /// <summary>
    /// 序章の1枚絵は**行が1つずつ現れる**ので、行数ぶんの絵を持っておいて差し替える。
    /// 原典は絵の中に3行とも描いてあり、パレットを差し替えて出していく
    /// （`lcall 0xf5f,0x461`。動画 Part1 t≈34〜41）。
    /// </summary>
    [SerializeField] Sprite[] steps = new Sprite[0];

    /// <summary>
    /// 終幕の塔の絵だけは**上へ流れていく**（原典 0x0ce83 の輪）。
    /// 26回、1回ごとに15ドット送って音7を鳴らし、30コマ待つ。規則は <see cref="Ending"/>。
    /// </summary>
    [SerializeField] bool scrollTower;

    /// <summary>
    /// 塔の絵に重ねる白。原典はパレットで色を白へ寄せる（`Ending.TowerWhiteMix`）ので、
    /// こちらは同じ形になるよう**白い板を上から薄くかける**。
    /// </summary>
    [SerializeField] UnityEngine.UI.Image whiteVeil;

    Coroutine towerRun;

    void LateUpdate()
    {
        if (controller == null || controller.Game == null) return;
        bool mine = controller.Game.CurrentKind == showOn;
        if (mine && steps.Length > 0 && art != null)
        {
            // ページの「番号」が何行目まで出すかを表す
            var n = Mathf.Clamp(controller.Game.CurrentRecord, 0, steps.Length - 1);
            if (steps[n] != null) art.sprite = steps[n];
        }
        if (shown != mine) SetVisible(mine);
        // 後ろの黒地は6つの役で1枚を分け合っているので、
        // **今どれかが出ているか**で決める（自分の出番だけで消すと取り合いになる）。
        if (backdrop != null)
        {
            var k = controller.Game.CurrentKind;
            bool anyFull = k.HasValue && PhoneLayoutView.IsFullScreen(k.Value);
            if (backdrop.activeSelf != anyFull) backdrop.SetActive(anyFull);
        }

        if (!scrollTower) return;
        if (mine && towerRun == null && isActiveAndEnabled) towerRun = StartCoroutine(ScrollTower());
        if (!mine && towerRun != null) { StopCoroutine(towerRun); towerRun = null; ResetTower(); }
    }

    /// <summary>塔の絵が上へ流れていく（原典 0x0ce83）。</summary>
    System.Collections.IEnumerator ScrollTower()
    {
        var rt = art != null ? art.rectTransform : null;
        var from = rt != null ? rt.anchoredPosition : Vector2.zero;
        for (int i = 0; i < Ending.TowerSteps; i++)
        {
            if (rt != null)
                rt.anchoredPosition = from + new Vector2(
                    0f, i * Ending.TowerScrollPerStep * DialogueWindow.CanvasScale);
            if (whiteVeil != null)
                whiteVeil.color = new Color(1f, 1f, 1f, Ending.TowerWhiteMix(i));
            if (controller.Sound != null) controller.Sound.PlayCue(SoundCues.SceneBeat);
            yield return new WaitForSeconds(Ending.TowerStepWait * MonsterAnimation.FrameSeconds);
        }
        towerRun = null;
    }

    void ResetTower()
    {
        if (whiteVeil != null) whiteVeil.color = new Color(1f, 1f, 1f, 0f);
        if (art == null) return;
        art.color = Color.white;
        art.rectTransform.anchoredPosition = Vector2.zero;
    }
}
