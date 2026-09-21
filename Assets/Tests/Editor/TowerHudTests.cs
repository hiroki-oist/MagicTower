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
        // 2桁では全角空白が数字に置き換わる（原典は「塔　　階」の真ん中2マスに右詰め）
        Assert.AreEqual("塔５０階", TowerHud.FloorLabel(50));
        // 数値は全角にして右詰め（原典 0xb8d6 は x + 2*(枠の数 - 桁数) から書く）
        Assert.AreEqual("　　４", GameText.NumRight(4, 3));
        Assert.AreEqual("５０", GameText.NumRight(50, 2));
        Assert.AreEqual("１２３", GameText.NumRight(123, 2), "枠より長ければそのまま");
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
        // 原典は名前の側に中央寄せの空白を持っている（欄は全角6文字＝96ドット）
        Assert.AreEqual(" Ｇスライム", i.Name);
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

/// <summary>
/// 高速モード（原典の旗31 / `es:[0x1f]`）。
/// </summary>
public class FastModeTests
{
    [Test]
    public void 旗の番号は原典と同じ三十一()
    {
        Assert.AreEqual(31, PlayerState.FastModeFlag);
    }

    [Test]
    public void 切り替えると旗が反転して知らせが出る()
    {
        var g = new TowerGame(new PlayerState { Hp = 1000, Floor = 1 });
        Assert.IsFalse(g.Player.FastMode, "はじめは標準モード");

        g.ToggleFastMode();
        Assert.IsTrue(g.Player.FastMode);
        Assert.AreEqual(GameText.Instance.Message("mode_fast"), g.Log[g.Log.Count - 1]);

        g.ToggleFastMode();
        Assert.IsFalse(g.Player.FastMode);
        Assert.AreEqual(GameText.Instance.Message("mode_normal"), g.Log[g.Log.Count - 1]);
    }

    [Test]
    public void 知らせは窓を開かない()
    {
        // 原典は右下の欄に書くだけで会話の窓は出さない
        var g = new TowerGame(new PlayerState { Hp = 1000, Floor = 1 });
        g.ToggleFastMode();
        Assert.IsFalse(g.IsShowingPage, "窓が開いてしまっている");
    }

    [Test]
    public void 旗はセーブに残る()
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mt_fast_test");
        if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        TowerSave.Directory = dir;
        try
        {
            var p = new PlayerState { Hp = 100, Floor = 1 };
            p.FastMode = true;
            TowerSave.Save(1, p);
            var q = TowerSave.Load(1);
            Assert.IsNotNull(q);
            Assert.IsTrue(q.FastMode, "高速モードが残っていない");
        }
        finally
        {
            TowerSave.Directory = null;
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        }
    }
}
