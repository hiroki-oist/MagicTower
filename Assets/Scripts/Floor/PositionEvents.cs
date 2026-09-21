using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 立ち入ると起きることの表。データは Resources/PositionEvents.txt。
///
/// 原典（PC98版 MTE.EXE の関数 0x1588）は物体ディスパッチャから毎手よばれ、
/// <code>
///   if (現在階==F &amp;&amp; X==x &amp;&amp; Y==y &amp;&amp; フラグN==0 &amp;&amp; …) { フラグN=1; 効果; }
/// </code>
/// を13件ならべている。原典の X,Y は 0〜10 なので x = X-5、y = 5-Y。
/// </summary>
public class PositionEvents
{
    public struct Change
    {
        public int Floor;
        public Vector2Int Pos;
        /// <summary>"門" / "壁" / "床"</summary>
        public string Kind;
    }

    public struct Event
    {
        public int Floor;
        /// <summary>どれか1つに立てば発火する。</summary>
        public List<Vector2Int> Spots;
        /// <summary>このマスが床でないと発火しない。無ければ null。</summary>
        public Vector2Int? NeedsCleared;
        /// <summary>このフラグが立っていないと発火しない。無ければ -1。</summary>
        public int NeedsFlag;
        /// <summary>
        /// このマスが門でないと発火しない。無ければ null。
        /// 終幕の引き金（24階の時空の門）だけがこれを使う（原典 0x2307 の `cmp …, 4`）。
        /// </summary>
        public Vector2Int? NeedsGate;
        /// <summary>発火したら立てるフラグ。無ければ -1（原典に一度きりの印が無いもの）。</summary>
        public int FlagId;
        /// <summary>
        /// 持っていないと発火しないアイテム。無ければ <see cref="SpecialItems.Kind.None"/>。
        /// 発火すると**使ってしまう**（原典 13階の雪の結晶 0x2002〜0x2022）。
        /// </summary>
        public SpecialItems.Kind NeedsItem;
        public List<Change> Changes;
        /// <summary>その場で出る会話。無ければ -1。</summary>
        public int Record;
        /// <summary>別仕立ての場面の名前。無ければ null。</summary>
        public string Scene;
    }

    readonly List<Event> events = new List<Event>();
    public IEnumerable<Event> All { get { return events; } }
    public int Count { get { return events.Count; } }

    static PositionEvents _instance;
    public static PositionEvents Instance
    {
        get { if (_instance == null) _instance = LoadDefault(); return _instance; }
    }

    static PositionEvents LoadDefault()
    {
        var e = new PositionEvents();
        var asset = Resources.Load<TextAsset>("PositionEvents");
        if (asset == null) Debug.LogError("PositionEvents: Resources/PositionEvents.txt が見つかりません");
        else e.Parse(asset.text);
        return e;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            if (p.Length < 5) continue;

            int floor, flag;
            if (!int.TryParse(p[0].Trim(), out floor)) continue;
            // フラグ欄が "-" のものは原典にも一度きりの印が無い（13階の雪の結晶）。
            // 持ち物を使い切ることが二度目を防いでいる。
            if (!int.TryParse(p[3].Trim(), out flag)) flag = -1;

            var ev = new Event
            {
                Floor = floor, FlagId = flag, NeedsFlag = -1, Record = -1,
                NeedsItem = SpecialItems.Kind.None,
                Spots = new List<Vector2Int>(), Changes = new List<Change>()
            };
            foreach (var s in p[1].Split(';'))
            {
                Vector2Int v;
                if (TryPos(s, out v)) ev.Spots.Add(v);
            }
            ParseCondition(p[2].Trim(), ref ev);
            ParseEffects(p[4], ref ev);
            events.Add(ev);
        }
    }

    static void ParseCondition(string s, ref Event ev)
    {
        if (s == "-" || s.Length == 0) return;
        if (s.StartsWith("マスが床:"))
        {
            Vector2Int v;
            if (TryPos(s.Substring("マスが床:".Length), out v)) ev.NeedsCleared = v;
        }
        else if (s.StartsWith("マスが門:"))
        {
            Vector2Int v;
            if (TryPos(s.Substring("マスが門:".Length), out v)) ev.NeedsGate = v;
        }
        else if (s.StartsWith("フラグ:"))
        {
            int n;
            if (int.TryParse(s.Substring("フラグ:".Length), out n)) ev.NeedsFlag = n;
        }
        else if (s.StartsWith("持ち物:"))
        {
            // 名前はほかのデータと同じくプレハブ名で書く（例: Crystal_Snow）
            ev.NeedsItem = SpecialItems.FromPrefabName(s.Substring("持ち物:".Length).Trim());
        }
    }

    static void ParseEffects(string s, ref Event ev)
    {
        foreach (var part in s.Split(';'))
        {
            var t = part.Trim();
            if (t.Length == 0 || t == "-") continue;
            if (t.StartsWith("地図 "))
            {
                // "地図 15:(+0,-3)=門"
                var body = t.Substring(3).Trim();
                int colon = body.IndexOf(':'), open = body.IndexOf('('),
                    comma = body.IndexOf(','), close = body.IndexOf(')'), eq = body.IndexOf('=');
                int f, x, y;
                if (colon < 0 || open < 0 || comma < 0 || close < 0 || eq < 0) continue;
                if (!int.TryParse(body.Substring(0, colon).Trim(), out f)) continue;
                if (!int.TryParse(body.Substring(open + 1, comma - open - 1).Trim(), out x)) continue;
                if (!int.TryParse(body.Substring(comma + 1, close - comma - 1).Trim(), out y)) continue;
                ev.Changes.Add(new Change
                {
                    Floor = f, Pos = new Vector2Int(x, y), Kind = body.Substring(eq + 1).Trim()
                });
            }
            else if (t.StartsWith("セリフ"))
            {
                int n;
                if (int.TryParse(t.Substring(3).Trim(), out n)) ev.Record = n;
            }
            else if (t.StartsWith("演出 "))
            {
                ev.Scene = t.Substring(3).Trim();
            }
        }
    }

    static bool TryPos(string s, out Vector2Int v)
    {
        v = default(Vector2Int);
        var t = s.Trim();
        if (t.Length == 0 || t == "-") return false;
        var xy = t.Split(',');
        int x, y;
        if (xy.Length != 2) return false;
        if (!int.TryParse(xy[0].Trim(), out x) || !int.TryParse(xy[1].Trim(), out y)) return false;
        v = new Vector2Int(x, y);
        return true;
    }

    /// <summary>そのマスで起きること。無ければ null。</summary>
    public Event? At(int floor, Vector2Int pos, HashSet<int> flags,
                     System.Func<Vector2Int, bool> isCleared,
                     System.Func<Vector2Int, bool> isGate = null,
                     System.Func<SpecialItems.Kind, bool> has = null)
    {
        foreach (var ev in events)
        {
            if (ev.Floor != floor) continue;
            if (!ev.Spots.Contains(pos)) continue;
            if (ev.FlagId >= 0 && flags != null && flags.Contains(ev.FlagId)) continue;
            if (ev.NeedsFlag >= 0 && (flags == null || !flags.Contains(ev.NeedsFlag))) continue;
            if (ev.NeedsCleared.HasValue &&
                (isCleared == null || !isCleared(ev.NeedsCleared.Value))) continue;
            if (ev.NeedsGate.HasValue &&
                (isGate == null || !isGate(ev.NeedsGate.Value))) continue;
            if (ev.NeedsItem != SpecialItems.Kind.None &&
                (has == null || !has(ev.NeedsItem))) continue;
            return ev;
        }
        return null;
    }
}
