using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム中の文言。2つのデータファイルを読む。
///   Resources/Dialogue.txt … 原典 PC98版 MT.GTX を復号した全文（145本）
///   Resources/Messages.txt … 値を埋め込むメッセージのひな型（PC98版の文言）
///
/// 【文言の設計方針・全体に適用】
///   ・Dialogue.txt の本文は原典の改行位置をそのまま持つ。実行時に折り返さない。
///   ・Messages.txt は {0} を値で置換するため長さが変わる。こちらだけ実行時に折り返す。
///   ・PC98版は話者名を別枠に出さず本文に含めるので、話者カラムは持たない。
/// </summary>
public class GameText
{
    public struct Line
    {
        public int Id;
        /// <summary>原典の改行をそのまま含む本文。</summary>
        public string Body;
    }

    readonly List<Line> dialogue = new List<Line>();
    readonly Dictionary<string, string> messages = new Dictionary<string, string>();

    static GameText _instance;
    public static GameText Instance
    {
        get
        {
            if (_instance == null) _instance = LoadDefault();
            return _instance;
        }
    }

    static GameText LoadDefault()
    {
        var g = new GameText();
        var d = Resources.Load<TextAsset>("Dialogue");
        if (d != null) g.ParseDialogue(d.text);
        else Debug.LogError("GameText: Resources/Dialogue.txt が見つかりません");
        var m = Resources.Load<TextAsset>("Messages");
        if (m != null) g.ParseMessages(m.text);
        else Debug.LogError("GameText: Resources/Messages.txt が見つかりません");
        return g;
    }

    void ParseDialogue(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split(new[] { '|' }, 2);
            if (p.Length < 2) continue;
            int id;
            if (!int.TryParse(p[0].Trim(), out id)) continue;
            // データ上の \n は原典の会話ボックス内の改行位置。
            // Trim() は全角スペース(U+3000)まで削ってしまうので、半角の空白だけを落とす。
            // 原典はタイトルの中央寄せや装備名の右寄せに全角スペースを使っている。
            dialogue.Add(new Line { Id = id, Body = TrimAscii(p[1]).Replace("\\n", "\n") });
        }
    }

    void ParseMessages(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split(new[] { '|' }, 2);
            if (p.Length < 2) continue;
            // データ上の \n は原典の窓の中の改行位置。
            // 値の側は Trim() せず半角の空白だけ落とす（全角スペースは原典の字下げ）。
            messages[p[0].Trim()] = TrimAscii(p[1]).Replace("\\n", "\n");
        }
    }

    public Line? Dialogue(int id)
    {
        foreach (var l in dialogue) if (l.Id == id) return l;
        return null;
    }

    /// <summary>
    /// 半角の空白・タブ・復帰だけを両端から落とす。
    /// <c>string.Trim()</c> は全角スペース(U+3000)も空白とみなして削ってしまうが、
    /// 原典は全角スペースで字下げ（タイトルの中央寄せ、装備名の6文字幅への右寄せ）を
    /// 表現しているので、それを残す必要がある。
    /// </summary>
    static string TrimAscii(string s)
    {
        return s == null ? null : s.Trim(' ', '\t', '\r', '\n');
    }

    /// <summary>
    /// 数値を全角数字にする。
    /// 原典は左パネルもメッセージ欄も数字を全角で描画するため、
    /// **ゲーム中に表示する数値はすべてここを通す**（設計方針・全体に適用）。
    /// </summary>
    public static string Num(int value)
    {
        var s = value.ToString();
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (c >= '0' && c <= '9') sb.Append((char)(c - '0' + '０'));
            else if (c == '-') sb.Append('－');
            else sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 数値を全角数字にし、**右詰めで width マスに揃える**（足りない分は全角空白）。
    /// 原典の数値描画（0xb8d6）が `x + 2*(枠の数 - 桁数)` から書いているのと同じ。
    /// </summary>
    public static string NumRight(int value, int width)
    {
        var s = Num(value);
        return s.Length >= width ? s : new string('　', width - s.Length) + s;
    }

    /// <summary>メッセージを取り出し、{0} を文字列で置換する。</summary>
    public string Message(string key, string value)
    {
        string s;
        if (!messages.TryGetValue(key, out s))
        {
            Debug.LogWarning($"GameText: メッセージ '{key}' が未定義");
            return "";
        }
        return s.Replace("{0}", value);
    }

    /// <summary>メッセージを取り出す。{0} があれば value（全角数字）で置換する。</summary>
    public string Message(string key, int value = 0)
    {
        string s;
        if (!messages.TryGetValue(key, out s))
        {
            Debug.LogWarning($"GameText: メッセージ '{key}' が未定義");
            return "";
        }
        return s.Replace("{0}", Num(value));
    }

    public int DialogueCount { get { return dialogue.Count; } }
    public int MessageCount { get { return messages.Count; } }
}
