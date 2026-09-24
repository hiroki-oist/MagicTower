using NUnit.Framework;
using UnityEngine;

/// <summary>
/// **倒して相手のマスへ踏み込んだあとの魔法攻撃**（作者の指定＝Java版に合わせる）。
///
/// 原典（PC98版）の戦闘 0x61ef〜0x6232 は踏み込みの一歩として X か Y を自分で動かし、
/// そのあと後処理 0x1588（魔法攻撃）を呼ばない。だから魔導師に挟まれたマスでも
/// 倒して入った瞬間は無傷だった。
///
/// Java版（2001年の公式移植）の `attackMonster` は charaXI/charaYI を書かない＝
/// 倒しても相手のマスに入らないので、そのマスへはあとから歩いて入る。
/// `eventMT` は「マスに乗っていて、かつ前のマスと違う」たびに `ichicheck2` を呼ぶので、
/// 挟まれたマスに入れば必ず食らう。49階のゼノ戦で神盾が実質必須になるのはこれ。
///
/// <see cref="AuthorStyle.MagicAfterBattleStep"/> を参照。
/// </summary>
public class PincerAfterBattleTests
{
    /// <summary>大魔導師の口上のあとの形（ゼノと魔導師8体）。</summary>
    static TowerGame OnNine(int shield)
    {
        var p = new PlayerState
        {
            Hp = 8000, Attack = 9999, Defense = 9999, ShieldRank = shield,
            Floor = 49, HighestFloor = 49, Position = new Vector2Int(0, 5),
        };
        var g = new TowerGame(p);
        g.EnterFloor(49, new Vector2Int(0, 5));
        g.Floor.Put(MagicSeal.Center, MagicSeal.BossPrefab);
        foreach (var c in MagicSeal.Corners) g.Floor.Put(c, "Enemy_ID93");
        foreach (var e in MagicSeal.Edges) g.Floor.Put(e, "Enemy_ID93");
        return g;
    }

    static void Drain(TowerGame g, int limit = 200)
    {
        for (int i = 0; i < limit && g.CurrentKind.HasValue; i++) g.NextPage();
    }

    [Test]
    public void 挟まれたマスは倒して入っても魔法攻撃を受ける()
    {
        Assert.IsTrue(AuthorStyle.MagicAfterBattleStep, "作者の指定で入れてある");

        var g = OnNine(0);
        // (0,+5) から1つ下の魔導師 (0,+4) を倒す。そのマスは左右 (-1,+4)/(1,+4) が魔導師。
        g.Step(Vector2Int.down);
        Drain(g);
        Assert.AreEqual(new Vector2Int(0, 4), g.Player.Position, "倒した相手のマスに立つ");
        Assert.AreEqual(4000, g.Player.Hp, "左右に挟まれて半分");
    }

    [Test]
    public void 神盾があれば踏み込んでも半分にならない()
    {
        var g = OnNine(MagicAttack.DivineShieldRank);
        g.Step(Vector2Int.down);
        Drain(g);
        Assert.AreEqual(new Vector2Int(0, 4), g.Player.Position);
        Assert.AreEqual(8000, g.Player.Hp, "神盾が魔法攻撃を防ぐ");
    }

    [Test]
    public void 挟まれていないマスなら踏み込んでも無傷()
    {
        var g = OnNine(0);
        foreach (var c in MagicSeal.Corners) g.Floor.Clear(c);   // 斜めを先に片付けておく
        g.Step(Vector2Int.down);
        Drain(g);
        Assert.AreEqual(new Vector2Int(0, 4), g.Player.Position);
        Assert.AreEqual(8000, g.Player.Hp);
    }

    /// <summary>四方どこから入っても同じ。上下に挟まれる形（左右の辺）も見る。</summary>
    [Test]
    public void 左右の辺は上下の魔導師に挟まれる()
    {
        var g = OnNine(0);
        g.EnterFloor(49, new Vector2Int(-2, 3));
        g.Step(Vector2Int.right);                 // (-1,+3) の魔導師を倒して入る
        Drain(g);
        Assert.AreEqual(new Vector2Int(-1, 3), g.Player.Position);
        Assert.AreEqual(4000, g.Player.Hp, "(-1,+4) と (-1,+2) に挟まれる");
    }

    /// <summary>四方を倒し切るまでに4回とも食らう＝神盾が無いと HP が 1/16 になる。</summary>
    [Test]
    public void 四体倒し切るには神盾が要る()
    {
        // 辺のマスと、その外側から踏み込む出発点
        var route = new[]
        {
            new[] { new Vector2Int(0, 4),  new Vector2Int(0, 5)  },
            new[] { new Vector2Int(-1, 3), new Vector2Int(-2, 3) },
            new[] { new Vector2Int(1, 3),  new Vector2Int(2, 3)  },
            new[] { new Vector2Int(0, 2),  new Vector2Int(0, 1)  },
        };

        var g = OnNine(0);
        foreach (var r in route)
        {
            g.EnterFloor(49, r[1]);
            g.Step(r[0] - r[1]);
            Drain(g);
            Assert.AreEqual(r[0], g.Player.Position, r[0] + " を倒して入る");
        }
        Assert.AreEqual(8000 / 16, g.Player.Hp, "4回とも半分になる");
        // 五の目の判定（原典 0x5cdb）は踏み込みではなく**次の一手の頭**で回る。
        Assert.IsTrue(MagicSeal.IsFormed(g.Floor), "五の目の形になっている");
        g.EnterFloor(49, new Vector2Int(0, 1));
        g.Step(Vector2Int.up);
        Drain(g);
        Assert.IsTrue(g.Player.EventFlags.Contains(MagicSeal.FlagId), "五の目は成立する");

        var h = OnNine(MagicAttack.DivineShieldRank);
        foreach (var r in route)
        {
            h.EnterFloor(49, r[1]);
            h.Step(r[0] - r[1]);
            Drain(h);
        }
        Assert.AreEqual(8000, h.Player.Hp, "神盾があれば無傷で四方を片付けられる");
    }
}
