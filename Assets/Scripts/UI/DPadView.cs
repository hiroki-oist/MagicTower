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

    void Update()
    {
        if (controller == null || pad == null) return;
        controller.TouchDir = Pressed();
    }

    void OnDisable()
    {
        if (controller != null) controller.TouchDir = Vector2Int.zero;
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
