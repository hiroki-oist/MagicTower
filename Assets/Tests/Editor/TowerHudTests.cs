using NUnit.Framework;

/// <summary>
/// 画面に出す文字が原典どおりかを確かめる。
/// 数字は全角、装備名は原典が持っている6文字幅の短縮名。
/// </summary>
public class TowerHudTests
{
    PlayerState player;

    [SetUp]
    public void SetUp()
    {
        player = new PlayerState
        {
            Hp = 652, Attack = 21, Defense = 10, Gold = 26,
            Floor = 4, SwordRank = 1, ShieldRank = 0,
        };
    }

    [Test]
    public void 数値は全角で出る()
    {
        // 動画の Part1 t=120（塔4階）の表示と同じ並び
        Assert.AreEqual("６５２", TowerHud.Hp(player));
        Assert.AreEqual("２１",   TowerHud.Attack(player));
        Assert.AreEqual("１０",   TowerHud.Defense(player));
        Assert.AreEqual("２６",   TowerHud.Gold(player));
    }

    [Test]
    public void 階の表示が原典の形になる()
    {
        Assert.AreEqual("塔　４階", TowerHud.FloorLabel(4));
        Assert.AreEqual("塔　５０階", TowerHud.FloorLabel(50));
    }

    [Test]
    public void 装備欄は原典の短縮名を使う()
    {
        Assert.AreEqual("　長剣", TowerHud.WeaponName(player), "動画の4階と同じ「長剣」");
        Assert.AreEqual("", TowerHud.ShieldName(player), "盾は持っていない");

        player.ShieldRank = 5;
        Assert.AreEqual("　神盾", TowerHud.ShieldName(player));
        player.SwordRank = 3;
        Assert.AreEqual("騎士の剣", TowerHud.WeaponName(player));
    }

    [Test]
    public void モンスター情報が原典の数値になる()
    {
        var i = TowerHud.Inspect(player, "Enemy_SlimeG");
        Assert.IsTrue(i.Valid);
        Assert.AreEqual("Ｇスライム", i.Name);
        Assert.AreEqual("３５", i.Hp);
        Assert.AreEqual("１８", i.Attack);
        Assert.AreEqual("１", i.Defense);
        Assert.IsNull(i.ExpectedDamage, "勇者のオーブが無ければ予想ダメージは出ない");
    }

    [Test]
    public void 勇者のオーブがあると予想ダメージが出る()
    {
        player.Give(SpecialItems.Kind.HeroOrb);
        var i = TowerHud.Inspect(player, "Enemy_SlimeG");
        Assert.IsNotNull(i.ExpectedDamage);

        // 倒せない相手なら「攻撃不可能」
        player.Attack = 1;
        var rock = TowerHud.Inspect(player, "Enemy_Rock");
        StringAssert.Contains("攻撃不可能", rock.ExpectedDamage);
    }

    [Test]
    public void 知らない相手は出せない()
    {
        Assert.IsFalse(TowerHud.Inspect(player, "Wall_Field").Valid);
    }
}
