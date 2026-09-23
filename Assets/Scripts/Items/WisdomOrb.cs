using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 知恵のオーブ。**それまでに聞いた情報の読み返し**。
/// 原典の説明（セリフ61）:
///   「所有しているアイテムの持つ能力を知る事ができる。
///     他にそれまでに聞いた情報を記憶しており、必要な時に聞き出す事ができる。」
///
/// 読み返しの表は原典 MTE.EXE 0x8a46 が抱えている **(フラグ, セリフ番号) 24件**で、
/// データは Resources/WisdomInfo.txt（`docs/make_wisdom_info.py` が取り出す）。
///
/// 原典 0x8a46 の流れ:
/// <code>
///   0x95d8(10行, 上端50, セリフ53)      「今までに聞いた情報を順次表示します。」→ キー待ち
///   表を上から順に（番号は**表の位置**そのもので、聞いていない分も数える）
///     if (フラグ[表の値 + 0x1e] == 1) {
///       0x958c(1, 0x46, "　情報番号：n")   ; 窓の上に番号を1行
///       0x95d8(8行, 上端90, そのセリフ)    ; **中身をそのまま出す** → キー待ち
///     }
/// </code>
/// **番号だけでなくセリフの中身を出す**のが原典。以前は番号の一覧だけを出していて、
/// 「読みたいのは会話ログなのに」と報告された。
/// また、**持ち物の能力はこの関数では出していない**（説明文はそう言っているが、
/// 0x8a46 は聞いた情報の読み返ししかしない）。手に入れたときの文言を並べていたのも
/// 原典に無い。
/// </summary>
public class WisdomOrb
{
    public struct Info
    {
        /// <summary>原典が出す「情報番号」。表の並び順（1から）。</summary>
        public int Number;
        /// <summary>この情報を聞いたかどうかの一度きりフラグ。</summary>
        public int Flag;
        /// <summary>読み返すセリフ。</summary>
        public int Record;
    }

    readonly List<Info> all = new List<Info>();

    static WisdomOrb _instance;
    public static WisdomOrb Instance
    {
        get { if (_instance == null) _instance = LoadDefault(); return _instance; }
    }

    static WisdomOrb LoadDefault()
    {
        var o = new WisdomOrb();
        var asset = Resources.Load<TextAsset>("WisdomInfo");
        if (asset == null) Debug.LogError("WisdomOrb: Resources/WisdomInfo.txt が見つかりません");
        else o.Parse(asset.text);
        return o;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash);
            var p = line.Split('|');
            if (p.Length < 3) continue;
            int n, f, r;
            if (!int.TryParse(p[0].Trim(), out n)) continue;
            if (!int.TryParse(p[1].Trim(), out f)) continue;
            if (!int.TryParse(p[2].Trim(), out r)) continue;
            all.Add(new Info { Number = n, Flag = f, Record = r });
        }
    }

    public int Count { get { return all.Count; } }
    public IEnumerable<Info> All { get { return all; } }

    /// <summary>その人が聞いた情報だけ、原典と同じ並びで返す。</summary>
    public List<Info> Heard(PlayerState p)
    {
        var list = new List<Info>();
        if (p == null) return list;
        foreach (var i in all) if (p.EventFlags.Contains(i.Flag)) list.Add(i);
        return list;
    }

    /// <summary>情報番号から引く。聞いていなければ -1。</summary>
    public int RecordOf(PlayerState p, int number)
    {
        foreach (var i in Heard(p)) if (i.Number == number) return i.Record;
        return -1;
    }

    /// <summary>前置きのセリフ（原典 0x8b3a が最初に出す 0x35）。</summary>
    public const int IntroRecord = 53;

    /// <summary>
    /// 窓に出す並び。原典と同じく
    /// 「今までに聞いた情報を順次表示します。」→ 聞いた情報を1件ずつ、の順。
    /// 1件は「　情報番号：n」の行とセリフの中身。**1件ずつキー待ち**なので、
    /// ここでも1件＝1ページにして返す。
    /// </summary>
    public static List<string> Pages(PlayerState p)
    {
        var pages = new List<string>();
        var intro = GameText.Instance.Dialogue(IntroRecord);
        if (intro.HasValue) pages.Add(intro.Value.Body);
        foreach (var i in Instance.Heard(p))
        {
            var line = GameText.Instance.Dialogue(i.Record);
            if (!line.HasValue) continue;
            pages.Add(GameText.Instance.Message("orb_info_no") + i.Number
                      + "\n" + line.Value.Body);
        }
        return pages;
    }
}
