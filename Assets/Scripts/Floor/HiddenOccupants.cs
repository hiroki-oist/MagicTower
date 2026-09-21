using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 隠れている者の表。データは Resources/HiddenOccupants.txt。
///
/// 原典（PC98版 MTE.EXE の関数 0x02c6）はこう書かれている:
/// <code>
///   if (階==12) 12階(+5,+5) = 19(商人);
///   if (階==16) 16階(+5,-5) = 25(仙人);
///   if (階==19) 19階(+0,+3) = 39(十字架);
///   if (階==41 &amp;&amp; 41階(-4,+4)==6) 41階(+4,+4) = 151(魔術士Ａ);
/// </code>
/// 地形 ID10（隠し壁と見た目が同じマス）へ歩こうとしたときに呼ばれる。
/// </summary>
public class HiddenOccupants
{
    public struct Hidden
    {
        public int Floor;
        public Vector2Int Pos;
        /// <summary>現れる物体のプレハブ名。</summary>
        public string What;
        /// <summary>原典の物体ID。</summary>
        public int Id;
        /// <summary>このマスが床でないと現れない。無ければ null。</summary>
        public Vector2Int? RequiresCleared;
    }

    readonly List<Hidden> list = new List<Hidden>();
    public IEnumerable<Hidden> All { get { return list; } }
    public int Count { get { return list.Count; } }

    static HiddenOccupants _instance;
    public static HiddenOccupants Instance
    {
        get
        {
            if (_instance == null) _instance = LoadDefault();
            return _instance;
        }
    }

    static HiddenOccupants LoadDefault()
    {
        var h = new HiddenOccupants();
        var asset = Resources.Load<TextAsset>("HiddenOccupants");
        if (asset == null) Debug.LogError("HiddenOccupants: Resources/HiddenOccupants.txt が見つかりません");
        else h.Parse(asset.text);
        return h;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            if (p.Length < 5) continue;

            int floor, id;
            if (!int.TryParse(p[0].Trim(), out floor)) continue;
            if (!int.TryParse(p[3].Trim(), out id)) id = -1;

            Vector2Int pos;
            if (!TryPos(p[1], out pos)) continue;

            Vector2Int need;
            var h = new Hidden
            {
                Floor = floor, Pos = pos, What = p[2].Trim(), Id = id,
                RequiresCleared = TryPos(p[4], out need) ? (Vector2Int?)need : null
            };
            list.Add(h);
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

    /// <summary>そのマスに隠れている者。無ければ null。</summary>
    public Hidden? At(int floor, Vector2Int pos)
    {
        foreach (var h in list)
            if (h.Floor == floor && h.Pos == pos) return h;
        return null;
    }
}
