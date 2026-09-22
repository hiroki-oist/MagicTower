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
    [SerializeField] RectTransform damageButton;
    [SerializeField] RectTransform[] slots = new RectTransform[0];

    /// <summary>いま何マスぶん送っているか（テストから見る）。</summary>
    public float Offset { get; private set; }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        var at = (Vector2)Input.mousePosition;

        if (Inside(leftArrow, at))  { Scroll(-1); return; }
        if (Inside(rightArrow, at)) { Scroll(+1); return; }
        if (Inside(damageButton, at))
        {
            if (controller != null) controller.ShowDamage();
            return;
        }
        if (!Inside(viewport, at)) return;
        for (int i = 0; i < slots.Length; i++)
            if (Inside(slots[i], at))
            {
                if (controller != null) controller.TapItem(i);
                return;
            }
    }

    /// <summary>1画面ぶん送る。端で止まる。</summary>
    public void Scroll(int dir)
    {
        if (viewport == null || content == null) return;
        float max = Mathf.Max(0f, content.rect.width - viewport.rect.width);
        Offset = Mathf.Clamp(Offset + dir * viewport.rect.width, 0f, max);
        content.anchoredPosition = new Vector2(-Offset, content.anchoredPosition.y);
    }

    static bool Inside(RectTransform rt, Vector2 screenPoint)
    {
        // 画布は画面に直に描く（Overlay）ので、カメラは要らない
        return rt != null && rt.gameObject.activeInHierarchy
            && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint, null);
    }
}
