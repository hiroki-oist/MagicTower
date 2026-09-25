using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 勇者のオーブ。原典 MTE.EXE 0x8c2a の表示をそのまま写したもの。
/// </summary>
public class HeroOrbTests
{
    static PlayerState Player(int floor, int atk, int def)
    {
        return new PlayerState
        {
            Hp = 1000, Attack = atk, Defense = def,
            Floor = floor, Position = new Vector2Int(0, 0),
        };
    }

    [Test]
    public void その階の相手を地図の順に最大9体まで拾う()
    {
        var g = new TowerGame(Player(1, 100, 100));
        var list = HeroOrb.MonstersOn(g.Floor);
        Assert.Greater(list.Count, 0, "1階には相手がいる");
        Assert.LessOrEqual(list.Count, HeroOrb.MaxRows, "枠は9つまで（原典の作り）");
        CollectionAssert.AllItemsAreUnique(list, "同じ相手は1回だけ");
        foreach (var n in list)
            Assert.IsTrue(ObjectIds.Instance.IsEnemy(n), n + " は敵");
    }

    [Test]
    public void 拾う順は盤面の上の行から()
    {
        var g = new TowerGame(Player(1, 100, 100));
        // 盤面をまっさらにして、上下に別の相手を置く
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                g.Floor.Clear(new Vector2Int(x, y));
        g.Floor.Put(new Vector2Int(0, -3), "Enemy_Bat");
        g.Floor.Put(new Vector2Int(0, 3), "Enemy_SlimeG");
        var list = HeroOrb.MonstersOn(g.Floor);
        Assert.AreEqual(new[] { "Enemy_SlimeG", "Enemy_Bat" }, list.ToArray(),
                        "原典は (-5,+5) から行順に見る");
    }

    [Test]
    public void 攻撃力が足りなければ攻撃不可能になる()
    {
        var g = new TowerGame(Player(1, 10, 10));
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                g.Floor.Clear(new Vector2Int(x, y));
        g.Floor.Put(new Vector2Int(0, 1), "Enemy_Rock");      // 防御力の高い相手
        var rows = HeroOrb.Rows(g.Floor, g.Player);
        Assert.AreEqual(1, rows.Count);
        Assert.IsFalse(rows[0].CanAttack, "攻撃力10では岩は抜けない");
        Assert.AreEqual(0, rows[0].Damage);
        StringAssert.Contains("攻撃不可能", HeroOrb.Text(g.Floor, g.Player));
    }

    [Test]
    public void 予想ダメージは戦闘の式と同じ()
    {
        var g = new TowerGame(Player(1, 300, 100));
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                g.Floor.Clear(new Vector2Int(x, y));
        g.Floor.Put(new Vector2Int(0, 1), "Enemy_SlimeG");
        var rows = HeroOrb.Rows(g.Floor, g.Player);
        var e = TowerBattle.LoadEnemy("Enemy_SlimeG");
        var want = BattleRules.Predict(300, 100, e.MaxHP, e.Attack, e.Defense, e.Coin,
                                       BattleRules.KindFromTag("Enemy_SlimeG"),
                                       BattleRules.Charms.None);
        Assert.IsTrue(rows[0].CanAttack);
        Assert.AreEqual(want.DamageTaken, rows[0].Damage);
        Assert.AreEqual(e.MaxHP, rows[0].Hp);
        Assert.AreEqual(e.Attack, rows[0].Attack);
        Assert.AreEqual(e.Defense, rows[0].Defense);
    }

    /// <summary>
    /// ★**十字架の2倍が入る**（作者の指定。<see cref="AuthorStyle.HeroOrbCountsCharms"/>）。
    /// 原典 0x8c2a は補正を掛けず、説明（レコード60）も「特殊なアイテムを持っている
    /// 場合には実際に受けるダメージと異なる場合がある」と断っているが、
    /// 実際の戦いは2倍で計算するので窓の数字だけが食い違って見えていた。
    /// </summary>
    [Test]
    public void 十字架の二倍が予想にも入る()
    {
        // 何撃も要る組み合わせにする（一撃で倒せると受けるダメージが両方0になって
        // 「2倍で楽になった」ことが見えない）。ゾンビ 260/85/5、ゾンビナイト 320/120/15。
        var p = Player(1, 60, 20);
        p.Give(SpecialItems.Kind.Cross);
        var g = new TowerGame(p);
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                g.Floor.Clear(new Vector2Int(x, y));
        // ゾンビ(ID85)とゾンビナイト(ID87)はどちらも十字架が効く（原典 0x6101）
        g.Floor.Put(new Vector2Int(0, 1), "Enemy_Zombie");
        g.Floor.Put(new Vector2Int(0, -1), "Enemy_ZombieWorrior");
        var rows = HeroOrb.Rows(g.Floor, g.Player);
        Assert.AreEqual(2, rows.Count);
        foreach (var row in rows)
        {
            var e = TowerBattle.LoadEnemy(row.Prefab);
            var kind = BattleRules.KindFromTag(row.Prefab);
            Assert.AreEqual(BattleRules.EnemyKind.Zombie, kind, row.Prefab);
            var plain = BattleRules.Predict(60, 20, e.MaxHP, e.Attack, e.Defense, e.Coin,
                                            kind, BattleRules.Charms.None);
            var doubled = BattleRules.Predict(60, 20, e.MaxHP, e.Attack, e.Defense, e.Coin,
                                              kind, BattleRules.Charms.Cross);
            Assert.AreEqual(doubled.DamageTaken, row.Damage, row.Prefab + " のダメージ");
            Assert.AreEqual(doubled.PlayerHits, row.Turns, row.Prefab + " の手数");
            Assert.Less(doubled.DamageTaken, plain.DamageTaken, "2倍のぶん楽になる");
            // 相手そのものの数値は触らない
            Assert.AreEqual(e.MaxHP, row.Hp);
            Assert.AreEqual(e.Attack, row.Attack);
            Assert.AreEqual(e.Defense, row.Defense);
            Assert.AreEqual(e.Coin, row.Gold);
        }
    }

    /// <summary>ドラゴンスレイヤーも同じく入る（効くのは ID122 ドラゴンだけ）。</summary>
    [Test]
    public void ドラゴンスレイヤーの二倍も予想に入る()
    {
        var p = Player(35, 700, 500);
        p.Give(SpecialItems.Kind.DragonSlayer);
        var g = new TowerGame(p);
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                g.Floor.Clear(new Vector2Int(x, y));
        g.Floor.Put(new Vector2Int(0, 1), "Boss_Dragon");
        var rows = HeroOrb.Rows(g.Floor, g.Player);
        Assert.AreEqual(1, rows.Count);
        var e = TowerBattle.LoadEnemy("Boss_Dragon");
        var kind = BattleRules.KindFromTag(rows[0].Prefab);
        var doubled = BattleRules.Predict(700, 500, e.MaxHP, e.Attack, e.Defense, e.Coin,
                                          kind, BattleRules.Charms.DragonSlayer);
        Assert.AreEqual(doubled.DamageTaken, rows[0].Damage);
        Assert.AreEqual(doubled.PlayerHits, rows[0].Turns);
    }

    /// <summary>持っていない相手には掛からない（原典 0x6101 が名指ししている3体だけ）。</summary>
    [Test]
    public void 十字架が効かない相手には掛からない()
    {
        var p = Player(1, 300, 100);
        p.Give(SpecialItems.Kind.Cross);
        var g = new TowerGame(p);
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                g.Floor.Clear(new Vector2Int(x, y));
        g.Floor.Put(new Vector2Int(0, 1), "Enemy_SlimeG");
        var rows = HeroOrb.Rows(g.Floor, g.Player);
        var e = TowerBattle.LoadEnemy("Enemy_SlimeG");
        var plain = BattleRules.Predict(300, 100, e.MaxHP, e.Attack, e.Defense, e.Coin,
                                        BattleRules.KindFromTag("Enemy_SlimeG"),
                                        BattleRules.Charms.None);
        Assert.AreEqual(plain.DamageTaken, rows[0].Damage);
    }

    [Test]
    public void 持ち物の帯から触ると窓に出る()
    {
        var g = new TowerGame(Player(1, 100, 100));
        g.Player.Give(SpecialItems.Kind.HeroOrb);
        Assert.IsTrue(g.TapItem(SpecialItems.Kind.HeroOrb));
        Assert.IsTrue(g.IsShowingPage, "中身が窓に出る");
    }
}
