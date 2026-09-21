using UnityEngine;

/// <summary>
/// 階の移動先と着地位置を決める。データは FloorConnections（PC98原典）に従う。
///
/// 原典の挙動:
///  - F10/F20/F30/F40 には上り階段が無い。10階ごとのブロックボスを倒すと開通する
///  - F44 は「幻の階」。着地位置が未定義で、通常の階段移動では入れない。
///    階段で移動すると F44 を飛ばして隣の階へ行く
///  - F49 には上り階段が無い（「実際には４９階までしか行けない」）
///  - F50 は階段を一切持たない
///  - 「昇華の翼」「降臨の翼」は1つ上/下の階へ強制的に移動する。
///    幻の階へはこれでしか行けない
/// </summary>
public static class FloorTransition
{
    /// <summary>
    /// 幻の階。階段では入れず、昇華の翼／降臨の翼でしか行けない。
    /// 原典は飛翔のオーブの階選びでもここだけを名指しで飛ばす（0x91d0 / 0x9210 の `cmp 0x2c`）。
    /// </summary>
    public const int PhantomFloor = 44;

    public struct Result
    {
        public bool Ok;
        public int Floor;
        public Vector2Int Arrive;
        public string Reason;   // 失敗時の説明
    }

    static Result Fail(string reason) => new Result { Ok = false, Reason = reason };

    /// <summary>上り階段を踏んだとき。幻の階は飛ばす。</summary>
    public static Result GoUp(int from) => Step(from, +1, skipUnreachable: true);

    /// <summary>下り階段を踏んだとき。幻の階は飛ばす。</summary>
    public static Result GoDown(int from) => Step(from, -1, skipUnreachable: true);

    /// <summary>昇華の翼 — 1つ上の階へ強制移動。幻の階にも入れる。</summary>
    public static Result WingUp(int from) => Step(from, +1, skipUnreachable: false);

    /// <summary>降臨の翼 — 1つ下の階へ強制移動。幻の階にも入れる。</summary>
    public static Result WingDown(int from) => Step(from, -1, skipUnreachable: false);

    static Result Step(int from, int dir, bool skipUnreachable)
    {
        var conn = FloorConnections.Instance;
        int target = from + dir;

        while (target >= FloorConnections.MinFloor && target <= FloorConnections.MaxFloor)
        {
            if (!conn.TryGet(target, out var info))
                return Fail($"{target}階の接続情報がありません");

            // 通常移動で着地位置が定義されていない階（幻の階）は飛ばす
            if (skipUnreachable && info.IsUnreachableByStairs)
            {
                target += dir;
                continue;
            }

            var arrive = dir > 0 ? info.ArriveFromBelow : info.ArriveFromAbove;
            if (arrive == null)
            {
                // ワープで入った場合など、着地位置が無い階は階段の位置に立たせる
                arrive = dir > 0 ? (info.Down ?? info.Up) : (info.Up ?? info.Down);
                if (arrive == null) return Fail($"{target}階に着地できる位置がありません");
            }
            return new Result { Ok = true, Floor = target, Arrive = arrive.Value };
        }
        return Fail(dir > 0 ? "これ以上、上の階はありません" : "これ以上、下の階はありません");
    }

    /// <summary>その階から上り階段で進めるか（ブロックボス未撃破なら上り階段自体が無い）。</summary>
    public static bool HasUpStairs(int floor)
        => FloorConnections.Instance.TryGet(floor, out var i) && i.Up != null;

    public static bool HasDownStairs(int floor)
        => FloorConnections.Instance.TryGet(floor, out var i) && i.Down != null;
}
