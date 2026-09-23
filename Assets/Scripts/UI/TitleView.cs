using UnityEngine;
using TMPro;

/// <summary>
/// 起動したときに出る題名画面（作者の指定。原典にこの画面は無い）。
///
/// 触るかキーを押すと**必ず最初から**始める（続きから始める入口は作らない、
/// という作者の指定。続きは下の「ロード」のボタンから読む）。
/// 「Touch to start game」は <see cref="TitleArt.BlinkSeconds"/> で点滅する。
///
/// 見た目は <see cref="TitleArt.Choice"/> の案に従い、要らない絵は出さない。
/// 絵はどれも原典のもの（MTMOJI・MTOWER1/2・LCG の石積み）。
/// </summary>
public class TitleView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] UnityEngine.UI.Image backdrop;   // 地（黒／石積み）
    [SerializeField] UnityEngine.UI.Image picture;    // 題名の1枚絵（MTMOJI・横長のまま）
    [SerializeField] UnityEngine.UI.Image tall;       // 同じ絵を縦長にしたもの
    [SerializeField] UnityEngine.UI.Image ground;     // 終幕の地面
    [SerializeField] UnityEngine.UI.Image tower;      // 終幕の塔
    [SerializeField] UnityEngine.UI.Image logo;       // 「魔法の塔」の看板
    [SerializeField] TextMeshProUGUI line1, line2;    // 原典の2行
    [SerializeField] TextMeshProUGUI start;           // 点滅する言葉（横長の絵の中）
    [SerializeField] TextMeshProUGUI startTall;       // 〃（縦長の絵の中）

    /// <summary>まだ題名を出しているか。出ている間は遊びの入力を受けない。</summary>
    public bool IsShowing { get; private set; }

    float blink;

    void Awake()
    {
        Apply(TitleArt.Choice);
        Show();
    }

    /// <summary>案に合わせて、使う絵だけ出す。</summary>
    public void Apply(TitleArt.Style style)
    {
        // 題名絵の案は**縦長に伸ばしたほう**を使う（画面が縦長なので）。
        bool pic = TitleArt.UsesPicture(style);
        if (tall    != null) tall.enabled    = pic;
        if (picture != null) picture.enabled = false;
        if (ground  != null) ground.enabled  = TitleArt.UsesTower(style);
        if (tower   != null) tower.enabled   = TitleArt.UsesTower(style);
        if (logo    != null) logo.enabled    = TitleArt.UsesLogo(style);
        if (line1   != null) line1.enabled   = TitleArt.UsesLines(style);
        if (line2   != null) line2.enabled   = TitleArt.UsesLines(style);
        if (backdrop != null)
        {
            // 石積みを敷くのは看板の案だけ。題名絵の案は**絵と同じ地の色**で埋めて
            // 継ぎ目を消す。ほかは真っ黒。
            backdrop.enabled = true;
            backdrop.type = TitleArt.UsesBrick(style)
                            ? UnityEngine.UI.Image.Type.Tiled
                            : UnityEngine.UI.Image.Type.Simple;
            backdrop.color = TitleArt.UsesBrick(style) ? Color.white
                           : pic ? TitleArt.Field : Color.black;
        }
    }

    /// <summary>今の案で点滅させる字。</summary>
    TextMeshProUGUI Blinking
    {
        get { return TitleArt.UsesPicture(TitleArt.Choice) ? startTall : start; }
    }

    public void Show()
    {
        IsShowing = true;
        blink = 0f;
        SetVisible(true);
    }

    public void Hide()
    {
        IsShowing = false;
        SetVisible(false);
    }

    void SetVisible(bool on)
    {
        // **自分の GameObject は消さない**（消すと Update が止まって出番が来ても出られない、
        // という1枚絵と同じ落とし穴）。絵と字の出し入れだけで見せ隠しする。
        if (!on)
        {
            if (backdrop != null) backdrop.enabled = false;
            foreach (var img in new[] { picture, tall, ground, tower, logo })
                if (img != null) img.enabled = false;
            foreach (var t in new[] { line1, line2, start, startTall })
                if (t != null) t.enabled = false;
            return;
        }
        Apply(TitleArt.Choice);
        if (line1 != null) line1.text = TitleScreen.Title;
        if (line2 != null) line2.text = TitleScreen.Copyright;
        if (start != null) start.enabled = false;
        if (startTall != null) startTall.enabled = false;
        var blink = Blinking;
        if (blink != null) { blink.enabled = true; blink.text = TitleArt.StartLine; }
    }

    void Update()
    {
        if (!IsShowing) return;

        // 点滅
        blink += Time.deltaTime;
        var text = Blinking;
        if (text != null)
            text.alpha = (blink % (TitleArt.BlinkSeconds * 2f)) < TitleArt.BlinkSeconds
                         ? 1f : 0f;

        // 触るかキーで始める。**必ず最初から**（作者の指定）。
        // 触りは**指を離したとき**に見る。押した瞬間に消してしまうと、その同じ一手が
        // 下のボタンや十字キーにも届いて、いきなり歩き出したりセーブを聞かれたりする。
        bool tapped = Input.GetMouseButtonUp(0);
        bool keyed = Input.anyKeyDown && !Input.GetMouseButton(0);
        if (!tapped && !keyed) return;
        Hide();
        if (controller != null) controller.BeginGame();
    }
}
