using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 下の「セーブ」「ロード」のボタン（作者の指定）。
/// 原典はメニューから8つのスロットを選ばせるが、こちらはスロット1つだけにして
/// その場で「はい／いいえ」を聞く。メニューの中は原典どおり8つのまま。
/// </summary>
public class QuickSaveTests
{
    string dir;

    [SetUp]
    public void Setup()
    {
        dir = System.IO.Path.Combine(Application.temporaryCachePath,
                                     "quicksave_" + System.Guid.NewGuid());
        TowerSave.Directory = dir;
    }

    [TearDown]
    public void Teardown()
    {
        TowerSave.Directory = null;
        if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
    }

    static TowerGame Game(int floor, Vector2Int at)
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 30, Defense = 20, Gold = 40,
            Floor = floor, HighestFloor = floor, Position = at
        };
        return new TowerGame(p);
    }

    [Test]
    public void セーブを押すと聞かれてハイで残る()
    {
        var g = Game(3, new Vector2Int(0, -5));
        Assert.IsTrue(g.AskSave());
        Assert.IsTrue(g.IsChoosing, "はい／いいえを聞く");
        StringAssert.Contains("セーブしますか？", g.CurrentPage);
        Assert.AreEqual(2, g.ChoiceCount);

        g.ConfirmChoice(); g.NextPage();                                   // 0 番＝はい
        Assert.IsTrue(TowerSave.Exists(PhoneScreen.QuickSlot), "残っている");
        Assert.AreEqual(3, TowerSave.Load(PhoneScreen.QuickSlot).Floor);
    }

    [Test]
    public void いいえなら何も残らない()
    {
        var g = Game(3, new Vector2Int(0, -5));
        g.AskSave();
        g.MoveChoice(1);                                     // 1 番＝いいえ
        g.ConfirmChoice(); g.NextPage();
        Assert.IsFalse(TowerSave.Exists(PhoneScreen.QuickSlot));
    }

    [Test]
    public void ロードすると階も盤面も戻る()
    {
        var g = Game(5, new Vector2Int(0, -5));
        g.Floor.Clear(new Vector2Int(0, -4));                // 倒したことにする
        g.AskSave();
        g.ConfirmChoice(); g.NextPage();

        // 別の遊びで読み込む
        var h = Game(1, new Vector2Int(0, -5));
        Assert.IsTrue(h.AskLoad());
        StringAssert.Contains("ロードしますか？", h.CurrentPage);
        h.ConfirmChoice(); h.NextPage();

        Assert.AreEqual(5, h.Player.Floor);
        Assert.IsTrue(h.Floor.IsCleared(new Vector2Int(0, -4)), "盤面も戻る");
    }

    /// <summary>
    /// **同じ階でロードしても盤面を作り直す**。ロードは今の盤面を捨てて組み直すので、
    /// ふだんの差分（ConsumeDirty）には1件も残らない。階が変わったときにしか
    /// 作り直していなかったため、倒した敵や拾った物が画面に戻らなかった（作者の報告）。
    /// </summary>
    [Test]
    public void 同じ階でロードしても作り直しの印が立つ()
    {
        var g = Game(5, new Vector2Int(0, -5));
        var at = new Vector2Int(0, 4);              // 黄色の鍵が置いてあるマス
        string wasThere = g.Floor.At(at);
        g.AskSave();
        g.ConfirmChoice(); g.NextPage();
        Assert.IsFalse(g.ConsumeViewRebuild(), "セーブでは作り直さない");

        g.Floor.Clear(at);                       // セーブのあとに倒した／拾った
        g.Floor.ConsumeDirty();                  // 見た目へはもう映した

        Assert.IsTrue(g.AskLoad());
        g.ConfirmChoice(); g.NextPage();
        Assert.AreEqual(wasThere, g.Floor.At(at), "中身は戻っている");
        Assert.AreEqual(0, g.Floor.ConsumeDirty().Count,
                        "差分では気づけない（盤面ごと組み直しているので）");
        Assert.IsTrue(g.ConsumeViewRebuild(), "だから作り直しの印で知らせる");
        Assert.IsFalse(g.ConsumeViewRebuild(), "印は1回で下りる");
    }

    [Test]
    public void データが無いときは原典の文言で断る()
    {
        var g = Game(1, new Vector2Int(0, -5));
        g.AskLoad();
        g.ConfirmChoice(); g.NextPage();
        StringAssert.Contains("セーブデータがありません。", string.Join("\n", g.Log));
    }

    [Test]
    public void セリフが出ている間は聞かない()
    {
        var g = Game(1, new Vector2Int(0, -5));
        g.ShowHeroOrb();
        Assert.IsFalse(g.AskSave());
        Assert.IsFalse(g.AskLoad());
    }

    [Test]
    public void 下のボタンの並びはセーブとロードが先頭()
    {
        Assert.AreEqual("セーブ", PhoneScreen.LeftButtons[PhoneScreen.SaveButtonIndex]);
        Assert.AreEqual("ロード", PhoneScreen.LeftButtons[PhoneScreen.LoadButtonIndex]);
    }
}
