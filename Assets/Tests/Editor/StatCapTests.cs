using NUnit.Framework;
using UnityEngine;

/// <summary>能力値の上限が原典（MTE.EXE 0xb96c の状態表示）どおりかを確かめる。</summary>
public class StatCapTests
{
    [Test]
    public void 上限の数値が原典の即値と一致する()
    {
        Assert.AreEqual(0x2706, PlayerState.MaxHp, "生命力 9990");
        Assert.AreEqual(0x03de, PlayerState.MaxAttack, "攻撃力 990");
        Assert.AreEqual(0x03de, PlayerState.MaxDefense, "防御力 990");
        Assert.AreEqual(0x2706, PlayerState.MaxGold, "所持金 9990");
    }

    [Test]
    public void 上限を超えたら押さえられる()
    {
        var p = new PlayerState { Hp = 20000, Attack = 5000, Defense = 1500, Gold = 99999 };
        p.ClampStats();
        Assert.AreEqual(9990, p.Hp);
        Assert.AreEqual(990, p.Attack);
        Assert.AreEqual(990, p.Defense);
        Assert.AreEqual(9990, p.Gold);
    }

    [Test]
    public void 上限以下なら触らない()
    {
        var p = new PlayerState { Hp = 1, Attack = 989, Defense = 990, Gold = 0 };
        p.ClampStats();
        Assert.AreEqual(1, p.Hp);
        Assert.AreEqual(989, p.Attack);
        Assert.AreEqual(990, p.Defense);
        Assert.AreEqual(0, p.Gold);
    }

    [Test]
    public void 祭壇で上限を超えない()
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 989, Defense = 989, Gold = 9999, Floor = 50
        };
        TowerNpc.Donate(p, TowerNpc.Wish.Attack);
        Assert.LessOrEqual(p.Attack, PlayerState.MaxAttack, "50階の祭壇は攻撃+2×ブロック");
    }

    [Test]
    public void 仙人の三パーセントでも上限を超えない()
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 985, Defense = 985, Floor = 2, Position = new Vector2Int(0, 0)
        };
        TowerNpc.TalkToMaster(p);
        Assert.AreEqual(990, p.Attack, "985 + 29 = 1014 → 990");
        Assert.AreEqual(990, p.Defense);
    }

    [Test]
    public void 一手進めるたびに押さえられる()
    {
        var p = new PlayerState
        {
            Hp = 9990, Attack = 999, Defense = 999, Gold = 20000,
            Floor = 1, Position = new Vector2Int(0, -5)
        };
        var g = new TowerGame(p);
        Assert.AreEqual(990, g.Player.Attack, "組み立て時にも押さえる");
        Assert.AreEqual(9990, g.Player.Gold);
    }
}
