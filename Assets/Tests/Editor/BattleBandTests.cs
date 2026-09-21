using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 戦闘中の黒い帯と、広い会話の窓（**作者の旧版の見せ方**、<see cref="AuthorStyle"/>）。
/// 原典の再現ではないので、原典側の寸法（<see cref="DialogueWindow"/>）とは別に確かめる。
/// </summary>
public class BattleBandTests
{
    [Test]
    public void 一撃ごとに相手の命が減り最後は零になる()
    {
        // 自分 命100 / 一撃25、相手 命100 / 一撃10、4撃で倒す
        var t = BattleBand.At(100, 100, 25, 10, 0, 0);
        Assert.AreEqual(100, t.EnemyHp);
        Assert.AreEqual(100, t.PlayerHp);
        Assert.AreEqual(1, t.Turn, "始めは第1ターン");

        Assert.AreEqual(75, BattleBand.At(100, 100, 25, 10, 1, 0).EnemyHp);
        Assert.AreEqual(50, BattleBand.At(100, 100, 25, 10, 2, 1).EnemyHp);
        Assert.AreEqual(0,  BattleBand.At(100, 100, 25, 10, 4, 3).EnemyHp);
        // 端数が出ても零より下へは行かない
        Assert.AreEqual(0, BattleBand.At(100, 10, 25, 10, 4, 3).EnemyHp);
    }

    [Test]
    public void 反撃のぶんだけ自分の命が減る()
    {
        Assert.AreEqual(100, BattleBand.At(100, 100, 25, 10, 1, 0).PlayerHp);
        Assert.AreEqual(90,  BattleBand.At(100, 100, 25, 10, 2, 1).PlayerHp);
        Assert.AreEqual(70,  BattleBand.At(100, 100, 25, 10, 4, 3).PlayerHp);
    }

    [Test]
    public void 旧版と同じ順で自分から殴り最後の反撃は無い()
    {
        var trace = BattleBand.Trace(100, 100, 25, 10, 4);
        // 始め + (斬る, 反撃) x3 + 斬る = 8
        Assert.AreEqual(8, trace.Count);
        Assert.AreEqual(100, trace[0].EnemyHp);
        Assert.AreEqual(75,  trace[1].EnemyHp, "まず自分が殴る");
        Assert.AreEqual(100, trace[1].PlayerHp, "その時点では反撃されていない");
        Assert.AreEqual(90,  trace[2].PlayerHp, "次に反撃される");
        Assert.AreEqual(0,   trace[trace.Count - 1].EnemyHp);
        Assert.AreEqual(70,  trace[trace.Count - 1].PlayerHp, "最後の一撃には反撃が無い");
    }

    [Test]
    public void 減る量は原典の戦闘規則と同じ()
    {
        // 自分 攻20/防10、相手 命50/攻30/防5 → 一撃15で4撃、被ダメージ20x3
        var p = BattleRules.Predict(20, 10, 50, 30, 5, 7,
                                    BattleRules.EnemyKind.Normal, BattleRules.Charms.None);
        var trace = BattleBand.Trace(100, 50, 15, 20, p.PlayerHits);
        Assert.AreEqual(4, p.PlayerHits);
        Assert.AreEqual(0, trace[trace.Count - 1].EnemyHp);
        Assert.AreEqual(100 - p.DamageTaken, trace[trace.Count - 1].PlayerHp);
    }

    [Test]
    public void 帯の中身は帯の中に収まる()
    {
        var band = AuthorStyle.Band;
        Assert.AreEqual(DialogueWindow.ScreenWidth, band.width, "画面いっぱいの帯");
        Assert.AreEqual(DialogueWindow.ScreenHeight / 2, band.center.y, 1f, "上下の中心");

        var rects = new System.Collections.Generic.List<RectInt> { AuthorStyle.TurnBox };
        foreach (var cx in new[] { AuthorStyle.PlayerCenterX, AuthorStyle.EnemyCenterX })
        {
            rects.Add(AuthorStyle.Icon(cx));
            rects.Add(AuthorStyle.Name(cx));
            for (int i = 0; i < AuthorStyle.StatTops.Length; i++)
                rects.Add(AuthorStyle.Stat(cx, i));
        }
        foreach (var r in rects)
        {
            Assert.GreaterOrEqual(r.yMin, band.yMin, $"{r} が帯の上へはみ出す");
            Assert.LessOrEqual(r.yMax, band.yMax, $"{r} が帯の下へはみ出す");
            Assert.GreaterOrEqual(r.xMin, band.xMin, $"{r} が帯の左へはみ出す");
            Assert.LessOrEqual(r.xMax, band.xMax, $"{r} が帯の右へはみ出す");
        }
    }

    [Test]
    public void 枠の中に中身が収まっている()
    {
        // 旧版は枠の絵（Assets/Images/Dialogue.png）を
        // 第Nターン・左右の絵と名前・左右の能力値 の5枚に分けて貼っていた。
        Contains(AuthorStyle.TurnPanel, AuthorStyle.TurnBox, "第Nターン");
        foreach (var cx in new[] { AuthorStyle.PlayerCenterX, AuthorStyle.EnemyCenterX })
        {
            Contains(AuthorStyle.SidePanel(cx), AuthorStyle.Icon(cx), "絵");
            Contains(AuthorStyle.SidePanel(cx), AuthorStyle.Name(cx), "名前");
            for (int i = 0; i < AuthorStyle.StatTops.Length; i++)
                Contains(AuthorStyle.StatPanel(cx), AuthorStyle.Stat(cx, i), $"能力値{i}");
        }
        // 枠どうしは重ならない
        Assert.LessOrEqual(AuthorStyle.SidePanel(AuthorStyle.PlayerCenterX).yMax,
                           AuthorStyle.StatPanel(AuthorStyle.PlayerCenterX).yMin);
        Assert.LessOrEqual(AuthorStyle.TurnPanel.yMax,
                           AuthorStyle.SidePanel(AuthorStyle.PlayerCenterX).yMin);
    }

    static void Contains(RectInt outer, RectInt inner, string what)
    {
        Assert.GreaterOrEqual(inner.xMin, outer.xMin, what);
        Assert.LessOrEqual(inner.xMax, outer.xMax, what);
        Assert.GreaterOrEqual(inner.yMin, outer.yMin, what);
        Assert.LessOrEqual(inner.yMax, outer.yMax, what);
    }

    [Test]
    public void 枠の絵は旧版と同じものを使う()
    {
        Assert.AreEqual("Assets/Images/Dialogue.png", AuthorStyle.PanelSpritePath);
        Assert.IsTrue(System.IO.File.Exists(AuthorStyle.PanelSpritePath), "絵が無い");
        var src = System.IO.File.ReadAllText("Assets/Editor/TowerSceneBuilder.cs");
        StringAssert.Contains("AuthorStyle.PanelSpritePath", src, "セリフの枠にも使う");
    }

    [Test]
    public void 左が自分で右が相手で重ならない()
    {
        // 旧版 BattleUI も PlayerImage が左（x=-125）、EnemyImage が右（x=+125）だった
        Assert.Less(AuthorStyle.PlayerCenterX, AuthorStyle.EnemyCenterX);
        foreach (var i in new[] { 0, 1, 2 })
            Assert.LessOrEqual(AuthorStyle.Stat(AuthorStyle.PlayerCenterX, i).xMax,
                               AuthorStyle.Stat(AuthorStyle.EnemyCenterX, i).xMin,
                               "左右の欄が重なる");
    }

    [Test]
    public void 見出しは旧版の文字そのまま()
    {
        CollectionAssert.AreEqual(new[] { "HP: ", "ATK: ", "DEF: " }, AuthorStyle.StatLabels);
        Assert.AreEqual("勇者", AuthorStyle.PlayerName);
    }

    [Test]
    public void 金の一言は全角の数で出す()
    {
        Assert.AreEqual("１０ ゴールド 手に入れた", BattleBand.GoldLine(10));
    }
}

/// <summary>広い会話の窓（作者の見せ方）。</summary>
public class AuthorDialogueTests
{
    [Test]
    public void 原典より広くて文字も大きい()
    {
        Assert.Greater(AuthorStyle.DialogueWidth, DialogueWindow.Width, "窓が広い");
        Assert.Greater(AuthorStyle.DialogueTextDots, 16f, "文字が大きい");
        Assert.AreEqual(64f / 3f, AuthorStyle.DialogueTextDots, 1e-4f, "原典の16ドットの4/3");
        Assert.Greater(AuthorStyle.DialogueLineAdvance, ScreenLayout.LineAdvance, "行送りも広い");
    }

    [Test]
    public void 窓の横幅は本文に合わせて中央に置く()
    {
        // 18文字の行なら、文字のぶん＋左右の余白ちょうど
        var wide = new string('あ', DialogueBox.CharsPerLine);
        var r = AuthorStyle.DialogueFor(wide);
        Assert.AreEqual(AuthorStyle.DialogueWidth, r.width, "折り返しの幅いっぱい");
        Assert.AreEqual(DialogueWindow.ScreenWidth - r.xMax, r.xMin, "左右の余白が同じ＝中央");

        // 短いセリフは窓も狭くなる（報告: 窓が広すぎて文字が左に寄る）
        var narrow = AuthorStyle.DialogueFor("はい");
        Assert.Less(narrow.width, r.width, "短い本文は窓も狭い");
        Assert.AreEqual(DialogueWindow.ScreenWidth - narrow.xMax, narrow.xMin, "こちらも中央");
        Assert.IsTrue(DialogueWindow.Fits(narrow), narrow.ToString());
    }

    [Test]
    public void 本文の幅は一番長い行で決まる()
    {
        Assert.AreEqual(5f, DialogueBox.MaxLineChars("あい\nあいうえお\nあ"), 1e-4f);
        Assert.AreEqual(1f, DialogueBox.MaxLineChars("ＡＢ".Substring(0, 1)), 1e-4f);
        Assert.AreEqual(1f, DialogueBox.MaxLineChars("ab"), 1e-4f, "半角は半分");
    }

    [Test]
    public void 実際のセリフでも文字が窓に収まる()
    {
        int seen = 0;
        for (int id = 100; id <= 170; id++)
        {
            var line = GameText.Instance.Dialogue(id);
            if (!line.HasValue) continue;
            foreach (var page in DialogueBox.Pages(line.Value))
            {
                var r = AuthorStyle.DialogueFor(page);
                Assert.IsTrue(DialogueWindow.Fits(r), $"{id}: {r}");
                float w = AuthorStyle.DialogueTextLeft * 2
                        + DialogueBox.MaxLineChars(page) * AuthorStyle.DialogueTextDots;
                Assert.LessOrEqual(w, r.width + 1f, $"{id} の文字が窓に収まる");
                seen++;
            }
        }
        Assert.Greater(seen, 20, "セリフが読めている");
    }

    [Test]
    public void どの行数でも画面からはみ出さない()
    {
        for (int n = 1; n <= 11; n++)
        {
            var r = AuthorStyle.DialogueFor(n);
            Assert.IsTrue(DialogueWindow.Fits(r), $"{n}行: {r}");
        }
    }

    [Test]
    public void 文字は窓の中に収まる()
    {
        for (int n = 1; n <= 11; n++)
        {
            int bottom = AuthorStyle.DialogueTextTop
                       + AuthorStyle.DialogueLineAdvance * (n - 1)
                       + Mathf.CeilToInt(AuthorStyle.DialogueTextDots);
            Assert.LessOrEqual(bottom, AuthorStyle.DialogueHeight(n), $"{n}行");
        }
    }

    [Test]
    public void 行間は行送りに合わせてある()
    {
        // TMP の行送り = 文字の大きさ x (1.084 + 行間/100)
        float advance = AuthorStyle.DialogueTextDots * (1.084f + AuthorStyle.DialogueLineSpacing / 100f);
        Assert.AreEqual(AuthorStyle.DialogueLineAdvance, advance, 0.5f);
    }

    [Test]
    public void 原典の寸法は変えていない()
    {
        // 作者の見せ方を入れても、原典側の式はそのまま残っていること
        Assert.AreEqual(160, DialogueWindow.Left);
        Assert.AreEqual(320, DialogueWindow.Width);
        Assert.AreEqual(152, DialogueWindow.Height(6));
    }

    [Test]
    public void 切り替えは一か所でできる()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/UI/AuthorStyle.cs");
        StringAssert.Contains("public static readonly bool Enabled", src);
        foreach (var f in new[] { "Assets/Scripts/UI/DialogueWindowView.cs",
                                  "Assets/Scripts/UI/BattleBandView.cs",
                                  "Assets/Scripts/UI/TowerHudView.cs",
                                  "Assets/Editor/TowerSceneBuilder.cs" })
            StringAssert.Contains("AuthorStyle.Enabled", System.IO.File.ReadAllText(f), f);
    }
}
