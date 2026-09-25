using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 勇者のオーブ。**その階にいるモンスターの能力と、受ける予想ダメージ**を出す。
/// 原典 MTE.EXE の表示関数 0x8c2a をそのまま写した。
///
/// 原典の作り:
///   ・地図の121マスを頭（(-5,+5)）から順に見て、属性表の種別が **8 か 10**
///     （＝モンスターの1コマ目／2コマ目）のマスを拾う
///   ・同じ相手は1回だけ。**最大9体**まで（枠が9つしか無い）
///   ・1体ごとに モンスター表（14バイト）の 命(+0x63e) / 攻(+0x640) / 防(+0x642) を並べ、
///     最後に「予想ダメージ」を出す。**攻撃力が相手の防御力以下なら「攻撃不可能」**
///   ・予想ダメージは 相手を倒すまでに受ける合計。原典は
///     `命 ÷ (攻−相手の防) の商`（割り切れるときは −1）× `(相手の攻−防)` で出していて、
///     これは <see cref="BattleRules.Predict"/> の DamageTaken と同じ式になる
/// </summary>
public static class HeroOrb
{
    /// <summary>枠の数。原典は9体分しか場所が無い。</summary>
    public const int MaxRows = 9;

    public struct Row
    {
        public string Prefab;
        public int Id;
        /// <summary>相手の名前（Battlers の数値アセットのもの）。</summary>
        public string Name;
        /// <summary>倒すと手に入る金。</summary>
        public int Gold;
        /// <summary>倒すまでに要る手数（こちらの攻撃回数）。</summary>
        public int Turns;
        public int Hp, Attack, Defense;
        /// <summary>攻撃できるか。攻撃力が相手の防御力以下なら false（「攻撃不可能」）。</summary>
        public bool CanAttack;
        /// <summary>倒すまでに受ける合計。攻撃できないときは 0。</summary>
        public int Damage;
    }

    /// <summary>その階の相手を、地図の並び順に拾う（同じ相手は1回だけ、最大9体）。</summary>
    public static List<string> MonstersOn(FloorState floor)
    {
        var list = new List<string>();
        if (floor == null) return list;
        var ids = ObjectIds.Instance;
        for (int y = FloorData.MaxY; y >= FloorData.MinY; y--)       // 原典は (-5,+5) から行順
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
            {
                var what = floor.At(new Vector2Int(x, y));
                if (!ids.IsEnemy(what)) continue;
                if (list.Contains(what)) continue;
                list.Add(what);
                if (list.Count >= MaxRows) return list;
            }
        return list;
    }

    /// <summary>出す中身。</summary>
    public static List<Row> Rows(FloorState floor, PlayerState player)
    {
        var rows = new List<Row>();
        if (player == null) return rows;
        foreach (var name in MonstersOn(floor))
        {
            var e = TowerBattle.LoadEnemy(name);
            if (e == null) continue;
            int ehp = e.MaxHP, eatk = e.Attack, edef = e.Defense;
            // ★**サイコロの五の目は数値そのものに効く**。原典 0x5cdb は
            // **モンスター表（DS:0x63c から14バイト/件）の ID91 を 800/500/100 に書き換える**
            // ので、同じ表を読むこの窓（0x8c2a）にも**書き換え後の値が出る**。
            // ここを通していなかったので、五の目を作ってもゼノは 8000/5000/1000 のままに
            // 見えていた（作者の報告）。
            MagicSeal.Adjust(player, name, ref ehp, ref eatk, ref edef);
            // ★**十字架・ドラゴンスレイヤーの2倍を入れる**（作者の指定。
            //   <see cref="AuthorStyle.HeroOrbCountsCharms"/> に理由を書いてある）。
            //   原典 0x8c2a は補正を掛けないが、実際の戦いのほうは2倍で計算するので
            //   窓の数字だけが食い違って見えていた。
            var kind = BattleRules.KindFromTag(name);
            var charms = AuthorStyle.HeroOrbCountsCharms
                       ? TowerBattle.CharmsOf(player) : BattleRules.Charms.None;
            var p = BattleRules.Predict(player.Attack, player.Defense,
                                        ehp, eatk, edef, e.Coin,
                                        kind, charms);
            rows.Add(new Row
            {
                Prefab = name,
                Id = ObjectIds.Instance.Of(name),
                Name = e.Name,
                // ★**幸運の金貨の2倍もここに出す**（作者の指定。原典 0x8c2a は
                //   表の値をそのまま出す）。掛け算は BattleRules.GoldFrom 1か所。
                Gold = BattleRules.GoldFrom(e.Coin, charms),
                Turns = p.PlayerHits,
                Hp = ehp, Attack = eatk, Defense = edef,
                CanAttack = p.CanWin,
                Damage = p.CanWin ? p.DamageTaken : 0,
            });
        }
        return rows;
    }

    /// <summary>窓に出す文字。原典の見出しと同じ語を使う。</summary>
    public static string Text(FloorState floor, PlayerState player)
    {
        var t = GameText.Instance;
        var sb = new System.Text.StringBuilder();
        var rows = Rows(floor, player);
        if (rows.Count == 0) return "";      // 原典もその階に相手が居なければ何も描かない
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            if (i > 0) sb.Append('\n');
            sb.Append(t.Message("orb_label_hp")).Append(r.Hp).Append(' ');
            sb.Append(t.Message("orb_label_atk")).Append(r.Attack).Append(' ');
            sb.Append(t.Message("orb_label_def")).Append(r.Defense).Append(' ');
            sb.Append(r.CanAttack
                      ? t.Message("orb_damage") + r.Damage
                      : t.Message("orb_no_attack").Trim());
        }
        return sb.ToString();
    }
}
