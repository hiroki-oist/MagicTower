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
    public const float RowHeight = 100f;
    public const int Rows = 3;
    public static float TopHeight { get { return RowHeight * Rows; } }

    /// <summary>
    /// 持ち物の帯と、下のボタンの高さ。
    /// 上の帯（330）と合わせて 840 にしてあるので、9:16 の画面では
    /// 盤面がちょうど**幅いっぱいの正方形**（1080x1080）になる。
    /// </summary>
    public const float ItemBarHeight = 110f, ButtonsHeight = 430f;
    public static float BottomHeight { get { return ItemBarHeight + ButtonsHeight; } }

    /// <summary>
    /// 9:16 より縦長の端末で下に空くぶんのうち、**下の帯に回す割合**（作者の指定）。
    /// 上の余白はノッチ避けに残したいので全部は使わない。
    /// </summary>
    public const float BottomSlackShare = 0.5f;

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
        { "セーブ", "ロード", "メニュー" };
    public static readonly string[] RightButtons =
        { "商店", "飛翔", "勇者" };

    /// <summary>左右のボタンの幅と、真ん中の十字の大きさ（作者の指定で十字を大きく）。</summary>
    /// <summary>
    /// ★作者の指定で十字を大きくした（決定と矢印を離すため）。
    /// 下の帯は端末が縦長なら余ったぶんだけ伸びる（<see cref="PhoneLayoutView"/>）。
    /// </summary>
    public const float SideButtonWidth = 190f, DPadSize = 480f;

    /// <summary>十字キーを下の帯の下端からどれだけ上げるか。</summary>
    public const float DPadBottomMargin = 24f;

    /// <summary>
    /// 右のボタンのうち、オーブの近道になっているもの（作者の指定）。
    /// **持っていない間は灰色にして、触ると持っていないことを知らせる。**
    /// </summary>
    public const int FlightButtonIndex = 1, HeroButtonIndex = 2;

    /// <summary>持っていないオーブのボタンの色（灰色に沈ませる）。</summary>
    public static readonly Color LockedButton = new Color32(0x3a, 0x3a, 0x42, 0xff);
    /// <summary>その上の字の色。ふだんの字は白。</summary>
    public static readonly Color LockedText   = new Color32(0x7a, 0x7a, 0x86, 0xff);
    public static readonly Color ButtonText   = Color.white;

    /// <summary>まだ持っていないオーブのボタンを触ったときの知らせ（作者の指定）。</summary>
    public const string OrbLocked = "{0}を手に入れると使用可能になります。";

    // ------------------------------------------------------------------ 課金の窓
    //
    // 作者の指定（2026-09-24）:
    //   「商店は課金用のボタンとして残します。ゴールドの購入課金で
    //     『一度購入すれば、次回プレイからは無料で同じゴールドを購入できる』と書いて。
    //     400ゴールドを160円、1000ゴールドを320円、4000ゴールドを640円に」

    /// <summary>
    /// 課金の窓の見出し（1行18文字まで）。
    ///
    /// ⚠ Apple の審査指針（3.1.1）に合わせて**買い切りである**ことと、
    /// **復元できる**ことを窓の中で言い切る。値段は下の <see cref="ShopYen"/> ではなく
    /// **App Store から受け取った現地通貨の表示**に差し替えること（<see cref="GoldShop"/>）。
    /// </summary>
    public static readonly string[] ShopIntro =
    {
        "ゴールドを購入します。",
        "どれも買い切りです。一度購入すれば、",
        "次回プレイからは無料で同じゴールドを",
        "受け取れます（機種を変えても復元可）",
    };

    /// <summary>
    /// 品目の名前（作者の指定）。App Store Connect の表示名と同じ書き方にそろえる。
    /// 頭に金貨の絵（<see cref="GoldIconPath"/> を TMP の絵札にしたもの）を置く。
    /// </summary>
    public const string ShopItemName = "{0}ゴールドの引換券";

    /// <summary>
    /// 品目の行の頭に空けておく幅（全角2つ）。ここに金貨の絵を重ねる。
    /// **全角空白2つで始まる行＝選ばせている行**、という決まりにも合う
    /// （<see cref="DialogueWindowView.IsChoiceLine"/>）。
    /// </summary>
    public const string ShopIndent = "　　";

    /// <summary>「購入を復元する」の行。Apple の審査指針 3.1.1 で置くことが要る。</summary>
    public const string ShopRestore = "購入を復元する";

    /// <summary>復元したときの知らせ。</summary>
    public const string ShopRestored = "購入を復元しました（{0}件）。";
    public const string ShopNothingToRestore = "復元できる購入はありませんでした。";
    public const string ShopRestoring = "購入を復元しています……";

    /// <summary>売り物。金額は円。</summary>
    public static readonly int[] ShopGold = { 400, 1000, 4000 };
    public static readonly int[] ShopYen  = { 160,  320,  640 };

    /// <summary>買ったあとの一言。</summary>
    public const string ShopBought = "{0}ＧＯＬＤを手に入れた！";

    /// <summary>もう買ってあるものの値段のかわりに出す字。</summary>
    public const string ShopFree = "　無料";

    /// <summary>
    /// メニューの最後の項目（作者の指定）。原典は「　　ゲーム終了」で DOS へ戻るが、
    /// スマホに「終了」は要らないので**題名画面へ戻る**にした。
    /// </summary>
    public const string BackToTitle = "　　タイトルへ戻る";

    /// <summary>
    /// 持ち物の帯を触ったときに聞く言葉（作者の指定。原典にこの入口は無い）。
    /// 「◎」は原典の選ばせる窓と同じ印で、見た目の層がこの行を数えて印を置く。
    /// </summary>
    public const string UseAsk = "{0}を使いますか？\n　　◎はい\n　　◎いいえ";

    /// <summary>
    /// 下の「セーブ」「ロード」を押したときに聞く言葉（作者の指定）。
    /// 原典はメニューから8つのスロットを選ばせるが、こちらは**1つだけ**にして
    /// その場で「はい／いいえ」を聞く。メニューの中は原典どおり8つのまま。
    /// </summary>
    public const string SaveAsk = "セーブしますか？\n　　◎はい\n　　◎いいえ";
    public const string LoadAsk = "ロードしますか？\n　　◎はい\n　　◎いいえ";
    public const string Saved   = "セーブしました。";
    public const string Loaded  = "ロードしました。";

    /// <summary>
    /// 飛翔のオーブの階選びの案内（作者の指定）。
    /// 原典の文言（Messages.txt の `teleport_menu`）は
    /// 「↑↓で移動する階を選択して、**リターンで**決定してください。」だが、
    /// スマホに「リターン」は無いのでそこだけ落としてある。
    /// </summary>
    public const string FlightPrompt = "↑↓で移動する階を選択して、決定してください。";

    /// <summary>ボタンで使うスロット（原典のスロット1）。</summary>
    public const int QuickSlot = 1;

    /// <summary>下の左の並びの中の「セーブ」「ロード」の位置。</summary>
    /// ★作者の指定で「ショップ」（左）と「妖精」（右）は無くした。
    /// 左は セーブ / ロード / メニュー の3つ。
    public const int SaveButtonIndex = 0, LoadButtonIndex = 1, MenuButtonIndex = 2;

    /// <summary>右の「商店」は**課金の窓**を開く（作者の指定）。</summary>
    public const int ShopButtonIndex = 0;

    /// <summary>
    /// 持ち物の帯に一度に見せる数（残りは左右の矢印で送る）。
    /// ★作者の指定で **8つ＝ちょうど2ページ**。1枠は 119単位＝iPhone で 44pt。
    /// **並びは物体IDの順のまま**（持っていない所が空くのが、そのまま
    /// 「まだ手に入れていないものがある」という手がかりになる、という作者の指定）。
    /// </summary>
    public const int ItemsPerPage = 8;

    /// <summary>
    /// 帯に取る「場所」の数。持ち物は15しか無いので**16個ぶんの幅**を取り、
    /// 最後の1つは**掘らずに石のまま**にしてある（作者の指定）。
    /// 8つずつのちょうど2ページに収まり、2ページ目の端に開いた石が
    /// 「持ち物は15で打ち止め」を見た目で伝える。
    /// </summary>
    public const int ItemBarPositions = 16;

    /// <summary>帯の左右に置く矢印の幅。</summary>
    public const float ArrowWidth = 64f;

    // ---- 持ち物の帯（★作者が選んだ「石壁に彫った棚」）----
    //
    // 地は**上下の帯と同じ石積み**を地続きに敷き、ますだけを彫り込む。
    // 持っていない枠も沈んだ石として見えるので、物体ID順に並べた空きが
    // そのまま「まだ手に入れていないものがある」という手がかりになる。
    // 以前は真っ黒な地（#0c0c12）で、上下の石積みからも窓からも浮いていた。

    /// <summary>ますの縁（封印扉のタイル ID4 の青灰から）。</summary>
    public static readonly Color SlotEdge      = new Color32(0x8f, 0x8f, 0xb4, 0xff);
    /// <summary>ますの地（持っているとき）。</summary>
    public static readonly Color SlotFill      = new Color32(0x3b, 0x3b, 0x46, 0xff);
    /// <summary>空いているますの縁と地（沈ませる）。</summary>
    public static readonly Color EmptySlotEdge = new Color32(0x5a, 0x5a, 0x72, 0xff);
    public static readonly Color EmptySlotFill = new Color32(0x2c, 0x2c, 0x34, 0xff);

    /// <summary>ますの外の余白と、縁の太さ（画布の単位）。</summary>
    public const float SlotInsetX = 5f, SlotInsetY = 6f, SlotEdgeWidth = 3f;

    /// <summary>
    /// 移動の十字キーの絵。**作者が6案から選んだもの**（B＝黒い円盤に矢印と中央のつまみ）。
    /// 絵は `docs/make_dpad.py` が作る。色は原典の枠（LCG.P58 #1）から取ってある。
    /// </summary>
    public const string DPadSpritePath = "Assets/Sprites/Hud/DPad_B.png";

    /// <summary>
    /// 十字キーは**5枚に分けて**置く（上・右・下・左の扇と、真ん中の決定ボタン）。
    /// 押している所だけ色を変えるため（1枚だとどこを押したのか分からない、という報告）。
    /// 扇は45度で切ってあり、真ん中のボタンとの間にも隙間がある。
    /// </summary>
    public static readonly string[] DPadPartPaths =
    {
        "Assets/Sprites/Hud/DPad_Up.png",
        "Assets/Sprites/Hud/DPad_Right.png",
        "Assets/Sprites/Hud/DPad_Down.png",
        "Assets/Sprites/Hud/DPad_Left.png",
        "Assets/Sprites/Hud/DPad_Center.png",
    };

    /// <summary>その並びの中での向き（0=上 1=右 2=下 3=左、4=決定）。</summary>
    public static int PartOf(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return 4;
        if (dir.y > 0) return 0;
        if (dir.x > 0) return 1;
        if (dir.y < 0) return 2;
        return 3;
    }

    // 十字キーの当たりは**絵と同じ**にしてある（docs/make_dpad.py）。
    //   1枚 240ドット（半径120）／中央のボタンの絵は半径 64-7 = 57
    //   ／扇の絵が始まるのは半径 64+7 = 71
    // 絵と当たりがずれていると「真ん中のボタンを押したのに歩く」「絵の隙間なのに
    // 歩く」が起きる。以前は 0.20 / 0.36 で、**絵の中央ボタンの外周が扇扱い**
    // だった（決定のつもりが歩いてしまう、という報告）。

    /// <summary>扇（歩く）が始まる半径の割合。ここより内側では歩かない。</summary>
    public const float DPadDeadZone = 71f / 120f;

    /// <summary>**決定になる**のはここまで（真ん中のボタンの絵の大きさ）。</summary>
    public const float DPadConfirmZone = 57f / 120f;

    /// <summary>真ん中の「決定」の丸の中か。輪の中（決定でも扇でもない所）は何も起きない。</summary>
    public static bool InConfirmZone(Vector2 delta, float radius)
    {
        return radius > 0f && delta.magnitude <= radius * DPadConfirmZone;
    }

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

    /// <summary>押している間の色（少し濃くする。どこを押しているか分かるように）。</summary>
    public static Color Pressed(Color c)
    {
        return new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, c.a);
    }

    /// <summary>盤面の枠の、画布の中での大きさ（正方形）。高さが足りなければ縮める。</summary>
    public static float MapSide(float canvasWidth, float canvasHeight)
    {
        float free = canvasHeight - TopHeight - BottomHeight;
        return Mathf.Max(0f, Mathf.Min(canvasWidth, free));
    }

    /// <summary>右上の「実績」の見出し。</summary>
    public const string TrophyLabel = "実績";

    /// <summary>見出しの隣に出す称号。まだもらっていなければこれ（作者の指定）。</summary>
    public const string NoRankLabel = "無";
}
