using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 課金の窓は**行を直接触って選ぶ**（作者の指定。審査 2.1(b) の指摘
/// 「1000と4000は灰色で押せなかった」）。
/// 1回目の触りでその行を選び（ほかは灰色）、選んでいる行をもう一度触ったら決める。
/// </summary>
public class GoldShopTapTests
{
    static TowerGame OpenShop()
    {
        PlayerPrefs.DeleteKey("GoldShop_400");
        PlayerPrefs.DeleteKey("GoldShop_1000");
        PlayerPrefs.DeleteKey("GoldShop_4000");
        var g = new TowerGame(new PlayerState { Hp = 100, Floor = 1 });
        Assert.IsTrue(g.AskGoldShop());
        return g;
    }

    [TearDown]
    public void Clean()
    {
        PlayerPrefs.DeleteKey("GoldShop_400");
        PlayerPrefs.DeleteKey("GoldShop_1000");
        PlayerPrefs.DeleteKey("GoldShop_4000");
    }

    [Test]
    public void 灰色の行を触るとその行が選ばれるだけで決まらない()
    {
        var g = OpenShop();
        Assert.AreEqual(0, g.ChoiceIndex, "開いたときは一番上");
        Assert.IsFalse(g.TapChoice(2), "1回目は決めない");
        Assert.AreEqual(2, g.ChoiceIndex, "4000の行が選ばれる");
        Assert.IsTrue(g.IsChoosing, "窓は出たまま");
        // 灰色に沈むのは選んでいない行（4000の行は沈まない）
        var dimmed = DialogueWindowView.Dim(GoldShop.Text(), g.ChoiceIndex);
        foreach (var line in dimmed.Split('\n'))
            if (line.Contains("４０００ゴールド"))
                Assert.IsFalse(line.StartsWith(DialogueWindowView.DimOpen), "選んだ行は白い");
    }

    [Test]
    public void 選んでいる行をもう一度触ると決まる()
    {
        var g = OpenShop();
        Assert.IsFalse(g.TapChoice(1));
        Assert.IsTrue(g.TapChoice(1), "2回目で決める");
        int gold = g.Player.Gold;
        g.ConfirmChoice();
        Assert.IsFalse(g.IsChoosing);
        // エディタでは見本の道（端末の覚え書き）でその場で受け取る
        Assert.AreEqual(gold + 1000, g.Player.Gold, "1000ゴールドが入る（400ではない）");
    }

    [Test]
    public void 別の行に触り直すと選び直すだけ()
    {
        var g = OpenShop();
        Assert.IsFalse(g.TapChoice(2));
        Assert.IsFalse(g.TapChoice(1), "違う行なら決めない");
        Assert.AreEqual(1, g.ChoiceIndex);
    }

    [Test]
    public void 一番上の行は最初から選ばれているので1回で決まる()
    {
        var g = OpenShop();
        Assert.IsTrue(g.TapChoice(0));
    }

    [Test]
    public void 行の外の番号は何もしない()
    {
        var g = OpenShop();
        Assert.IsFalse(g.TapChoice(-1));
        Assert.IsFalse(g.TapChoice(g.ChoiceCount));
        Assert.AreEqual(0, g.ChoiceIndex);
    }
}
