using NUnit.Framework;

/// <summary>
/// 物を拾ったときの効果が原典どおりかを確かめる。
/// 効果量は表ではなく式（block = (階-1)/10 + 1）で決まる。
/// </summary>
public class ItemPickupTests
{
    ItemPowerTable power;
    PlayerState player;

    [SetUp]
    public void SetUp()
    {
        power = ItemPowerTable.Instance;
        player = new PlayerState { Floor = 1, Hp = 1000, Attack = 10, Defense = 10 };
    }

    [Test]
    public void 鍵は色ごとに増える()
    {
        ItemPickup.Apply(player, "Key_Yellow", power);
        ItemPickup.Apply(player, "Key_Blue", power);
        ItemPickup.Apply(player, "Key_Red", power);
        Assert.AreEqual(1, player.YellowKeys);
        Assert.AreEqual(1, player.PurpleKeys, "Key_Blue は原典の紫の鍵");
        Assert.AreEqual(1, player.RedKeys);
    }

    [Test]
    public void 回復薬の量は階で変わる()
    {
        player.Floor = 1;  player.Hp = 100;
        var r = ItemPickup.Apply(player, "Potion_Red", power);
        Assert.AreEqual(50, r.Value, "1〜10階は block=1 で 50");
        Assert.AreEqual(150, player.Hp);

        player.Floor = 45; player.Hp = 100;
        r = ItemPickup.Apply(player, "Potion_Blue", power);
        Assert.AreEqual(1000, r.Value, "41〜50階は block=5 で 200×5");
    }

    [Test]
    public void 生命力は9990で頭打ち()
    {
        player.Floor = 45; player.Hp = 9900;
        ItemPickup.Apply(player, "Potion_Blue", power);
        Assert.AreEqual(PlayerState.MaxHp, player.Hp, "原典マニュアル「上限は９９９０ポイント」");
    }

    [Test]
    public void クリスタルは階の段階だけ上がる()
    {
        player.Floor = 33;
        ItemPickup.Apply(player, "Gem_Attack", power);
        ItemPickup.Apply(player, "Gem_Defense", power);
        Assert.AreEqual(14, player.Attack,  "31〜40階は block=4");
        Assert.AreEqual(14, player.Defense);
    }

    [Test]
    public void 剣と盾は段階ごとの値だけ上がる()
    {
        ItemPickup.Apply(player, "Sword_1", power);
        Assert.AreEqual(20, player.Attack, "長剣は+10");
        Assert.AreEqual(1, player.SwordRank);

        ItemPickup.Apply(player, "Sword_5", power);
        Assert.AreEqual(120, player.Attack, "神剣は+100");
        Assert.AreEqual(5, player.SwordRank);

        ItemPickup.Apply(player, "Shield_5", power);
        Assert.AreEqual(110, player.Defense, "神盾は+100");
        Assert.AreEqual(5, player.ShieldRank, "神盾を装備すると魔法攻撃を防げる");
    }

    [Test]
    public void 特殊アイテムは所持フラグが立つ()
    {
        var r = ItemPickup.Apply(player, "Orb_Flight", power);
        Assert.IsTrue(r.Taken);
        Assert.AreEqual("orb_flight", r.MessageKey);
        Assert.IsTrue(player.Has(SpecialItems.Kind.FlightOrb));

        ItemPickup.Apply(player, "Ball_Destruction", power);
        Assert.IsTrue(player.Has(SpecialItems.Kind.DestructionOrb), "ID42 は破壊の玉");

        ItemPickup.Apply(player, "Coin_Lucky", power);
        Assert.IsTrue(player.Has(SpecialItems.Kind.LuckyCoin));
    }

    [Test]
    public void 知らない名前は拾わない()
    {
        var r = ItemPickup.Apply(player, "Wall_Field", power);
        Assert.IsFalse(r.Taken);
    }

    [Test]
    public void メッセージが原典の文言になる()
    {
        player.Floor = 1;
        var r = ItemPickup.Apply(player, "Potion_Red", power);
        Assert.AreEqual("回復薬を手に入れた！　生命力　５０ポイントＵＰ！",
                        GameText.Instance.Message(r.MessageKey, r.Value));
    }
}
