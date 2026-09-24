using NUnit.Framework;
using UnityEngine;

/// <summary>
/// できごとと音の結び付き。表は Resources/SoundCues.txt（原典の呼び出し61か所から作った）。
/// 本線が実際にその音を鳴らしているかを、鳴らした番号の並びで確かめる。
/// </summary>
public class SoundCueTests
{
    static readonly Vector2Int Up = new Vector2Int(0, 1);

    static TowerGame Game(int floor, Vector2Int at)
    {
        var p = new PlayerState
        {
            Hp = 5000, Attack = 500, Defense = 500, Gold = 1000,
            YellowKeys = 9, PurpleKeys = 9, RedKeys = 9,
            Floor = floor, Position = at,
        };
        return new TowerGame(p);
    }

    [Test]
    public void 合図の表は原典の音番号だけを指す()
    {
        var c = SoundCues.Instance;
        // 22 は原典の61か所を仕分けた数。＋2 は**原典に無い**「触る」と
        // 「会話を送る」（作者の指定）。
        Assert.AreEqual(24, c.Count, "61か所を仕分けた合図の数＋触る＋会話を送る");
        foreach (var name in c.Names)
        {
            int n = c.Number(name);
            Assert.IsNotNull(SoundBank.Instance.Get(n), $"{name} の音{n}が譜面にある");
            if (name == SoundCues.Tap || name == SoundCues.Advance)
            {
                Assert.GreaterOrEqual(n, 90, $"{name} は原典に無い音（90番台）");
                continue;
            }
            Assert.Less(n, 90, $"{name} は原典の音");
            Assert.IsNotNull(c.Source(name), $"{name} に原典の場所が控えてある");
        }
    }

    /// <summary>
    /// 触ったときの音（作者の指定。原典に無い）。触る＝91、会話を送る＝95 で、
    /// **別の音**にしてある（会話はいちばん多く鳴るので決定と同じだと煩い）。
    /// </summary>
    [Test]
    public void 触る音と会話を送る音は別の番号()
    {
        var c = SoundCues.Instance;
        Assert.AreEqual(91, c.Number(SoundCues.Tap), "触る＝音91");
        Assert.AreEqual(95, c.Number(SoundCues.Advance), "会話を送る＝音95");
        Assert.IsNotNull(SoundBank.Instance.Get(91));
        Assert.IsNotNull(SoundBank.Instance.Get(95));
    }

    [Test]
    public void 本線が使う合図は全部表にある()
    {
        foreach (var name in new[] { SoundCues.TakeItem, SoundCues.OpenDoor, SoundCues.Stairs,
                                     SoundCues.TalkMaster, SoundCues.TalkShop, SoundCues.Reveal,
                                     SoundCues.Slash, SoundCues.Hit, SoundCues.Magic,
                                     SoundCues.SnowMelt, SoundCues.Unlock, SoundCues.BigUnlock,
                                     SoundCues.UseSpecial, SoundCues.SceneBeat,
                                     SoundCues.FieldMusic, SoundCues.TitleMusic })
            Assert.Greater(SoundCues.Instance.Number(name), 0, name);
    }

    [Test]
    public void 物を拾うと音6が鳴る()
    {
        var g = Game(1, new Vector2Int(0, 0));
        g.Floor.Put(new Vector2Int(0, 1), "Key_Yellow");
        g.ConsumeSounds();
        g.Step(Up);
        Assert.Contains(6, g.ConsumeSounds(), "拾う＝音6（原典 0x23ee）");
    }

    [Test]
    public void 階段を踏むと音8が鳴る()
    {
        var g = Game(1, new Vector2Int(0, 0));
        g.Floor.Put(new Vector2Int(0, 1), ">");
        g.ConsumeSounds();
        g.Step(Up);
        Assert.Contains(8, g.ConsumeSounds(), "階段＝音8（原典 0x28e6）");
    }

    [Test]
    public void 扉を開けると音11が鳴る()
    {
        // 原典 0x23ee 自身は鳴らさず、扉が開く動き 0x7be6 の中で 0xb(11) を鳴らす。
        // **こちらも同じで、鳴らすのは見た目の側（DoorOpenView）が動きの頭で**。
        // 本線でも鳴らしていて二重に聞こえていた（作者の報告）。
        var g = Game(1, new Vector2Int(0, 0));
        g.Floor.Put(new Vector2Int(0, 1), "y");
        g.ConsumeSounds();
        g.Step(Up);
        Assert.IsFalse(new System.Collections.Generic.List<int>(g.ConsumeSounds()).Contains(11),
                       "本線では鳴らさない（動きの頭で鳴る）");
        Assert.AreEqual(1, g.ConsumeDoorOpens().Count, "開いた扉が積まれる");
        var dov = System.IO.File.ReadAllText("Assets/Scripts/UI/DoorOpenView.cs");
        StringAssert.Contains("SoundCues.OpenDoor", dov, "動きの頭で鳴らす");
    }

    [Test]
    /// <summary>
    /// 原典は一撃ごとに音9（自分の攻撃）、相手が生き残れば音10（反撃）。
    /// 一撃で倒すときは音10が鳴らない。
    /// </summary>
    public void 戦闘の音は攻撃と反撃で鳴り分ける()
    {
        // 何度も殴らないと倒せない相手
        var weak = new PlayerState
        {
            Hp = 5000, Attack = 30, Defense = 1, Floor = 1, Position = new Vector2Int(0, 0),
        };
        var g = new TowerGame(weak);
        g.Floor.Put(new Vector2Int(0, 1), "Enemy_SlimeG");
        g.ConsumeSounds();
        g.Step(Up);
        var s = g.ConsumeSounds();
        Assert.Contains(9, s, "自分の攻撃＝音9（原典 0x6263）");
        Assert.Contains(10, s, "相手の反撃＝音10（原典 0x6456）");
        int nine = 0, ten = 0;
        foreach (var n in s) { if (n == 9) nine++; if (n == 10) ten++; }
        Assert.AreEqual(nine - 1, ten, "反撃は攻撃より1回少ない");

        // 一撃で倒せる相手なら反撃は鳴らない
        var strong = Game(1, new Vector2Int(0, 0));
        strong.Floor.Put(new Vector2Int(0, 1), "Enemy_SlimeG");
        strong.ConsumeSounds();
        strong.Step(Up);
        var s2 = strong.ConsumeSounds();
        Assert.Contains(9, s2);
        CollectionAssert.DoesNotContain(s2, 10, "一撃なら反撃は無い");
    }

    [Test]
    public void 音を取り出すと空になる()
    {
        var g = Game(1, new Vector2Int(0, 0));
        g.Floor.Put(new Vector2Int(0, 1), "Key_Yellow");
        g.Step(Up);
        Assert.Greater(g.ConsumeSounds().Count, 0);
        Assert.AreEqual(0, g.ConsumeSounds().Count, "2回目は空");
    }
}
