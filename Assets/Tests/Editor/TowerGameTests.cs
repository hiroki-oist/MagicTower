using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 一手の流れ（移動 → 取得/戦闘 → 魔法攻撃 → 解錠判定）が通しで動くかを確かめる。
/// </summary>
public class TowerGameTests
{
    static readonly Vector2Int Up    = new Vector2Int(0, 1);
    static readonly Vector2Int Down  = new Vector2Int(0, -1);
    static readonly Vector2Int Right = new Vector2Int(1, 0);

    TowerGame NewGameOn(int floor, Vector2Int at)
    {
        var p = new PlayerState { Hp = 1000, Attack = 20, Defense = 10, Floor = floor, Position = at };
        return new TowerGame(p);
    }

    [Test]
    public void 一階から始められる()
    {
        var g = new TowerGame(PlayerState.NewGame());
        Assert.AreEqual(1, g.Player.Floor, "序章は1階から");
        Assert.AreEqual(100, g.Player.Attack, "序章は攻撃力100");
        Assert.AreEqual(5, g.Player.SwordRank, "神剣を装備している");
        Assert.IsNotNull(g.Floor);
    }

    [Test]
    public void 物を拾うと盤面から消えてメッセージが出る()
    {
        var g = NewGameOn(1, new Vector2Int(0, 0));
        g.Floor.Clear(new Vector2Int(0, 0));
        g.Floor.Put(new Vector2Int(0, 1), "Key_Yellow");

        Assert.AreEqual(TowerPlayer.Outcome.TookItem, g.Step(Up));
        Assert.AreEqual(1, g.Player.YellowKeys);
        Assert.AreEqual(FloorState.Floor, g.Floor.At(new Vector2Int(0, 1)), "拾ったマスは床になる");
        Assert.AreEqual("鍵を手に入れた！", g.Log[g.Log.Count - 1]);
    }

    [Test]
    public void 敵を倒すとマスが床になって踏み込んだ先に立つ()
    {
        var g = NewGameOn(1, new Vector2Int(0, 0));
        g.Floor.Clear(new Vector2Int(0, 0));
        g.Floor.Put(new Vector2Int(0, 1), "Enemy_SlimeR");

        Assert.AreEqual(TowerPlayer.Outcome.Encounter, g.Step(Up));
        Assert.AreEqual(FloorState.Floor, g.Floor.At(new Vector2Int(0, 1)));
        Assert.AreEqual(new Vector2Int(0, 1), g.Player.Position,
                        "戦闘処理の踏み込み（原典 0x61ef）で相手のマスに立つ");
        Assert.AreEqual(980, g.Player.Hp, "被ダメージ20");
    }

    [Test]
    public void 守護者を2体倒すと門が開く()
    {
        // F17 の左の門(-4,+2) は (-5,+1) と (-3,+1) を倒すと開く
        var g = NewGameOn(17, new Vector2Int(-4, 1));
        g.Player.Attack = 500; g.Player.Defense = 500; g.Player.Hp = 9000;
        var gate = new Vector2Int(-4, 2);
        Assert.AreEqual("s", g.Floor.At(gate));

        g.Player.Position = new Vector2Int(-4, 1);
        g.Step(new Vector2Int(-1, 0));           // (-5,+1) の守護者
        Assert.AreEqual("s", g.Floor.At(gate), "片方だけでは開かない");

        g.Player.Position = new Vector2Int(-4, 1);
        g.Step(Right);                            // (-3,+1) の守護者
        Assert.AreEqual(FloorState.Floor, g.Floor.At(gate), "両方倒すと門が消える");
    }

    [Test]
    public void 階段を踏むと階が変わる()
    {
        var g = NewGameOn(2, new Vector2Int(0, 0));
        var up = FloorData.Load(2);
        Vector2Int stair = default(Vector2Int);
        foreach (var p in up.Find(FloorData.Terrain.StairsUp)) { stair = p; break; }

        g.Player.Position = stair + Down;
        g.Floor.Clear(g.Player.Position);
        g.Step(Up);
        Assert.AreEqual(3, g.Player.Floor, "2階の上り階段で3階へ");
    }

    [Test]
    public void 翼なら幻の階に入れる()
    {
        var g = NewGameOn(43, new Vector2Int(0, 0));
        g.Player.Give(SpecialItems.Kind.WingUp);
        Assert.IsTrue(g.UseWing(true));
        Assert.AreEqual(44, g.Player.Floor, "階段と違い44階を飛ばさない");
        Assert.IsFalse(g.Player.Has(SpecialItems.Kind.WingUp), "翼は使い切り");
    }

    [Test]
    public void 魔導師に挟まれると生命力が半分になる()
    {
        var g = NewGameOn(1, new Vector2Int(0, 0));
        foreach (var d in new[] { new Vector2Int(0,0), new Vector2Int(0,1) })
            g.Floor.Clear(d);
        g.Floor.Put(new Vector2Int(-1, 1), "Enemy_ID93");
        g.Floor.Put(new Vector2Int(1, 1),  "Enemy_ID93");

        g.Player.Hp = 1000; g.Player.ShieldRank = 0;
        g.Step(Up);
        Assert.AreEqual(500, g.Player.Hp);

        g.Player.Position = new Vector2Int(0, 0);
        g.Player.Hp = 1000; g.Player.ShieldRank = 5;
        g.Step(Up);
        Assert.AreEqual(1000, g.Player.Hp, "神盾なら無効");
    }
}
