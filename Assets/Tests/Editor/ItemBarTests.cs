using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 持ち物の帯を触ったとき（作者の指定。原典にこの入口は無い）。
///   ・使い切りの道具は「〜を使いますか？」と聞いてから使う
///   ・**勇者のオーブだけは聞かずに一覧**を出す（右の「勇者」のボタンと同じ）
///   ・持っているだけで効くもの（十字架など）は何も起きない
/// </summary>
public class ItemBarTests
{
    static TowerGame Game()
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100,
            Floor = 3, HighestFloor = 50, Position = new Vector2Int(0, 0),
        };
        return new TowerGame(p);
    }

    [Test]
    public void 使い切りの道具は聞いてから使う()
    {
        var g = Game();
        g.Player.Give(SpecialItems.Kind.Elixir);
        int hp = g.Player.Hp;

        Assert.IsTrue(g.TapItem(SpecialItems.Kind.Elixir), "触ると聞いてくる");
        Assert.IsTrue(g.IsChoosing, "はい／いいえ");
        Assert.AreEqual(TowerGame.ChoiceKind.UseItem, g.Choice);
        StringAssert.Contains("エリクサー", g.CurrentPage);
        Assert.IsTrue(g.Player.Has(SpecialItems.Kind.Elixir), "まだ使っていない");

        g.ConfirmChoice();                               // 0番目＝はい
        Assert.IsFalse(g.Player.Has(SpecialItems.Kind.Elixir), "使った");
        Assert.Greater(g.Player.Hp, hp, "生命力が増えた");
    }

    [Test]
    public void いいえなら使わない()
    {
        var g = Game();
        g.Player.Give(SpecialItems.Kind.Elixir);
        g.TapItem(SpecialItems.Kind.Elixir);
        g.MoveChoice(1);                                 // いいえ
        g.ConfirmChoice();
        Assert.IsTrue(g.Player.Has(SpecialItems.Kind.Elixir), "持ったまま");
    }

    [Test]
    public void 勇者のオーブは聞かずに一覧が出る()
    {
        var g = Game();
        g.Player.Give(SpecialItems.Kind.HeroOrb);
        Assert.IsTrue(g.TapItem(SpecialItems.Kind.HeroOrb));
        Assert.IsFalse(g.IsChoosing, "聞かない");
        Assert.AreEqual(TowerGame.PageKind.HeroOrb, g.CurrentKind);
    }

    [Test]
    public void ダメージ表示のボタンでも同じ一覧が出る()
    {
        var g = Game();
        Assert.IsTrue(g.ShowHeroOrb(), "持っていなくてもボタンからは出る");
        Assert.AreEqual(TowerGame.PageKind.HeroOrb, g.CurrentKind);
    }

    [Test]
    public void 持っているだけで効くものは何も起きない()
    {
        var g = Game();
        g.Player.Give(SpecialItems.Kind.Cross);
        Assert.IsFalse(g.TapItem(SpecialItems.Kind.Cross));
        Assert.IsFalse(g.IsShowingPage);
        Assert.IsTrue(g.Player.Has(SpecialItems.Kind.Cross), "持ったまま");
    }

    [Test]
    public void 持っていない道具は触っても何も起きない()
    {
        var g = Game();
        Assert.IsFalse(g.TapItem(SpecialItems.Kind.Matock));
        Assert.IsFalse(g.IsShowingPage);
    }

    [Test]
    public void 窓が出ている間は触れない()
    {
        var g = Game();
        g.Player.Give(SpecialItems.Kind.Elixir);
        g.Speak("なにか");
        Assert.IsFalse(g.TapItem(SpecialItems.Kind.Elixir), "会話中は受けない");
    }

    [Test]
    public void 帯は六つずつ送る()
    {
        Assert.AreEqual(6, PhoneScreen.ItemsPerPage, "★作者の指定で6つずつ（1枠59pt）");
        Assert.AreEqual(15, ScreenLayout.ItemPanelSlots, "並ぶのは原典と同じ15");
        // 1枠の幅。狭すぎると選びづらい（8つだと 119 ＝ iPhone で 44pt しかない）
        float slot = (PhoneScreen.ReferenceWidth - PhoneScreen.ArrowWidth * 2f)
                     / PhoneScreen.ItemsPerPage;
        Assert.Greater(slot, 140f, "1枠が狭すぎない");
        // ★作者の指定で右のボタンはオーブの近道（持っていない間は灰色）
        Assert.AreEqual("飛翔", PhoneScreen.RightButtons[PhoneScreen.FlightButtonIndex]);
        Assert.AreEqual("勇者", PhoneScreen.RightButtons[PhoneScreen.HeroButtonIndex]);
    }

    /// <summary>
    /// 持っていないオーブのボタンを触ると、知らせるだけで何も起きない（作者の指定）。
    /// </summary>
    [Test]
    public void 持っていないオーブのボタンは知らせるだけ()
    {
        var g = Game();
        g.NoteLockedOrb(SpecialItems.Kind.FlightOrb);
        StringAssert.Contains("飛翔のオーブを手に入れると使用可能になります",
                              g.Log[g.Log.Count - 1]);
        Assert.IsFalse(g.IsShowingPage, "窓は開かない");

        g.NoteLockedOrb(SpecialItems.Kind.HeroOrb);
        StringAssert.Contains("勇者のオーブ", g.Log[g.Log.Count - 1]);
    }

    [Test]
    public void オーブの名前は原典のメニューの文言から取る()
    {
        Assert.AreEqual("勇者のオーブ", TowerMenu.OrbName(SpecialItems.Kind.HeroOrb));
        Assert.AreEqual("飛翔のオーブ", TowerMenu.OrbName(SpecialItems.Kind.FlightOrb));
        Assert.AreEqual("知恵のオーブ", TowerMenu.OrbName(SpecialItems.Kind.WisdomOrb));
    }

    [Test]
    public void 道具の名前は原典の一覧の文言から取る()
    {
        Assert.AreEqual("エリクサー", TowerMenu.ItemName(SpecialItems.Kind.Elixir));
        Assert.AreEqual("マトック", TowerMenu.ItemName(SpecialItems.Kind.Matock));
        Assert.AreEqual("", TowerMenu.ItemName(SpecialItems.Kind.Cross), "一覧に出ないもの");
    }
}

/// <summary>デバッグ用の細工（作者の頼み）。原典の値も控えてある。</summary>
public class DebugKeysTests
{
    [Test]
    public void ゼノのあとの鍵は三十本ずつ()
    {
        var p = PlayerState.NewGame();
        p.ApplyZenoEvent();
        int want = DebugPlay.KeysAfterZeno ? DebugPlay.StartKeys : 0;
        Assert.AreEqual(want, p.YellowKeys);
        Assert.AreEqual(want, p.PurpleKeys);
        Assert.AreEqual(want, p.RedKeys);
    }

    [Test]
    public void 原典の値は零本()
    {
        // 細工を外したときに 0 に戻ることを、表の側で押さえておく
        Assert.AreEqual(30, DebugPlay.StartKeys, "作者の頼みは30本ずつ");
        Assert.AreEqual(0, GameStart.Prologue.YellowKeys, "序章は鍵を持たない");
    }
}

/// <summary>
/// 触りで進めるところ（作者の指定。原典はキーボードしか無い）。
/// </summary>
public class TouchControlTests
{
    /// <summary>
    /// ★作者の指定。十字の真ん中は
    ///   ・**決定の丸**（半径の20%まで）… 押して離すと決定
    ///   ・その外の**何も起きない輪**（36%まで）… 矢印を狙って外したときの逃げ場
    ///   ・扇（36%〜100%）… 歩く
    /// の3段になっている。間違えて決定を押してしまう、という報告への手当て。
    /// </summary>
    [Test]
    public void 十字の真ん中は決定の丸と何も起きない輪に分かれる()
    {
        Assert.Less(PhoneScreen.DPadConfirmZone, PhoneScreen.DPadDeadZone,
                    "決定の丸のほうが内側");
        // 真ん中ちょうど＝決定の丸
        Assert.IsTrue(PhoneScreen.InConfirmZone(Vector2.zero, 100f));
        Assert.AreEqual(Vector2Int.zero, PhoneScreen.DirFromTap(Vector2.zero, 100f));
        // 輪の中＝決定でも向きでもない
        var ring = new Vector2(100f * (PhoneScreen.DPadConfirmZone
                                     + PhoneScreen.DPadDeadZone) / 2f, 0f);
        Assert.IsFalse(PhoneScreen.InConfirmZone(ring, 100f), "決定にならない");
        Assert.AreEqual(Vector2Int.zero, PhoneScreen.DirFromTap(ring, 100f), "歩きもしない");
        // 扇＝歩く
        Assert.AreEqual(Vector2Int.right, PhoneScreen.DirFromTap(new Vector2(80f, 0f), 100f));
    }

    /// <summary>決定だけは「押して離す」まで効かない（作者の指定）。</summary>
    [Test]
    public void 決定は指を離すまで効かない()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/UI/DPadView.cs");
        StringAssert.Contains("Input.GetMouseButtonUp(0)", src);
        StringAssert.Contains("pressedConfirm && InConfirm()", src,
                              "押し始めも離すときも決定の丸の中");
    }

    [Test]
    public void 扉が開き切るまでと戦闘の帯が出ている間は動かない()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerController.cs");
        StringAssert.Contains("doorView != null && doorView.IsPlaying", src);
        StringAssert.Contains("band != null && band.IsShowing", src);
    }
}
