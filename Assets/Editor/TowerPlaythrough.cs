using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 実際にゲームを動かしてみる道具。単体テストは「規則が正しいか」しか見ないので、
/// **通しで遊べるか**を確かめるためにこれを使う。
///
/// やること: 1階から順に、その階の上り階段まで自動で歩く。
///   道の探索は盤面の上の幅優先探索。途中の敵・アイテム・扉は本線の `TowerGame.Step` に
///   そのまま任せるので、戦闘も鍵の消費も解錠も本番と同じ経路を通る。
///   進めなくなったらそこで止めて、理由を書き出す。
/// </summary>
public static class TowerPlaythrough
{
    [MenuItem("MagicTower/通しで動かしてみる")]
    public static void Run()
    {
        var report = Play(50);
        Debug.Log(report);
    }

    /// <summary>バッチから呼ぶ用。結果を標準出力に出す。</summary>
    public static void RunBatch()
    {
        var report = Play(50);
        System.Console.WriteLine(report);
        Debug.Log(report);
    }

    [MenuItem("MagicTower/全階を総当たりで確かめる")]
    public static void Sweep()
    {
        Debug.Log(SweepAll());
    }

    /// <summary>バッチから呼ぶ用。</summary>
    public static void SweepBatch()
    {
        var report = SweepAll();
        System.Console.WriteLine(report);
        Debug.Log(report);
    }

    /// <summary>
    /// 各階を「じゅうぶん強い状態」で始めて、上り階段まで行けるかを確かめる。
    /// 鍵も能力も足りている前提なので、**届かない階は仕組みの側の問題**と分かる。
    /// </summary>
    public static string SweepAll()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== 全階の総当たり ===");
        int ok = 0, ng = 0;
        for (int f = 1; f <= 50; f++)
        {
            var p = new PlayerState
            {
                Hp = PlayerState.MaxHp, Attack = PlayerState.MaxAttack,
                Defense = PlayerState.MaxDefense, Gold = PlayerState.MaxGold,
                YellowKeys = 99, PurpleKeys = 99, RedKeys = 99,
                SwordRank = 5, ShieldRank = 5,
                Floor = f, Position = new Vector2Int(0, 0),
            };
            var game = new TowerGame(p);
            game.ApplyAllArrivals();      // 階段のできごとは済んでいるものとして見る
            var start = StartCell(game);
            if (!start.HasValue) { sb.AppendLine($"F{f,2}: 立てる場所が無い"); ng++; continue; }
            game.Player.Position = start.Value;

            var goal = FindStairs(game.Floor, ">");
            if (!goal.HasValue)
            {
                // ブロックの関門は最初から上り階段を持たない。ボスを倒すと出るので、
                // 行き先を決めずに盤面のものへ触りきってから、もう一度さがす。
                var first = ExploreFloor(game, null, 3000);
                goal = FindStairs(game.Floor, ">");
                if (!goal.HasValue)
                {
                    // 原典の接続表が「上から降りてくる着地位置」を持たない階は、
                    // そもそも階段で上がる階ではない（50階＝最上階、49階＝終盤の演出）。
                    var info = FloorConnections.Instance.Get(f);
                    if (info.ArriveFromAbove == null)
                    {
                        sb.AppendLine($"F{f,2}: 上り階段は原典にも無い（接続表の着地位置が '-'）");
                        ok++; continue;
                    }
                    sb.AppendLine($"F{f,2}: 触りきっても上り階段が出ない — {first}");
                    ng++; continue;
                }
            }
            var why = ExploreFloor(game, goal.Value, 3000);
            if (game.Player.Floor != f) { ok++; continue; }
            sb.AppendLine($"F{f,2}: 届かない — {why}");
            ng++;
        }
        sb.AppendLine($"届いた {ok}階 / 届かない {ng}階");
        return sb.ToString();
    }

    /// <summary>その階で立てる場所（下り階段の隣、無ければ床のどこか）。</summary>
    static Vector2Int? StartCell(TowerGame game)
    {
        var down = FindStairs(game.Floor, "<");
        if (down.HasValue)
            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var c = down.Value + d;
                if (c.x < FloorData.MinX || c.x > FloorData.MaxX ||
                    c.y < FloorData.MinY || c.y > FloorData.MaxY) continue;
                if (game.Floor.IsCleared(c)) return c;
            }
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
            {
                var c = new Vector2Int(x, y);
                if (game.Floor.IsCleared(c)) return c;
            }
        return null;
    }

    public static string Play(int upTo)
    {
        var sb = new StringBuilder();
        var game = new TowerGame(PlayerState.NewGame());
        sb.AppendLine("=== 通し ===");
        sb.AppendLine($"開始: {Describe(game)}");

        for (int n = 0; n < upTo; n++)
        {
            int floor = game.Player.Floor;
            var goal = FindStairs(game.Floor, ">");
            if (!goal.HasValue)
            {
                sb.AppendLine($"F{floor}: 上り階段が無いので終わり（{Describe(game)}）");
                break;
            }

            var why = ExploreFloor(game, goal.Value, 4000);
            if (game.Player.Floor == floor)
            {
                sb.AppendLine($"F{floor}: 階段({goal.Value.x:+#;-#;0},{goal.Value.y:+#;-#;0}) に届かない — {why}");
                sb.AppendLine($"        {Describe(game)}");
                break;
            }
            sb.AppendLine($"F{floor} → F{game.Player.Floor}  {Describe(game)}");
            if (game.Player.Hp <= 0) { sb.AppendLine("生命力が尽きた"); break; }
        }
        return sb.ToString();
    }

    static string Describe(TowerGame g)
    {
        var p = g.Player;
        return $"生命力{p.Hp} 攻{p.Attack} 防{p.Defense} 金{p.Gold} "
             + $"鍵{p.YellowKeys}/{p.PurpleKeys}/{p.RedKeys} 剣{p.SwordRank} 盾{p.ShieldRank}";
    }

    static Vector2Int? FindStairs(FloorState floor, string what)
    {
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
            {
                var c = new Vector2Int(x, y);
                if (floor.At(c) == what) return c;
            }
        return null;
    }

    /// <summary>
    /// その階を探索する。行ける範囲の「触れるもの」（アイテム・敵・開けられる扉）に
    /// 近い順にぶつかっていき、何も残らなくなったら上り階段へ向かう。
    /// 開けられなかった扉などは覚えておき、鍵が増えたら忘れて再挑戦する。
    /// </summary>
    static string ExploreFloor(TowerGame game, Vector2Int? goalOrNull, int limit)
    {
        // 行き先が無い（＝関門で階段がまだ出ていない）ときは、
        // 触れるものが尽きるまで触り続けるだけにする。
        var goal = goalOrNull ?? Vector2Int.zero;
        bool hasGoal = goalOrNull.HasValue;
        int startFloor = game.Player.Floor;
        var giveUp = new HashSet<Vector2Int>();
        // 用の済んだ人。商人は買うたびに鍵の数が変わるので、
        // 諦めた扉と同じ扱いにすると「買う→諦めを忘れる→また買う」を延々くり返す。
        var done = new HashSet<Vector2Int>();
        int keysBefore = KeySum(game.Player);
        int sameGoal = 0, lastGoalTry = -2;
        var trace = new List<string>();

        for (int step = 0; step < limit; step++)
        {
            if (game.Player.Floor != startFloor) return "";
            if (game.Player.Hp <= 0) return "生命力が尽きた";
            while (game.IsShowingPage) game.NextPage();

            if (KeySum(game.Player) != keysBefore)
            {
                giveUp.Clear();                  // 鍵が変わったら開かなかった扉を試し直す
                foreach (var d in done) giveUp.Add(d);
                keysBefore = KeySum(game.Player);
            }

            var next = NearestTarget(game, giveUp);
            if (!hasGoal && next == null) return "触れるものが残っていない";
            // 扉しか残っていないのに階段へは行けるなら、鍵を使わずに上がる。
            // 原典の遊び方に近く、次の階へ鍵を持ち越せる（4階は鍵1本が要る）。
            if (hasGoal && next.HasValue && IsDoor(game.Floor.At(next.Value))
                && Route(game.Floor, game.Player.Position, goal, giveUp, game) != null)
                next = null;
            var target = next ?? goal;
            if (step % 500 == 499)
                trace.Add($"{step}歩: 位置({game.Player.Position.x},{game.Player.Position.y}) "
                        + $"→({target.x},{target.y}){game.Floor.At(target)} "
                        + $"諦め{giveUp.Count} 鍵{game.Player.YellowKeys}/{game.Player.PurpleKeys}/{game.Player.RedKeys}");
            if (hasGoal && target == goal && lastGoalTry == step - 1) sameGoal++; else sameGoal = 0;
            if (hasGoal && target == goal) lastGoalTry = step;
            if (sameGoal > 200) return Stuck(game, goal, giveUp, "階段へ向かい続けて進まない");
            var path = Route(game.Floor, game.Player.Position, target, giveUp, game);
            if (path == null || path.Count == 0)
            {
                if (target == goal && hasGoal) return Stuck(game, goal, giveUp, "階段まで道が無い");
                if (target == goal) return "触れるものに道が無い";
                giveUp.Add(target);
                continue;
            }

            var before = game.Player.Position;
            int hpBefore = game.Player.Hp;
            int goldBefore = game.Player.Gold;
            var what = game.Floor.At(path[0]);
            game.Step(path[0] - before);

            // 商人と祭壇は「話す」だけでは何も進まないので、ここで実際に取引する
            bool traded = false;
            if (path[0] == target)
            {
                if (TowerNpc.IsMerchant(what)) traded = BuyAll(game);
                else if (TowerNpc.IsAltar(what)) traded = DonateSome(game);
                if (TowerNpc.IsPerson(what)) { done.Add(target); giveUp.Add(target); }
            }

            // 原典は行き先が床のときだけ進むので、「動かなかった＝何も起きなかった」ではない。
            // マスの中身が変わったかどうかで判断する。
            if (!traded && game.Player.Position == before
                && game.Player.Floor == startFloor
                && game.Floor.At(path[0]) == what
                && game.Player.Hp == hpBefore && game.Player.Gold == goldBefore)
            {
                giveUp.Add(path[0]);             // ぶつかったが何も起きなかった
            }
        }
        return Stuck(game, goal, giveUp, "手数を使い切った")
             + (trace.Count > 0 ? "\n        途中: " + string.Join(" | ", trace.ToArray()) : "");
    }

    /// <summary>止まった理由を詳しく書く。</summary>
    static string Stuck(TowerGame game, Vector2Int goal, HashSet<Vector2Int> giveUp, string why)
    {
        var sb = new StringBuilder(why);
        var path = Route(game.Floor, game.Player.Position, goal, giveUp, game);
        sb.Append(path == null ? " / 階段まで道が無い" : $" / 階段まで{path.Count}歩");

        var left = new List<string>();
        foreach (var c in giveUp)
            left.Add($"({c.x:+#;-#;0},{c.y:+#;-#;0}){game.Floor.At(c)}");
        left.Sort();
        if (left.Count > 0)
            sb.Append(" / 触れなかったもの: " + string.Join(" ", left.ToArray()));

        sb.Append("\n        止まったときの盤面（＠が自分）:");
        for (int y = FloorData.MaxY; y >= FloorData.MinY; y--)
        {
            sb.Append($"\n        {y,3}|");
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
            {
                var c = new Vector2Int(x, y);
                if (c == game.Player.Position) { sb.Append('＠'); continue; }
                var w = game.Floor.At(c);
                sb.Append(Glyph(w));
            }
        }
        return sb.ToString();
    }

    /// <summary>盤面を1文字で表す。</summary>
    static string Glyph(string what)
    {
        switch (what)
        {
            case ".": return "・"; case "#": return "■"; case "%": return "▨";
            case "~": return "炎"; case "*": return "星"; case ">": return "上";
            case "<": return "下"; case "y": return "黄"; case "b": return "紫";
            case "r": return "赤"; case "s": return "門"; case "x": return "特";
        }
        if (what.StartsWith("Enemy_") || what.StartsWith("Boss_")) return "敵";
        if (TowerNpc.IsPerson(what)) return "人";
        if (what.StartsWith("Key_")) return "鍵";
        return "品";
    }

    /// <summary>買えるだけ買う。鍵が足りなくなって詰まるのを防ぐ。</summary>
    static bool BuyAll(TowerGame game)
    {
        bool any = false;
        foreach (var o in game.MerchantOffersHere())
        {
            for (int i = 0; i < 8; i++)
            {
                int gold = game.Player.Gold;
                int keys = KeySum(game.Player);
                TowerNpc.Buy(game.Player, o);
                if (game.Player.Gold == gold && KeySum(game.Player) == keys) break;
                any = true;
            }
        }
        return any;
    }

    /// <summary>金があるうちに祭壇で攻撃力を上げる。</summary>
    static bool DonateSome(TowerGame game)
    {
        bool any = false;
        for (int i = 0; i < 4; i++)
        {
            if (game.Player.Gold < TowerNpc.AltarCost(game.Player)) break;
            int atk = game.Player.Attack;
            TowerNpc.Donate(game.Player, TowerNpc.Wish.Attack);
            if (game.Player.Attack == atk) break;
            any = true;
        }
        return any;
    }

    /// <summary>その敵と戦うと痛すぎるか。</summary>
    static bool TooCostly(TowerGame game, string enemy)
    {
        var e = TowerBattle.LoadEnemy(enemy);
        if (e == null) return true;
        var p = game.Player;
        var kind = BattleRules.KindFromTag(enemy);
        var charms = TowerBattle.CharmsOf(p);
        var r = BattleRules.Predict(p.Attack, p.Defense, e.MaxHP, e.Attack, e.Defense,
                                    e.Coin, kind, charms);
        if (!r.CanWin) return true;
        // 隣の魔術士からの魔法攻撃は読み切れないので、余裕をもって避ける。
        return r.DamageTaken * 4 >= p.Hp;
    }

    static int KeySum(PlayerState p)
    {
        return p.YellowKeys * 10000 + p.PurpleKeys * 100 + p.RedKeys;
    }

    /// <summary>
    /// 行ける範囲のうち、次に触れるマス。**種類で順を付ける**:
    ///   ① アイテム（鍵を先に拾わないと扉で詰まる）
    ///   ② 人（商人で鍵を買い、祭壇で強くなる）
    ///   ③ 敵（痛すぎる相手は後回し）
    ///   ④ 扉（鍵を使うので最後）
    /// 同じ種類なら近い順。
    /// </summary>
    static Vector2Int? NearestTarget(TowerGame game, HashSet<Vector2Int> giveUp)
    {
        var floor = game.Floor;
        var from = game.Player.Position;
        var seen = new HashSet<Vector2Int> { from };
        var q = new Queue<Vector2Int>();
        q.Enqueue(from);
        var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        var best = new Vector2Int?[5];

        while (q.Count > 0)
        {
            var at = q.Dequeue();
            foreach (var d in dirs)
            {
                var n = at + d;
                if (seen.Contains(n)) continue;
                if (n.x < FloorData.MinX || n.x > FloorData.MaxX ||
                    n.y < FloorData.MinY || n.y > FloorData.MaxY) continue;
                seen.Add(n);

                var what = floor.At(n);
                if (what == FloorState.Floor || what == "~") { q.Enqueue(n); continue; }
                if (what == ">" || what == "<") continue;      // 階段は最後
                if (giveUp.Contains(n)) continue;

                int rank = Rank(game, what);
                if (rank < 0) continue;
                if (!best[rank].HasValue) best[rank] = n;      // 近い順に見ているので先勝ち
            }
        }
        foreach (var b in best) if (b.HasValue) return b;
        return null;
    }

    static bool IsDoor(string what)
    {
        return what == "y" || what == "b" || what == "r";
    }

    /// <summary>触れる順。小さいほど先。触れないものは -1。</summary>
    static int Rank(TowerGame game, string what)
    {
        if (what == "y" || what == "b" || what == "r") return 3;   // 扉は最後
        if (TowerNpc.IsPerson(what)) return 1;
        if (ObjectIds.Instance.IsEnemy(what)) return TooCostly(game, what) ? -1 : 2;
        if (!Passable(what)) return -1;
        return 0;                                                  // アイテム
    }

    /// <summary>幅優先で道を引く。通れないマスは避ける。</summary>
    static List<Vector2Int> Route(FloorState floor, Vector2Int from, Vector2Int to,
                                  HashSet<Vector2Int> avoid = null, TowerGame game = null)
    {
        var prev = new Dictionary<Vector2Int, Vector2Int>();
        var seen = new HashSet<Vector2Int> { from };
        var q = new Queue<Vector2Int>();
        q.Enqueue(from);
        var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (q.Count > 0)
        {
            var at = q.Dequeue();
            if (at == to)
            {
                var path = new List<Vector2Int>();
                var c = to;
                while (c != from) { path.Add(c); c = prev[c]; }
                path.Reverse();
                return path;
            }
            foreach (var d in dirs)
            {
                var n = at + d;
                if (seen.Contains(n)) continue;
                if (n.x < FloorData.MinX || n.x > FloorData.MaxX ||
                    n.y < FloorData.MinY || n.y > FloorData.MaxY) continue;
                if (avoid != null && n != to && avoid.Contains(n)) continue;
                var w = floor.At(n);
                if (n != to && (w == ">" || w == "<")) continue;   // 階段は踏まない
                if (!Passable(w) && n != to) continue;
                // 勝てない（割に合わない）敵のマスは通らない。通ると戦う羽目になる。
                if (n != to && game != null && ObjectIds.Instance.IsEnemy(w)
                    && TooCostly(game, w)) continue;
                seen.Add(n);
                prev[n] = at;
                q.Enqueue(n);
            }
        }
        return null;
    }

    /// <summary>そのマスへ進もうとして良いか。壁と門と特殊扉は通らない。</summary>
    static bool Passable(string what)
    {
        switch (what)
        {
            case "#": case "*": case "s": case "x":
            case "Marker_ID10":
                return false;
            // 隠し壁は壊せば床になる（壊す手＋入る手の2手かかる）ので道に数える
            case "%":
                return true;
            default:
                // 人や物（仙人・商人・盗賊・人形・祭壇）は**通り抜けられない**。
                // 触れても上に乗らないので、道の途中に置くと進めなくなる。
                if (TowerNpc.IsPerson(what)) return false;
                return true;      // 床・扉・階段・アイテム・敵はぶつかってみる
        }
    }
}
