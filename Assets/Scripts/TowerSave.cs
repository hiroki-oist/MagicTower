using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// セーブとロード。原典 PC98版のセーブ処理（MTE.EXE 0x97ec）に合わせる。
///
/// 原典が保存しているのは次の並び:
/// <code>
///   生命力 / 攻撃力 / 防御力 / 所持金 / 現在の階 / 到達最高階 / X / Y /
///   鍵 / 赤い鍵 / 紫の鍵
/// </code>
/// 空きスロットは**全項目が 0xffff** で、そのときは
/// 「セーブデータがありません。」と出る。スロットは8つ（データ１〜８）。
///
/// リメイクでは装備の段階・特殊アイテム・寄進回数も要るので、
/// 原典の並びのうしろに足す形にした。
///
/// **地図も一緒に保存する**。原典のセーブ（0x9b4e）は
/// `DS:0x1ae8` から 0x1930 ワード＝**52ブロックぶんの地図をまるごと**書き出していて、
/// 倒した敵・開けた扉・拾った物がセーブに含まれる。こちらは元の階データとの
/// **差だけ**を持たせて同じ結果にする（4つ目の区画）。
///
/// ファイルの形: `値,…|行った階,…|旗,…|階:x,y=記号;…/階:…`
/// 4つ目が無い古いファイルも読めるようにしてある。
/// </summary>
public static class TowerSave
{
    /// <summary>原典と同じくスロットは8つ。</summary>
    public const int SlotCount = 8;

    /// <summary>空きスロットの印。原典は全項目 0xffff。</summary>
    public const int Empty = 0xffff;

    /// <summary>保存先。テストから差し替えられるようにしてある。</summary>
    public static string Directory = null;

    static string Dir
    {
        get { return string.IsNullOrEmpty(Directory) ? Application.persistentDataPath : Directory; }
    }

    static string PathOf(int slot) { return Path.Combine(Dir, $"save{slot}.txt"); }

    public static bool Exists(int slot)
    {
        return slot >= 1 && slot <= SlotCount && File.Exists(PathOf(slot));
    }

    /// <summary>セーブから読み出したもの。地図の差も持つ。</summary>
    public class Snapshot
    {
        public PlayerState Player;
        /// <summary>階 → 元のデータと違うマス。</summary>
        public readonly System.Collections.Generic.Dictionary<
            int, System.Collections.Generic.List<KeyValuePair<Vector2Int, string>>> Floors =
            new System.Collections.Generic.Dictionary<
                int, System.Collections.Generic.List<KeyValuePair<Vector2Int, string>>>();
    }

    public static void Save(int slot, PlayerState p,
        System.Collections.Generic.IDictionary<int, FloorState> floors = null)
    {
        if (p == null || slot < 1 || slot > SlotCount) return;
        System.IO.Directory.CreateDirectory(Dir);
        // 原典の並びを先に、リメイクで足した項目をうしろに
        var fields = new[]
        {
            p.Hp, p.Attack, p.Defense, p.Gold,
            p.Floor, p.HighestFloor, p.Position.x, p.Position.y,
            p.YellowKeys, p.RedKeys, p.PurpleKeys,
            p.SwordRank, p.ShieldRank, (int)p.Items, p.DonationCount,
        };
        // 飛翔のオーブが使う「行った事のある階」も残す
        var visited = new System.Collections.Generic.List<int>(p.Visited);
        visited.Sort();
        // 一度きりのできごと（仙人と話した等）も残す
        var flags = new System.Collections.Generic.List<int>(p.EventFlags);
        flags.Sort();
        // 地図の差（原典は地図をまるごと書き出している）
        var maps = new System.Collections.Generic.List<string>();
        if (floors != null)
        {
            var nums = new System.Collections.Generic.List<int>(floors.Keys);
            nums.Sort();
            foreach (var n in nums)
            {
                var data = FloorData.Load(n);
                if (data == null || floors[n] == null) continue;
                var diff = floors[n].Diff(data);
                if (diff.Count == 0) continue;
                var one = new System.Collections.Generic.List<string>();
                foreach (var kv in diff) one.Add($"{kv.Key.x},{kv.Key.y}={kv.Value}");
                maps.Add(n + ":" + string.Join(";", one));
            }
        }
        File.WriteAllText(PathOf(slot),
            string.Join(",", fields) + "|" + string.Join(",", visited)
                                     + "|" + string.Join(",", flags)
                                     + "|" + string.Join("/", maps)
                                     + "|" + (p.Rank ?? ""));   // 妖精にもらった称号
    }

    /// <summary>読み込む。スロットが空なら null（地図の差は捨てる）。</summary>
    public static PlayerState Load(int slot)
    {
        var s = Read(slot);
        return s != null ? s.Player : null;
    }

    /// <summary>地図の差も含めて読み込む。スロットが空なら null。</summary>
    public static Snapshot Read(int slot)
    {
        if (!Exists(slot)) return null;
        var text = File.ReadAllText(PathOf(slot));
        var halves = text.Split('|');
        var parts = halves[0].Split(',');
        if (parts.Length < 15) return null;

        if (Field(parts, 0) == Empty) return null;   // 原典の「空き」の印

        var state = new PlayerState
        {
            Hp = Field(parts, 0), Attack = Field(parts, 1),
            Defense = Field(parts, 2), Gold = Field(parts, 3),
            Floor = Field(parts, 4), HighestFloor = Field(parts, 5),
            Position = new Vector2Int(Field(parts, 6), Field(parts, 7)),
            YellowKeys = Field(parts, 8), RedKeys = Field(parts, 9),
            PurpleKeys = Field(parts, 10),
            SwordRank = Field(parts, 11), ShieldRank = Field(parts, 12),
            Items = (SpecialItems.Kind)Field(parts, 13),
            DonationCount = Field(parts, 14),
        };
        if (halves.Length > 1 && halves[1].Length > 0)
            foreach (var v in halves[1].Split(','))
            {
                int f;
                if (int.TryParse(v, out f)) state.Visited.Add(f);
            }
        if (halves.Length > 2 && halves[2].Length > 0)
            foreach (var v in halves[2].Split(','))
            {
                int f;
                if (int.TryParse(v, out f)) state.EventFlags.Add(f);
            }

        if (halves.Length > 4) state.Rank = halves[4].Trim();   // 妖精にもらった称号

        var snap = new Snapshot { Player = state };
        if (halves.Length > 3 && halves[3].Length > 0)
            foreach (var one in halves[3].Split('/'))
            {
                int colon = one.IndexOf(':');
                int floor;
                if (colon <= 0 || !int.TryParse(one.Substring(0, colon), out floor)) continue;
                var list = new System.Collections.Generic.List<KeyValuePair<Vector2Int, string>>();
                foreach (var c in one.Substring(colon + 1).Split(';'))
                {
                    int eq = c.IndexOf('=');
                    if (eq <= 0) continue;
                    var xy = c.Substring(0, eq).Split(',');
                    int x, y;
                    if (xy.Length != 2 || !int.TryParse(xy[0], out x)
                        || !int.TryParse(xy[1], out y)) continue;
                    list.Add(new KeyValuePair<Vector2Int, string>(
                        new Vector2Int(x, y), c.Substring(eq + 1)));
                }
                if (list.Count > 0) snap.Floors[floor] = list;
            }
        return snap;
    }

    static int Field(string[] parts, int i)
    {
        int v;
        return i < parts.Length && int.TryParse(parts[i], out v) ? v : 0;
    }

    public static void Delete(int slot)
    {
        if (Exists(slot)) File.Delete(PathOf(slot));
    }

    /// <summary>空きスロットを選んだときに出す文言のキー。</summary>
    public const string EmptyMessageKey = "no_save_data";
}
