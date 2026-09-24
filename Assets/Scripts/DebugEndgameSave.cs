using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **調べもの用のセーブを1つ作る**。原典の再現ではないので、
/// 見終わったら <see cref="DebugPlay.MakeEndgameSave"/> を false に戻すこと。
///
/// 作者の頼み（2026-09-24）:
/// 「24階と26階とドラゴンを除いて48階までの敵を全て倒してイベント全部消化して
///   特殊アイテムもドラゴンスレイヤー以外全部持ってる状態のセーブデータがある状態で
///   初期化してくれない？　49階ボス〜エンディングまでだけ見たい」
///   「攻撃力と守備力、生命力はカンストさせて。セーブボタンとロードボタンを押した際に
///     書き込むセーブデータスロットでお願いします。……2階丸ごと手付かずで、
///     代わりに鍵を全種類30個ずつ持たせてください。」
///
/// 作り方は**盤面を直に書き換えるのではなく、原典の表をそのまま回す**:
///   1. 48階までの敵と拾い物と扉を消す（35階のドラゴンと24・26階は手付かず）
///   2. 立ち入りのできごとの印を立て、その地図の書き換えも当てる
///   3. <see cref="FloorState.ApplyUnlocks"/> を回す（守護マスが床になったので門が開き、褒美が出る）
///   4. 出てきた褒美をまた 1 で消す — 変わらなくなるまで繰り返す
/// こうすると「本当に登ってきた人のセーブ」と同じ形になる。
/// </summary>
public static class DebugEndgameSave
{
    /// <summary>どこから始めるセーブを作るか。</summary>
    public const string BeforeZeno = "49階のボスの手前";
    public const string BeforeGate = "24階の時空の門の1歩手前";

    /// <summary>ここまでの階を片付ける。49階の手前から始めるなら48階まで。</summary>
    public static int LastClearedFor(string at)
    {
        return at == BeforeGate ? 49 : 48;
    }

    /// <summary>まるごと手付かずにする階。門の手前から始めるならもう通ってきている。</summary>
    public static int[] UntouchedFor(string at)
    {
        return at == BeforeGate ? new int[0] : new[] { 24, 26 };
    }

    /// <summary>ここまでの階を片付ける（既定の入り口）。</summary>
    public const int LastCleared = 48;

    /// <summary>まるごと手付かずにする階（作者の指定）。</summary>
    public static readonly int[] Untouched = { 24, 26 };

    /// <summary>倒さずに残す相手。</summary>
    public const int DragonFloor = 35;
    public const string DragonPrefab = "Boss_Dragon";

    /// <summary>始める場所。49階の下り階段＝48階から上がってきたところ。</summary>
    public const int StartFloor = 49;
    public static readonly Vector2Int StartPos = new Vector2Int(-5, -5);

    /// <summary>
    /// 時空の門の1歩手前。24階の (0,-2)＝赤い扉のあったマスで、
    /// **上へ1歩踏み込むと (0,-1) に立って終幕が始まる**（原典 0x22cc）。
    /// </summary>
    public const int GateStartFloor = 24;
    public static readonly Vector2Int GateStartPos = new Vector2Int(0, -2);

    /// <summary>鍵の本数（作者の指定）。</summary>
    public const int Keys = 30;

    /// <summary>盗賊と階段のできごとの印（表に階が無いのでここに書く）。</summary>
    static readonly int[] ThiefFlags = { 88, 107, 118 };
    static readonly int[] ArrivalFlags = { 108 };

    /// <summary>そのマスを片付けるか（敵・拾い物・妖精・扉）。</summary>
    static bool ShouldClear(int floor, Vector2Int at, string cell)
    {
        if (string.IsNullOrEmpty(cell) || cell == FloorState.Floor) return false;
        // 地形はそのまま。ただし扉と門は「開けた」ことにする
        switch (cell)
        {
            case "y": case "b": case "r": case "s": case "x": return true;
            case "#": case "%": case "~": case ">": case "<": case "*": return false;
        }
        // 話しかける相手（仙人・商人・祭壇・盗賊・人形）は残す
        if (TowerNpc.IsMerchant(cell) || TowerNpc.IsAltar(cell) || TowerNpc.IsMaster(cell)
            || TowerNpc.IsThief(cell) || TowerNpc.IsDoll(cell)) return false;
        // 35階のドラゴンだけは倒さない（作者の指定）
        if (floor == DragonFloor && cell == DragonPrefab) return false;
        return true;      // 敵・拾い物・妖精
    }

    /// <summary>その状態を組み立てる（既定の入り口＝<see cref="DebugPlay.EndgameSaveAt"/>）。</summary>
    public static void Build(out PlayerState player, out Dictionary<int, FloorState> floors)
    {
        Build(DebugPlay.EndgameSaveAt, out player, out floors);
    }

    /// <summary>その状態を組み立てる。</summary>
    public static void Build(string at,
                             out PlayerState player, out Dictionary<int, FloorState> floors)
    {
        int lastCleared = LastClearedFor(at);
        var untouched = UntouchedFor(at);
        System.Func<int, bool> cleared = f =>
        {
            if (f < 1 || f > lastCleared) return false;
            foreach (var u in untouched) if (u == f) return false;
            return true;
        };

        player = new PlayerState
        {
            Hp = PlayerState.MaxHp,
            Attack = PlayerState.MaxAttack,
            Defense = PlayerState.MaxDefense,
            Gold = PlayerState.MaxGold,
            YellowKeys = Keys, PurpleKeys = Keys, RedKeys = Keys,
            SwordRank = 5, ShieldRank = 5,          // 神剣・神盾
            Floor = at == BeforeGate ? GateStartFloor : StartFloor,
            HighestFloor = StartFloor,
            Position = at == BeforeGate ? GateStartPos : StartPos,
        };
        // ドラゴンスレイヤー以外の14種（門の手前まで来ているならゼノを倒しているので15種）
        foreach (SpecialItems.Kind k in System.Enum.GetValues(typeof(SpecialItems.Kind)))
            if (k != SpecialItems.Kind.None
                && (at == BeforeGate || k != SpecialItems.Kind.DragonSlayer))
                player.Items |= k;
        for (int f = 1; f <= StartFloor; f++) player.Visited.Add(f);

        // 一度きりの印。片付けた階のものだけ立てる
        foreach (var e in PositionEvents.Instance.All)
            if (e.FlagId >= 0 && cleared(e.Floor)) player.EventFlags.Add(e.FlagId);
        foreach (var f in ThiefFlags) player.EventFlags.Add(f);
        foreach (var f in ArrivalFlags) player.EventFlags.Add(f);
        // 門の手前まで来ているなら、49階のサイコロの五の目も済んでいる
        if (at == BeforeGate) player.EventFlags.Add(MagicSeal.FlagId);

        floors = new Dictionary<int, FloorState>();
        for (int f = 1; f <= StartFloor; f++)
        {
            var data = FloorData.Load(f);
            if (data != null) floors[f] = new FloorState(data);
        }

        // 立ち入りのできごとが書き換える地図も当てる（15階の門、30階の門など）
        foreach (var e in PositionEvents.Instance.All)
        {
            if (!cleared(e.Floor) || e.Changes == null) continue;
            foreach (var c in e.Changes)
            {
                FloorState st;
                if (cleared(c.Floor) && floors.TryGetValue(c.Floor, out st))
                    st.Put(c.Pos, FloorState.SymbolOfKind(c.Kind));
            }
        }

        // 片付ける → 門が開いて褒美が出る → その褒美をまた片付ける、を落ち着くまで
        for (int pass = 0; pass < 8; pass++)
        {
            bool changed = false;
            for (int f = 1; f <= lastCleared; f++)
            {
                if (!cleared(f) || !floors.ContainsKey(f)) continue;
                var st = floors[f];
                for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
                    for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (!ShouldClear(f, cell, st.At(cell))) continue;
                        st.Clear(cell);
                        changed = true;
                    }
                if (st.ApplyUnlocks(player.EventFlags.Contains).Count > 0) changed = true;
            }
            if (!changed) break;
        }
        // 26階の人形に触れたあとなら、24階の中央に時空の門が出ている（原典 0x2a44）
        if (at == BeforeGate)
        {
            FloorState gate;
            if (floors.TryGetValue(SpacetimeGate.GateFloor, out gate))
                gate.Put(SpacetimeGate.GatePos, "s");
        }
        player.ClampStats();
    }

    /// <summary>そのスロットに書き込む。</summary>
    public static void WriteTo(int slot) { WriteTo(slot, DebugPlay.EndgameSaveAt); }

    /// <summary>そのスロットに書き込む。</summary>
    public static void WriteTo(int slot, string at)
    {
        PlayerState p;
        Dictionary<int, FloorState> floors;
        Build(at, out p, out floors);
        TowerSave.Save(slot, p, floors);
    }
}
