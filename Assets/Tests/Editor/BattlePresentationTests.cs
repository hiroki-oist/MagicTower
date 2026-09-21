using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 戦闘の演出。原典 MTE.EXE 0x6082 の流れをそのまま並べたもの。
/// </summary>
public class BattlePresentationTests
{
    static readonly Vector2Int From = new Vector2Int(0, 0);
    static readonly Vector2Int To = new Vector2Int(0, 1);

    [Test]
    public void 一撃で倒すときは反撃が無い()
    {
        var s = BattlePresentation.Steps(From, To, 1);
        Assert.AreEqual("踏み込む", s[0].Kind);
        Assert.AreEqual("待つ", s[1].Kind);
        Assert.AreEqual(BattlePresentation.StepWait, s[1].Value);
        Assert.AreEqual("音", s[2].Kind);
        Assert.AreEqual(BattlePresentation.SlashSound, s[2].Value);
        Assert.AreEqual("斬る", s[3].Kind);
        Assert.AreEqual("相手が消える", s[4].Kind);
        Assert.AreEqual(5, s.Count);
    }

    [Test]
    public void 三撃なら反撃は二回()
    {
        var s = BattlePresentation.Steps(From, To, 3);
        int slash = 0, counter = 0;
        foreach (var x in s)
        {
            if (x.Kind == "斬る") slash++;
            if (x.Kind == "反撃") counter++;
        }
        Assert.AreEqual(3, slash, "斬るのは倒すまでの回数");
        Assert.AreEqual(2, counter, "反撃は1つ少ない（最後の一撃で倒す）");
    }

    [Test]
    public void 演出の位置は相手のマス()
    {
        foreach (var x in BattlePresentation.Steps(From, To, 2))
            Assert.AreEqual(To, x.At, x.Kind + " は相手のマスで起きる");
    }

    [Test]
    public void 音は9と10だけ()
    {
        foreach (var x in BattlePresentation.Steps(From, To, 4))
            if (x.Kind == "音")
                Assert.IsTrue(x.Value == 9 || x.Value == 10, "原典が鳴らすのはこの2つ");
    }

    /// <summary>回数は戦闘の計算と同じもの。</summary>
    [Test]
    public void くり返す回数は予測と同じ()
    {
        var e = TowerBattle.LoadEnemy("Enemy_SlimeG");
        var p = BattleRules.Predict(100, 100, e.MaxHP, e.Attack, e.Defense, e.Coin,
                                    BattleRules.KindFromTag("Enemy_SlimeG"),
                                    BattleRules.Charms.None);
        var s = BattlePresentation.Steps(From, To, p.PlayerHits);
        int slash = 0;
        foreach (var x in s) if (x.Kind == "斬る") slash++;
        Assert.AreEqual(p.PlayerHits, slash);
    }

    /// <summary>待ちの単位は垂直帰線の回数。原典の `lcall 0xf5f, 0x3f2` がそれ。</summary>
    [Test]
    public void 待ちは六十分の一秒きざみ()
    {
        Assert.AreEqual(1f / 60f, BattlePresentation.FrameSeconds, 1e-6f);
        Assert.AreEqual(10f / 60f, BattlePresentation.WaitSeconds(10), 1e-6f);

        // 一撃で倒すなら 10（踏み込みの待ち）＋5（斬りの絵）
        var s = BattlePresentation.Steps(From, To, 1);
        Assert.AreEqual(15f / 60f, BattlePresentation.Duration(s), 1e-6f);

        // 三撃なら 3×(10+5) ＋ 反撃2回×(10+5)
        var s3 = BattlePresentation.Steps(From, To, 3);
        Assert.AreEqual((3 * 15 + 2 * 15) / 60f, BattlePresentation.Duration(s3), 1e-6f);
    }

    /// <summary>本線が戦闘のたびに演出の並びを残す。</summary>
    [Test]
    public void 戦闘のあと演出の並びが残る()
    {
        var p = new PlayerState
        {
            Hp = 5000, Attack = 500, Defense = 500,
            Floor = 1, Position = new Vector2Int(0, 0),
        };
        var g = new TowerGame(p);
        Assert.IsNull(g.LastBattle, "戦う前は無い");
        g.Floor.Put(new Vector2Int(0, 1), "Enemy_SlimeG");
        g.Step(new Vector2Int(0, 1));
        Assert.IsNotNull(g.LastBattle);
        Assert.AreEqual("踏み込む", g.LastBattle[0].Kind);
        Assert.AreEqual(new Vector2Int(0, 1), g.LastBattle[0].At);
        Assert.Greater(BattlePresentation.Duration(g.LastBattle), 0f);
    }

    /// <summary>
    /// 魔法攻撃を受けると、**自分の立っているマス**に閃光が出る（原典 0x1b75〜0x1ba7）。
    /// 絵は戦闘のときと同じもの。
    /// </summary>
    [Test]
    public void 魔法攻撃を受けると自分のマスに閃光が出る()
    {
        var p = new PlayerState
        {
            Hp = 5000, Attack = 10, Defense = 10, ShieldRank = 0,
            Floor = 1, Position = new Vector2Int(0, 0),
        };
        var g = new TowerGame(p);
        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                g.Floor.Clear(new Vector2Int(x, y));
        // 魔術士Ａ（原典ID151）の隣に立つと200ポイント受ける
        g.Floor.Put(new Vector2Int(0, 2), "Enemy_ID151");
        g.ConsumeEffects();

        g.Step(new Vector2Int(0, 1));            // (0,1) へ動くと魔術士の隣になる
        var fx = g.ConsumeEffects();
        Assert.Contains(new Vector2Int(0, 1), fx, "自分の立ったマスに閃光");
        Assert.AreEqual(0, g.ConsumeEffects().Count, "取り出すと空になる");
    }

    [Test]
    public void 攻撃できない相手には何も起きない()
    {
        Assert.IsEmpty(BattlePresentation.Steps(From, To, 0));
    }
}
