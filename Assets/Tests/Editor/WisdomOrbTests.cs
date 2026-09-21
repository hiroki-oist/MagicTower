using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 知恵のオーブ。読み返しの表は原典 MTE.EXE 0x8a46 から取り出したもの。
/// </summary>
public class WisdomOrbTests
{
    [Test]
    public void 読み返しの表は24件で番号が通し()
    {
        var o = WisdomOrb.Instance;
        Assert.AreEqual(24, o.Count, "原典の表は24件");
        int n = 1;
        foreach (var i in o.All)
        {
            Assert.AreEqual(n, i.Number, "情報番号は1から通し");
            Assert.IsTrue(GameText.Instance.Dialogue(i.Record).HasValue, $"セリフ{i.Record}がある");
            n++;
        }
    }

    /// <summary>フラグ69〜86＝仙人18人、50〜68＝商人。原典のフラグ配列の並びどおり。</summary>
    [Test]
    public void フラグは仙人と商人の範囲に収まる()
    {
        int masters = 0, merchants = 0;
        foreach (var i in WisdomOrb.Instance.All)
        {
            if (i.Flag >= 69 && i.Flag <= 86) { masters++; Assert.IsTrue(i.Record >= 80 && i.Record <= 97); }
            else if (i.Flag >= 50 && i.Flag <= 68) { merchants++; Assert.IsTrue(i.Record >= 12 && i.Record <= 31); }
            else Assert.Fail($"知らない範囲のフラグ {i.Flag}");
        }
        Assert.AreEqual(15, masters, "仙人側");
        Assert.AreEqual(9, merchants, "商人側");
    }

    [Test]
    public void 聞いた情報だけが並ぶ()
    {
        var p = new PlayerState { Floor = 1, Position = new Vector2Int(0, 0) };
        Assert.AreEqual(0, WisdomOrb.Instance.Heard(p).Count, "はじめは何も聞いていない");

        var first = new List<WisdomOrb.Info>(WisdomOrb.Instance.All)[0];
        p.EventFlags.Add(first.Flag);
        var heard = WisdomOrb.Instance.Heard(p);
        Assert.AreEqual(1, heard.Count);
        Assert.AreEqual(first.Record, heard[0].Record);
        Assert.AreEqual(first.Record, WisdomOrb.Instance.RecordOf(p, first.Number));
        Assert.AreEqual(-1, WisdomOrb.Instance.RecordOf(p, 99), "無い番号は -1");
    }

    [Test]
    public void 持ち物の能力と情報番号が窓に出る()
    {
        var p = new PlayerState { Floor = 1, Position = new Vector2Int(0, 0) };
        p.Give(SpecialItems.Kind.WisdomOrb);
        var first = new List<WisdomOrb.Info>(WisdomOrb.Instance.All)[0];
        p.EventFlags.Add(first.Flag);

        var text = WisdomOrb.Text(p);
        StringAssert.Contains("知恵のオーブ", text, "持ち物の能力");
        StringAssert.Contains("情報番号", text);
        StringAssert.Contains(first.Number.ToString(), text);
    }

    [Test]
    public void メニューから選ぶと窓に出る()
    {
        var p = new PlayerState { Hp = 1000, Attack = 10, Defense = 10,
                                  Floor = 1, Position = new Vector2Int(0, 0) };
        p.Give(SpecialItems.Kind.WisdomOrb);
        var g = new TowerGame(p);
        g.OpenMenu();
        while (g.MenuCurrent != TowerMenu.Entry.WisdomOrb) g.MenuMove(1);
        g.MenuConfirm();
        Assert.IsTrue(g.IsShowingPage, "中身が窓に出る");
    }
}
