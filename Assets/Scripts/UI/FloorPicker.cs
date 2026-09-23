using UnityEngine;

/// <summary>
/// 飛翔のオーブの階選びの画面（作者の指定。原典は窓に1行出すだけ）。
///
/// 原典（0x91b4〜0x921d）は「↑↓で移動する階を選択して、リターンで決定してください。」
/// と出して、**今どの階を選んでいるかは数字を1つ書き換えるだけ**で見せていた。
/// こちらは 1階〜50階を並べて出し、
///   ・飛べる階（1階〜到達最高階。**44階の幻の階は飛ばす**）… はっきり
///   ・まだ行けない階                                        … 沈ませる
/// と描き分けて、選んでいる階に印を置く。
/// </summary>
public static class FloorPicker
{
    /// <summary>並べる階の数（塔の高さ）。</summary>
    public const int Floors = 50;

    /// <summary>横に並べる数。1行10階ぶんなので、左右で10階ずつ跳べる。</summary>
    public const int Columns = 10;
    public static int Rows { get { return (Floors + Columns - 1) / Columns; } }

    // ---- 原典の画面（640x400）の中での置き場所 ----

    /// <summary>窓（案内と一覧を収める）。</summary>
    public static readonly RectInt Window = new RectInt(20, 40, 600, 320);

    /// <summary>案内の文字を置く場所。</summary>
    public static readonly RectInt Prompt = new RectInt(36, 56, 568, 40);

    /// <summary>ますの大きさと間合い。</summary>
    public const int CellWidth = 56, CellHeight = 40, RowPitch = 44;
    /// <summary>一覧の左上。</summary>
    public const int GridLeft = 40, GridTop = 118;

    /// <summary>n 階（1から）のますの場所。</summary>
    public static RectInt CellOf(int floor)
    {
        int i = Mathf.Clamp(floor, 1, Floors) - 1;
        return new RectInt(GridLeft + (i % Columns) * CellWidth,
                           GridTop + (i / Columns) * RowPitch,
                           CellWidth, CellHeight);
    }

    /// <summary>
    /// そこへ飛べるか。原典（0x91b4）は**1階から到達最高階まで**を上下するだけで、
    /// 訪れたかどうかは見ていない。**44階（幻の階）だけ名指しで飛ばす**。
    /// </summary>
    public static bool CanGo(PlayerState p, int floor)
    {
        if (p == null || floor < 1 || floor > Floors) return false;
        if (floor == FloorTransition.PhantomFloor) return false;
        return floor <= p.HighestFloor;
    }

    /// <summary>飛べる階の色。</summary>
    public static readonly Color Reachable   = new Color32(0xff, 0xff, 0xff, 0xff);
    /// <summary>まだ行けない階の色（沈ませる）。</summary>
    public static readonly Color Unreachable = new Color32(0x6a, 0x6a, 0x6a, 0xff);
    /// <summary>選んでいる階に敷く色と、その上の字の色。</summary>
    public static readonly Color Highlight   = new Color32(0xd8, 0xd8, 0xd8, 0xff);
    public static readonly Color OnHighlight = new Color32(0x22, 0x22, 0x26, 0xff);
}
