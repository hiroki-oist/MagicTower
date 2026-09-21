using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 階に入ったときに盤面を描いていく順番。原典 PC98版 MTE.EXE の 0xafa6 が
/// 前置きで積む (x,y) の表そのもので、**左上から外周を回って中央で終わる渦巻き**。
///
/// 原典は **3マス描くごとに縦帰線を1回待つ**（`番号 % 3 == 0` で `lcall 0xf5f,0x3d9`）。
/// 高速モード（旗31）のときは **10マスごと**になるので、その分だけ早く出そろう。
///
/// 表の出どころは `docs/make_draw_order.py`（→ `Assets/Resources/FloorDrawOrder.txt`）。
/// </summary>
public class FloorDrawOrder
{
    /// <summary>何マス描くごとに1回待つか。</summary>
    public const int CellsPerWait = 3;

    /// <summary>高速モードのとき。</summary>
    public const int FastCellsPerWait = 10;

    static FloorDrawOrder instance;

    public static FloorDrawOrder Instance
    {
        get { return instance ?? (instance = Load()); }
    }

    readonly List<Vector2Int> cells = new List<Vector2Int>();

    /// <summary>描く順に並んだ121マス。</summary>
    public IReadOnlyList<Vector2Int> Cells { get { return cells; } }

    /// <summary>その数のときの待ちの回数ぶんの長さ（秒）。</summary>
    public static float Seconds(int cellCount, bool fast)
    {
        int per = fast ? FastCellsPerWait : CellsPerWait;
        return cellCount / per * MonsterAnimation.FrameSeconds;
    }

    static FloorDrawOrder Load()
    {
        var o = new FloorDrawOrder();
        var asset = Resources.Load<TextAsset>("FloorDrawOrder");
        if (asset == null)
        {
            Debug.LogWarning("FloorDrawOrder: 表が読めない");
            return o;
        }
        foreach (var line in asset.text.Split('\n'))
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith("#")) continue;
            var parts = t.Split('|');
            if (parts.Length < 3) continue;
            int n, x, y;
            if (!int.TryParse(parts[0].Trim(), out n)) continue;
            if (!int.TryParse(parts[1].Trim(), out x)) continue;
            if (!int.TryParse(parts[2].Trim(), out y)) continue;
            o.cells.Add(new Vector2Int(x, y));
        }
        return o;
    }
}
