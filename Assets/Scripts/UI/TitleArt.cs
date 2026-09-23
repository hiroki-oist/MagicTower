using UnityEngine;

/// <summary>
/// 起動時の題名画面の**見た目の案**（作者の指定。原典にこの画面は無い）。
///
/// 原典 PC98版は起動するといきなり1階から始まり、題名は序章の途中で1度出るだけ
/// （<see cref="TitleScreen"/>）。スマホ版では**触るまで待つ画面**が要るので、
/// ここに案を並べて <see cref="Choice"/> で1つ選ぶ。
/// どの案も絵は原典のものだけで作ってある。
///
/// <code>
///   看板     … MTMOJI（Title_Text.png）から抜いた「魔法の塔」の看板を、
///              石積みの地の上に置く。
///   題名絵   … MTMOJI をそのまま1枚絵として敷く（石積みの枠つき）。
///   塔       … 終幕の塔（MTOWER1/2）を立てて、その上に看板を置く。
///   原典     … 原典の題名画面そのまま（真っ黒に2行）。
/// </code>
/// </summary>
public static class TitleArt
{
    public enum Style { Signboard, Picture, Tower, Original }

    /// <summary>今どの案を出すか（作者が選ぶ）。</summary>
    public const Style Choice = Style.Picture;

    /// <summary>点滅させる言葉（作者の指定）。</summary>
    public const string StartLine = "Touch to start game";

    /// <summary>点滅の周期（秒）。消えている間と出ている間で同じ長さ。</summary>
    public const float BlinkSeconds = 0.6f;

    /// <summary>看板の絵（MTMOJI から抜いたもの。docs/make_title.py が作る）。</summary>
    public const string LogoPath  = "Assets/Sprites/Screens/Title_Logo.png";
    /// <summary>題名の1枚絵（MTMOJI そのもの）。</summary>
    public const string PicturePath = "Assets/Sprites/Screens/Title_Text.png";
    /// <summary>
    /// その絵を**縦長の画面に合わせて縦に伸ばした**もの（docs/make_title.py が作る）。
    /// 枠は封印扉のタイルでできていて y=256..288 の帯が1ドットも違わず繰り返せるので、
    /// その帯を挟んで部屋を下へ伸ばしてある。題名は上に残る。
    /// </summary>
    public const string TallPath = "Assets/Sprites/Screens/Title_Tall.png";
    public static readonly Vector2Int TallSize = new Vector2Int(640, 1424);
    /// <summary>縦長の絵のどの高さに点滅を置くか（絵のドット）。部屋の真ん中あたり。</summary>
    public const int TallStartY = 700;
    /// <summary>絵の地の色（＝絵そのものの背景）。縦に足りないぶんをこれで埋める。</summary>
    public static readonly Color Field = new Color32(0x44, 0x44, 0x44, 0xff);
    /// <summary>終幕の塔と、その足元の地面。</summary>
    public const string TowerPath  = "Assets/Sprites/Screens/Ending_Tower1.png";
    public const string GroundPath = "Assets/Sprites/Screens/Ending_Tower2.png";
    /// <summary>石積み（上下の帯と同じもの）。</summary>
    public const string BrickPath  = PhoneScreen.BrickPath;

    // ---- 原典の画面（640x400）の中での置き場所 ----

    /// <summary>看板の大きさ（切り出したまま）。</summary>
    public static readonly Vector2Int LogoSize = new Vector2Int(300, 104);

    /// <summary>看板の中心。塔の案は塔とぶつからないよう上へ寄せる。</summary>
    public static Vector2Int LogoAt(Style s)
    {
        return s == Style.Tower ? new Vector2Int(320, 80) : new Vector2Int(320, 150);
    }

    /// <summary>塔の中心と大きさ（終幕の絵を縮めて地面の上に立たせる）。</summary>
    public static readonly Vector2Int TowerAt = new Vector2Int(320, 245);
    public static readonly Vector2Int TowerSize = new Vector2Int(130, 165);

    /// <summary>点滅する言葉の中心。案ごとに空いている場所が違う。</summary>
    public static Vector2Int StartAt(Style s)
    {
        switch (s)
        {
            case Style.Picture:  return new Vector2Int(320, 285);   // 石積みの枠の中の空き
            case Style.Tower:    return new Vector2Int(320, 355);   // 地面の上
            case Style.Original: return new Vector2Int(320, 300);   // 2行の下
            default:             return new Vector2Int(320, 270);   // 看板の下
        }
    }

    /// <summary>その案で地に敷くもの。</summary>
    public static bool UsesBrick(Style s)   { return s == Style.Signboard; }
    public static bool UsesPicture(Style s) { return s == Style.Picture; }
    public static bool UsesTower(Style s)   { return s == Style.Tower; }
    public static bool UsesLogo(Style s)    { return s == Style.Signboard || s == Style.Tower; }
    public static bool UsesLines(Style s)   { return s == Style.Original; }
}
