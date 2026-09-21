using UnityEngine;

/// <summary>
/// 縦画面の並べ方（<see cref="PhoneScreen"/>）に合わせて、
/// **盤面の枠を正方形にし、その枠へカメラを合わせる**役。
///
/// 盤面はカメラが写す（画布ではない）ので、画面の中のどこに出すかは
/// カメラの `rect`（画面に対する割合）で決める。枠の大きさは端末の縦横比で
/// 変わるので、毎コマ合わせ直す。
/// </summary>
[ExecuteAlways]
public class PhoneLayoutView : MonoBehaviour
{
    [SerializeField] Camera boardCamera;
    [SerializeField] RectTransform mapArea;
    [SerializeField] RectTransform screen;     // 原典の座標で置く入れ物（会話の窓など）
    [SerializeField] RectTransform topBand, bottomBand;

    void LateUpdate() { Apply(); }
    void OnEnable() { Apply(); }

    public void Apply()
    {
        if (mapArea == null) return;
        var parent = mapArea.parent as RectTransform;
        if (parent == null) return;

        // 盤面の枠は正方形。幅いっぱいか、上下の帯を除いた高さのどちらか小さいほう。
        float side = PhoneScreen.MapSide(parent.rect.width, parent.rect.height);
        mapArea.sizeDelta = new Vector2(side, side);
        // 上の帯と下の帯の間の真ん中に置く
        float center = (PhoneScreen.BottomHeight - PhoneScreen.TopHeight) / 2f;
        mapArea.anchoredPosition = new Vector2(0f, center);

        // 上下の帯は**盤面の枠にぴったり接する**まで伸ばす。カメラは盤面の枠しか塗らず、
        // 画布（Overlay）は絵の上に重なるので、帯で埋めない所は塗り残しになる。
        float half = parent.rect.height / 2f;
        if (topBand != null)
            topBand.sizeDelta = new Vector2(0f, Mathf.Max(0f, half - center - side / 2f));
        if (bottomBand != null)
            bottomBand.sizeDelta = new Vector2(0f, Mathf.Max(0f, half + center - side / 2f));

        if (boardCamera != null)
        {
            var c = new Vector3[4];
            mapArea.GetWorldCorners(c);      // 画布が Overlay なので world = 画面の画素
            float w = Mathf.Max(1f, c[2].x - c[0].x), h = Mathf.Max(1f, c[2].y - c[0].y);
            boardCamera.rect = new Rect(c[0].x / Screen.width, c[0].y / Screen.height,
                                        w / Screen.width, h / Screen.height);
            // 正方形の枠に 11 マスちょうど。横長に崩れても縦は 11 マスのまま。
            boardCamera.orthographicSize = PhoneScreen.CameraSize;
        }

        // 会話やメニューの窓・戦闘の帯・1枚絵は原典の座標（640x400）で置いてあるので、
        // 入れ物ごと縮めて**原典の画面の幅が画布の幅にちょうど入る**ようにし、
        // 盤面の真ん中に重ねる（原典も窓は盤面の上に開く）。
        if (screen != null)
        {
            float k = parent.rect.width
                    / (DialogueWindow.ScreenWidth * DialogueWindow.CanvasScale);
            screen.localScale = new Vector3(k, k, 1f);
            screen.anchoredPosition = mapArea.anchoredPosition;
        }
    }
}
