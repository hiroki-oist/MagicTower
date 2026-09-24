using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 生命力が尽きたときのふるまい。原典 PC98版 MTE.EXE の `0x097ac`
/// （戦闘 0x64cd / 魔法攻撃 0x001df2・0x001fd0 から呼ばれる）。
/// </summary>
public class GameOverTests
{
    [Test]
    public void 原典の文字と位置()
    {
        Assert.AreEqual("ＧＡＭＥ　ＯＶＥＲ", GameOverScreen.Line);
        // 原典が 0x9e30 に渡す (0x1f, 0xbe)。x はバイト＝8ドット、y はドット。
        Assert.AreEqual(0x1f, GameOverScreen.X);
        Assert.AreEqual(0xbe, GameOverScreen.Y);
        Assert.AreEqual(new Vector2Int(248, 190), GameOverScreen.Dot);
        // 題名の副題と同じ高さに出る（原典もどちらも 0xbe）
        Assert.AreEqual(TitleScreen.CopyrightDot.y, GameOverScreen.Dot.y);
    }

    [Test]
    public void 相手の一撃で尽きるなら死ぬ()
    {
        // 自分 攻20/防10、緑スライム（命35/攻18/防1）
        // 一撃19なので2撃、反撃は1回ぶん 18-10=8
        var p = new PlayerState { Hp = 30, Attack = 20, Defense = 10, Floor = 1 };
        var r = TowerBattle.Resolve(p, "Enemy_SlimeG");
        Assert.IsTrue(r.Won);
        Assert.IsFalse(r.Died);
        Assert.AreEqual(22, p.Hp);

        // 命8 ちょうどなら 8 <= 8 で死ぬ（原典 0x64cd は「生命力 > 一撃」でのみ続く）
        var q = new PlayerState { Hp = 8, Attack = 20, Defense = 10, Floor = 1 };
        var r2 = TowerBattle.Resolve(q, "Enemy_SlimeG");
        Assert.IsTrue(r2.Died);
        Assert.IsFalse(r2.Won, "倒す前に死ぬので勝ちではない");
        Assert.AreEqual(0, q.Hp);
        Assert.AreEqual(0, r2.GoldGained, "金は入らない");
    }

    [Test]
    public void 死ぬと相手は盤面に残る()
    {
        var g = NewGame(hp: 8, attack: 20, defense: 10);
        var at = new Vector2Int(0, 0);
        g.Floor.Clear(new Vector2Int(0, -1));
        g.Floor.Put(at, "Enemy_SlimeG");
        g.Player.Position = new Vector2Int(0, -1);

        g.Step(Vector2Int.up);
        // **まず倒れる様子**（主人公が点滅して消え、暗くなる）を見せてから画面を出す
        Assert.IsTrue(g.ConsumeDeath(), "倒れる様子を見せる番が来る");
        g.Pump(false);
        Assert.IsTrue(g.IsGameOver, "そのあとにゲームオーバーの画面が出る");
        Assert.AreEqual(0, g.Player.Hp);
        Assert.AreEqual("Enemy_SlimeG", g.Floor.At(at), "倒せていないので相手は残る");
        Assert.AreEqual(new Vector2Int(0, -1), g.Player.Position, "踏み込んでいない");
        Assert.AreEqual(GameOverScreen.Line, g.CurrentPage);
    }

    [Test]
    public void 魔法攻撃で尽きてもゲームオーバー()
    {
        var g = NewGame(hp: 100, attack: 999, defense: 999);
        // 魔術士Ａ(ID151)が隣にいると200ダメージ（原典 0x1b75〜）
        g.Floor.Clear(new Vector2Int(0, 0));
        g.Floor.Clear(new Vector2Int(0, -1));
        g.Floor.Put(new Vector2Int(-1, 0), "Enemy_ID151");
        g.Player.Position = new Vector2Int(0, -1);
        g.Player.ShieldRank = 1;                   // 神盾でなければ効く

        g.Step(Vector2Int.up);
        Assert.AreEqual(0, g.Player.Hp);
        g.ConsumeDeath();
        g.Pump(false);
        Assert.IsTrue(g.IsGameOver);
    }

    [Test]
    public void 画面を送ると最初からになる()
    {
        // 原典は INITIAL.DAT を読み直す。入力側がそのやり直しを持っている。
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerController.cs");
        // 倒れたら中断データも捨てて最初から
        StringAssert.Contains(
            "if (Game.IsGameOver) { TowerGame.DropResume(); StartNewGame(); return; }", src);
        StringAssert.Contains("INITIAL.DAT を読み直す", src);
    }

    static TowerGame NewGame(int hp, int attack, int defense)
    {
        var p = new PlayerState
        {
            Hp = hp, Attack = attack, Defense = defense, Floor = 1,
            Position = new Vector2Int(0, 0)
        };
        return new TowerGame(p);
    }
}
