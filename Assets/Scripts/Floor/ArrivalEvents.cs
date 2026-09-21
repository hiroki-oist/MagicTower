using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 階段で着いたときに起きることの表。データは Resources/ArrivalEvents.txt。
///
/// 原典（MTE.EXE 0x2982）は階段の処理の中で階を見て、一度きりフラグで地図を書き換える。
/// EXE 全体を調べた限りこの形のできごとは **28階に降りたとき の1件だけ**。
/// </summary>
public class ArrivalEvents
{
    public struct Change
    {
        public int Floor;
        public Vector2Int Pos;
        /// <summary>"床" かプレハブ名。</summary>
        public string What;
    }

    public struct Event
    {
        public int Floor;
        /// <summary>true なら下り階段で着いたときだけ。</summary>
        public bool Descending;
        public int FlagId;
        public List<Change> Changes;
    }

    readonly List<Event> events = new List<Event>();
    public IEnumerable<Event> All { get { return events; } }
    public int Count { get { return events.Count; } }

    static ArrivalEvents _instance;
    public static ArrivalEvents Instance
    {
        get { if (_instance == null) _instance = LoadDefault(); return _instance; }
    }

    static ArrivalEvents LoadDefault()
    {
        var a = new ArrivalEvents();
        var asset = Resources.Load<TextAsset>("ArrivalEvents");
        if (asset == null) Debug.LogError("ArrivalEvents: Resources/ArrivalEvents.txt が見つかりません");
        else a.Parse(asset.text);
        return a;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            if (p.Length < 4) continue;

            int floor, flag;
            if (!int.TryParse(p[0].Trim(), out floor)) continue;
            if (!int.TryParse(p[2].Trim(), out flag)) continue;

            var e = new Event
            {
                Floor = floor,
                Descending = p[1].Trim() == "下り",
                FlagId = flag,
                Changes = new List<Change>()
            };
            foreach (var part in p[3].Split(';'))
            {
                var t = part.Trim();
                if (!t.StartsWith("地図 ")) continue;
                var body = t.Substring(3).Trim();
                int colon = body.IndexOf(':'), open = body.IndexOf('('),
                    comma = body.IndexOf(','), close = body.IndexOf(')'), eq = body.IndexOf('=');
                int f, x, y;
                if (colon < 0 || open < 0 || comma < 0 || close < 0 || eq < 0) continue;
                if (!int.TryParse(body.Substring(0, colon).Trim(), out f)) continue;
                if (!int.TryParse(body.Substring(open + 1, comma - open - 1).Trim(), out x)) continue;
                if (!int.TryParse(body.Substring(comma + 1, close - comma - 1).Trim(), out y)) continue;
                e.Changes.Add(new Change
                {
                    Floor = f, Pos = new Vector2Int(x, y), What = body.Substring(eq + 1).Trim()
                });
            }
            events.Add(e);
        }
    }

    /// <summary>その到着で起きること。無ければ null。</summary>
    public Event? At(int floor, bool descending, HashSet<int> flags)
    {
        foreach (var e in events)
        {
            if (e.Floor != floor || e.Descending != descending) continue;
            if (flags != null && flags.Contains(e.FlagId)) continue;
            return e;
        }
        return null;
    }
}
