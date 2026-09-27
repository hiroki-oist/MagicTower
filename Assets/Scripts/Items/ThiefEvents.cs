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
        /// <summary>
        /// 書き込む中身。"床" なら床に戻す（＝居なくなる）。
        /// **物体の名前も書ける** — 2階で2度目に話すと、原典 0x7aa が
        /// 35階(-1,-4) に盗賊(ID21)を書き込み、続けて (-2,-3) の抜け穴を開ける。
        /// </summary>
        public string What;
        /// <summary>
        /// ★**その階を出てから**効かせる書き換えか（作者の指定）。
        /// 35階で会った盗賊は、話したあとも**その階にいる間は見えていて**、
        /// 階を出ると居なくなる。原典は話した時点で地図を床にするが、
        /// そのマスを描き直さないので、階を出て戻るまで姿が残る。
        /// こちらは変わったマスを毎コマ描き直すので、同じ見え方にするには
        /// 「階を出るときに書き換える」と決めておく必要がある。
        /// 書き方は "35:(-1,-4)=床@階を出たら"。
        /// </summary>
        public bool OnLeave;
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
        /// <summary>
        /// 去っていく道順の出発マス。歩かないなら <see cref="Vector2Int.zero"/> で
        /// <see cref="Walk"/> が null。原典 0x3e0/0x529 で `0x39c6` を呼ぶのは
        /// 2階（下へ2歩）と15階（左へ2歩）の2件だけ。
        /// </summary>
        public Vector2Int WalkFrom;
        /// <summary>通っていくマス。歩かないなら null。</summary>
        public List<Vector2Int> Walk;
    }

    readonly List<Event> events = new List<Event>();
    /// <summary>「その階を出てから」を表す印（書き方は "…=床@階を出たら"）。</summary>
    public const string LeaveMark = "階を出たら";

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
            if (p.Length > 5) ParseWalk(p[5].Trim(), ref e);
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

    /// <summary>"(x,y) 1,2,3" を読む。"-" なら歩かない。</summary>
    static void ParseWalk(string s, ref Event e)
    {
        if (s == "-" || s.Length == 0) return;
        int close = s.IndexOf(')');
        if (!s.StartsWith("(") || close < 0) return;
        var xy = s.Substring(1, close - 1).Split(',');
        int x, y;
        if (xy.Length != 2 || !int.TryParse(xy[0].Trim(), out x)
                           || !int.TryParse(xy[1].Trim(), out y)) return;
        var at = new Vector2Int(x, y);
        e.WalkFrom = at;
        e.Walk = new List<Vector2Int>();
        foreach (var t in s.Substring(close + 1).Split(','))
        {
            int d;
            if (!int.TryParse(t.Trim(), out d)) continue;
            var move = Cutscenes.StepOf(d);
            if (move == Vector2Int.zero) continue;
            at += move;
            e.Walk.Add(at);
        }
        if (e.Walk.Count == 0) e.Walk = null;
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
            int eq = c[1].IndexOf('=', close);
            var what = eq >= 0 ? c[1].Substring(eq + 1).Trim() : "床";
            bool onLeave = false;
            int at = what.IndexOf('@');
            if (at >= 0)
            {
                onLeave = what.Substring(at + 1).Trim() == LeaveMark;
                what = what.Substring(0, at).Trim();
            }
            if (what.Length == 0) what = "床";
            list.Add(new Change
            {
                Floor = floor, Pos = new Vector2Int(x, y), What = what, OnLeave = onLeave
            });
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
