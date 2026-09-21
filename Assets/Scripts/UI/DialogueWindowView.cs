using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 会話ウィンドウを画面に出す役。寸法は <see cref="DialogueWindow"/>（原典由来）に従う。
///
/// 原典は1ページぶんを出したあとキー入力を待ってから次へ進む
/// （セリフ表示のあとに必ず `lcall 0x107e, 0x4fa`）。ここでも同じで、
/// 出ている間は <see cref="TowerGame.CurrentPage"/> が null になるまで送る。
/// </summary>
public class DialogueWindowView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] RectTransform panel;
    [SerializeField] Image background;
    [SerializeField] TextMeshProUGUI body;

    /// <summary>原典のパレットの色（DS:0x908）。内側は索引0、文字は索引15。</summary>
    public static readonly Color Fill = new Color32(0x44, 0x44, 0x44, 0xff);
    public static readonly Color Text = new Color32(0xff, 0xff, 0xff, 0xff);

    /// <summary>今出ている窓。出ていなければ null。</summary>
    public string Showing { get; private set; }

    void LateUpdate()
    {
        if (controller == null || controller.Game == null) return;
        var page = controller.Game.CurrentPage;

        // 題名と終幕の1枚絵は会話の窓ではなく FullScreenView が出す。
        bool dialogue = controller.Game.CurrentKind == TowerGame.PageKind.Dialogue;
        bool show = dialogue && !string.IsNullOrEmpty(page);
        if (panel != null && panel.gameObject.activeSelf != show)
            panel.gameObject.SetActive(show);
        if (!show) { Showing = null; return; }

        if (page != Showing)
        {
            Showing = page;
            if (body != null) { body.text = page; body.color = Text; }
            if (background != null) background.color = Fill;
            Layout(page);
            StartOpening(page);
        }
    }

    /// <summary>開いている途中のコルーチン。出し直すときは止める。</summary>
    Coroutine opening;

    void StartOpening(string page)
    {
        if (panel == null) return;
        if (opening != null) StopCoroutine(opening);
        if (!isActiveAndEnabled) return;
        opening = StartCoroutine(Open(page));
    }

    /// <summary>
    /// 窓が開く様子。原典（0x94ba / 0x9412）は
    /// **上の帯（32行）を貼ってから、本体を16行ぶんずつ4行きざみで下へ塗り広げる**。
    /// 1枚塗るごとに縦帰線を1回待つ（`lcall 0xf5f, 0x3d9`）ので、
    /// **1きざみ = 1/60秒**。塗る回数は 5×行数+1 で、届く下端は 32+4×きざみ。
    /// 高速モード（旗31）のときは待ちを入れない（0x9478 / 0x9546）。
    /// 文字は塗り終わってから書く（原典 0x9563 も塗りのあと）。
    /// </summary>
    System.Collections.IEnumerator Open(string page)
    {
        var r = DialogueWindow.ForBody(page, high: HighFor(page));
        int lines = DialogueBox.WindowLines(page);
        int times = 0x14 * lines / DialogueWindow.RowStep + 1;
        bool fast = controller != null && controller.Game != null
                    && controller.Game.Player.FastMode;

        if (body != null) body.enabled = false;
        for (int i = 0; i <= times - 1; i++)
        {
            int h = Mathf.Min(DialogueWindow.StripHeight + DialogueWindow.RowStep * i,
                              r.height);
            panel.sizeDelta = new Vector2(r.width * DialogueWindow.CanvasScale,
                                          h * DialogueWindow.CanvasScale);
            if (!fast) yield return new WaitForSeconds(MonsterAnimation.FrameSeconds);
        }
        panel.sizeDelta = DialogueWindow.SizeOnCanvas(r);
        if (body != null) body.enabled = true;
        opening = null;
    }

    /// <summary>開いている途中か（テストから見る）。</summary>
    public bool IsOpening { get { return opening != null; } }

    /// <summary>原典の寸法で窓を置き直す。</summary>
    void Layout(string page)
    {
        if (panel == null) return;
        var r = DialogueWindow.ForBody(page, high: HighFor(page));
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = DialogueWindow.ToCanvas(r);
        panel.sizeDelta = DialogueWindow.SizeOnCanvas(r);
    }

    /// <summary>
    /// 姿の見えない相手の声か。原典で上端50に開くのはセリフ107・159・156の3件だけ。
    /// </summary>
    public static bool HighFor(string page)
    {
        if (string.IsNullOrEmpty(page)) return false;
        foreach (var id in HighRecords)
        {
            var l = GameText.Instance.Dialogue(id);
            if (l.HasValue && l.Value.Body == page) return true;
        }
        return false;
    }

    /// <summary>上端50に開くセリフ（原典 0x0006eb / 0x001aa9 / 0x005d9e）。</summary>
    public static readonly int[] HighRecords = { 107, 159, 156 };

    /// <summary>次のページへ。もう無ければ false。</summary>
    public bool Advance()
    {
        if (controller == null || controller.Game == null) return false;
        return controller.Game.NextPage();
    }
}
