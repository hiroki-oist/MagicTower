using UnityEngine;

/// <summary>
/// 1マス分の移動を解決する。原典 PC98版の規則をそのまま写した純粋な処理で、
/// Unity の当たり判定には頼らず <see cref="FloorState"/> の盤面だけを見る。
///
/// 原典の扉の規則（MTE.EXE 0x23ee）:
///   属性表の種別が1、すなわち **ID1,2,3 の「鍵の扉」だけ**が、対応する鍵を1本使って開く。
///   鍵が無ければ開かない。
///   **門(ID4) と特殊扉(ID5) はこの分岐に入らない**ので、触れても開かず壁と同じ。
///   門は解錠判定（守護マスが全部床になる）が満たされたときに地図側から床へ変わる。
///
/// 原典の移動の規則（MTE.EXE 0x00a6fb）:
///   行き先のマスが **床(6) のときだけ**プレーヤーの座標が動く。
///   それ以外（アイテム・敵・扉・隠し壁・階段）は対応処理 0x23ee を呼ぶだけで、
///   **その手では進まない**。アイテムも敵も隠し壁も「マスが床(6)になる」だけなので、
///   次のひと押しで初めてそこへ入る。リメイクもこの2手構えをそのまま写す。
/// </summary>
public static class TowerPlayer
{
    public enum Outcome
    {
        Blocked,        // 壁・門・鍵が足りない扉。動けない
        Moved,          // ただ進んだ
        OpenedDoor,     // 鍵を使って扉を開けた。この手では進まない
        TookItem,       // 物を拾った
        Encounter,      // 敵に当たった。戦闘へ
        Stairs,         // 階段を踏んだ
        Talk,           // 人や物に触れた。**その場で用が起きるので進まない**
        BrokeWall,      // 隠し壁を壊した。マスは床になるが、この手では進まない
    }

    public struct Result
    {
        public Outcome Outcome;
        public Vector2Int Target;   // 動こうとした先
        public string What;         // そのマスにあったもの
    }

    static Result Make(Outcome o, Vector2Int at, string what)
    {
        return new Result { Outcome = o, Target = at, What = what };
    }

    /// <summary>
    /// <paramref name="dir"/> の方向へ1マス動く。盤面と所持品を書き換え、何が起きたかを返す。
    /// </summary>
    public static Result Step(FloorState floor, PlayerState player, Vector2Int dir)
    {
        if (floor == null || player == null) return Make(Outcome.Blocked, Vector2Int.zero, null);

        var to = player.Position + dir;
        if (to.x < FloorData.MinX || to.x > FloorData.MaxX ||
            to.y < FloorData.MinY || to.y > FloorData.MaxY)
            return Make(Outcome.Blocked, to, null);

        var what = floor.At(to);

        switch (what)
        {
            case "%":                      // 隠し壁。壊すと床になる（原典 0x2f26 → 0x25f6）
                floor.Clear(to);
                return Make(Outcome.BrokeWall, to, what);

            case "#":                      // 壁
            case "Marker_ID10":            // 隠し壁と見た目が同じ「何かが隠れている」マス
            case "*":                      // 星空（F50 の周り）
            case "s":                      // 門。触れても開かない
            case "x":                      // 特殊扉。同じく開かない
                return Make(Outcome.Blocked, to, what);

            case "y": return TryOpen(floor, player, to, ref player.YellowKeys, what);
            case "b": return TryOpen(floor, player, to, ref player.PurpleKeys, what);
            case "r": return TryOpen(floor, player, to, ref player.RedKeys, what);

            case "~":                      // マグマ。原典では通れる（ダメージは別処理）
            case FloorState.Floor:
                player.Position = to;
                return Make(Outcome.Moved, to, what);

            case ">":
            case "<":
                // 階段も床(6)ではないので座標はここでは動かない。
                // 階の移動そのものが着地点を決める（TowerGame.EnterFloor）。
                return Make(Outcome.Stairs, to, what);
        }

        // 物体が乗っているマス
        if (ObjectIds.Instance.IsEnemy(what))
            return Make(Outcome.Encounter, to, what);

        // 人や物（仙人・商人・盗賊・人形・祭壇）は**上に乗らない**。原典も同じで、
        // ディスパッチャがそれぞれの処理を呼ぶだけでプレーヤーの位置は変わらない。
        if (TowerNpc.IsPerson(what))
            return Make(Outcome.Talk, to, what);

        return Make(Outcome.TookItem, to, what);
    }

    /// <summary>鍵を1本使って扉を開ける。足りなければ動かない。</summary>
    static Result TryOpen(FloorState floor, PlayerState player,
                          Vector2Int at, ref int keys, string what)
    {
        if (keys <= 0) return Make(Outcome.Blocked, at, what);
        keys--;
        floor.Clear(at);                   // 扉が消えて床になる
        return Make(Outcome.OpenedDoor, at, what);
    }
}
