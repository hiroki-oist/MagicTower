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

    /// <summary>印の絵を2枚で切り替える（原典 0x9e78 の数え。規則は <see cref="ChoiceCursor"/>）。</summary>
    [SerializeField] Sprite cursorFrame2;

    /// <summary>
    /// ★作者の指定で項目の文字を大きくしたので、**絵の印は使わない**。
    /// 行ごとに高さが変わるため、原典と同じ「行の高さ×行数」では位置が合わない。
    /// 印は <see cref="Compose"/> が置く文字の「▶」。
    /// </summary>
    void PlaceCursor(TowerGame g)
    {
        if (cursor != null && cursor.enabled) cursor.enabled = false;
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

        // ⚠ 印を置き直すのは**文字が変わったときだけ**にしていたので、
        //   上下キーで選ぶ場所を動かしても印が動かなかった（印の絵を使うときは
        //   文字の側が変わらないため）。印と点滅は毎コマ見る。
        Layout();
        PlaceCursor(g);

        var text = Display(g);
        if (text == shown) return;
        shown = text;
        if (body != null)
        {
            body.text = text;
            body.color = DialogueWindowView.Text;
        }
        if (background != null) background.color = DialogueWindowView.Fill;
    }

    /// <summary>
    /// 窓に出す文字。見出しはそのまま、**項目だけ大きく**する（作者の指定）。
    /// 印は文字の「▶」を使う。行ごとに大きさが違うので、
    /// 絵の印（原典の小さな剣）では位置が合わせられない。
    /// </summary>
    public static string Display(TowerGame g)
    {
        var lines = Compose(g).Split('\n');
        if (lines.Length > 0) lines[0] = TowerMenu.Heading(lines[0]);
        // 末尾の添え書き（「ゲームをリセットして…」）だけ小さく出す。
        int notes = TowerMenu.Note.Length;
        for (int i = 1; i < lines.Length; i++)
            lines[i] = i < lines.Length - notes ? TowerMenu.Big(lines[i])
                                                : TowerMenu.Small(lines[i]);
        return string.Join("\n", lines);
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
