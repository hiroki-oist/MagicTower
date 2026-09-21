using UnityEngine;

/// <summary>
/// **作者（このリメイクの持ち主）が先に書いた旧版の見せ方**。原典 PC98版の再現ではない。
///
/// 遊んでもらった報告の5番目
/// 「戦闘時の見せ方は旧版を参考にしてほしい。黒い帯が出てきてHPが1ターンごとに減る。
///   セリフも私が作ったやつの方が見やすいのでそのやり方に」
/// を受けて入れたもの。**ここだけは完全再現から外れる**ので、
/// <see cref="Enabled"/> を false にすれば原典どおりの見せ方に戻る。
///
/// 寸法の出どころは旧版のシーン `Assets/Scenes/Floor1.unity`（commit a58a72a）の
/// BattleUI と、`Assets/Scenes/Floor_1.unity`（commit 2b383ea^）の DialogueUI。
/// どちらも Canvas は等倍（ScaleMode 0）で、**縦長のスマホ画面（およそ 600x1200）**
/// を前提に置かれていた:
///
/// <code>
///   BattleUI/BattleCanvas/Turns        幅 画面-300, 高さ  50, 中心 (   0, 800)
///   BattleUI/BattleCanvas/PlayerImage  幅 画面-310, 高さ 200, 中心 (-125, 600)
///   BattleUI/BattleCanvas/EnemyImage   幅 画面-310, 高さ 200, 中心 ( 125, 600)
///   BattleUI/BattleCanvas/PlayerStats  幅 画面-350, 高さ 150, 中心 (-125, 350)  HP:/ATK:/DEF:
///   BattleUI/BattleCanvas/EnemyStats   幅 画面-350, 高さ 150, 中心 ( 125, 350)  HP:/ATK:/DEF:
///   DialogueUI/DialogueCanvas/Dialogue 700x250, 中心 (0, 500), 文字 20
/// </code>
///
/// こちらの画布は横長（原典と同じ 640x400 を1.8倍）なので、
/// **左半分が自分・右半分が相手**の帯に置き換えてある。
/// 数字は原典の画面座標（640x400、左上が原点）で書く。
/// </summary>
public static class AuthorStyle
{
    /// <summary>作者の見せ方にするか。false なら原典どおり。</summary>
    public static readonly bool Enabled = true;

    // ------------------------------------------------------------------ 会話

    /// <summary>会話の窓の左端と横幅（ドット）。旧版は画面いっぱいに近かった。</summary>
    public const int DialogueLeft = 16, DialogueWidth = 608;

    /// <summary>窓の中心の高さ。旧版は画面の下から42%（1200中の500）＝上から58%。</summary>
    public const int DialogueCenterY = 232;

    /// <summary>行送り（ドット）。原典の20より広く取る。</summary>
    public const int DialogueLineAdvance = 26;

    /// <summary>窓の中の文字の位置（窓の左上からのずれ、ドット）。</summary>
    public const int DialogueTextLeft = 24, DialogueTextTop = 14;

    /// <summary>
    /// 文字の大きさ（ドット）。旧版は画面幅600に対して20なので、
    /// 原典の16ドット/640 より 4/3 だけ大きい。
    /// </summary>
    public const float DialogueTextDots = 64f / 3f;

    /// <summary>画布の上での文字の大きさ。</summary>
    public const float DialogueTextOnCanvas = DialogueTextDots * DialogueWindow.CanvasScale;

    /// <summary>
    /// TMP の行間（文字の大きさに対する％）。
    /// 行送り = 文字の大きさ × (1.084 + 行間/100) になるよう合わせてある
    /// （原典側の <see cref="TowerHud.InfoLineSpacing"/> と同じ出し方）。
    /// </summary>
    public const float DialogueLineSpacing = 13.5f;

    /// <summary>行数から窓の高さ（ドット）。上下の余白は原典と同じ32ドット。</summary>
    public static int DialogueHeight(int lines)
    {
        if (lines < 1) lines = 1;
        return DialogueLineAdvance * lines + DialogueWindow.StripHeight;
    }

    /// <summary>行数から窓の位置と大きさ。画面の中心に寄せる。</summary>
    public static RectInt DialogueFor(int lines)
    {
        int h = DialogueHeight(lines);
        int top = Mathf.Clamp(DialogueCenterY - h / 2, 0, DialogueWindow.ScreenHeight - h);
        return new RectInt(DialogueLeft, top, DialogueWidth, h);
    }

    // ------------------------------------------------------------------ 戦闘の帯

    /// <summary>黒い帯。画面いっぱいの横幅で、上下の中心に出す。</summary>
    public static RectInt Band { get { return new RectInt(0, 96, 640, 208); } }

    /// <summary>帯の色。旧版の EarnedCoinBG と同じく、下が透ける黒。</summary>
    public static readonly Color BandFill = new Color(0f, 0f, 0f, 0.85f);

    /// <summary>「第 N ターン」の欄（旧版 Turns）。</summary>
    public static RectInt TurnBox { get { return new RectInt(0, 104, 640, 22); } }

    /// <summary>左半分が自分、右半分が相手（旧版は PlayerImage が左、EnemyImage が右）。</summary>
    public const int PlayerCenterX = 160, EnemyCenterX = 480;

    /// <summary>
    /// 絵の大きさと上端。盤面のタイルは32ドットなので、**その2倍**にして
    /// 拡大が整数倍になるようにしてある（半端に伸ばすと絵の点がゆがむ）。
    /// </summary>
    public const int IconSize = 64, IconTop = 130;

    /// <summary>名前の欄。</summary>
    public const int NameTop = 198, NameWidth = 240, NameHeight = 22;

    /// <summary>能力値の欄（HP: / ATK: / DEF: の3行、旧版 List の並びそのまま）。</summary>
    public const int StatWidth = 160, StatHeight = 22;
    public static readonly int[] StatTops = { 226, 250, 274 };

    /// <summary>旧版の見出し（EnemyStats/List の m_text そのまま）。</summary>
    public static readonly string[] StatLabels = { "HP: ", "ATK: ", "DEF: " };

    /// <summary>自分の名前。旧版 PlayerName の文字そのまま。</summary>
    public const string PlayerName = "勇者";

    public static RectInt Icon(int centerX)
    {
        return new RectInt(centerX - IconSize / 2, IconTop, IconSize, IconSize);
    }

    public static RectInt Name(int centerX)
    {
        return new RectInt(centerX - NameWidth / 2, NameTop, NameWidth, NameHeight);
    }

    public static RectInt Stat(int centerX, int row)
    {
        return new RectInt(centerX - StatWidth / 2, StatTops[row], StatWidth, StatHeight);
    }
}
