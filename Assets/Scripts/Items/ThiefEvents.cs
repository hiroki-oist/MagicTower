using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 盗賊（Mikata, ID21）のできごと表。データは Resources/ThiefEvents.txt。
///
/// 原典（PC98版 MTE.EXE の関数 0x03e0）はこう書かれている:
/// <code>
///   if (階==2  && フラグ94==0)               { セリフ116; 2階(-5,-2)を床に; }
///   else if (階==15)                          { セリフ120; 15階(+2,+5)と(+3,+5)を床に; }
///   else if (階==10)                          { セリフ107; }
///   else if (階==2 && フラグ94 && !フラグ88)   { フラグ88=1; セリフ141; 35階(-2,-3)を床に; }
///   ...
/// </code>
/// 上から順に見て最初に当てはまった1件だけを実行する。
///
/// 【設計方針・全体に適用】
///   仙人（MasterHints）・商人（Shop）と同じく、
///   **原典の分岐の並びをそのまま表として持ち、上から最初に合った行を使う**。
/// </summary>
public class ThiefEvents
{
    /// <summary>地図の書き換え1件。</summary>
    public struct Change
    {
        public int Floor;
        public Vector2Int Pos;
    }

    /// <summary>できごと1件。</summary>
    public struct Event
    {
        public int Floor;
        /// <summary>この番号のフラグが立っていないと出ない。無条件なら空。</summary>
        public List<KeyValuePair<int, bool>> Requires;
        /// <summary>y がこの値以下でないと出ない。無ければ int.MaxValue。</summary>
        public int MaxY;
        public int Record;
        /// <summary>実行時に立てるフラグ。無ければ -1。</summary>
        public int SetsFlag;
        public List<Change> Changes;
    }

    readonly List<Event> events = new List<Event>();
    public IEnumerable<Event> All { get { return events; } }
    public int Count { get { return events.Count; } }

    static ThiefEvents _instance;
    public static ThiefEvents Instance
    {
        get
        {
            if (_instance == null) _instance = LoadDefault();
            return _instance;
        }
    }

    static ThiefEvents LoadDefault()
    {
        var t = new ThiefEvents();
        var asset = Resources.Load<TextAsset>("ThiefEvents");
        if (asset == null) Debug.LogError("ThiefEvents: Resources/ThiefEvents.txt が見つかりません");
        else t.Parse(asset.text);
        return t;
    }

    public static ThiefEvents FromText(string text)
    {
        var t = new ThiefEvents();
        t.Parse(text);
        return t;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            if (p.Length < 5) continue;

            int floor, rec;
            if (!int.TryParse(p[0].Trim(), out floor)) continue;
            if (!int.TryParse(p[2].Trim(), out rec)) continue;

            int sets;
            if (!int.TryParse(p[3].Trim(), out sets)) sets = -1;

            var e = new Event
            {
                Floor = floor, Record = rec, SetsFlag = sets,
                MaxY = int.MaxValue,
                Requires = new List<KeyValuePair<int, bool>>(),
                Changes = ParseChanges(p[4])
            };
            ParseCondition(p[1].Trim(), ref e);
            events.Add(e);
        }
    }

    static void ParseCondition(string s, ref Event e)
    {
        if (s == "-" || s.Length == 0) return;
        foreach (var part in s.Split('&'))
        {
            var t = part.Trim();
            if (t.StartsWith("フラグ"))
            {
                var kv = t.Substring(3).Split('=');
                int n, v;
                if (kv.Length == 2 && int.TryParse(kv[0], out n) && int.TryParse(kv[1], out v))
                    e.Requires.Add(new KeyValuePair<int, bool>(n, v != 0));
            }
            else if (t.StartsWith("y<="))
            {
                int v;
                if (int.TryParse(t.Substring(3), out v)) e.MaxY = v;
            }
        }
    }

    static List<Change> ParseChanges(string s)
    {
        var list = new List<Change>();
        foreach (var part in s.Split(';'))
        {
            var t = part.Trim();
            if (t.Length == 0 || t == "-") continue;
            // "35:(-2,-3)=床"
            var c = t.Split(':');
            int floor;
            if (c.Length != 2 || !int.TryParse(c[0].Trim(), out floor)) continue;
            int open = c[1].IndexOf('('), comma = c[1].IndexOf(','), close = c[1].IndexOf(')');
            if (open < 0 || comma < 0 || close < 0) continue;
            int x, y;
            if (!int.TryParse(c[1].Substring(open + 1, comma - open - 1).Trim(), out x)) continue;
            if (!int.TryParse(c[1].Substring(comma + 1, close - comma - 1).Trim(), out y)) continue;
            list.Add(new Change { Floor = floor, Pos = new Vector2Int(x, y) });
        }
        return list;
    }

    /// <summary>
    /// その場でどのできごとが起きるかを上から順に探す。無ければ null。
    /// </summary>
    public Event? For(int floor, int y, HashSet<int> flags)
    {
        foreach (var e in events)
        {
            if (e.Floor != floor) continue;
            if (y > e.MaxY) continue;
            bool ok = true;
            foreach (var r in e.Requires)
                if (flags.Contains(r.Key) != r.Value) { ok = false; break; }
            if (ok) return e;
        }
        return null;
    }
}
