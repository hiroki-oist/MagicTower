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

    /// <summary>
    /// 原典 0x8a46 は「今までに聞いた情報を順次表示します。」（セリフ53）のあと、
    /// 聞いた情報を**1件ずつ、番号とセリフの中身**を出してキーを待つ。
    /// 番号だけの一覧を出していて「読みたいのは会話ログなのに」と報告された。
    /// </summary>
    [Test]
    public void 聞いた情報が中身ごと一件ずつ出る()
    {
        var p = new PlayerState { Floor = 1, Position = new Vector2Int(0, 0) };
        p.Give(SpecialItems.Kind.WisdomOrb);
        var first = new List<WisdomOrb.Info>(WisdomOrb.Instance.All)[0];
        p.EventFlags.Add(first.Flag);

        var pages = WisdomOrb.Pages(p);
        Assert.AreEqual(2, pages.Count, "前置き1つ＋聞いた情報1件");
        StringAssert.Contains("今までに聞いた情報", pages[0], "セリフ53が前置き");
        StringAssert.Contains("情報番号", pages[1]);
        StringAssert.Contains(first.Number.ToString(), pages[1]);
        // **中身がそのまま入っている**のが肝
        var body = GameText.Instance.Dialogue(first.Record).Value.Body;
        StringAssert.Contains(body, pages[1], "セリフの中身がそのまま");
        // 手に入れたときの文言は**出さない**（原典 0x8a46 は持ち物を触らない）
        foreach (var t in pages)
            StringAssert.DoesNotContain("手に入れた", t, "持ち物の取得文は出さない");
    }

    [Test]
    public void 何も聞いていなければ前置きだけ()
    {
        var p = new PlayerState { Floor = 1, Position = new Vector2Int(0, 0) };
        p.Give(SpecialItems.Kind.WisdomOrb);
        Assert.AreEqual(1, WisdomOrb.Pages(p).Count);
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
