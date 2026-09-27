using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 実行中の1フロアの盤面。`FloorData`（読み取り専用の初期配置）に対する**可変の現在状態**。
///
/// 【設計方針・全体に適用】
///   原典 PC98版は地図を**1枚の可変配列**として持ち、すべての仕掛けがそこを読み書きする。
///   敵を倒すとそのマスは床(6)になり、門の解錠判定はその「床になっているか」を見る。
///   リメイクでもこの構造をそのまま写す。つまり
///     ・敵を倒したら <see cref="Clear"/> でマスを床にする
///     ・門が開くときも同じく床にする
///     ・イベントでマスに物を置くときは <see cref="Put"/>
///   こうしておけば、解錠条件を原典と同じ「守護マスが床か」で書ける。
/// </summary>
public class FloorState
{
    /// <summary>床を表す印。原典の値6に相当する。</summary>
    public const string Floor = ".";

    public int FloorNumber { get; private set; }

    /// <summary>マスの中身。物体があればプレハブ名、無ければ地形の記号。</summary>
    readonly Dictionary<Vector2Int, string> cells = new Dictionary<Vector2Int, string>();

    /// <summary>発火済みの解錠（原典の一度きりフラグ）。</summary>
    readonly HashSet<int> firedFlags = new HashSet<int>();

    /// <summary>前回見た目に反映してから変わったマス。描画側がこれを見て差分だけ作り直す。</summary>
    readonly List<Vector2Int> dirty = new List<Vector2Int>();

    public FloorState(FloorData data)
    {
        if (data == null) return;
        FloorNumber = data.FloorNumber;
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                cells[new Vector2Int(x, y)] = SymbolOf(data.At(x, y));
        foreach (var o in data.Objects) cells[o.Pos] = o.Prefab;
    }

    static string SymbolOf(FloorData.Terrain t)
    {
        switch (t)
        {
            case FloorData.Terrain.Floor: return Floor;
            case FloorData.Terrain.Wall: return "#";
            case FloorData.Terrain.SecretWall: return "%";
            case FloorData.Terrain.Magma: return "~";
            case FloorData.Terrain.StairsUp: return ">";
            case FloorData.Terrain.StairsDown: return "<";
            case FloorData.Terrain.LockYellow: return "y";
            case FloorData.Terrain.LockPurple: return "b";
            case FloorData.Terrain.LockRed: return "r";
            case FloorData.Terrain.LockSealed: return "s";
            case FloorData.Terrain.LockSpecial: return "x";
            case FloorData.Terrain.Void: return "*";
            default: return Floor;
        }
    }

    /// <summary>
    /// 解錠やできごとの結果に書かれた語を、盤面の記号に直す。
    ///
    /// 原典は地図（1マス1ワード）へ値を直接書き込むので、抽出した表には
    /// 地形の語（床/壁/隠し壁/門/上り）と物体のID（IDnn）の両方が現れる。
    ///
    /// 【設計方針・全体に適用】この変換はここ1か所だけに置き、
    /// 解錠（<see cref="ApplyUnlocks"/>）・座標のできごと・階段のできごとの
    /// 3系統すべてがこれを通す。語のまま盤面へ置くと、壁のはずのマスが
    /// 「名前の分からない物体」になってしまう（38階の消える壁・41階の壁3マス・
    /// 関門3階の上り階段が、実際にこれで壊れていた）。
    /// </summary>
    public static string SymbolOfKind(string kind)
    {
        switch (kind)
        {
            case "床":     return Floor;
            case "壁":     return "#";
            case "隠し壁": return "%";
            case "門":     return "s";
            case "上り":   return ">";
            case "下り":   return "<";
            case "マグマ": return "~";
        }
        return ObjectIds.Instance.Resolve(kind);
    }

    /// <summary>
    /// 盤面の記号を見た目側のプレハブ名に直す。床は null（何も置かない）。
    /// <see cref="SymbolOf"/> の逆向きで、登録簿の鍵は地形の名前そのもの。
    /// </summary>
    public static string ViewKeyOf(string cell)
    {
        if (string.IsNullOrEmpty(cell) || cell == Floor) return null;
        switch (cell)
        {
            case "#": return FloorData.Terrain.Wall.ToString();
            case "%": return FloorData.Terrain.SecretWall.ToString();
            case "~": return FloorData.Terrain.Magma.ToString();
            case ">": return FloorData.Terrain.StairsUp.ToString();
            case "<": return FloorData.Terrain.StairsDown.ToString();
            case "y": return FloorData.Terrain.LockYellow.ToString();
            case "b": return FloorData.Terrain.LockPurple.ToString();
            case "r": return FloorData.Terrain.LockRed.ToString();
            case "s": return FloorData.Terrain.LockSealed.ToString();
            case "x": return FloorData.Terrain.LockSpecial.ToString();
            case "*": return FloorData.Terrain.Void.ToString();
        }
        return cell;      // 物体はプレハブ名がそのまま鍵
    }

    /// <summary>
    /// 元の階データと違うマスだけを並べる。セーブに使う。
    ///
    /// 原典のセーブ（0x9b4e）は **52ブロックぶんの地図をまるごと**書き出している
    /// （`DS:0x1ae8` から 0x1930 ワード）。つまり倒した敵・開けた扉・拾った物は
    /// セーブに含まれる。こちらは元のデータとの差だけを持たせて同じ結果にする。
    /// </summary>
    public List<KeyValuePair<Vector2Int, string>> Diff(FloorData data)
    {
        var outp = new List<KeyValuePair<Vector2Int, string>>();
        if (data == null) return outp;
        var basis = new FloorState(data);
        foreach (var kv in cells)
            if (basis.At(kv.Key) != kv.Value)
                outp.Add(new KeyValuePair<Vector2Int, string>(kv.Key, kv.Value));
        outp.Sort((a, b) => a.Key.y != b.Key.y ? b.Key.y - a.Key.y : a.Key.x - b.Key.x);
        return outp;
    }

    /// <summary>差を当てる（ロードで使う）。</summary>
    public void Apply(IEnumerable<KeyValuePair<Vector2Int, string>> diff)
    {
        if (diff == null) return;
        foreach (var kv in diff) Put(kv.Key, kv.Value);
    }

    public string At(Vector2Int p)
    {
        string v;
        return cells.TryGetValue(p, out v) ? v : "#";
    }

    /// <summary>そのマスが床になっているか。門の解錠判定はこれを見る。</summary>
    public bool IsCleared(Vector2Int p) { return At(p) == Floor; }

    /// <summary>マスを床にする。敵を倒したとき・門が開いたときに呼ぶ。</summary>
    public void Clear(Vector2Int p) { cells[p] = Floor; dirty.Add(p); }

    /// <summary>マスに物を置く。イベントでアイテムや門が出現するときに呼ぶ。</summary>
    public void Put(Vector2Int p, string what) { cells[p] = what; dirty.Add(p); }

    /// <summary>変わったマスを取り出して記録を空にする。</summary>
    public List<Vector2Int> ConsumeDirty()
    {
        var copy = new List<Vector2Int>(dirty);
        dirty.Clear();
        return copy;
    }

    /// <summary>
    /// 解錠条件を判定し、満たしたものを適用する。
    /// 原典の判定関数（MTE.EXE 0x4762）と同じく、移動や戦闘のあとに毎回呼ぶ。
    /// </summary>
    /// <returns>適用した解錠の一覧。</returns>
    /// <param name="hasFlag">立っている一度きりフラグ（本線は遊び手の旗を渡す）。</param>
    /// <param name="mark">
    /// 発火した印を控える先。**本線は必ず渡すこと** — ここで控えないと、
    /// 印が階の器（この役）の中だけに残り、**セーブから読み直すと消える**。
    /// 守護マスはもう床のままなので解錠がもう一度走り、
    /// **取ったはずの褒美と妖精が生き返る**（作者の報告。10階で起きた）。
    /// 原典は旗を1本の配列に持っていて、セーブ（0x9b38）で
    /// `0x1229:0x1e` から 60 ワードぶんを地図と一緒に書き出している。
    /// </param>
    public List<SealedDoors.Unlock> ApplyUnlocks(System.Func<int, bool> hasFlag = null,
                                                 System.Action<int> mark = null)
    {
        var fired = new List<SealedDoors.Unlock>();
        // 前提フラグは「この階で既に開いた門」と「物語の一度きりフラグ」の両方から探す。
        // 原典はどちらも同じ1本の配列に並んでいる。
        System.Func<int, bool> has = f => firedFlags.Contains(f)
                                       || (hasFlag != null && hasFlag(f));
        foreach (var u in SealedDoors.Instance.ForFloor(FloorNumber))
        {
            if (has(u.FlagId)) continue;
            if (!SealedDoors.IsSatisfied(u, IsCleared, has)) continue;
            firedFlags.Add(u.FlagId);
            if (mark != null) mark(u.FlagId);
            foreach (var r in u.Results)
            {
                if (r.Kind == "表駆動" || r.Kind == "封印") { /* 地図は変えない */ }
                else Put(r.Pos, SymbolOfKind(r.Kind));
            }
            fired.Add(u);
        }
        return fired;
    }

    /// <summary>上下左右のマスの中身。魔法攻撃の判定に渡す。</summary>
    public MagicAttack.Neighbors NeighborsOf(Vector2Int p, System.Func<string, int> idOf)
    {
        return new MagicAttack.Neighbors
        {
            Up    = idOf(At(p + new Vector2Int(0, 1))),
            Down  = idOf(At(p + new Vector2Int(0, -1))),
            Left  = idOf(At(p + new Vector2Int(-1, 0))),
            Right = idOf(At(p + new Vector2Int(1, 0))),
        };
    }

    public bool HasFired(int flagId) { return firedFlags.Contains(flagId); }

    /// <summary>
    /// **目の前に続いているマグマ**（原典の地形 "~"）。雪の結晶で溶かす相手。
    ///
    /// 13階は盤面の大半がマグマ（81マス）なので、階ごと溶かしてはいけない。
    /// 原典（0x1fd5）が溶かすのも (0,-4) の上に続く4マスだけで、その先は壁。
    /// </summary>
    public List<Vector2Int> MagmaRun(Vector2Int from, Vector2Int dir)
    {
        var list = new List<Vector2Int>();
        if (dir == Vector2Int.zero) return list;
        var at = from + dir;
        while (At(at) == "~")
        {
            list.Add(at);
            at += dir;
        }
        return list;
    }
}
