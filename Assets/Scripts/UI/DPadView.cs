using UnityEngine;

/// <summary>
/// 移動の十字キーの**触り**を受ける役（作者の指定。原典はキーボードで遊ぶ）。
///
/// 押している間ずっと向きを入れるので、**押しっぱなしで歩き続ける**
/// （鍵を押しっぱなしにしたときと同じ扱い。<see cref="TowerController.TouchDir"/>）。
/// どこを押したかを向きに直す決まりは <see cref="PhoneScreen.DirFromTap"/>。
/// </summary>
public class DPadView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] RectTransform pad;
    /// <summary>上下の帯。**ここより外（盤面）を触ったら「決定・送り」**にする。</summary>
    [SerializeField] RectTransform topBand, bottomBand;

    /// <summary>
    /// 十字キーの5枚（上・右・下・左の扇と、真ん中の決定ボタン）。
    /// **押している1枚だけ**色を濃くする（どこを押しているか分かるように）。
    /// </summary>
    [SerializeField] UnityEngine.UI.Image[] parts = new UnityEngine.UI.Image[5];
    Color[] partColors;
    int lit = -1;

    void Awake()
    {
        if (parts == null) return;
        partColors = new Color[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            if (parts[i] != null) partColors[i] = parts[i].color;
    }

    void Update()
    {
        if (controller == null || pad == null) return;
        var dir = Pressed();
        controller.TouchDir = dir;
        // 押している所だけ色を変える（真ん中を押しているときは決定ボタンだけ）
        bool onPad = Input.GetMouseButton(0)
                     && Inside(pad, (Vector2)Input.mousePosition);
        Light(!onPad ? -1 : PhoneScreen.PartOf(dir));

        if (!Input.GetMouseButtonDown(0)) return;
        var at = (Vector2)Input.mousePosition;
        if (dir != Vector2Int.zero) { controller.TouchStep = dir; return; }
        // **十字の真ん中は「決定」**（会話を送る／選んだものを決める）。
        if (Inside(pad, at)) { controller.TouchConfirm = true; return; }
        // 帯の上（ボタンや持ち物）は、それぞれの受け口に任せる。
        // 盤面のあたりを触ったときも「決定・送り」。
        if (Inside(topBand, at) || Inside(bottomBand, at)) return;
        controller.TouchConfirm = true;
    }

    /// <summary>その1枚だけ濃くする。-1 なら全部もとの色に戻す。</summary>
    void Light(int index)
    {
        if (parts == null || partColors == null || lit == index) return;
        if (lit >= 0 && lit < parts.Length && parts[lit] != null)
            parts[lit].color = partColors[lit];
        lit = index;
        if (lit >= 0 && lit < parts.Length && parts[lit] != null)
            parts[lit].color = PhoneScreen.Pressed(partColors[lit]);
    }

    void OnDisable()
    {
        if (controller != null) controller.TouchDir = Vector2Int.zero;
    }

    static bool Inside(RectTransform rt, Vector2 screenPoint)
    {
        return rt != null && rt.gameObject.activeInHierarchy
            && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint, null);
    }

    Vector2Int Pressed()
    {
        if (!Input.GetMouseButton(0)) return Vector2Int.zero;
        var corners = new Vector3[4];
        pad.GetWorldCorners(corners);          // 画布は Overlay なので world = 画面の画素
        var center = (Vector2)(corners[0] + corners[2]) * 0.5f;
        float radius = (corners[2].x - corners[0].x) * 0.5f;
        return PhoneScreen.DirFromTap((Vector2)Input.mousePosition - center, radius);
    }
}
