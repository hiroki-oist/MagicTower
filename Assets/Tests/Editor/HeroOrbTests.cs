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
    /// 原典の説明（レコード60）が「特殊なアイテムを持っていると実際と異なる場合がある」と
    /// 断っているとおり、**十字架などの補正は入らない**。
    /// </summary>
    [Test]
    public void 十字架を持っていても予想は素の攻撃力で出す()
    {
        var p = Player(1, 300, 100);
        p.Give(SpecialItems.Kind.Cross);
        var g = new TowerGame(p);
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                g.Floor.Clear(new Vector2Int(x, y));
        g.Floor.Put(new Vector2Int(0, 1), "Enemy_Zombie");    // 十字架が効く相手
        var rows = HeroOrb.Rows(g.Floor, g.Player);
        var e = TowerBattle.LoadEnemy("Enemy_Zombie");
        var plain = BattleRules.Predict(300, 100, e.MaxHP, e.Attack, e.Defense, e.Coin,
                                        BattleRules.KindFromTag("Enemy_Zombie"),
                                        BattleRules.Charms.None);
        Assert.AreEqual(plain.DamageTaken, rows[0].Damage, "補正抜きの数字が出る");
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
