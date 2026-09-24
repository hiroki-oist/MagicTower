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
    ///
    /// ⚠ 以前は「▶」(U+25B6) を使っていたが、**この字は原典の書体に無い**
    /// （JIS X 0208 に入っているのは ▲▼△▽ だけ）ので、実機では豆腐（□）に
    /// なっていた。選ばせる窓と同じ「◎」に揃えてある。
    /// </summary>
    public const string Cursor = "◎";

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
    /// 窓に出す文字。**大きさは行の表（<see cref="TowerMenu.Rows"/>）どおり**に付ける。
    /// 見出し・項目・添え書きで大きさが違う。印は文字の「▶」を使う
    /// （行ごとに高さが違うので、絵の印では位置が合わせられない）。
    /// </summary>
    public static string Display(TowerGame g)
    {
        var lines = Compose(g).Split('\n');
        var rows = TowerMenu.Rows(g.Player);
        for (int i = 0; i < lines.Length; i++)
        {
            float scale = i < rows.Count ? rows[i].Scale : TowerMenu.TextScale;
            lines[i] = TowerMenu.Sized(lines[i], scale);
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// 選んでいる**項目**の行の先頭2文字を印に置き換える。
    /// 添え書きの行は選べないので、行の表で項目の番号から行を引く。
    /// </summary>
    public static string Compose(TowerGame g)
    {
        var lines = g.MenuStageText.Split('\n');
        var rows = TowerMenu.Rows(g.Player);
        int pick = -1;
        for (int i = 0; i < rows.Count; i++)
            if (rows[i].Item == g.MenuStageIndex) { pick = i; break; }
        if (pick >= 0 && pick < lines.Length)
        {
            var t = lines[pick];
            lines[pick] = t.StartsWith("　　") ? Cursor + "　" + t.Substring(2) : Cursor + t;
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// **写しを撮るための口**（エディタから使う）。遊んでいないと本線が無いので、
    /// 外から遊びの状態を渡して、**本線とまったく同じ組み方**（<see cref="Display"/>）で
    /// 窓を出す。道を分けると「写しでは収まっているのに実機では溢れる」が起きる。
    /// </summary>
    public void Preview(TowerGame g)
    {
        if (panel != null) panel.gameObject.SetActive(true);
        Layout();
        PlaceCursor(g);
        if (body != null)
        {
            body.text = Display(g);
            body.color = DialogueWindowView.Text;
        }
        if (background != null) background.color = DialogueWindowView.Fill;
        shown = null;
    }

    /// <summary>組まれた文字（写しを撮るときに行数を数えるため）。</summary>
    public TMP_Text Body { get { return body; } }

    void Layout()
    {
        if (panel == null) return;
        var r = TowerMenu.Window;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = DialogueWindow.ToCanvas(r);
        panel.sizeDelta = DialogueWindow.SizeOnCanvas(r);
    }
}
