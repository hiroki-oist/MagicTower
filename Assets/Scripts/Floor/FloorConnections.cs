using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 階段の接続と、階段で移動したときの着地位置。
/// 出典は PC98版 INITIAL.DAT の各ブロック先頭3ワード
/// （フロア番号 / 下り階段横の着地位置 / 上り階段横の着地位置）。
/// </summary>
public class FloorConnections
{
    public struct Info
    {
        public int Floor;
        public Vector2Int? Up;               // 上り階段の位置
        public Vector2Int? Down;             // 下り階段の位置
        public Vector2Int? ArriveFromAbove;  // 上の階から降りてきたときの着地位置（この階の上り階段の隣）
        public Vector2Int? ArriveFromBelow;  // 下の階から登ってきたときの着地位置（この階の下り階段の隣）

        /// <summary>通常の階段移動で着地できない階（幻の階・最上階など）。</summary>
        public bool IsUnreachableByStairs => ArriveFromAbove == null && ArriveFromBelow == null;
    }

    public const int MinFloor = 0;
    public const int MaxFloor = 50;

    readonly Dictionary<int, Info> _map = new Dictionary<int, Info>();

    static FloorConnections _instance;
    public static FloorConnections Instance
    {
        get
        {
            if (_instance == null) _instance = LoadDefault();
            return _instance;
        }
    }

    static FloorConnections LoadDefault()
    {
        var asset = Resources.Load<TextAsset>("FloorConnections");
        if (asset == null)
        {
            Debug.LogError("FloorConnections: Resources/FloorConnections.txt が見つかりません");
            return new FloorConnections();
        }
        return Parse(asset.text);
    }

    public static FloorConnections Parse(string text)
    {
        var fc = new FloorConnections();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            if (p.Length < 5) continue;
            var info = new Info
            {
                Floor = int.Parse(p[0].Trim()),
                Up = ParsePos(p[1]),
                Down = ParsePos(p[2]),
                ArriveFromBelow = ParsePos(p[3]),   // 下から登ってきた着地位置
                ArriveFromAbove = ParsePos(p[4]),   // 上から降りてきた着地位置
            };
            fc._map[info.Floor] = info;
        }
        return fc;
    }

    static Vector2Int? ParsePos(string s)
    {
        s = s.Trim();
        if (s == "-" || s.Length == 0) return null;
        var xy = s.Split(',');
        return new Vector2Int(int.Parse(xy[0]), int.Parse(xy[1]));
    }

    public bool TryGet(int floor, out Info info) => _map.TryGetValue(floor, out info);

    public Info Get(int floor) { Info i; return _map.TryGetValue(floor, out i) ? i : new Info(); }
}
