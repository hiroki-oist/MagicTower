using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Assets/Resources/Floors/Floor_N.txt を読み解いたフロア1枚分のデータ。
/// 元データは PC98版 INITIAL.DAT から docs/decode_pc98.py で生成している。
/// </summary>
public class FloorData
{
    public const int Width = 11;
    public const int Height = 11;
    // 盤面は 11×11。原点は中心（SealedDoors / 原典コードの座標と同じ取り方）。
    public const int MinX = -5, MaxX = 5, MinY = -5, MaxY = 5;

    public enum Terrain
    {
        Floor, Wall, SecretWall, Magma, Void,
        StairsUp, StairsDown,
        LockYellow, LockPurple, LockRed, LockSealed, LockSpecial,
    }

    public struct Placement
    {
        public Vector2Int Pos;
        public string Prefab;
        public string Note;
    }

    public int FloorNumber { get; private set; }
    public Terrain[,] Tiles { get; private set; }
    public List<Placement> Objects { get; private set; }

    static readonly Dictionary<char, Terrain> Symbols = new Dictionary<char, Terrain>
    {
        ['.'] = Terrain.Floor,      ['#'] = Terrain.Wall,
        ['%'] = Terrain.SecretWall, ['~'] = Terrain.Magma,
        ['*'] = Terrain.Void,
        ['>'] = Terrain.StairsUp,   ['<'] = Terrain.StairsDown,
        ['y'] = Terrain.LockYellow, ['b'] = Terrain.LockPurple,
        ['r'] = Terrain.LockRed,    ['s'] = Terrain.LockSealed,
        ['x'] = Terrain.LockSpecial,
        ['O'] = Terrain.Floor,      // 物体が乗っているマス。地形としては床
    };

    static readonly Regex RowRe = new Regex(@"^\s*([+-]\d+) \| (.{11})$");
    static readonly Regex ObjRe = new Regex(@"^\s*([+-]\d+) ([+-]\d+)\s+(\S+)\s*(.*)$");

    /// <summary>Resources/Floors/Floor_{n} を読み込む。</summary>
    public static FloorData Load(int floorNumber)
    {
        var asset = Resources.Load<TextAsset>($"Floors/Floor_{floorNumber}");
        if (asset == null)
        {
            Debug.LogError($"FloorData: Floors/Floor_{floorNumber} が見つかりません");
            return null;
        }
        return Parse(floorNumber, asset.text);
    }

    public static FloorData Parse(int floorNumber, string text)
    {
        var data = new FloorData
        {
            FloorNumber = floorNumber,
            Tiles = new Terrain[Width, Height],
            Objects = new List<Placement>(),
        };

        bool inObjects = false;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith("objects:")) { inObjects = true; continue; }

            if (!inObjects)
            {
                var m = RowRe.Match(line);
                if (!m.Success) continue;
                int y = int.Parse(m.Groups[1].Value);
                var row = m.Groups[2].Value;
                for (int c = 0; c < Width; c++)
                {
                    if (!Symbols.TryGetValue(row[c], out var t))
                    {
                        Debug.LogWarning($"Floor_{floorNumber}: 未知の地形記号 '{row[c]}' at ({c + MinX},{y})");
                        t = Terrain.Floor;
                    }
                    data.Tiles[c, MaxY - y] = t;
                }
            }
            else
            {
                var m = ObjRe.Match(line);
                if (!m.Success) continue;
                data.Objects.Add(new Placement
                {
                    Pos = new Vector2Int(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)),
                    Prefab = m.Groups[3].Value,
                    Note = m.Groups[4].Value.Trim(),
                });
            }
        }
        return data;
    }

    public Terrain At(int x, int y) => Tiles[x - MinX, MaxY - y];

    public bool InBounds(int x, int y) => x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;

    /// <summary>指定した地形の座標を全て返す（階段の探索などに使う）。</summary>
    public IEnumerable<Vector2Int> Find(Terrain t)
    {
        for (int y = MaxY; y >= MinY; y--)
            for (int x = MinX; x <= MaxX; x++)
                if (At(x, y) == t) yield return new Vector2Int(x, y);
    }
}
