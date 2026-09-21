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

    public static void Save(int slot, PlayerState p)
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
        File.WriteAllText(PathOf(slot),
            string.Join(",", fields) + "|" + string.Join(",", visited)
                                     + "|" + string.Join(",", flags));
    }

    /// <summary>読み込む。スロットが空なら null。</summary>
    public static PlayerState Load(int slot)
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
        return state;
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
