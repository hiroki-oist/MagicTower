using NUnit.Framework;
using UnityEngine;

/// <summary>仙人のヒント表が原典（MTE.EXE 関数 0x0000）どおりかを確かめる。</summary>
public class MasterHintsTests
{
    [Test]
    public void 表は十八件で階は原典の並びどおり()
    {
        var h = MasterHints.Instance;
        Assert.AreEqual(18, h.Count, "原典 jii[18][3]");

        var floors = new System.Collections.Generic.List<int>();
        foreach (var e in h.All) floors.Add(e.Floor);
        CollectionAssert.AreEqual(
            new[] { 2, 3, 4, 6, 16, 16, 18, 21, 27, 31, 33, 36, 37, 39, 42, 45, 46, 48 },
            floors);
    }

    [Test]
    public void 通し番号は三十九から連番でフラグは番号足す三十()
    {
        int expect = 39;
        foreach (var e in MasterHints.Instance.All)
        {
            Assert.AreEqual(expect, e.MasterId);
            Assert.AreEqual(e.MasterId + 0x1e, e.FlagId, "原典 jii[i][1] + 0x1e");
            expect++;
        }
    }

    [Test]
    public void 十六階は左の仙人と隠れた仙人を場所で分ける()
    {
        // 原典: if (現在階 == 16 && X > 8) i++;   X = x + 5 なので x >= +4
        Assert.AreEqual(83, MasterHints.Instance.For(16, -5).Value.Record, "見えている方");
        Assert.AreEqual(83, MasterHints.Instance.For(16, +3).Value.Record);
        Assert.AreEqual(85, MasterHints.Instance.For(16, +4).Value.Record, "隠し壁の奥");
        Assert.AreEqual(85, MasterHints.Instance.For(16, +5).Value.Record);
    }

    [Test]
    public void 仙人の居ない階は引けない()
    {
        Assert.IsNull(MasterHints.Instance.For(1, 0));
        Assert.IsNull(MasterHints.Instance.For(50, 0));
    }

    [Test]
    public void 表の階には必ず盤面に仙人が置いてある()
    {
        foreach (var e in MasterHints.Instance.All)
        {
            var asset = Resources.Load<TextAsset>($"Floors/Floor_{e.Floor}");
            Assert.IsNotNull(asset, $"Floor_{e.Floor}");
            StringAssert.Contains("Master", asset.text, $"{e.Floor}階に仙人が居ない");
        }
    }

    [Test]
    public void セリフは原典の八十番から九十七番を一度ずつ使う()
    {
        var seen = new System.Collections.Generic.SortedSet<int>();
        foreach (var e in MasterHints.Instance.All)
        {
            Assert.IsTrue(e.Record >= 80 && e.Record <= 97, $"{e.Record}");
            Assert.IsTrue(seen.Add(e.Record), $"{e.Record} が重複");
            Assert.IsTrue(GameText.Instance.Dialogue(e.Record).HasValue,
                          $"セリフ {e.Record} が Dialogue.txt に無い");
        }
        Assert.AreEqual(18, seen.Count);
    }

    [Test]
    public void 三十九階の仙人は三時の秘宝を教える()
    {
        var rec = MasterHints.Instance.For(39, 0).Value.Record;
        StringAssert.Contains("３時になれば", GameText.Instance.Dialogue(rec).Value.Body);
    }

    [Test]
    public void 四十八階の仙人はサイコロの五の目を教える()
    {
        var rec = MasterHints.Instance.For(48, 0).Value.Record;
        StringAssert.Contains("サイコロの５の目", GameText.Instance.Dialogue(rec).Value.Body);
    }

    // ---- 話しかけたときの動き ----

    static PlayerState At(int floor, int x)
    {
        return new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100, Gold = 0,
            Floor = floor, Position = new Vector2Int(x, 0)
        };
    }

    [Test]
    public void 二階の仙人は攻撃力と防御力を三パーセント上げる()
    {
        var p = At(2, 0);
        var line = TowerNpc.TalkToMaster(p);
        StringAssert.Contains("たすけてくれてありがとう", line);
        StringAssert.Contains("３パ\nーセント", line, "原文の改行位置のまま");
        Assert.AreEqual(103, p.Attack);
        Assert.AreEqual(103, p.Defense);
    }

    [Test]
    public void 三パーセントは原典どおり切り捨て()
    {
        var p = At(2, 0);
        p.Attack = 10; p.Defense = 33;
        TowerNpc.TalkToMaster(p);
        Assert.AreEqual(10, p.Attack, "10*3/100 = 0");
        Assert.AreEqual(33, p.Defense, "33*3/100 = 0");
    }

    [Test]
    public void 三階の仙人は勇者のオーブをくれる()
    {
        var p = At(3, 0);
        StringAssert.Contains("勇者のオーブ", TowerNpc.TalkToMaster(p));
        Assert.IsTrue(p.Has(SpecialItems.Kind.HeroOrb));
    }

    [Test]
    public void 十六階の隠れた仙人だけがエリクサーをくれる()
    {
        var seen = At(16, -5);
        TowerNpc.TalkToMaster(seen);
        Assert.IsFalse(seen.Has(SpecialItems.Kind.Elixir), "見えている方はくれない");

        var hidden = At(16, +4);
        StringAssert.Contains("エリクサー", TowerNpc.TalkToMaster(hidden));
        Assert.IsTrue(hidden.Has(SpecialItems.Kind.Elixir));
    }

    [Test]
    public void 二度目は話は終わりじゃと言うだけ()
    {
        var p = At(2, 0);
        TowerNpc.TalkToMaster(p);
        int atk = p.Attack;

        var second = TowerNpc.TalkToMaster(p);
        Assert.AreEqual("もう、話は終わりじゃ。", second);
        Assert.AreEqual(atk, p.Attack, "ごほうびは一度きり");
    }

    [Test]
    public void 十六階の二人のフラグは別々()
    {
        var p = At(16, -5);
        TowerNpc.TalkToMaster(p);

        p.Position = new Vector2Int(+4, 0);
        StringAssert.Contains("エリクサー", TowerNpc.TalkToMaster(p),
                              "片方と話しても もう片方は残る");
    }

    [Test]
    public void 話した覚えは保存され読み込まれる()
    {
        var dir = System.IO.Path.Combine(Application.temporaryCachePath, "mhtest");
        if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        TowerSave.Directory = dir;
        try
        {
            var p = At(3, 0);
            TowerNpc.TalkToMaster(p);
            TowerSave.Save(1, p);

            var back = TowerSave.Load(1);
            Assert.AreEqual("もう、話は終わりじゃ。", TowerNpc.TalkToMaster(back));
        }
        finally { TowerSave.Directory = null; }
    }
}
