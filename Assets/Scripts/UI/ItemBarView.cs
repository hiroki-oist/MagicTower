using UnityEngine;

/// <summary>
/// 持ち物の帯（黒い地に道具を並べ、左右の矢印で送る）と、
/// 下の「ダメージ表示」のボタンの**触り**を受ける役。**作者の指定**で、原典には無い。
///
/// 触った道具は <see cref="TowerGame.TapItem"/> へ渡す。使い切りの道具は
/// 「〜を使いますか？」と聞いてから使い、**勇者のオーブだけは聞かずに一覧**を出す
/// （「ダメージ表示」のボタンも同じ一覧を出す）。
///
/// 押し場所の判定は自分で見る（<see cref="RectTransformUtility"/>）。
/// ボタンの部品を使うと、組み立て器から結び先を保存するのが難しいため。
/// </summary>
public class ItemBarView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] RectTransform viewport;
    [SerializeField] RectTransform content;
    [SerializeField] RectTransform leftArrow, rightArrow;
    /// <summary>右の「勇者」「飛翔」（オーブの近道。作者の指定）。</summary>
    [SerializeField] RectTransform heroButton, flightButton;
    /// <summary>その2つの枠と字（持っていない間は灰色に沈ませる）。</summary>
    [SerializeField] UnityEngine.UI.Image heroImage, flightImage;
    [SerializeField] TMPro.TMP_Text heroLabel, flightLabel;
    /// <summary>下の左の並びの「セーブ」「ロード」（作者の指定）。</summary>
    [SerializeField] RectTransform saveButton, loadButton, menuButton;
    [SerializeField] RectTransform[] slots = new RectTransform[0];

    /// <summary>いま何マスぶん送っているか（テストから見る）。</summary>
    public float Offset { get; private set; }

    /// <summary>押している間だけ色を変える枠（左右の矢印と、下のボタン）。</summary>
    [SerializeField] UnityEngine.UI.Image[] buttons = new UnityEngine.UI.Image[0];
    Color[] colors;
    int held = -1;

    void Update()
    {
        if (controller != null && controller.AtTitle) return;   // 題名の間は受けない
        Feedback();
        PaintOrbs();
        if (!Input.GetMouseButtonDown(0)) return;
        var at = (Vector2)Input.mousePosition;

        if (Inside(leftArrow, at))  { Scroll(-1); return; }
        if (Inside(rightArrow, at)) { Scroll(+1); return; }
        if (Inside(heroButton, at))   { Orb(SpecialItems.Kind.HeroOrb); return; }
        if (Inside(flightButton, at)) { Orb(SpecialItems.Kind.FlightOrb); return; }
        if (Inside(saveButton, at)) { if (controller != null) controller.AskSave(); return; }
        if (Inside(loadButton, at)) { if (controller != null) controller.AskLoad(); return; }
        if (Inside(menuButton, at)) { if (controller != null) controller.ToggleMenu(); return; }
        if (!Inside(viewport, at)) return;
        for (int i = 0; i < slots.Length; i++)
            if (Inside(slots[i], at))
            {
                if (controller != null) controller.TapItem(i);
                return;
            }
    }

    /// <summary>
    /// 右のオーブのボタン。持っていれば持ち物の帯を触ったのと同じ扱い、
    /// 持っていなければ知らせるだけ（作者の指定）。
    /// </summary>
    void Orb(SpecialItems.Kind kind)
    {
        if (controller == null || controller.Game == null) return;
        if (controller.Game.Player.Has(kind)) controller.TapItem(kind);
        else controller.NoteLockedOrb(kind);
    }

    /// <summary>持っていないオーブのボタンを灰色にする。</summary>
    void PaintOrbs()
    {
        if (controller == null || controller.Game == null) return;
        Paint(heroImage, heroLabel, controller.Game.Player.Has(SpecialItems.Kind.HeroOrb));
        Paint(flightImage, flightLabel,
              controller.Game.Player.Has(SpecialItems.Kind.FlightOrb));
    }

    void Paint(UnityEngine.UI.Image img, TMPro.TMP_Text label, bool owned)
    {
        if (img == null) return;
        var c = owned ? PhoneScreen.RightButton : PhoneScreen.LockedButton;
        // 押している間の濃い色は Feedback が上から塗るので、控えの色も合わせて直す
        int i = System.Array.IndexOf(buttons, img);
        if (colors != null && i >= 0 && i < colors.Length) colors[i] = c;
        if (i != held) img.color = c;
        if (label != null)
            label.color = owned ? PhoneScreen.ButtonText : PhoneScreen.LockedText;
    }

    /// <summary>1画面ぶん送る。端で止まる。</summary>
    public void Scroll(int dir)
    {
        if (viewport == null || content == null) return;
        float max = Mathf.Max(0f, content.rect.width - viewport.rect.width);
        Offset = Mathf.Clamp(Offset + dir * viewport.rect.width, 0f, max);
        content.anchoredPosition = new Vector2(-Offset, content.anchoredPosition.y);
    }

    /// <summary>押している枠を濃くする。</summary>
    void Feedback()
    {
        if (buttons == null || buttons.Length == 0) return;
        if (colors == null)
        {
            colors = new Color[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i] != null) colors[i] = buttons[i].color;
        }
        int now = -1;
        if (Input.GetMouseButton(0))
        {
            var at = (Vector2)Input.mousePosition;
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i] != null
                    && Inside(buttons[i].rectTransform, at)) { now = i; break; }
        }
        if (now == held) return;
        if (held >= 0 && held < buttons.Length && buttons[held] != null)
            buttons[held].color = colors[held];
        held = now;
        if (held >= 0 && buttons[held] != null)
            buttons[held].color = PhoneScreen.Pressed(colors[held]);
    }

    static bool Inside(RectTransform rt, Vector2 screenPoint)
    {
        // 画布は画面に直に描く（Overlay）ので、カメラは要らない
        return rt != null && rt.gameObject.activeInHierarchy
            && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint, null);
    }
}
