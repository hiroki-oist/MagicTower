using UnityEngine;

/// <summary>
/// **スマホ縦画面の並べ方**。作者の指定で、中国語版スマホゲームの画面に合わせたもの。
/// 原典 PC98版（横長・左右に帯）の再現ではない（<see cref="AuthorStyle"/> と同じ扱い）。
///
/// 見せてもらった画面の並び（上から）:
/// <code>
///   金 / 黄の鍵 / 紫の鍵 / 赤の鍵          … 4つの小さな枠
///   第N階 / 生命 / 攻撃 / 防御              … 階の旗と3つの枠
///   剣の名前＋絵 / 盾の名前＋絵             … 装備
///   盤面（11x11。画面の幅いっぱい）
///   持ち物の帯（15マス）
///   下のボタン（左に4つ・中央に十字・右に4つ）
/// </code>
///
/// 上下の帯の地は、原典の枠の左右の欄と同じ**石積み**（`Assets/Sprites/Hud/Brick.png`。
/// 枠の絵 LCG.P58 #1 から切り出したもの）。作者の指定。
///
/// 盤面はカメラが写すので、<see cref="PhoneLayoutView"/> が
/// 「盤面の枠（MapArea）の画面上の位置」にカメラを合わせる。
/// </summary>
public static class PhoneScreen
{
    /// <summary>画布の基準（縦画面）。幅を基準に合わせるので、高さは端末で変わる。</summary>
    public const float ReferenceWidth = 1080f, ReferenceHeight = 1920f;

    /// <summary>上の帯の3段の高さ（画布の単位）。</summary>
    public const float RowHeight = 110f;
    public const int Rows = 3;
    public static float TopHeight { get { return RowHeight * Rows; } }

    /// <summary>
    /// 持ち物の帯と、下のボタンの高さ。
    /// 上の帯（330）と合わせて 840 にしてあるので、9:16 の画面では
    /// 盤面がちょうど**幅いっぱいの正方形**（1080x1080）になる。
    /// </summary>
    public const float ItemBarHeight = 110f, ButtonsHeight = 400f;
    public static float BottomHeight { get { return ItemBarHeight + ButtonsHeight; } }

    /// <summary>盤面のマス数（原典と同じ11x11）。</summary>
    public const int MapCells = ScreenLayout.MapCells;

    /// <summary>盤面を写すカメラの大きさ（正方形の枠に 11 マスちょうど）。</summary>
    public const float CameraSize = MapCells / 2f;

    /// <summary>枠の中の余白（画布の単位）。</summary>
    public const float Pad = 8f;

    /// <summary>上下の帯に敷く石積みと、欄の小さな絵。</summary>
    public const string BrickPath   = "Assets/Sprites/Hud/Brick.png";
    public const string HpIconPath  = "Assets/Sprites/Hud/Icon_Hp.png";
    public const string AtkIconPath = "Assets/Sprites/Hud/Icon_Attack.png";
    public const string DefIconPath = "Assets/Sprites/Hud/Icon_Defense.png";
    public const string GoldIconPath= "Assets/Sprites/Hud/Icon_Gold.png";

    /// <summary>枠の中の色。原典の窓の中と同じ濃い灰。</summary>
    public static readonly Color BoxFill = new Color32(0x44, 0x44, 0x44, 0xd8);
    /// <summary>階の旗の色（見せてもらった画面の赤茶）。</summary>
    public static readonly Color FloorFill = new Color32(0xa8, 0x32, 0x28, 0xff);
    /// <summary>左のボタン（緑）と右のボタン（紫）。見せてもらった画面に合わせた。</summary>
    public static readonly Color LeftButton  = new Color32(0x1e, 0x6e, 0x50, 0xff);
    public static readonly Color RightButton = new Color32(0x4a, 0x3e, 0x7a, 0xff);

    /// <summary>
    /// 下のボタンの文字。**まだどれも繋がっていない**（押しても何も起きない）。
    /// 作者の指定で、右の4つは課金ものの見た目だけ。移動は当面キーボードの矢印のまま。
    /// </summary>
    /// 文字は**短く**してある（作者の指定。左右を細くして真ん中の十字を大きくするため）。
    public static readonly string[] LeftButtons =
        { "セーブ", "ロード", "ショップ", "メニュー" };
    public static readonly string[] RightButtons =
        { "商店", "飛行器", "ダメージ", "妖精" };

    /// <summary>左右のボタンの幅と、真ん中の十字の大きさ（作者の指定で十字を大きく）。</summary>
    public const float SideButtonWidth = 190f, DPadSize = 380f;

    /// <summary>右のボタンのうち、**繋がっている**のはこれ（勇者のオーブの一覧を出す）。</summary>
    public const int DamageButtonIndex = 2;

    /// <summary>
    /// 持ち物の帯を触ったときに聞く言葉（作者の指定。原典にこの入口は無い）。
    /// 「◎」は原典の選ばせる窓と同じ印で、見た目の層がこの行を数えて印を置く。
    /// </summary>
    public const string UseAsk = "{0}を使いますか？\n　　◎はい\n　　◎いいえ";

    /// <summary>持ち物の帯に一度に見せる数（残りは左右の矢印で送る）。</summary>
    public const int ItemsPerPage = 8;

    /// <summary>帯の左右に置く矢印の幅。</summary>
    public const float ArrowWidth = 64f;

    /// <summary>帯の地の色（見せてもらった画面に合わせて黒）。</summary>
    public static readonly Color ItemBarFill = new Color32(0x0c, 0x0c, 0x12, 0xff);

    /// <summary>
    /// 移動の十字キーの絵。**作者が6案から選んだもの**（B＝黒い円盤に矢印と中央のつまみ）。
    /// 絵は `docs/make_dpad.py` が作る。色は原典の枠（LCG.P58 #1）から取ってある。
    /// </summary>
    public const string DPadSpritePath = "Assets/Sprites/Hud/DPad_B.png";

    /// <summary>十字キーの真ん中の「押しても動かない」部分（半径に対する割合）。</summary>
    public const float DPadDeadZone = 0.28f;

    /// <summary>
    /// 十字キーのどこを押したかを向きに直す。
    /// 円の外と、真ん中のつまみ（<see cref="DPadDeadZone"/>）は動かさない。
    /// 斜めは、ずれの大きいほうの向きに倒す（原典も斜めには進めない）。
    /// </summary>
    public static Vector2Int DirFromTap(Vector2 delta, float radius)
    {
        if (radius <= 0f) return Vector2Int.zero;
        float d = delta.magnitude;
        if (d > radius || d < radius * DPadDeadZone) return Vector2Int.zero;
        return Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
             ? new Vector2Int(delta.x > 0f ? 1 : -1, 0)
             : new Vector2Int(0, delta.y > 0f ? 1 : -1);
    }

    /// <summary>
    /// 知らせ（「鍵を手に入れた！」など）を**画面の中央に出して、少し置いてから
    /// 薄くして消す**ときの時間（秒）。作者の指定（旧版がこの見せ方だった）。
    /// </summary>
    public const float ToastHold = 0.5f, ToastFade = 0.5f;

    /// <summary>その文字の大きさと、下に敷く帯の高さ（画布の単位）。</summary>
    public const float ToastFontSize = 46f, ToastHeight = 120f;

    /// <summary>盤面の枠の、画布の中での大きさ（正方形）。高さが足りなければ縮める。</summary>
    public static float MapSide(float canvasWidth, float canvasHeight)
    {
        float free = canvasHeight - TopHeight - BottomHeight;
        return Mathf.Max(0f, Mathf.Min(canvasWidth, free));
    }
}
