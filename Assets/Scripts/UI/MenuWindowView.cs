using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// メニューの窓を画面に出す役。寸法は <see cref="TowerMenu"/>（原典由来）に従う。
///
/// 原典は窓の指定が **11行・上端50 で固定**（会話と違って文章の行数に合わせない）。
/// 選んでいる項目には印が付く（原典は小さな剣の絵、ここでは文字で代える）。
/// </summary>
public class MenuWindowView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] RectTransform panel;
    [SerializeField] Image background;
    [SerializeField] TextMeshProUGUI body;

    /// <summary>選んでいる項目に付ける印。原典は剣の絵。</summary>
    /// <summary>
    /// 選んでいる行に置く印。**原典は PICTURE.P58 の4枚目**（16x16、剣を横にした形）を
    /// `0x9e78` が変位 -0x337 から貼る。鍵3枚の次に並んでいて間隔も合う。
    /// 絵が繋がっていないときのために、文字の印も残してある。
    /// </summary>
    public const string Cursor = "▶";

    /// <summary>原典の印の絵。無ければ文字の印になる。</summary>
    [SerializeField] UnityEngine.UI.Image cursor;

    /// <summary>印を置く行へ動かす。</summary>
    void PlaceCursor(TowerGame g)
    {
        if (cursor == null) return;
        bool show = g.IsMenuOpen;
        if (cursor.enabled != show) cursor.enabled = show;
        if (!show) return;
        int line = g.MenuStageIndex + 1;                 // 1行目は見出し
        var rt = cursor.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(16f * DialogueWindow.CanvasScale,
                                   16f * DialogueWindow.CanvasScale);
        rt.anchoredPosition = new Vector2(
            DialogueWindow.TextLeft * DialogueWindow.CanvasScale,
            -(DialogueWindow.TextTop + ScreenLayout.LineAdvance * line) * DialogueWindow.CanvasScale);
    }

    string shown;

    void LateUpdate()
    {
        if (controller == null || controller.Game == null) return;
        var g = controller.Game;

        bool show = g.IsMenuOpen;
        if (panel != null && panel.gameObject.activeSelf != show)
            panel.gameObject.SetActive(show);
        if (!show) { shown = null; return; }

        var text = cursor != null ? g.MenuStageText : Compose(g);
        if (text == shown) return;
        shown = text;
        if (body != null)
        {
            body.text = text;
            body.color = DialogueWindowView.Text;
        }
        if (background != null) background.color = DialogueWindowView.Fill;
        Layout();
        PlaceCursor(g);
    }

    /// <summary>選んでいる行の先頭2文字を印に置き換える。</summary>
    public static string Compose(TowerGame g)
    {
        var lines = g.MenuStageText.Split('\n');
        // 1行目は見出し。段によって「今選んでいる行」が違う（本体・アイテム・スロット）。
        int pick = g.MenuStageIndex + 1;
        for (int i = 1; i < lines.Length; i++)
        {
            if (i != pick) continue;
            var t = lines[i];
            lines[i] = t.StartsWith("　　") ? Cursor + "　" + t.Substring(2) : Cursor + t;
        }
        return string.Join("\n", lines);
    }

    void Layout()
    {
        if (panel == null) return;
        var r = TowerMenu.Window;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = DialogueWindow.ToCanvas(r);
        panel.sizeDelta = DialogueWindow.SizeOnCanvas(r);
    }
}
