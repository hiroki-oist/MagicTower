using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 39階の「時計盤」（原典 MTE.EXE 0x2433〜0x24ff）。
/// 黄色い扉が 3x3 で並んでいて、**12時と3時の2枚だけを開ける**と
/// 中央に空間転移の秘宝(ID43) が現れる。
///   原典ヒント91（39階の仙人）「３時になれば空間移動の秘宝が現れるじゃろう。」
///   未使用レコード228「３９階は２ヶ所の扉を開けることでスペシャルアイテムが
///   手に入るぞ。　９個の扉を時計盤にみたてるのだ。」
/// </summary>
public class ClockDoorsTests
{
    static readonly Vector2Int[] Face =
    {
        new Vector2Int(-4, 4), new Vector2Int(-2, 4), new Vector2Int(0, 4),
        new Vector2Int(-4, 2), new Vector2Int(-2, 2), new Vector2Int(0, 2),
        new Vector2Int(-4, 0), new Vector2Int(-2, 0), new Vector2Int(0, 0),
    };
    static readonly Vector2Int Twelve = new Vector2Int(-2, 4);   // 12時
    static readonly Vector2Int Three  = new Vector2Int(0, 2);    // 3時
    static readonly Vector2Int Center = new Vector2Int(-2, 2);   // 針の交わる所

    static FloorState Floor39()
    {
        var st = new FloorState(FloorData.Load(39));
        foreach (var p in Face) Assert.AreEqual("y", st.At(p), $"{p} は黄色の扉");
        return st;
    }

    [Test]
    public void 文字盤は黄色い扉九枚()
    {
        Floor39();   // 中で9マスとも確かめている
    }

    [Test]
    public void 十二時と三時だけ開けると中央に秘宝が出る()
    {
        var st = Floor39();
        st.Clear(Twelve);
        Assert.AreEqual(0, st.ApplyUnlocks().Count, "12時だけでは出ない");

        st.Clear(Three);
        var fired = st.ApplyUnlocks();
        Assert.AreEqual(1, fired.Count, "2枚そろうと出る");
        Assert.AreEqual("Relic_Teleport", st.At(Center), "中央に空間転移の秘宝");
    }

    [Test]
    public void 余計な扉を開けていると出ない()
    {
        var st = Floor39();
        st.Clear(Twelve);
        st.Clear(Three);
        st.Clear(new Vector2Int(-4, 0));          // 8時のあたりも開けてしまった
        Assert.AreEqual(0, st.ApplyUnlocks().Count, "残り7枚は閉じたままでないといけない");
    }

    [Test]
    public void 中央を開けてしまうと出ない()
    {
        var st = Floor39();
        st.Clear(Twelve);
        st.Clear(Three);
        st.Clear(Center);
        Assert.AreEqual(0, st.ApplyUnlocks().Count, "中央の扉も閉じたまま");
    }

    [Test]
    public void 二度は出ない()
    {
        var st = Floor39();
        st.Clear(Twelve);
        st.Clear(Three);
        Assert.AreEqual(1, st.ApplyUnlocks().Count);
        Assert.AreEqual(0, st.ApplyUnlocks().Count, "一度きり");
    }

    /// <summary>拾うと持ち物に入る（原典 0x2e19 が所持フラグを立てる）。</summary>
    [Test]
    public void 拾うと空間転移の秘宝が手に入る()
    {
        Assert.AreEqual(SpecialItems.Kind.TeleportRelic,
                        SpecialItems.FromPrefabName("Relic_Teleport"));
        Assert.AreEqual(43, ObjectIds.Instance.Of("Relic_Teleport"));
    }
}

/// <summary>
/// 空間転移の秘宝（原典 MTE.EXE 0x86f7〜0x8758）。
/// その階の中心について**点対称**のマスへ飛ぶ。飛び先が床でなければ断る。
/// 生命力800は成功したときだけ払い、**使っても無くならない**。
/// </summary>
public class TeleportRelicTests
{
    static TowerGame Game(int floor, Vector2Int at)
    {
        var p = new PlayerState
        {
            Hp = 5000, Attack = 100, Defense = 100, Floor = floor, Position = at
        };
        p.Give(SpecialItems.Kind.TeleportRelic);
        return new TowerGame(p);
    }

    [Test]
    public void 飛び先は中心について点対称()
    {
        Assert.AreEqual(new Vector2Int(0, 0),
                        SpecialItemEffects.TeleportTarget(new Vector2Int(0, 0)));
        Assert.AreEqual(new Vector2Int(-3, 2),
                        SpecialItemEffects.TeleportTarget(new Vector2Int(3, -2)));
        Assert.AreEqual(new Vector2Int(5, 5),
                        SpecialItemEffects.TeleportTarget(new Vector2Int(-5, -5)));
        Assert.AreEqual(800, SpecialItemEffects.TeleportHpCost, "原典 0x320");
    }

    [Test]
    public void 床なら飛んで生命力を八百払う()
    {
        var g = Game(1, new Vector2Int(2, -2));
        g.Floor.Clear(new Vector2Int(2, -2));
        g.Floor.Clear(new Vector2Int(-2, 2));
        int hp = g.Player.Hp;
        Assert.IsTrue(g.UseItem(SpecialItems.Kind.TeleportRelic));
        Assert.AreEqual(new Vector2Int(-2, 2), g.Player.Position);
        Assert.AreEqual(hp - 800, g.Player.Hp);
        Assert.IsTrue(g.Player.Has(SpecialItems.Kind.TeleportRelic), "使っても無くならない");
    }

    [Test]
    public void 飛び先が床でなければ断って生命力も減らない()
    {
        var g = Game(1, new Vector2Int(2, -2));
        g.Floor.Clear(new Vector2Int(2, -2));
        g.Floor.Put(new Vector2Int(-2, 2), "#");
        int hp = g.Player.Hp;
        Assert.IsFalse(g.UseItem(SpecialItems.Kind.TeleportRelic));
        Assert.AreEqual(new Vector2Int(2, -2), g.Player.Position, "動かない");
        Assert.AreEqual(hp, g.Player.Hp, "払わない");
        StringAssert.Contains("障害物があって空間転移できない",
                              string.Join("\n", g.Log));
    }

    /// <summary>
    /// ★**持ち物の帯から使える**こと（作者の報告「タップしても効果音が鳴るだけ」）。
    /// 使う道具かどうかを「使うと無くなるか」で判っていたため、使っても無くならない
    /// 空間転移の秘宝だけが窓を開けられなかった。
    /// </summary>
    [Test]
    public void 帯を触ると使うか聞いてくる()
    {
        Assert.IsTrue(SpecialItemEffects.IsUsable(SpecialItems.Kind.TeleportRelic),
                      "使う道具に入っている");
        Assert.IsFalse(SpecialItemEffects.IsConsumedOnUse(SpecialItems.Kind.TeleportRelic),
                       "ただし使っても無くならない（原典 0x86f7）");

        var p = new PlayerState
        {
            Hp = 9990, Attack = 100, Defense = 100,
            Floor = 1, Position = new Vector2Int(1, 1),
        };
        p.Give(SpecialItems.Kind.TeleportRelic);
        var g = new TowerGame(p);
        g.Floor.Clear(new Vector2Int(-1, -1));          // 飛び先を空けておく
        Assert.IsTrue(g.TapItem(SpecialItems.Kind.TeleportRelic), "窓が開く");
        Assert.IsTrue(g.IsChoosing, "使うか聞いてくる");
        StringAssert.Contains("空間転移の秘宝", g.CurrentPage);

        g.ConfirmChoice();                              // ◎はい
        Assert.AreEqual(new Vector2Int(-1, -1), g.Player.Position, "点対称のマスへ飛ぶ");
        Assert.IsTrue(g.Player.Has(SpecialItems.Kind.TeleportRelic), "使っても残る");
    }

    /// <summary>原典のメニューの並び（special_1〜8）と「使う道具」が一致する。</summary>
    [Test]
    public void 使う道具は原典の一覧と同じ()
    {
        foreach (var k in TowerMenu.Usable)
            Assert.IsTrue(SpecialItemEffects.IsUsable(k), k.ToString());
        foreach (SpecialItems.Kind k in System.Enum.GetValues(typeof(SpecialItems.Kind)))
            if (SpecialItemEffects.IsUsable(k))
                CollectionAssert.Contains(TowerMenu.Usable, k, k.ToString());
    }
}
