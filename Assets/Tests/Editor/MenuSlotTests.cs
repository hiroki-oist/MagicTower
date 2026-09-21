using NUnit.Framework;
using UnityEngine;

/// <summary>セーブ／ロードのスロット選択が原典（MTE.EXE 0x0d60〜0x0e19）どおりかを確かめる。</summary>
public class MenuSlotTests
{
    static TowerGame Open()
    {
        var p = new PlayerState
        {
            Hp = 1234, Attack = 56, Defense = 78, Gold = 90,
            Floor = 1, Position = new Vector2Int(0, -5)
        };
        var g = new TowerGame(p);
        g.OpenMenu();
        return g;
    }

    [Test]
    public void スロットは八つでセリフは五十と五十一()
    {
        Assert.AreEqual(8, TowerGame.SlotCount, "原典も選ぶ処理に 8 を渡す");
        Assert.AreEqual(50, TowerGame.LoadRecord);
        Assert.AreEqual(51, TowerGame.SaveRecord);
        Assert.AreEqual(10, TowerGame.SlotWindowLines, "原典 0xa");
        Assert.AreEqual(50, TowerGame.SlotWindowTop, "原典 0x32");
    }

    [Test]
    public void セリフ五十は問いかけと八つの並び()
    {
        var body = GameText.Instance.Dialogue(50).Value.Body;
        var lines = body.Split('\n');
        StringAssert.Contains("どのデータをロードしますか？", lines[0]);
        Assert.AreEqual(10, lines.Length, "問いかけ1行＋データ1〜8＋やっぱりやめる");
        Assert.AreEqual("　　やっぱりやめる", lines[9]);
        Assert.AreEqual(TowerGame.SlotWindowLines, lines.Length, "10行の窓にちょうど収まる");
    }

    [Test]
    public void ロードを選ぶとスロットの段へ移る()
    {
        var g = Open();
        int i = g.MenuEntries.IndexOf(TowerMenu.Entry.Load);
        g.MenuMove(i);
        g.MenuConfirm();
        Assert.AreEqual(TowerGame.MenuStage.LoadSlots, g.Stage);
        StringAssert.Contains("ロードしますか", g.MenuStageText);
    }

    [Test]
    public void スロットは上下に動いて端で止まる()
    {
        var g = Open();
        g.MenuMove(g.MenuEntries.IndexOf(TowerMenu.Entry.Save));
        g.MenuConfirm();
        Assert.AreEqual(0, g.SlotIndex);
        g.SlotMove(-1);
        Assert.AreEqual(0, g.SlotIndex);
        for (int i = 0; i < 20; i++) g.SlotMove(1);
        Assert.AreEqual(TowerGame.SlotCancelIndex, g.SlotIndex, "一番下は「やっぱりやめる」");
    }

    [Test]
    public void やめると一つ前の段に戻る()
    {
        var g = Open();
        g.MenuMove(g.MenuEntries.IndexOf(TowerMenu.Entry.Load));
        g.MenuConfirm();
        Assert.AreEqual(TowerGame.MenuStage.LoadSlots, g.Stage);
        g.MenuBack();
        Assert.AreEqual(TowerGame.MenuStage.Root, g.Stage);
        Assert.IsTrue(g.IsMenuOpen, "メニュー自体はまだ開いている");
        g.MenuBack();
        Assert.IsFalse(g.IsMenuOpen);
    }

    [Test]
    public void 保存して読み直すと状態が戻る()
    {
        var dir = System.IO.Path.Combine(Application.temporaryCachePath, "slottest");
        if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        TowerSave.Directory = dir;
        try
        {
            var g = Open();
            g.MenuMove(g.MenuEntries.IndexOf(TowerMenu.Entry.Save));
            g.MenuConfirm();
            g.SlotMove(2);                       // データ3
            Assert.IsTrue(g.ConfirmSlot());
            Assert.AreEqual(TowerGame.MenuStage.Root, g.Stage);

            g.Player.Hp = 1;                     // わざと壊す

            g.MenuMove(g.MenuEntries.IndexOf(TowerMenu.Entry.Load) - g.MenuIndex);
            g.MenuConfirm();
            g.SlotMove(2);
            Assert.IsTrue(g.ConfirmSlot());
            Assert.AreEqual(1234, g.Player.Hp, "保存した値に戻る");
            Assert.IsFalse(g.IsMenuOpen, "読み込んだらメニューは閉じる");
        }
        finally { TowerSave.Directory = null; }
    }

    [Test]
    public void 空のスロットを読んでも壊れない()
    {
        var dir = System.IO.Path.Combine(Application.temporaryCachePath, "slotempty");
        if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        TowerSave.Directory = dir;
        try
        {
            var g = Open();
            g.MenuMove(g.MenuEntries.IndexOf(TowerMenu.Entry.Load));
            g.MenuConfirm();
            Assert.IsFalse(g.ConfirmSlot(), "空なので読めない");
            Assert.AreEqual(TowerGame.MenuStage.Root, g.Stage);
            Assert.AreEqual(1234, g.Player.Hp, "状態はそのまま");
        }
        finally { TowerSave.Directory = null; }
    }

    [Test]
    public void やっぱりやめるを選ぶと何も起きない()
    {
        var dir = System.IO.Path.Combine(Application.temporaryCachePath, "slotcancel");
        if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        TowerSave.Directory = dir;
        try
        {
            var g = Open();
            g.MenuMove(g.MenuEntries.IndexOf(TowerMenu.Entry.Save));
            g.MenuConfirm();
            g.SlotMove(TowerGame.SlotCancelIndex);
            Assert.AreEqual(8, g.SlotIndex);
            Assert.IsFalse(g.ConfirmSlot());
            Assert.AreEqual(TowerGame.MenuStage.Root, g.Stage);
            Assert.IsFalse(TowerSave.Exists(1), "何も書かれない");
        }
        finally { TowerSave.Directory = null; }
    }
}
