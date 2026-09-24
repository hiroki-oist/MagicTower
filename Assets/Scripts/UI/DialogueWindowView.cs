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
        // **動きが終わるまでは窓そのものを出さない**。以前は高さを0にして隠していたが、
        // 作者の枠の絵は9分割（Sliced）なので、高さ0でも上下の縁が帯のように描かれ、
        // 点滅しているゼノを覆ってしまっていた（「セリフが先に出る」という報告）。
        bool show = dialogue && !string.IsNullOrEmpty(page) && !Waiting();
        if (panel != null && panel.gameObject.activeSelf != show)
        {
            panel.gameObject.SetActive(show);
            // **メニューの窓より前に出す**。飛翔のオーブを断られたときのように、
            // メニューを開いたまま窓が開くことがあり、後ろに隠れると読めない。
            if (show) panel.SetAsLastSibling();
        }
        if (!show) { Showing = null; waiting = typing = false; return; }

        PlaceCursor(page);
        PlaceFrames(page);
        // 印の絵が無いときだけ、選んでいる行に文字の印を付ける
        var shownText = (cursor == null && controller.Game.IsChoosing)
                      ? WithMark(page, controller.Game) : page;
        // 課金の窓だけは**矢印を出さず、選んでいない行を灰色に沈める**（作者の指定）
        if (Boxed) shownText = Dim(page, controller.Game.ChoiceIndex);
        if (page != Showing || (body != null && body.text != shownText))
        {
            Showing = page;
            if (body != null)
            {
                // ★**この窓では TMP に折り返させない**。本文はこちらで（DialogueBox で）
                //   すでに行に分けてあるので、TMP が重ねて折り返すと値段だけが
                //   次の行へ落ちる（作者の報告「.99が二行目に振り分けられる」）。
                body.enableWordWrapping = !Boxed;
                // 課金の窓だけ字を大きくする（作者の指定）
                ApplyBodySize(Boxed);
                body.text = shownText;
                body.color = Text;
            }
            if (background != null)
                background.color = AuthorStyle.Enabled ? AuthorFill : Fill;
            Layout(page);
            StartOpening(page);
        }
    }

    /// <summary>
    /// **写しを撮るための入口**（`TowerScreenshot`）。遊ばずに、課金の窓を組んだ形で出す。
    /// 本線と同じ <see cref="Dim"/> と <see cref="PlaceFrames"/> を通すので、
    /// ここで見えたものが実機でもそのまま出る。
    /// </summary>
    /// <summary>写しの道具から本文の組まれ方を見るための口。</summary>
    public TMPro.TMP_Text Body { get { return body; } }

    public void PreviewBoxed(string page, int choice)
    {
        if (panel == null) return;
        panel.gameObject.SetActive(true);
        var r = Rect(page);
        panel.anchoredPosition = DialogueWindow.ToCanvas(r);
        panel.sizeDelta = DialogueWindow.SizeOnCanvas(r);
        if (background != null)
            background.color = AuthorStyle.Enabled ? AuthorFill : Fill;
        if (body != null)
        {
            body.enabled = true;
            body.enableWordWrapping = false;
            ApplyBodySize(true);
            body.text = Dim(page, choice);
            body.color = Text;
            body.maxVisibleCharacters = int.MaxValue;
        }
        if (cursor != null) cursor.enabled = false;
        PlaceFrames(page, true);
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
    /// <summary>
    /// **選択肢を枠で囲って出す窓**か（作者の指定。いまは課金の窓だけ）。
    /// この窓では矢印を出さず、選んでいない行を灰色に沈める。
    /// </summary>
    bool Boxed
    {
        get
        {
            return controller != null && controller.Game != null
                && controller.Game.Choice == TowerGame.ChoiceKind.GoldShop;
        }
    }

    /// <summary>選択肢を囲む枠（外側の線と内側の地）。組み立て器が並べて入れる。</summary>
    [SerializeField] UnityEngine.UI.Image[] optionEdges = new UnityEngine.UI.Image[0];
    [SerializeField] UnityEngine.UI.Image[] optionFills = new UnityEngine.UI.Image[0];

    /// <summary>
    /// 品目の行の頭に置く**金貨の絵**（上の帯の所持金の欄と同じ絵）。
    /// ⚠ TMP の絵札（`&lt;sprite&gt;`）でやろうとしたら**本文ごと描かれなくなった**ので、
    /// 普通の絵として置いている（作者の報告「商店がわけわからないUIになってる」）。
    /// </summary>
    [SerializeField] UnityEngine.UI.Image[] optionCoins = new UnityEngine.UI.Image[0];

    /// <summary>
    /// 選んでいない行を灰色に沈める（<see cref="Boxed"/> の窓だけ）。
    /// 金貨の絵も同じ札で沈むので、絵ごとグレースケールになる。
    /// </summary>
    public static string Dim(string page, int choice)
    {
        if (string.IsNullOrEmpty(page)) return page;
        var lines = page.Split('\n');
        int seen = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (!IsChoiceLine(lines[i])) continue;
            if (seen != choice)
                lines[i] = DimOpen + lines[i] + DimClose;
            seen++;
        }
        return string.Join("\n", lines);
    }

    /// <summary>沈める色。原典の窓の中と同じ濃さの灰。</summary>
    public const string DimOpen = "<color=#7a7a7aff>", DimClose = "</color>";

    /// <summary>ふだんの字の大きさ（課金の窓で大きくしたあと戻すために控える）。</summary>
    float bodySize;

    /// <summary>
    /// 課金の窓だけ字を大きくする（作者の指定）。
    ///
    /// ⚠ **行送りも一緒に詰める**。字を大きくすると TMP の行の高さも増えるので、
    /// そのままだと1行ごとに数ドットずつ下へずれて、枠と字が合わなくなる
    /// （実際にずれて、下の2行が窓からはみ出した）。
    /// 行送りは `字の大きさ x (1.084 + 行間/100)` なので、
    /// 行送りが <see cref="AuthorStyle.DialogueLineAdvance"/> のままになる行間を入れる。
    /// </summary>
    void ApplyBodySize(bool boxed)
    {
        if (body == null) return;
        if (bodySize <= 0f) bodySize = body.fontSize;
        if (!boxed)
        {
            body.fontSize = bodySize;
            body.lineSpacing = AuthorStyle.DialogueLineSpacing;
            return;
        }
        body.fontSize = bodySize * PhoneScreen.ShopFontScale;
        float dots = AuthorStyle.DialogueTextDots * PhoneScreen.ShopFontScale;
        body.lineSpacing = (AuthorStyle.DialogueLineAdvance / dots - 1.084f) * 100f;
    }

    /// <summary>枠と枠のあいだに空ける隙間（ドット。作者の指定）。</summary>
    public const int ShopRowGap = 8;

    /// <summary>選択肢を囲む枠を置く。</summary>
    void PlaceFrames(string page) { PlaceFrames(page, Boxed && controller.Game.IsChoosing); }

    /// <summary>
    /// 選択肢を囲む枠を置く。**写しの道具からも同じ道を通す**ので、
    /// 本線（TowerGame）に頼らず「出すかどうか」だけを受け取る。
    /// </summary>
    public void PlaceFrames(string page, bool boxed)
    {
        bool show = boxed;
        int shown = 0, coins = 0;
        if (show && panel != null)
        {
            int left    = AuthorStyle.Enabled ? AuthorStyle.DialogueTextLeft
                                              : DialogueWindow.TextLeft;
            int top     = AuthorStyle.Enabled ? AuthorStyle.DialogueTextTop
                                              : DialogueWindow.TextTop;
            int advance = AuthorStyle.Enabled ? AuthorStyle.DialogueLineAdvance
                                              : ScreenLayout.LineAdvance;
            float w = panel.sizeDelta.x - 2f * left * DialogueWindow.CanvasScale;
            var lines = page.Split('\n');
            for (int i = 0; i < lines.Length && shown < optionEdges.Length; i++)
            {
                if (!IsChoiceLine(lines[i])) continue;
                // ★品目の行は**2行ぶんの高さ**（作者の指定）。字の行を真ん中にしたいので、
                //   半行ぶん上から始めて2行ぶんの高さを取る。空行はそのぶんを受け持つ。
                bool item = shown < GoldShop.Count;
                int rows = item ? PhoneScreen.ShopRowLines : 1;
                float half = item ? advance * 0.5f : 0f;
                // 1行ぶんの選択肢は、字がはみ出さないぎりぎりまで詰める
                // 品目でない行（復元・やめる）の枠は、字が縁に掛からないよう少し広げる
                // （作者の指定でおよそ1.2倍。あいだに1行空けてあるので重ならない）
                float gap = item ? ShopRowGap : -3;
                float y = -(top + advance * i - (item ? 3 : 2) - half)
                        * DialogueWindow.CanvasScale;
                float h = (advance * rows + 2 - gap) * DialogueWindow.CanvasScale;
                Box(optionEdges[shown], left * DialogueWindow.CanvasScale - 6f, y,
                    w + 12f, h, PhoneScreen.SlotEdge);
                Box(optionFills[shown], left * DialogueWindow.CanvasScale - 4f, y - 2f,
                    w + 8f, h - 4f, PhoneScreen.SlotFill);
                // 品目の行だけ、頭の空きに金貨を置く（高さに合わせて大きくする）
                if (item && shown < optionCoins.Length && optionCoins[shown] != null)
                {
                    float c = AuthorStyle.DialogueTextDots * PhoneScreen.ShopCoinChars
                            * DialogueWindow.CanvasScale;
                    Box(optionCoins[shown], left * DialogueWindow.CanvasScale,
                        y - (h - c) / 2f, c, c, Color.white);
                    coins = shown + 1;
                }
                shown++;
            }
        }
        for (int i = 0; i < optionEdges.Length; i++)
        {
            bool on = i < shown;
            if (optionEdges[i] != null && optionEdges[i].enabled != on)
                optionEdges[i].enabled = on;
            if (optionFills[i] != null && optionFills[i].enabled != on)
                optionFills[i].enabled = on;
        }
        for (int i = 0; i < optionCoins.Length; i++)
        {
            bool on = i < coins;
            if (optionCoins[i] != null && optionCoins[i].enabled != on)
                optionCoins[i].enabled = on;
        }
    }

    static void Box(UnityEngine.UI.Image img, float x, float y, float w, float h, Color c)
    {
        if (img == null) return;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        img.color = c;
    }

    void PlaceCursor(string page)
    {
        if (cursor == null) return;
        // 枠で囲う窓では矢印を出さない（作者の指定）
        int line = controller.Game.IsChoosing && !Boxed ? ChoiceLine(page, controller.Game) : -1;
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

    /// <summary>
    /// まだ窓が出ていない（動きの終わりを待っている）か。
    ///
    /// **窓を出す前の段階も含める**。窓そのものを出さないようにしたので、
    /// 開く処理（Open）に入る前は `waiting` が立たず、その隙に押した決定で
    /// セリフが飛ばされていた（3階のゼノ・2階の会話・20階の合体）。
    /// </summary>
    public bool IsWaiting { get { return waiting || Waiting(); } }
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
        if (controller != null && controller.Game != null
            && (controller.Game.HasBlinks || controller.Game.HasPendingView))
            return true;      // 渡したばかりで、まだ動き出していないものも待つ
        if (controller != null && controller.IsSliding) return true;   // 歩き終わるまで
        // **人が歩いている間も待つ**。10階の盗賊は9階から歩いてきて
        // 主人公の目の前で止まってから話し出す（作者の指摘）。
        if (controller != null && controller.IsWalking) return true;
        // **盤面を描き出している間も待つ**。暗転中にセリフだけ浮かんでいた（同）。
        if (controller != null && controller.IsDrawing) return true;
        // **ひと呼吸おく**。合体が終わった直後・暗転が明けた直後に喋らせない。
        if (controller != null && controller.IsSettling) return true;
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
