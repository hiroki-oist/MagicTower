using NUnit.Framework;
using UnityEngine;

/// <summary>
/// ★**引換券は1回の冒険につき1枚**（作者の指定。録画中に見つかった不具合)。
///
/// 課金の品は非消耗型なので、一度買えば App Store の権利は残り続ける。
/// そのままだと商店で何度でも押せてゴールドが無限に増えてしまっていた
/// （作者の報告「一回買うと、無限に増やせてしまう」）。
/// 二度目は何も渡さずに「交換済みの引換券です。」と知らせ、
/// 最初から遊び直せばまた1枚引き換えられる。
/// </summary>
public class GoldTicketTests
{
    PlayerState player;

    [SetUp]
    public void SetUp()
    {
        player = new PlayerState { Gold = 0, Floor = 1 };
    }

    [Test]
    public void 一度目は受け取れる()
    {
        bool got;
        var line = GoldShop.Claim(player, 0, out got);
        Assert.IsTrue(got);
        Assert.AreEqual(GoldShop.GoldOf(0), player.Gold);
        StringAssert.Contains("手に入れた", line);
    }

    [Test]
    public void 二度目からは交換済みと出て増えない()
    {
        bool got;
        GoldShop.Claim(player, 0, out got);
        int after = player.Gold;

        for (int n = 0; n < 5; n++)
        {
            var line = GoldShop.Claim(player, 0, out got);
            Assert.IsFalse(got, "何も渡さない");
            Assert.AreEqual(PhoneScreen.ShopUsed, line);
            Assert.AreEqual(after, player.Gold, "ゴールドは増えない");
        }
        Assert.IsTrue(GoldShop.Used(player, 0));
    }

    [Test]
    public void 券ごとに別で数える()
    {
        bool got;
        GoldShop.Claim(player, 0, out got);
        Assert.IsTrue(GoldShop.Used(player, 0));
        Assert.IsFalse(GoldShop.Used(player, 1), "別の券はまだ使っていない");
        GoldShop.Claim(player, 1, out got);
        Assert.IsTrue(got);
        Assert.AreEqual(GoldShop.GoldOf(0) + GoldShop.GoldOf(1), player.Gold);
    }

    [Test]
    public void 新しい冒険ならまた一枚引き換えられる()
    {
        bool got;
        GoldShop.Claim(player, 0, out got);
        Assert.IsTrue(GoldShop.Used(player, 0));

        // 「最初から遊ぶ」は新しい遊び手を作る
        var next = PlayerState.NewGame();
        Assert.IsFalse(GoldShop.Used(next, 0), "新しい冒険では使っていない");
        int before = next.Gold;
        GoldShop.Claim(next, 0, out got);
        Assert.IsTrue(got);
        Assert.AreEqual(before + GoldShop.GoldOf(0), next.Gold);
    }

    [Test]
    public void 使った券はセーブに残る()
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                                         "mt_ticket_" + System.Guid.NewGuid());
        TowerSave.Directory = dir;
        try
        {
            bool got;
            player.Hp = 1000; player.Floor = 5;
            GoldShop.Claim(player, 2, out got);
            TowerSave.Save(2, player);

            var back = TowerSave.Load(2);
            Assert.IsTrue(GoldShop.Used(back, 2), "読み直しても交換済みのまま");
            Assert.IsFalse(GoldShop.Used(back, 0));
        }
        finally
        {
            TowerSave.Directory = null;
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        }
    }

    [Test]
    public void 上限を超えても交換済みの印は立つ()
    {
        // 9990 で頭打ちになっても「受け取った」ことに変わりはない
        bool got;
        player.Gold = PlayerState.MaxGold;
        GoldShop.Claim(player, 0, out got);
        Assert.IsTrue(got);
        Assert.AreEqual(PlayerState.MaxGold, player.Gold);
        Assert.IsTrue(GoldShop.Used(player, 0));
    }
}
