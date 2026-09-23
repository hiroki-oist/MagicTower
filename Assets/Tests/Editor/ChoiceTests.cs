using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 「◎はい／◎いいえ」と祭壇の望みを**選べる**こと。原典 `0x9e78`
/// （上下で印を動かし、リターン 0x1c かスペース 0x34 で決める）。
///
/// ここが繋がっていなくて、**商人から何も買えず、祭壇にも寄進できなかった**。
/// 売り口上は出るのに何も起きない、という形だったので遊んでも気づきにくい。
/// </summary>
public class ChoiceTests
{
    static TowerGame Shopper(int floor, Vector2Int at, int gold)
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100, Gold = gold,
            Floor = floor, HighestFloor = floor, Position = at,
        };
        return new TowerGame(p);
    }

    /// <summary>その階の商人に触れる。</summary>
    static TowerGame TalkToMerchantOn(int floor)
    {
        var data = FloorData.Load(floor);
        Assert.IsNotNull(data);
        foreach (var o in data.Objects)
        {
            if (o.Prefab != "Merchant") continue;
            var g = Shopper(floor, o.Pos + Vector2Int.down, 5000);
            g.Floor.Clear(o.Pos + Vector2Int.down);
            g.Step(Vector2Int.up);                     // 触れる（上には乗らない）
            return g;
        }
        Assert.Fail($"F{floor} に商人が居ない");
        return null;
    }

    [Test]
    public void 商人に触れると選ばされる()
    {
        var g = TalkToMerchantOn(7);                   // 7階＝黄の鍵5本 50GOLD
        Assert.IsTrue(g.IsShowingPage, "売り口上が出る");
        Assert.IsTrue(g.IsChoosing, "選ばせている");
        Assert.AreEqual(2, g.ChoiceCount, "はい／いいえ");
        Assert.AreEqual(0, g.ChoiceIndex);
        g.MoveChoice(1);
        Assert.AreEqual(1, g.ChoiceIndex, "下へ動く");
        g.MoveChoice(-5);
        Assert.AreEqual(0, g.ChoiceIndex, "上で止まる");
    }

    [Test]
    public void はいを選ぶと買えて金が減る()
    {
        var g = TalkToMerchantOn(7);
        int gold = g.Player.Gold, keys = g.Player.YellowKeys;
        g.ConfirmChoice();
        Assert.IsFalse(g.IsChoosing);
        Assert.AreEqual(keys + 5, g.Player.YellowKeys, "黄の鍵が5本増える");
        Assert.AreEqual(gold - 50, g.Player.Gold, "50GOLD 払う");
    }

    [Test]
    public void いいえを選ぶと何も起きない()
    {
        var g = TalkToMerchantOn(7);
        int gold = g.Player.Gold, keys = g.Player.YellowKeys;
        g.MoveChoice(1);
        g.ConfirmChoice();
        Assert.AreEqual(keys, g.Player.YellowKeys);
        Assert.AreEqual(gold, g.Player.Gold);
    }

    [Test]
    public void 金が足りなければ買えない()
    {
        var data = FloorData.Load(7);
        foreach (var o in data.Objects)
        {
            if (o.Prefab != "Merchant") continue;
            var g = Shopper(7, o.Pos + Vector2Int.down, 10);
            g.Floor.Clear(o.Pos + Vector2Int.down);
            g.Step(Vector2Int.up);
            g.ConfirmChoice();
            Assert.AreEqual(10, g.Player.Gold, "金は減らない");
            Assert.AreEqual(0, g.Player.YellowKeys, "鍵も増えない");
            return;
        }
        Assert.Fail();
    }

    [Test]
    public void 祭壇は望みを四つから選ばせる()
    {
        var data = FloorData.Load(4);                  // 4階の祭壇
        foreach (var o in data.Objects)
        {
            if (o.Prefab != "Shop_Altar") continue;
            var g = Shopper(4, o.Pos + Vector2Int.down, 5000);
            g.Floor.Clear(o.Pos + Vector2Int.down);
            g.Step(Vector2Int.up);
            // 原典はまず寄進額を言って「寄進するか？」と聞く
            Assert.AreEqual(TowerGame.ChoiceKind.AltarOffer, g.Choice);
            Assert.AreEqual(2, g.ChoiceCount, "はい／いいえ");
            StringAssert.Contains("ＧＯＬＤ", g.CurrentPage, "いくら要るか言う");
            g.ConfirmChoice();                         // はい
            g.NextPage();

            Assert.IsTrue(g.IsChoosing, "望みを選ばせている");
            Assert.AreEqual(4, g.ChoiceCount, "生命力／攻撃力／防御力／やめる");

            int atk = g.Player.Attack, gold = g.Player.Gold;
            g.MoveChoice(1);                           // 攻撃力
            g.ConfirmChoice();
            Assert.Greater(g.Player.Attack, atk, "攻撃力が上がる");
            Assert.Less(g.Player.Gold, gold, "寄進した");
            return;
        }
        Assert.Fail("4階に祭壇が無い");
    }

    [Test]
    public void やめるを選ぶと何も起きない()
    {
        var data = FloorData.Load(4);
        foreach (var o in data.Objects)
        {
            if (o.Prefab != "Shop_Altar") continue;
            var g = Shopper(4, o.Pos + Vector2Int.down, 5000);
            g.Floor.Clear(o.Pos + Vector2Int.down);
            g.Step(Vector2Int.up);
            int atk = g.Player.Attack, gold = g.Player.Gold;
            g.MoveChoice(3);
            g.ConfirmChoice();
            Assert.AreEqual(atk, g.Player.Attack);
            Assert.AreEqual(gold, g.Player.Gold);
            return;
        }
        Assert.Fail();
    }

    [Test]
    public void 入力の側が選ばせに繋がっている()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerController.cs");
        StringAssert.Contains("Game.IsChoosing", src);
        StringAssert.Contains("Game.MoveChoice", src);
        StringAssert.Contains("Game.ConfirmChoice", src);
    }
}

/// <summary>
/// メニューの「スペシャル・アイテムを使う」。原典の一覧は
/// エリクサー／マトック／破壊の玉／空間転移の秘宝／昇華の翼／降臨の翼／魔法の鍵／
/// スーパーマトック／やっぱりやめる の9行（Messages.txt の special_1〜9）。
///
/// ここが繋がっていなくて、**スペシャル・アイテムが一度も使えなかった**。
/// </summary>
public class SpecialItemMenuTests
{
    static TowerGame WithItems(SpecialItems.Kind items)
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100, Floor = 5,
            Position = new Vector2Int(0, 0), HighestFloor = 10,
        };
        p.Give(items);
        return new TowerGame(p);
    }

    [Test]
    public void 一覧は原典の並び順()
    {
        CollectionAssert.AreEqual(new[]
        {
            SpecialItems.Kind.Elixir, SpecialItems.Kind.Matock,
            SpecialItems.Kind.DestructionOrb, SpecialItems.Kind.TeleportRelic,
            SpecialItems.Kind.WingUp, SpecialItems.Kind.WingDown,
            SpecialItems.Kind.MagicKey, SpecialItems.Kind.SuperMatock,
        }, TowerMenu.Usable);
        Assert.AreEqual(9, TowerMenu.ItemCount, "8つ＋やっぱりやめる");
        var text = TowerMenu.ItemText().Split('\n');
        Assert.AreEqual(10, text.Length, "見出し＋9行");
        StringAssert.Contains("エリクサー", text[1]);
        StringAssert.Contains("やっぱりやめる", text[9]);
    }

    /// <summary>
    /// ★作者の指定でメニューからは道具の一覧を外した（持ち物の帯から直に使う）。
    /// 一覧の文言そのもの（原典 0x7d50 の並び）は上の検査で見ている。
    /// </summary>
    [Test]
    public void 持ち物の帯から使うと効く()
    {
        var g = WithItems(SpecialItems.Kind.Elixir);
        g.Player.Hp = 100;
        Assert.IsTrue(g.TapItem(SpecialItems.Kind.Elixir), "「使いますか？」と聞く");
        Assert.IsTrue(g.IsChoosing);
        g.ConfirmChoice();                        // ◎はい
        Assert.Greater(g.Player.Hp, 100, "生命力が増える");
        Assert.IsFalse(g.Player.Has(SpecialItems.Kind.Elixir), "使い切り");
    }

    [Test]
    public void いいえなら使わない()
    {
        var g = WithItems(SpecialItems.Kind.Elixir);
        g.Player.Hp = 100;
        g.TapItem(SpecialItems.Kind.Elixir);
        g.MoveChoice(1);                          // ◎いいえ
        g.ConfirmChoice();
        Assert.AreEqual(100, g.Player.Hp);
        Assert.IsTrue(g.Player.Has(SpecialItems.Kind.Elixir), "持ったまま");
    }

    [Test]
    public void 持っていないものは触れない()
    {
        var g = WithItems(SpecialItems.Kind.Elixir);
        Assert.IsFalse(g.TapItem(SpecialItems.Kind.Matock));
    }
}

/// <summary>
/// メニューの「ゲーム終了」。原典はここで DOS へ戻る。
/// 本線は印を立てるだけにして、実際に閉じるのは入力の側。
/// </summary>
public class QuitMenuTests
{
    [Test]
    public void ゲーム終了を選ぶと終わりたいと言う()
    {
        var g = new TowerGame(new PlayerState
        {
            Hp = 1000, Attack = 10, Defense = 10, Floor = 2,
            Position = new Vector2Int(0, 0),
        });
        g.OpenMenu();
        Assert.IsFalse(g.Quitting, "開いただけでは終わらない");
        while (g.MenuCurrent != TowerMenu.Entry.Quit) g.MenuMove(1);
        g.MenuConfirm();
        Assert.IsTrue(g.Quitting, "終わりたいと言う");
        Assert.IsFalse(g.IsMenuOpen, "メニューは閉じる");
    }

    [Test]
    public void 入力の側が終わりを受け取る()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerController.cs");
        StringAssert.Contains("if (Game.Quitting) QuitGame();", src);
        StringAssert.Contains("Application.Quit()", src);
    }

    [Test]
    public void メニューの項目に空の枝が無い()
    {
        // 「出しているのに何も起きない」項目を作らない。
        // 商人・祭壇・アイテム・終了は、この形で3つ見落としていた。
        // ★今メニューに出しているのは「ゲームに戻る」「ゲーム終了」の2つだけなので、
        //   見るのもその2つ（残りの項目は文言だけ原典から残してある）。
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerGame.cs");
        foreach (var e in TowerMenu.Entries(new PlayerState()))
            StringAssert.Contains($"TowerMenu.Entry.{e}", src, $"{e} の枝が無い");
    }
}

/// <summary>選ばせている窓を決めずに閉じたときの後始末。</summary>
public class ChoiceCleanupTests
{
    [Test]
    public void 決めずに閉じたらいいえと同じ()
    {
        var data = FloorData.Load(7);
        foreach (var o in data.Objects)
        {
            if (o.Prefab != "Merchant") continue;
            var p = new PlayerState
            {
                Hp = 1000, Attack = 100, Defense = 100, Gold = 5000,
                Floor = 7, HighestFloor = 7, Position = o.Pos + Vector2Int.down,
            };
            var g = new TowerGame(p);
            g.Floor.Clear(o.Pos + Vector2Int.down);
            g.Step(Vector2Int.up);
            Assert.IsTrue(g.IsChoosing);

            g.NextPage();                       // 決めずに送る
            Assert.IsFalse(g.IsChoosing, "印が残らない");
            Assert.AreEqual(5000, g.Player.Gold, "買っていない");
            return;
        }
        Assert.Fail("7階に商人が居ない");
    }

    [Test]
    public void 通しの自動プレイは聞かれたら答える()
    {
        // 答えないと商人からも祭壇からも何も得られないまま進んでしまう
        var src = System.IO.File.ReadAllText("Assets/Editor/TowerPlaythrough.cs");
        StringAssert.Contains("game.IsChoosing", src);
        StringAssert.Contains("game.ConfirmChoice()", src);
    }
}
