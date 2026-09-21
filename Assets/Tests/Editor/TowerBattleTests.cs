using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 戦闘の決着が原典どおりかを確かめる。
/// 数値は Resources/Battlers/*.asset（原典 MTE.EXE のモンスター表そのまま）を読む。
/// </summary>
public class TowerBattleTests
{
    PlayerState player;

    [SetUp]
    public void SetUp()
    {
        player = new PlayerState { Hp = 1000, Attack = 20, Defense = 10, Floor = 1 };
    }

    [Test]
    public void 原典の数値が読める()
    {
        var g = TowerBattle.LoadEnemy("Enemy_SlimeG");
        Assert.IsNotNull(g, "Ｇスライムが読めない");
        Assert.AreEqual(35, g.MaxHP);
        Assert.AreEqual(18, g.Attack);
        Assert.AreEqual(1,  g.Defense);
    }

    [Test]
    public void Rスライムとの戦闘が原典と一致する()
    {
        // Ｒスライム HP45 攻20 防2 / プレーヤー 攻20 防10
        var r = TowerBattle.Resolve(player, "Enemy_SlimeR");
        Assert.IsTrue(r.Won);
        Assert.AreEqual(3, r.Hits, "1撃18ダメージなので3撃");
        Assert.AreEqual(20, r.DamageTaken, "反撃は2回（最後の一撃には反撃が無い）");
        Assert.AreEqual(980, player.Hp);
        Assert.AreEqual(2, player.Gold);
    }

    [Test]
    public void 防御力が高すぎると攻撃できない()
    {
        player.Attack = 10;
        var r = TowerBattle.Resolve(player, "Enemy_Rock");   // ロック 防68
        Assert.IsTrue(r.Fought);
        Assert.IsFalse(r.Won);
        Assert.AreEqual("cannot_attack", r.MessageKey);
        Assert.AreEqual(1000, player.Hp, "生命力は減らない");
        Assert.AreEqual("相手の防御力が高すぎて攻撃できない。",
                        GameText.Instance.Message(r.MessageKey));
    }

    [Test]
    public void 十字架はゾンビ系に効く()
    {
        player.Attack = 200; player.Defense = 200; player.Hp = 9000;
        var before = TowerBattle.Resolve(player, "Enemy_Zombie");   // ゾンビ HP260 防5

        var withCross = new PlayerState { Hp = 9000, Attack = 200, Defense = 200, Floor = 1 };
        withCross.Give(SpecialItems.Kind.Cross);
        var after = TowerBattle.Resolve(withCross, "Enemy_Zombie");

        Assert.Less(after.Hits, before.Hits, "十字架があると少ない撃数で倒せる");
    }

    [Test]
    public void ドラゴンスレイヤーはドラゴンにだけ効く()
    {
        var a = new PlayerState { Hp = 99999, Attack = 700, Defense = 700, Floor = 35 };
        var b = new PlayerState { Hp = 99999, Attack = 700, Defense = 700, Floor = 35 };
        b.Give(SpecialItems.Kind.DragonSlayer);

        var ra = TowerBattle.Resolve(a, "Enemy_Dragon");
        var rb = TowerBattle.Resolve(b, "Enemy_Dragon");
        Assert.Less(rb.Hits, ra.Hits, "ドラゴンには効く");

        var c = new PlayerState { Hp = 9000, Attack = 200, Defense = 200, Floor = 1 };
        var d = new PlayerState { Hp = 9000, Attack = 200, Defense = 200, Floor = 1 };
        d.Give(SpecialItems.Kind.DragonSlayer);
        Assert.AreEqual(TowerBattle.Resolve(c, "Enemy_Zombie").Hits,
                        TowerBattle.Resolve(d, "Enemy_Zombie").Hits,
                        "ゾンビには効かない");
    }

    [Test]
    public void 幸運の金貨で金が2倍になる()
    {
        var a = new PlayerState { Hp = 1000, Attack = 20, Defense = 10, Floor = 1 };
        var b = new PlayerState { Hp = 1000, Attack = 20, Defense = 10, Floor = 1 };
        b.Give(SpecialItems.Kind.LuckyCoin);

        var ra = TowerBattle.Resolve(a, "Enemy_SlimeR");
        var rb = TowerBattle.Resolve(b, "Enemy_SlimeR");
        Assert.AreEqual(ra.GoldGained * 2, rb.GoldGained);
    }

    // ---- 演出（原典 0x6082 の後半）----

    [Test]
    public void 演出の待ち時間と音が原典の値と一致する()
    {
        Assert.AreEqual(10, BattlePresentation.StepWait, "原典 0xa");
        Assert.AreEqual(5, BattlePresentation.HitWait, "原典 0x5");
        Assert.AreEqual(9, BattlePresentation.SlashSound, "原典 0x10da:0x20b に渡す 9");
    }

    [Test]
    public void 踏み込む先は敵のマス()
    {
        var from = new Vector2Int(0, 0);
        Assert.AreEqual(new Vector2Int(0, 1),
                        BattlePresentation.StepInto(from, Vector2Int.up));
        Assert.AreEqual(new Vector2Int(-1, 0),
                        BattlePresentation.StepInto(from, Vector2Int.left));
    }

    [Test]
    public void 演出の位置は盤面と同じ式で出る()
    {
        Assert.AreEqual(ScreenLayout.FromCell(new Vector2Int(0, 0)),
                        BattlePresentation.ScreenOf(new Vector2Int(0, 0)));
        Assert.AreEqual(new Vector2Int(304, 184),
                        BattlePresentation.ScreenOf(new Vector2Int(0, 0)), "中央のマス");
    }

    [Test]
    public void ダメージの欄は戦闘用で鍵の欄ではない()
    {
        Assert.AreEqual(new RectInt(568, 296, 48, 16), BattlePresentation.DamageBox);
        Assert.AreEqual(new Vector2Int(528, 144), ScreenLayout.KeyIconOrigin,
                        "鍵は絵を並べて出す");
        Assert.AreEqual(6, ScreenLayout.KeyIconsPerRow, "1行に6本");
        Assert.AreEqual(24, ScreenLayout.KeyIconMax, "通しで24本まで");
    }
}
