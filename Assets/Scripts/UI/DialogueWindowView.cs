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
    [SerializeField] BlinkView blink;
    /// <summary>
    /// 選んでいる行に置く印。原典は PICTURE.P58 の4枚目を
    /// `0x9e78` が変位 -0x337 から貼る（メニューの窓と同じ絵）。
    /// </summary>
    [SerializeField] UnityEngine.UI.Image cursor;
    [SerializeField] BossAppearView boss;

    /// <summary>原典のパレットの色（DS:0x908）。内側は索引0、文字は索引15。</summary>
    public static readonly Color Fill = new Color32(0x44, 0x44, 0x44, 0xff);
    public static readonly Color Text = new Color32(0xff, 0xff, 0xff, 0xff);

    /// <summary>
    /// 作者の見せ方のときの枠の色。枠の絵（`Assets/Images/Dialogue.png`）が
    /// もともと黒いので、色は掛けない（白＝そのまま）。
    /// </summary>
    public static readonly Color AuthorFill = Color.white;

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
        {
            panel.gameObject.SetActive(show);
            // **メニューの窓より前に出す**。飛翔のオーブを断られたときのように、
            // メニューを開いたまま窓が開くことがあり、後ろに隠れると読めない。
            if (show) panel.SetAsLastSibling();
        }
        if (!show) { Showing = null; waiting = typing = false; return; }

        PlaceCursor(page);
        // 印の絵が無いときだけ、選んでいる行に文字の印を付ける
        var shownText = (cursor == null && controller.Game.IsChoosing)
                      ? WithMark(page, controller.Game) : page;
        if (page != Showing || (body != null && body.text != shownText))
        {
            Showing = page;
            if (body != null) { body.text = shownText; body.color = Text; }
            if (background != null)
                background.color = AuthorStyle.Enabled ? AuthorFill : Fill;
            Layout(page);
            StartOpening(page);
        }
    }

    /// <summary>選んでいる行が本文の何行目か。無ければ -1。</summary>
    static int ChoiceLine(string page, TowerGame game)
    {
        if (string.IsNullOrEmpty(page)) return -1;
        var lines = page.Split('\n');
        int seen = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (!IsChoiceLine(lines[i])) continue;
            if (seen == game.ChoiceIndex) return i;
            seen++;
        }
        return -1;
    }

    /// <summary>
    /// 選ばせている行か。原典の文面は2通りある:
    ///   ・「◎はい／◎いいえ」のように印が文字として入っているもの
    ///   ・祭壇の望みや持ち物のように、**全角の空白2つで字下げ**してあるだけのもの
    /// 後者で印が出ないという報告があったので、字下げも見るようにした。
    /// </summary>
    public static bool IsChoiceLine(string line)
    {
        if (string.IsNullOrEmpty(line)) return false;
        return line.IndexOf('◎') >= 0 || line.StartsWith("　　");
    }

    /// <summary>
    /// 印の絵を選んでいる行へ置く。原典（0x9e78）は行の高さぶんずつ印を動かす。
    /// </summary>

    /// <summary>印の絵を2枚で切り替える（原典 0x9e78 の数え。規則は <see cref="ChoiceCursor"/>）。</summary>
    [SerializeField] Sprite cursorFrame2;

    void Blink()
    {
        if (cursor == null || cursorFrame2 == null) return;
        if (cursorFirst == null) cursorFirst = cursor.sprite;      // 組み立て器が入れた1枚目
        cursor.sprite = ChoiceCursor.FrameAt(Time.time) == 0 ? cursorFirst : cursorFrame2;
    }

    Sprite cursorFirst;
    void PlaceCursor(string page)
    {
        if (cursor == null) return;
        int line = controller.Game.IsChoosing ? ChoiceLine(page, controller.Game) : -1;
        bool show = line >= 0;
        if (cursor.enabled != show) cursor.enabled = show;
        if (!show) return;
        Blink();
        int left    = AuthorStyle.Enabled ? AuthorStyle.DialogueTextLeft : DialogueWindow.TextLeft;
        int top     = AuthorStyle.Enabled ? AuthorStyle.DialogueTextTop  : DialogueWindow.TextTop;
        int advance = AuthorStyle.Enabled ? AuthorStyle.DialogueLineAdvance
                                          : ScreenLayout.LineAdvance;
        var rt = cursor.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(16f * DialogueWindow.CanvasScale,
                                   16f * DialogueWindow.CanvasScale);
        rt.anchoredPosition = new Vector2(left * DialogueWindow.CanvasScale,
                                          -(top + advance * line) * DialogueWindow.CanvasScale);
    }

    /// <summary>
    /// 選んでいる行の頭に文字の印を置く（印の絵が繋がっていないときの控え）。
    /// 文言の「◎」は原典の文字列にもともと入っているもの。
    /// </summary>
    static string WithMark(string page, TowerGame game)
    {
        if (string.IsNullOrEmpty(page)) return page;
        var lines = page.Split('\n');
        int seen = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (!IsChoiceLine(lines[i])) continue;
            if (seen == game.ChoiceIndex)
                lines[i] = "→" + lines[i].TrimStart('　');
            seen++;
        }
        return string.Join("\n", lines);
    }

    /// <summary>開いている途中のコルーチン。出し直すときは止める。</summary>
    Coroutine opening;

    void StartOpening(string page)
    {
        if (panel == null) return;
        // 途中で止めると印が立ったままになるので、ここで戻しておく
        if (opening != null) StopCoroutine(opening);
        waiting = typing = revealAll = false;
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
        var r = Rect(page);
        int lines = DialogueBox.WindowLines(page);
        int times = 0x14 * lines / DialogueWindow.RowStep + 1;
        bool fast = controller != null && controller.Game != null
                    && controller.Game.Player.FastMode;

        // **一瞬で出す**（作者の指定。AuthorStyle.InstantWindow）。
        // 原典どおり塗り広げたいときは false にする。
        if (AuthorStyle.InstantWindow)
        {
            // **待っている間は窓も文字も出さない**。ここで高さを0にしておかないと、
            // 「ゼノが現れるのと同時にセリフが出る」ことになる（作者の報告）。
            if (body != null) body.enabled = false;
            panel.sizeDelta = new Vector2(r.width * DialogueWindow.CanvasScale, 0f);
            waiting = true;
            while (Waiting()) yield return null;      // 歩き終わり・点滅・登場を待つ
            waiting = false;
            panel.sizeDelta = DialogueWindow.SizeOnCanvas(r);
            if (body != null) body.enabled = true;
            yield return Type();
            opening = null;
            yield break;
        }

        if (body != null) body.enabled = false;
        // 原典は**点滅が終わってから**窓を出す（0x6ae0 は点滅→0x1e待ち→0x94ba）。
        if (Waiting())
        {
            panel.sizeDelta = new Vector2(r.width * DialogueWindow.CanvasScale, 0f);
            while (Waiting()) yield return null;
        }
        for (int i = 0; i <= times - 1; i++)
        {
            // 作者の見せ方では窓が原典より高いので、同じきざみ数で下端まで届かせる。
            int h = AuthorStyle.Enabled
                ? Mathf.Min(DialogueWindow.StripHeight
                            + Mathf.RoundToInt((r.height - DialogueWindow.StripHeight)
                                               * i / (float)Mathf.Max(1, times - 1)),
                            r.height)
                : Mathf.Min(DialogueWindow.StripHeight + DialogueWindow.RowStep * i,
                            r.height);
            panel.sizeDelta = new Vector2(r.width * DialogueWindow.CanvasScale,
                                          h * DialogueWindow.CanvasScale);
            if (!fast) yield return new WaitForSeconds(MonsterAnimation.FrameSeconds);
        }
        panel.sizeDelta = DialogueWindow.SizeOnCanvas(r);
        if (body != null) body.enabled = true;
        yield return Type();
        opening = null;
    }

    /// <summary>まだ窓が出ていない（動きの終わりを待っている）か。</summary>
    public bool IsWaiting { get { return waiting; } }
    /// <summary>本文を出している途中か。</summary>
    public bool IsTyping { get { return typing; } }

    bool waiting, typing, revealAll;

    /// <summary>残りを全部出す（出し終わる前に決定を押したとき）。</summary>
    public void RevealAll() { revealAll = true; }

    /// <summary>
    /// 本文を1文字ずつ出す（作者の指定。<see cref="AuthorStyle.TypeCharsPerSecond"/>）。
    /// 選ばせている窓は原典どおり一度に出す（印を動かして選ぶので途中では困る）。
    /// </summary>
    System.Collections.IEnumerator Type()
    {
        if (body == null) yield break;
        bool choosing = controller != null && controller.Game != null
                        && controller.Game.IsChoosing;
        if (AuthorStyle.TypeCharsPerSecond <= 0f || choosing)
        {
            body.maxVisibleCharacters = int.MaxValue;
            yield break;
        }
        body.ForceMeshUpdate();
        int total = body.textInfo.characterCount;
        typing = true; revealAll = false;
        body.maxVisibleCharacters = 0;
        float shown = 0f;
        while (body.maxVisibleCharacters < total && !revealAll)
        {
            shown += Time.deltaTime * AuthorStyle.TypeCharsPerSecond;
            body.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
            yield return null;
        }
        body.maxVisibleCharacters = int.MaxValue;
        typing = false; revealAll = false;
    }

    /// <summary>
    /// 盤面の演出が終わるのを待っているか。原典は**動きが終わってから**窓を出す
    /// （3階の 0x6ae0 は点滅→待ち→0x94ba、20階の 0x3044 は10コマ→待ち→セリフ121）。
    /// </summary>
    bool Waiting()
    {
        // **これから点滅させるもの**も待つ。積んだ直後はまだ動き出していないので、
        // これを見ないと「ゼノが出るより先にセリフが出る」ことになる。
        if (controller != null && controller.Game != null && controller.Game.HasBlinks)
            return true;
        if (controller != null && controller.IsSliding) return true;   // 歩き終わるまで
        // **人が歩いている間も待つ**。10階の盗賊は9階から歩いてきて
        // 主人公の目の前で止まってから話し出す（作者の指摘）。
        if (controller != null && controller.IsWalking) return true;
        // **盤面を描き出している間も待つ**。暗転中にセリフだけ浮かんでいた（同）。
        if (controller != null && controller.IsDrawing) return true;
        return (blink != null && blink.IsPlaying) || (boss != null && boss.IsPlaying);
    }

    /// <summary>開いている途中か（テストから見る）。</summary>
    public bool IsOpening { get { return opening != null; } }

    /// <summary>
    /// この一枚を出す窓の位置と大きさ。ふだんは原典どおり（0x94ba）だが、
    /// **作者の見せ方**（<see cref="AuthorStyle"/>）のときは画面いっぱいに近い広い窓にする。
    /// 遊んでもらった報告の5番目「セリフも私が作ったやつの方が見やすい」に合わせたもの。
    /// </summary>
    public static RectInt Rect(string page)
    {
        if (AuthorStyle.Enabled) return AuthorStyle.DialogueFor(page);
        return DialogueWindow.ForBody(page, high: HighFor(page));
    }

    /// <summary>窓を置き直す。</summary>
    void Layout(string page)
    {
        if (panel == null) return;
        var r = Rect(page);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = DialogueWindow.ToCanvas(r);
        panel.sizeDelta = DialogueWindow.SizeOnCanvas(r);
    }

    /// <summary>
    /// 上端50に開く窓か。原典の会話表示の呼び出しを全部拾った
    /// `docs/original/dialogue_windows.txt` から取ったもの。
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

    /// <summary>
    /// 上端50に開くセリフ。**以前は107・159・156の3件だけだと書いていたが誤り**で、
    /// `docs/original/dialogue_windows.txt`（原典の呼び出しを全部拾った表）を見ると
    /// 場面の中から呼ばれるものを含めて10件ある。
    ///
    /// <code>
    ///   0x008b5b  53 … 姿の見えない声（10行の窓）
    ///   0x0006eb 107 … 盗賊との再会
    ///   0x006be1 110 … 3階のゼノの口上
    ///   0x006930 130 / 0x006a49 131 … 金騎士敗走
    ///   0x007257 150 / 0x007294 151 … 金騎士処刑（ゼノの声）
    ///   0x00d391 155 … 最後のゼノ
    ///   ...      156 / 0x001aa9 159 … 姿の見えない声
    /// </code>
    /// </summary>
    public static readonly int[] HighRecords = { 53, 107, 110, 130, 131,
                                                 150, 151, 155, 156, 159 };

    /// <summary>次のページへ。もう無ければ false。</summary>
    public bool Advance()
    {
        if (controller == null || controller.Game == null) return false;
        return controller.Game.NextPage();
    }
}
