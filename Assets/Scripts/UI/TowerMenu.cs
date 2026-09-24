using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// メニュー画面と3つのオーブの機能。原典 MTE.EXE の文字列そのままの並び。
///
/// <code>
///   メニューの選択をしてください。
///   　　勇者のオーブを使う        … 持っているときだけ出る
///   　　飛翔のオーブを使う        … 〃
///   　　知恵のオーブを使う        … 〃
///   　　スペシャル・アイテムを使う
///   　　データ・ロード
///   　　データ・セーブ
///   　　ゲームに戻る
///   　　ゲーム終了
/// </code>
/// 選択カーソルは小さな剣のアイコン（動画より）。
///
/// オーブの機能（原典 MT.GTX のレコード60〜62）:
///   勇者 … セーブと、その階のモンスターの能力・受けるダメージの表示
///   知恵 … 所持アイテムの能力と、それまでに聞いた情報の読み返し
///   飛翔 … 行った事のある階へ移動。**階段の隣でなければ使えない**
/// </summary>
public static class TowerMenu
{
    public enum Entry
    {
        HeroOrb, FlightOrb, WisdomOrb, SpecialItems,
        Load, Save, Resume, Quit,
    }

    /// <summary>
    /// 「スペシャル・アイテムを使う」で並ぶもの。**原典の並び順そのまま**
    /// （Messages.txt の `special_1`〜`special_8`）。
    /// 十字架・ドラゴンスレーヤー・幸運の金貨は**持っているだけで効く**ので並ばない。
    /// </summary>
    public static readonly SpecialItems.Kind[] Usable =
    {
        SpecialItems.Kind.Elixir,          // special_1 エリクサー
        SpecialItems.Kind.Matock,          // special_2 マトック
        SpecialItems.Kind.DestructionOrb,  // special_3 破壊の玉
        SpecialItems.Kind.TeleportRelic,   // special_4 空間転移の秘宝
        SpecialItems.Kind.WingUp,          // special_5 昇華の翼
        SpecialItems.Kind.WingDown,        // special_6 降臨の翼
        SpecialItems.Kind.MagicKey,        // special_7 魔法の鍵
        SpecialItems.Kind.SuperMatock,     // special_8 スーパーマトック
    };

    /// <summary>
    /// アイテムの一覧の文字。原典は**持っていなくても8つとも並べて**、
    /// 最後に「やっぱりやめる」を置く（文言が固定なのがその証拠）。
    /// </summary>
    public static string ItemText()
    {
        var t = GameText.Instance;
        var sb = new System.Text.StringBuilder(t.Message("special_ask"));
        for (int i = 1; i <= Usable.Length; i++) sb.Append("\n").Append(t.Message("special_" + i));
        sb.Append("\n").Append(t.Message("special_9"));
        return sb.ToString();
    }

    /// <summary>
    /// 道具の名前（原典の一覧の文言 `special_1`〜`special_8` から、頭の空白を落としたもの）。
    /// 持ち物の帯を触ったときの「〜を使いますか？」に使う。
    /// </summary>
    public static string ItemName(SpecialItems.Kind kind)
    {
        for (int i = 0; i < Usable.Length; i++)
            if (Usable[i] == kind)
                return GameText.Instance.Message("special_" + (i + 1)).Trim('　', ' ');
        return "";
    }

    /// <summary>
    /// オーブの名前。原典のメニューの文言「　　勇者のオーブを使う」から
    /// 前後を落として取る（勝手な文字を足さないため）。
    /// </summary>
    public static string OrbName(SpecialItems.Kind kind)
    {
        string key = kind == SpecialItems.Kind.HeroOrb   ? "menu_orb_hero"
                   : kind == SpecialItems.Kind.FlightOrb ? "menu_orb_fly"
                   : kind == SpecialItems.Kind.WisdomOrb ? "menu_orb_wis" : null;
        if (key == null) return ItemName(kind);
        var t = GameText.Instance.Message(key).Trim('　', ' ');
        const string tail = "を使う";
        return t.EndsWith(tail) ? t.Substring(0, t.Length - tail.Length) : t;
    }

    /// <summary>並びの数（やめるを含む）。</summary>
    public static int ItemCount { get { return Usable.Length + 1; } }

    /// <summary>
    /// 今出すべき項目。
    ///
    /// ★**作者の指定で「ゲームに戻る」「ゲーム終了」の2つだけ**にしてある。
    /// 原典は上の一覧のとおり8項目だが、スマホ版では
    ///   オーブ3種と道具 … 持ち物の帯を触る
    ///   セーブ・ロード   … 下のボタン
    /// で足りるので、メニューに二重に置かない。
    /// </summary>
    public static List<Entry> Entries(PlayerState p)
    {
        return new List<Entry> { Entry.Resume, Entry.Quit };
    }

    /// <summary>項目に出す文字。原典の文言そのまま。</summary>
    public static string Label(Entry e)
    {
        var t = GameText.Instance;
        switch (e)
        {
            case Entry.HeroOrb:      return t.Message("menu_orb_hero");
            case Entry.FlightOrb:    return t.Message("menu_orb_fly");
            case Entry.WisdomOrb:    return t.Message("menu_orb_wis");
            case Entry.SpecialItems: return t.Message("menu_special");
            case Entry.Load:         return t.Message("menu_load");
            case Entry.Save:         return t.Message("menu_save");
            case Entry.Resume:       return t.Message("menu_return");
            // ★作者の指定。原典は「　　ゲーム終了」（DOS へ戻る）
            case Entry.Quit:         return PhoneScreen.BackToTitle;
            default: return "";
        }
    }

    public static string Title() { return GameText.Instance.Message("menu_title"); }

    /// <summary>
    /// 原典の窓の指定。会話と同じ仕組み（行数, 上端）で、メニューは **11行・上端50**
    /// で開く（MTE.EXE 0x0d1e〜0x0d26 が `lcall 0x68b:0x2b3c` に渡す 0xb と 0x32）。
    /// 行数は文章に合わせず固定なので、オーブを持っていなくても窓の大きさは変わらない。
    /// </summary>
    public const int WindowLines = 11, TopEdge = 50;

    /// <summary>
    /// ★作者の指定。項目が2つになったので窓は**5行**に詰め、代わりに項目の
    /// **文字を大きく**する（小さすぎる、という報告）。
    ///
    /// 大きくするのは項目だけで、見出しは原典のまま。見出しは15文字あり、
    /// 窓の幅（320ドット、文字の入る幅は約296ドット）に 16ドット角で
    /// ちょうど収まる大きさなので、これ以上大きくすると溢れる。
    /// 項目は「　　ゲームに戻る」の8文字なので 1.9 倍でも収まる。
    /// </summary>
    public const int PhoneWindowLines = 5;
    public const float TextScale = 1.9f;

    /// <summary>
    /// 見出しの大きさ。項目に比べて小さすぎる、という報告で上げた。
    /// 見出しは15文字あるので、窓の幅に収まるぶんだけ（<see cref="Window"/> を広げてある）。
    /// </summary>
    public const float TitleScale = 1.25f;

    /// <summary>
    /// ★作者の指定でメニューの窓だけ会話の窓より広げてある（見出しを大きくするため）。
    /// 盤面の幅（原典の画面で x=144〜496）に収まる範囲。
    /// </summary>
    public const int MenuLeft = 152, MenuWidth = 336;

    /// <summary>項目1行を大きく出すための書き方（TMP の大きさ指定）。</summary>
    public static string Big(string line) { return Sized(line, TextScale); }

    /// <summary>見出しを少し大きく出すための書き方。</summary>
    public static string Heading(string line) { return Sized(line, TitleScale); }

    static string Sized(string line, float scale)
    {
        return $"<size={Mathf.RoundToInt(scale * 100f)}%>{line}</size>";
    }

    /// <summary>選ぶ印の位置（0x68b:0x35c8 に渡す 0x52 と 0x17）。</summary>
    public const int CursorX = 82, CursorY = 23;

    /// <summary>
    /// メニューを開く鍵。原典の主処理（MTE.EXE 0xa8e2 / 0xa8eb）が
    /// **0x1c と 0x34 のどちらでも**メニューへ飛ぶ。PC98 の鍵の番号で
    /// 0x1c = リターン、0x34 = スペース。
    /// </summary>
    public static readonly int[] OpenKeyCodes = { 0x1c, 0x34 };

    /// <summary>
    /// 窓に出す文字を原典と同じ組み立て方で作る。
    /// 「メニューの選択をしてください。」のあとに、出す項目を順につなぐだけ
    /// （原典 0x0b00〜0x0d1e は文字列をこの順に連結している）。
    /// </summary>
    public static string Text(PlayerState p)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(Title());
        foreach (var e in Entries(p))
        {
            sb.Append('\n');
            sb.Append(Label(e));
        }
        return sb.ToString();
    }

    /// <summary>メニューの窓の位置と大きさ（作者の指定）。</summary>
    public static RectInt Window
    {
        get { return new RectInt(MenuLeft, TopEdge, MenuWidth,
                                 DialogueWindow.Height(PhoneWindowLines)); }
    }

    // ---- 飛翔のオーブ ------------------------------------------------

    /// <summary>階段の隣にいるか。飛翔のオーブはここでしか使えない。</summary>
    public static bool NextToStairs(FloorState floor, Vector2Int at)
    {
        if (floor == null) return false;
        var around = new[]
        {
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(-1, 0), new Vector2Int(1, 0),
        };
        foreach (var d in around)
        {
            var c = floor.At(at + d);
            if (c == ">" || c == "<") return true;
        }
        return false;
    }

    /// <summary>
    /// 飛べる階。原典（0x91b4〜0x921d）は**1階から到達最高階まで**を上下するだけで、
    /// 訪れたかどうかは見ていない。**44階（幻の階）だけ名指しで飛ばす**。
    /// </summary>
    public static List<int> FlightTargets(PlayerState p)
    {
        var list = new List<int>();
        if (p == null) return list;
        for (int f = 1; f <= p.HighestFloor; f++)
        {
            if (f == FloorTransition.PhantomFloor) continue;
            list.Add(f);
        }
        return list;
    }

    /// <summary>飛翔のオーブが使えないときの文言。</summary>
    public static string FlightBlockedMessage()
    {
        return GameText.Instance.Message("fly_need_stair");   // 「階段の隣でなければ使えません。」
    }

    /// <summary>
    /// 階を選ぶときの案内。原典の文言（`teleport_menu`）から
    /// スマホに無い「リターンで」だけ落としてある（作者の指定）。
    /// </summary>
    public static string FlightPrompt() { return PhoneScreen.FlightPrompt; }

    // ---- 知恵のオーブ ------------------------------------------------
    //
    // 中身は <see cref="WisdomOrb"/>。**聞いた情報の読み返しだけ**で、
    // 持ち物の能力の一覧は原典 0x8a46 に無い（説明のセリフ61はそう言っているが、
    // 実際の処理は情報の読み返ししかしない）。ここに「手に入れた」の文言を
    // 並べる助けを置いていたが、それを窓に出していたのは原典に無い振る舞いだった。
}
