using NUnit.Framework;
using UnityEngine;

/// <summary>立ち入ると起きること（原典 MTE.EXE 0x1588）を確かめる。</summary>
public class PositionEventsTests
{
    [Test]
    public void 表は十八件で原典の並びどおり()
    {
        var e = PositionEvents.Instance;
        Assert.AreEqual(18, e.Count, "物語の引き金16件＋終わりの2件");

        var seq = new System.Collections.Generic.List<string>();
        foreach (var x in e.All) seq.Add($"{x.Floor}:{x.FlagId}");
        CollectionAssert.AreEqual(
            new[] { "3:89", "10:91", "10:94",
                    // 13階の雪の結晶だけは一度きりの印を持たない（原典 0x1fd5）
                    "13:-1", "15:95", "20:103", "25:105", "30:109",
                    "32:111", "33:114", "33:113", "38:119", "40:121", "42:123",
                    // 43階・47階の逃げる相手も印を持たない（原典 0x217b / 0x209a）
                    "43:-1", "47:-1",
                    // 物語の終わり（原典 0x2277 と 0x22cc〜0x23e5）
                    "49:130", "24:131" }, seq);
    }

    /// <summary>
    /// 終幕の引き金だけは「そのマスが門か」を見る（原典 0x2307 の `cmp …, 4`）。
    /// 24階の時空の門(0,0)の真下に立つと始まる。
    /// </summary>
    [Test]
    public void 終幕の引き金は門を見る()
    {
        PositionEvents.Event? gate = null;
        foreach (var x in PositionEvents.Instance.All)
            if (x.Floor == 24 && x.FlagId == 131) gate = x;
        Assert.IsNotNull(gate, "24階の引き金がある");
        Assert.AreEqual(new Vector2Int(0, 0), gate.Value.NeedsGate.Value);
        Assert.AreEqual("終幕", gate.Value.Scene);

        var flags = new System.Collections.Generic.HashSet<int>();
        System.Func<Vector2Int, bool> floorYes = p => true;
        var at = new Vector2Int(0, -1);
        Assert.IsNull(PositionEvents.Instance.At(24, at, flags, floorYes, p => false),
                      "門が無ければ始まらない");
        Assert.IsNotNull(PositionEvents.Instance.At(24, at, flags, floorYes, p => true),
                         "門があれば始まる");
    }

    [Test]
    public void フラグ番号はどれも重ならない()
    {
        var seen = new System.Collections.Generic.HashSet<int>();
        foreach (var x in PositionEvents.Instance.All)
        {
            if (x.FlagId < 0) continue;      // 一度きりの印を持たない行（13/43/47階）
            Assert.IsTrue(seen.Add(x.FlagId), $"{x.FlagId} が重複");
        }
    }

    [Test]
    public void 三十三階だけ立つ場所が二か所ある()
    {
        foreach (var x in PositionEvents.Instance.All)
        {
            // 47階は左の列のどこでも起きる（原典の条件は X==0 かつ Y>2）
            int want = (x.Floor == 33 && x.FlagId == 113) ? 2 : (x.Floor == 47 ? 8 : 1);
            Assert.AreEqual(want, x.Spots.Count, $"F{x.Floor} フラグ{x.FlagId}");
        }
    }

    static TowerGame Game(int floor, Vector2Int at)
    {
        var p = new PlayerState
        {
            Hp = 9990, Attack = 999, Defense = 999, Floor = floor, Position = at
        };
        return new TowerGame(p);
    }

    [Test]
    public void 二十五階の中央下に立つと門が出て人形の声がする()
    {
        var g = Game(25, new Vector2Int(0, -2));
        g.Step(Vector2Int.down);
        Assert.AreEqual(new Vector2Int(0, -3), g.Player.Position);
        Assert.IsTrue(g.Player.EventFlags.Contains(105));
        Assert.AreEqual("s", g.Floor.At(new Vector2Int(0, -4)), "門が現れる");
        StringAssert.Contains("ユウシャ", string.Join("\n", g.Log), "セリフ159");
    }

    [Test]
    public void 三十階の中央に立つと上下に門が出る()
    {
        var g = Game(30, new Vector2Int(0, -1));
        g.Step(Vector2Int.up);
        Assert.IsTrue(g.Player.EventFlags.Contains(109));
        Assert.AreEqual("s", g.Floor.At(new Vector2Int(0, 2)));
        Assert.AreEqual("s", g.Floor.At(new Vector2Int(0, -1)));
    }

    [Test]
    public void 十階のボスは下のマスが空いてからでないと出ない()
    {
        var g = Game(10, new Vector2Int(0, 1));
        g.Floor.Put(new Vector2Int(0, -1), "Enemy_ID151");   // まだ倒していない
        g.Step(Vector2Int.down);
        Assert.IsFalse(g.Player.EventFlags.Contains(91));

        g.Floor.Clear(new Vector2Int(0, -1));
        g.Step(Vector2Int.up);
        g.Step(Vector2Int.down);
        Assert.IsTrue(g.Player.EventFlags.Contains(91));
        Assert.AreEqual("ボススケルトン登場", g.PendingScene);
    }

    [Test]
    public void 十階の盗賊はボスのあとでないと現れない()
    {
        var g = Game(10, new Vector2Int(0, -2));
        g.Floor.Clear(new Vector2Int(0, -3));   // 原典ではここは赤い扉。開けた状態にする
        g.Step(Vector2Int.down);
        Assert.IsFalse(g.Player.EventFlags.Contains(94), "フラグ91がまだ");

        g.Player.EventFlags.Add(91);
        g.Step(Vector2Int.up);
        g.Step(Vector2Int.down);
        Assert.IsTrue(g.Player.EventFlags.Contains(94));
        Assert.AreEqual("盗賊との再会", g.PendingScene);
    }

    [Test]
    public void 三階のゼノとの初対面は物語の始まり()
    {
        var g = Game(3, new Vector2Int(-1, -2));
        g.Step(Vector2Int.down);
        Assert.IsTrue(g.Player.EventFlags.Contains(89));
        Assert.AreEqual("ゼノとの初対面", g.PendingScene);
    }

    [Test]
    public void 場面の表はセリフ番号が全部原典にある()
    {
        var asset = Resources.Load<TextAsset>("Cutscenes");
        Assert.IsNotNull(asset);
        int scenes = 0;
        foreach (var raw in asset.text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            Assert.AreEqual(3, p.Length, line);
            scenes++;
            foreach (var r in p[2].Split(';'))
            {
                var t = r.Trim();
                if (t == "題名" || t == "奪われる" || t == "牢が開く"
                 || t == "最後の画面" || t == "駆け上がる" || t == "閃光"
                 || t == "斬りかかる" || t == "序章の絵"
                 || t == "塔の絵" || t == "終わりの言葉"
                 || t == "ボスが現れる") continue;                   // セリフ以外の段
                Vector2Int at; string kind;
                if (Cutscenes.TryParseMap(t, out at, out kind))
                {
                    Assert.IsTrue(at.x >= FloorData.MinX && at.x <= FloorData.MaxX
                               && at.y >= FloorData.MinY && at.y <= FloorData.MaxY, t);
                    Assert.IsFalse(string.IsNullOrEmpty(kind), t);
                    continue;                                   // 地図の書き換えの段
                }
                int n;
                Assert.IsTrue(int.TryParse(t, out n), t);
                Assert.IsTrue(GameText.Instance.Dialogue(n).HasValue, $"セリフ{n}");
            }
        }
        Assert.AreEqual(9, scenes, "七つ＋物語の終わりの2つ");
    }

    [Test]
    public void 表の演出名は全部場面の表にある()
    {
        var asset = Resources.Load<TextAsset>("Cutscenes");
        var names = new System.Collections.Generic.HashSet<string>();
        foreach (var raw in asset.text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            names.Add(line.Split('|')[0].Trim());
        }
        foreach (var x in PositionEvents.Instance.All)
            if (!string.IsNullOrEmpty(x.Scene))
                Assert.IsTrue(names.Contains(x.Scene), x.Scene);
    }
}
