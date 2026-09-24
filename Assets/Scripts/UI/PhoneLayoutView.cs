using UnityEngine;

/// <summary>
/// 縦画面の並べ方（<see cref="PhoneScreen"/>）を、**端末の縦横比によらず同じ形**で出す役。
///
/// 画布は 1080x1920 の「スマホの画面」を1枚の枠（<see cref="frame"/>）として持ち、
/// 窓がそれより横長でも縦長でも、**枠はいつも 1080x1920 のまま**中央に置く
/// （余った所は盤面の下地の色で埋まる）。以前は帯の高さだけを画布の単位で固定して
/// いたので、横長の窓で開くと帯が画面を食い尽くして盤面が潰れていた。
///
/// 盤面はカメラが写す（画布ではない）ので、**枠の中の盤面の場所にちょうど 11 マスが
/// 入るよう**カメラの大きさと位置を毎コマ合わせる。カメラは画面いっぱいのままに
/// しておく（一部だけを写すと、外側が塗り残しになるため）。
/// </summary>
[ExecuteAlways]
public class PhoneLayoutView : MonoBehaviour
{
    [SerializeField] Camera boardCamera;
    [SerializeField] RectTransform frame;      // 1080x1920 のスマホの画面
    [SerializeField] RectTransform mapArea;
    [SerializeField] RectTransform screen;     // 原典の座標で置く入れ物（会話の窓など）
    [SerializeField] RectTransform topBand, bottomBand;
    /// <summary>1枚絵（題名・序章の絵・終幕）を出している間は、ほかを全部消す。</summary>
    [SerializeField] TowerController controller;

    /// <summary>画面いっぱいの1枚絵を出す種類か。</summary>
    public static bool IsFullScreen(TowerGame.PageKind k)
    {
        return k == TowerGame.PageKind.Title
            || k == TowerGame.PageKind.Prologue
            || k == TowerGame.PageKind.EndingTower
            || k == TowerGame.PageKind.EndingText
            || k == TowerGame.PageKind.GameOver
            || k == TowerGame.PageKind.FinalScreen;
    }

    // 1枚絵の間にほかを全部消す仕掛けは**取りやめた**（作者の指示）。
    // 出なかった原因は層ではなく、1枚絵の役が自分の入れ物を消して
    // 二度と動かなくなっていたことだった（FullScreenView）。

    void LateUpdate() { Apply(); }
    void OnEnable() { Apply(); }

    public void Apply()
    {
        if (frame == null || mapArea == null) return;

        // 枠は幅 1080、高さは 1920 に**下だけ少し**足したもの（作者の指定）。
        //
        // 9:16 より縦長の端末では上下に余りが出る。以前は 1080x1920 を中央に置いて
        // 上下に同じだけ余白を残していた。**上の余白はノッチ避けにちょうどよい**ので
        // そのまま残し、下の余りの**半分だけ**を下の帯に回す。
        // 一度「余り全部を下へ」にしたら、上の帯が画面の上端まで上がって
        // ノッチに隠れ、操作系も下がりすぎた（作者の報告）。
        var canvasRt = frame.parent as RectTransform;
        float canvasH = canvasRt != null ? canvasRt.rect.height
                                         : PhoneScreen.ReferenceHeight;
        // 中央に置いたときに上下へ空くぶん
        float slack = Mathf.Max(0f, canvasH - PhoneScreen.ReferenceHeight) / 2f;
        float extra = slack * PhoneScreen.BottomSlackShare;
        float frameHeight = PhoneScreen.ReferenceHeight + extra;
        frame.sizeDelta = new Vector2(PhoneScreen.ReferenceWidth, frameHeight);
        // **上端は中央に置いたときと同じ場所**に留める（下へ伸ばすだけ）。
        // 枠は画布の中央から `extra/2` だけ下がることになる。
        float frameDrop = extra / 2f;
        frame.anchoredPosition = new Vector2(0f, -frameDrop);

        float side = PhoneScreen.MapSide(frame.rect.width, frame.rect.height);
        // 上の帯は原典の欄の高さのまま。**余ったぶんは全部下**へ。
        float center = frame.rect.height / 2f - (PhoneScreen.TopHeight + side / 2f);
        mapArea.sizeDelta = new Vector2(side, side);
        mapArea.anchoredPosition = new Vector2(0f, center);

        // 上下の帯は盤面にぴったり接するまで伸ばす
        float half = frame.rect.height / 2f;
        if (topBand != null)
            topBand.sizeDelta = new Vector2(0f, Mathf.Max(0f, half - center - side / 2f));
        if (bottomBand != null)
            bottomBand.sizeDelta = new Vector2(0f, Mathf.Max(0f, half + center - side / 2f));

        if (boardCamera != null && side > 0f)
        {
            // 画面いっぱいに写したまま、**盤面の枠にちょうど11マス**が来るようにする。
            // 1画布単位あたりの世界の長さ = 11 / 枠の一辺
            float canvasHeight = canvasRt != null ? canvasRt.rect.height : frame.rect.height;
            float perUnit = PhoneScreen.MapCells / side;
            boardCamera.rect = new Rect(0f, 0f, 1f, 1f);
            boardCamera.orthographicSize = canvasHeight * perUnit / 2f;
            var p = boardCamera.transform.position;
            // ⚠ `center` は**枠の中心から**の高さ。カメラは画布の中心を基準に動かすので、
            //   枠を下げたぶん（frameDrop）を引いてから渡す。これを忘れていたので
            //   盤面が上の帯に半マスぶん潜り込み、**一番上の列が隠れていた**
            //   （フィールドが全部見えない、という作者の報告）。
            float boardFromCanvasCenter = center - frameDrop;
            boardCamera.transform.position =
                new Vector3(0f, -boardFromCanvasCenter * perUnit, p.z);
        }

        // 会話やメニューの窓・戦闘の帯・1枚絵は原典の座標（640x400）で置いてあるので、
        // 入れ物ごと縮めて**原典の画面の幅が枠の幅にちょうど入る**ようにし、
        // 盤面の真ん中に重ねる。
        if (screen != null)
        {
            float k = frame.rect.width
                    / (DialogueWindow.ScreenWidth * DialogueWindow.CanvasScale);
            screen.localScale = new Vector3(k, k, 1f);
            screen.anchoredPosition = mapArea.anchoredPosition;
        }

    }
}
