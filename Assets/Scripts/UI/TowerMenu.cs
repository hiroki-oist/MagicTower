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

    /// <summary>今出すべき項目。持っていないオーブは並ばない。</summary>
    public static List<Entry> Entries(PlayerState p)
    {
        var list = new List<Entry>();
        if (p != null && p.Has(SpecialItems.Kind.HeroOrb))   list.Add(Entry.HeroOrb);
        if (p != null && p.Has(SpecialItems.Kind.FlightOrb)) list.Add(Entry.FlightOrb);
        if (p != null && p.Has(SpecialItems.Kind.WisdomOrb)) list.Add(Entry.WisdomOrb);
        list.Add(Entry.SpecialItems);
        list.Add(Entry.Load);
        list.Add(Entry.Save);
        list.Add(Entry.Resume);
        list.Add(Entry.Quit);
        return list;
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
            case Entry.Quit:         return t.Message("menu_quit");
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

    /// <summary>メニューの窓の位置と大きさ。</summary>
    public static RectInt Window
    {
        get { return new RectInt(DialogueWindow.Left, TopEdge,
                                 DialogueWindow.Width, DialogueWindow.Height(WindowLines)); }
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

    /// <summary>階を選ぶときの案内。</summary>
    public static string FlightPrompt()
    {
        return GameText.Instance.Message("teleport_menu");
    }

    // ---- 知恵のオーブ ------------------------------------------------

    /// <summary>持っている特殊アイテムの説明を並べる。</summary>
    public static List<string> ItemKnowledge(PlayerState p)
    {
        var list = new List<string>();
        if (p == null) return list;
        foreach (SpecialItems.Kind k in System.Enum.GetValues(typeof(SpecialItems.Kind)))
        {
            if (k == SpecialItems.Kind.None || !p.Has(k)) continue;
            list.Add(SpecialItems.MessageOf(k));
        }
        return list;
    }
}
