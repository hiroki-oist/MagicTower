using NUnit.Framework;
using UnityEngine;

/// <summary>作者の報告（2026-09-22 その3）で直した点を固定する。</summary>
public class FeedbackFixTests
{
    static TowerGame NewGameOn(int floor, Vector2Int at)
    {
        var p = PlayerState.NewGame();
        p.Floor = floor; p.Position = at;
        return new TowerGame(p);
    }

    [Test]
    public void 倒れるときは倒れるまでの斬りしか出さない()
    {
        var p = PlayerState.NewGame();
        p.Hp = 50; p.Attack = 10; p.Defense = 0;
        var before = p.Hp;
        var r = TowerBattle.Resolve(p, "Enemy_SlimeR");
        if (!r.Died) Assert.Ignore("この相手では死なない");
        Assert.AreEqual(0, p.Hp);
        Assert.Greater(r.Hits, 0);
        // 反撃1回ぶんずつ減って、生命力が尽きるところで止まる
        Assert.LessOrEqual((r.Hits - 1) * r.EnemyDamage, before);
        Assert.GreaterOrEqual(r.Hits * r.EnemyDamage, before);
    }

    [Test]
    public void 倒れたらゲームオーバーの画面が出る()
    {
        var g = NewGameOn(1, new Vector2Int(0, 0));
        g.Player.Hp = 10; g.Player.Attack = 10; g.Player.Defense = 0;
        g.Floor.Clear(new Vector2Int(0, 0));
        g.Floor.Put(new Vector2Int(0, 1), "Enemy_SlimeR");
        g.Step(Vector2Int.up);
        // **すぐには画面を出さない**（作者の指定）。まず倒れる様子を見せる。
        Assert.IsFalse(g.IsGameOver, "斬り合いの最中はまだ画面を出さない");
        Assert.AreEqual(TowerGame.PageKind.Effect, g.CurrentKind);
        Assert.IsTrue(g.ConsumeDeath(), "倒れる様子を見せる番が来ている");
        Assert.AreEqual("Enemy_SlimeR", g.Floor.At(new Vector2Int(0, 1)),
                        "倒す前に死ぬので相手は残る（原典 0x64cd）");
        g.Pump(false);                              // 倒れる様子が終わった
        Assert.IsTrue(g.IsGameOver, "そのあとで ＧＡＭＥ　ＯＶＥＲ");
    }

    [Test]
    public void 戦いの後始末は絵が動いている間は当たらない()
    {
        var g = NewGameOn(1, new Vector2Int(0, 0));
        g.Floor.Clear(new Vector2Int(0, 0));
        g.Floor.Put(new Vector2Int(0, 1), "Enemy_SlimeR");
        g.Step(Vector2Int.up);
        g.Pump(true);                                  // 絵が動いている間
        Assert.AreEqual("Enemy_SlimeR", g.Floor.At(new Vector2Int(0, 1)));
        g.Pump(false);                                 // 動き終わった
        Assert.AreEqual(FloorState.Floor, g.Floor.At(new Vector2Int(0, 1)));
        Assert.AreEqual(new Vector2Int(0, 1), g.Player.Position);
    }

    [Test]
    public void 妖精の称号を覚えて右上に出せる()
    {
        var g = NewGameOn(10, new Vector2Int(0, 2));
        g.Player.Hp = 99999; g.Player.Attack = 999; g.Player.Defense = 999;
        g.Player.YellowKeys = 99; g.Player.PurpleKeys = 99;
        Assert.AreEqual("", g.Player.Rank, "まだ何ももらっていない");
        g.Floor.Put(new Vector2Int(0, 3), "Fairy");
        g.Step(Vector2Int.up);                         // 妖精に話しかける
        CollectionAssert.Contains(Fairy.Ranks, g.Player.Rank, "もらった称号を覚える");
    }

    [Test]
    public void 渡したばかりの動きがある間は手を止める()
    {
        var g = NewGameOn(10, new Vector2Int(0, -1));
        g.Step(Vector2Int.up);                         // 骸骨に囲まれる場面
        Assert.IsTrue(g.HasPendingView, "押し上げの1歩を渡したところ");
        g.ConsumePlayerSlide();
        Assert.IsFalse(g.HasPendingView);
    }

    [Test]
    public void 本文は一秒に十文字ずつ出す()
    {
        Assert.AreEqual(10f, AuthorStyle.TypeCharsPerSecond,
                        "作者の指定（出し終わる前に決定で全部出し、もう一度で送る）");
    }

    [Test]
    public void 妖精は読み終えてから消える()
    {
        var g = NewGameOn(10, new Vector2Int(0, 2));
        g.Player.Hp = 99999; g.Player.Attack = 999; g.Player.Defense = 999;
        g.Floor.Put(new Vector2Int(0, 3), "Fairy");
        g.Step(Vector2Int.up);
        Assert.AreEqual("Fairy", g.Floor.At(new Vector2Int(0, 3)), "話している間は居る");
        while (g.PendingPages > 0)
        {
            if (g.CurrentKind == TowerGame.PageKind.Effect) g.Pump(false);
            else g.NextPage();
        }
        Assert.IsTrue(g.Floor.IsCleared(new Vector2Int(0, 3)), "送り終えたら消える");
    }

    [Test]
    public void 場面の終わりの片付けは終わりに効く()
    {
        // 42階は魔導士4体を出して、最後にその4マスを片付ける。
        // 片付けを「その場で当てる」と場面の頭で効いてしまい、魔導士が残っていた。
        var g = NewGameOn(42, new Vector2Int(0, -4));
        g.Player.Hp = 99999; g.Player.Attack = 999; g.Player.Defense = 999;
        Assert.IsTrue(g.PlayScene("金騎士処刑とゼノ登場"));
        bool sawMage = false;
        for (int i = 0; i < 400 && g.PendingPages > 0; i++)
        {
            if (g.CurrentKind == TowerGame.PageKind.Effect) g.Pump(false);
            else g.NextPage();
            g.ConsumeBlinks();
            if (g.Floor.At(new Vector2Int(0, -1)) == "Enemy_ID93") sawMage = true;
        }
        Assert.IsTrue(sawMage, "途中で魔導士が出る");
        foreach (var at in new[] { new Vector2Int(0, -1), new Vector2Int(0, -3),
                                   new Vector2Int(-1, -2), new Vector2Int(1, -2) })
            Assert.IsTrue(g.Floor.IsCleared(at), $"終わりに片付く {at}");
        // ★作者の指定: ゼノも最後に片付ける（原典 0x7182 は (0,0) を戻していないので、
        //   そのままだと居座る）。
        Assert.IsTrue(g.Floor.IsCleared(new Vector2Int(0, 0)), "ゼノも片付く");
        Assert.IsTrue(g.Floor.IsCleared(new Vector2Int(0, -2)), "金騎士は処刑されて消える");
    }

    [Test]
    public void 倒したあとの解錠はひと呼吸を待たずに効く()
    {
        // 15階のタコ（3x3）は (0,0) を倒すと残り8マスが床になり、真ん中に
        // 仙人のつるはしが出る（解錠 0x60）。**解錠をひと呼吸あとにしていたため**、
        // その間に一歩進めてしまい、まだ消えていないタコともう一度戦えてしまった。
        var p = new PlayerState { Hp = 999990, Attack = 9990, Defense = 9990,
                                  Floor = 15, Position = new Vector2Int(0, -1) };
        var g = new TowerGame(p);
        Assert.AreEqual("Boss_GiantOctopus", g.Floor.At(new Vector2Int(0, 0)));
        g.Step(Vector2Int.up);                       // 踏み込んで戦う
        g.Pump(true, true);                          // 斬り合いの絵が動いている
        Assert.AreEqual("Boss_GiantOctopus", g.Floor.At(new Vector2Int(0, 1)),
                        "動いている間はまだ変わらない");
        // 絵は止まったが「ひと呼吸」の最中（ここで一歩動けてしまう）
        g.Pump(true, false);
        Assert.AreEqual(new Vector2Int(0, 0), g.Player.Position, "相手のマスへ進んでいる");
        Assert.IsTrue(g.Floor.IsCleared(new Vector2Int(0, 2)), "解錠はもう効いている");
        Assert.AreEqual("Matock", g.Floor.At(new Vector2Int(0, 1)),
                        "真ん中はつるはしになっている（もう一度戦わされない）");
    }

    [Test]
    public void 題名の曲は題名を出すときに切り替える()
    {
        // 場面を積んだ時点で鳴らしていたため、3階で踏み込んだ瞬間に曲が変わり、
        // その読み込みで一瞬止まっていた（作者の報告）。
        var p3 = PlayerState.NewGame();
        p3.Floor = 3; p3.Position = new Vector2Int(-1, -2);
        var g = new TowerGame(p3);
        int music = SoundCues.Instance.Number(SoundCues.TitleMusic);
        g.Step(Vector2Int.down);                       // ゼノとの初対面が始まる
        CollectionAssert.DoesNotContain(g.Sounds, music, "積んだ時点では鳴らさない");

        for (int i = 0; i < 200 && g.PendingPages > 0; i++)
        {
            if (g.CurrentKind == TowerGame.PageKind.Title) break;
            if (g.CurrentKind == TowerGame.PageKind.Effect) g.Pump(false);
            else g.NextPage();
        }
        Assert.AreEqual(TowerGame.PageKind.Title, g.CurrentKind, "題名まで来る");
        CollectionAssert.Contains(g.Sounds, music, "題名を出すときに鳴らす");
    }

    [Test]
    public void 奪われたら本編の曲に戻る()
    {
        var p3 = PlayerState.NewGame();
        p3.Floor = 3; p3.Position = new Vector2Int(-1, -2);
        var g = new TowerGame(p3);
        g.Step(Vector2Int.down);
        for (int i = 0; i < 300 && g.PendingPages > 0; i++)
        {
            if (g.CurrentKind == TowerGame.PageKind.Effect) g.Pump(false);
            else g.NextPage();
        }
        Assert.AreEqual(2, g.Player.Floor, "2階の牢まで進む");
        CollectionAssert.Contains(g.Sounds, SoundCues.Instance.Number(SoundCues.FieldMusic),
                                  "本編の曲に戻る（原典 0x7038）");
    }

    [Test]
    public void 一枚絵の間はほかを全部消す()
    {
        foreach (var k in new[] { TowerGame.PageKind.Title, TowerGame.PageKind.Prologue,
                                  TowerGame.PageKind.GameOver, TowerGame.PageKind.FinalScreen,
                                  TowerGame.PageKind.EndingTower, TowerGame.PageKind.EndingText })
            Assert.IsTrue(PhoneLayoutView.IsFullScreen(k), k.ToString());
        Assert.IsFalse(PhoneLayoutView.IsFullScreen(TowerGame.PageKind.Dialogue));
        Assert.IsFalse(PhoneLayoutView.IsFullScreen(TowerGame.PageKind.Effect));
    }

    [Test]
    public void 一枚絵の役は消えても動き続ける()
    {
        // ⚠ この役は1枚絵の入れ物そのものに付いている。入れ物を SetActive(false)
        // していたため、起動して最初の1コマで自分を消し、**二度と出せなくなって**いた。
        // 題名も序章の絵もゲームオーバーの画面も出なかったのはこれが原因。
        var go = new GameObject("FullScreenTest", typeof(RectTransform));
        try
        {
            var view = go.AddComponent<FullScreenView>();
            view.Hide();
            Assert.IsTrue(go.activeSelf, "入れ物は消さない（消すと自分も止まる）");
            Assert.IsFalse(view.IsShowing);
            view.Show(null, "あ", "い");
            Assert.IsTrue(view.IsShowing, "出し直せる");
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void ジャバ版の曲が階のかたまりで切り替わる()
    {
        Assert.AreEqual("Music/Block1", JavaMusic.ForFloor(1));
        Assert.AreEqual("Music/Block1", JavaMusic.ForFloor(10));
        Assert.AreEqual("Music/Block2", JavaMusic.ForFloor(11));
        Assert.AreEqual("Music/Block3", JavaMusic.ForFloor(30));
        Assert.AreEqual("Music/Block4", JavaMusic.ForFloor(40));
        Assert.AreEqual("Music/Block5", JavaMusic.ForFloor(41));
        Assert.AreEqual("Music/Block5", JavaMusic.ForFloor(50));
    }

    [Test]
    public void ボスと戦うときだけ戦いの曲になる()
    {
        Assert.IsTrue(JavaMusic.IsBoss("Enemy_BossSkeleton"), "10階の金の骸骨騎士");
        Assert.IsTrue(JavaMusic.IsBoss("Enemy_ID95"), "20階のヴァンパイア");
        Assert.IsTrue(JavaMusic.IsBoss("Enemy_Worrior"), "32階・40階の金騎士");
        Assert.IsTrue(JavaMusic.IsBoss("Boss_Zeno"), "49・50階のゼノ");
        Assert.IsFalse(JavaMusic.IsBoss("Enemy_SlimeR"), "ふつうの敵は変えない");
    }

    [Test]
    public void 曲の音が全部そろっている()
    {
        for (int i = 1; i <= 5; i++)
            Assert.IsNotNull(Resources.Load<AudioClip>(JavaMusic.ForFloor((i - 1) * 10 + 1)),
                             $"かたまり{i}の曲");
        Assert.IsNotNull(Resources.Load<AudioClip>(JavaMusic.Boss), "戦いの曲");
    }
}
