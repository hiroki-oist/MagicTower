using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 飛翔のオーブの階選びの画面（作者の指定）。
/// 原典は「今どの階か」を数字1つでしか見せていなかったので、
/// 1階〜50階を並べて飛べる階だけはっきり出す。
/// </summary>
public class FloorPickerTests
{
    static TowerGame Game(int floor, int highest)
    {
        var p = new PlayerState
        {
            Hp = 5000, Attack = 500, Defense = 500,
            Floor = floor, HighestFloor = highest, Position = new Vector2Int(0, 0),
        };
        p.Give(SpecialItems.Kind.FlightOrb);
        var g = new TowerGame(p);
        g.Floor.Put(p.Position + Vector2Int.up, ">");     // 階段の隣に立たせる
        return g;
    }

    [Test]
    public void 五十階ぶんが十かける五で並ぶ()
    {
        Assert.AreEqual(50, FloorPicker.Floors);
        Assert.AreEqual(10, FloorPicker.Columns);
        Assert.AreEqual(5, FloorPicker.Rows);

        var one = FloorPicker.CellOf(1);
        var ten = FloorPicker.CellOf(10);
        var eleven = FloorPicker.CellOf(11);
        Assert.AreEqual(one.y, ten.y, "1〜10階は同じ段");
        Assert.Greater(eleven.y, one.y, "11階は次の段");
        Assert.AreEqual(one.x, eleven.x, "段の頭は同じ列");

        // ますが窓からはみ出さない
        var w = FloorPicker.Window;
        for (int f = 1; f <= FloorPicker.Floors; f++)
        {
            var c = FloorPicker.CellOf(f);
            Assert.GreaterOrEqual(c.xMin, w.xMin, $"{f}階");
            Assert.LessOrEqual(c.xMax, w.xMax, $"{f}階");
            Assert.LessOrEqual(c.yMax, w.yMax, $"{f}階");
        }
        Assert.IsTrue(DialogueWindow.Fits(w), "窓が画面に収まる");
    }

    [Test]
    public void 飛べる階だけはっきり出す()
    {
        var p = new PlayerState { HighestFloor = 12 };
        Assert.IsTrue(FloorPicker.CanGo(p, 1));
        Assert.IsTrue(FloorPicker.CanGo(p, 12));
        Assert.IsFalse(FloorPicker.CanGo(p, 13), "まだ行っていない階");
        Assert.IsFalse(FloorPicker.CanGo(p, 0));
        Assert.IsFalse(FloorPicker.CanGo(p, 51));

        var far = new PlayerState { HighestFloor = 50 };
        Assert.IsFalse(FloorPicker.CanGo(far, 44), "幻の階は飛べない");
        Assert.IsTrue(FloorPicker.CanGo(far, 45));
        Assert.AreNotEqual(FloorPicker.Reachable, FloorPicker.Unreachable, "色を分ける");
    }

    /// <summary>案内から「リターンで」を落としてある（スマホにリターンは無い）。</summary>
    [Test]
    public void 案内にリターンは出てこない()
    {
        var t = TowerMenu.FlightPrompt();
        StringAssert.Contains("決定してください", t);
        StringAssert.DoesNotContain("リターン", t);
        StringAssert.Contains("リターン", GameText.Instance.Message("teleport_menu"),
                              "原典の文言のほうは残してある");
    }

    [Test]
    public void 階段の隣でなければ何も開かない()
    {
        var g = Game(5, 10);
        g.Floor.Clear(g.Player.Position + Vector2Int.up);     // 階段を消す
        Assert.IsFalse(g.BeginFlight());
        Assert.IsFalse(g.IsChoosingFloor, "階選びは開かない");
        Assert.IsFalse(g.IsMenuOpen, "メニューも開かない");
        StringAssert.Contains("階段の隣", g.Log[g.Log.Count - 1]);
    }

    [Test]
    public void 階段の隣なら今の階から選び始める()
    {
        var g = Game(5, 10);
        Assert.IsTrue(g.BeginFlight());
        Assert.IsTrue(g.IsChoosingFloor);
        Assert.IsFalse(g.IsMenuOpen, "メニューとは別の画面");
        Assert.AreEqual(5, g.FlightFloor);
    }

    /// <summary>
    /// ★作者の指定。押した向きへ印がそのまま動く。
    /// 一覧は1段10階で左から右なので、左右が隣の階、上下が1段ぶん。
    /// </summary>
    [Test]
    public void 押した向きへ印が動く()
    {
        var g = Game(5, 40);
        g.BeginFlight();
        g.FlightMove(1);                          // →
        Assert.AreEqual(6, g.FlightFloor, "右は隣の階");
        g.FlightMove(-1);                         // ←
        Assert.AreEqual(5, g.FlightFloor, "左は1つ戻る");
        g.FlightMove(FloorPicker.Columns);        // ↓
        Assert.AreEqual(15, g.FlightFloor, "下は1段ぶん");
        g.FlightMove(-FloorPicker.Columns);       // ↑
        Assert.AreEqual(5, g.FlightFloor, "上は1段戻る");

        // 一覧の上での位置も、ちゃんと1段ぶん動いている
        var a = FloorPicker.CellOf(5);
        var b = FloorPicker.CellOf(15);
        Assert.AreEqual(a.x, b.x, "同じ列");
        Assert.AreEqual(a.y + FloorPicker.RowPitch, b.y, "1段下");

        for (int i = 0; i < 10; i++) g.FlightMove(FloorPicker.Columns);
        Assert.AreEqual(40, g.FlightFloor, "到達最高階で止まる");
        for (int i = 0; i < 10; i++) g.FlightMove(-FloorPicker.Columns);
        Assert.AreEqual(1, g.FlightFloor, "1階で止まる");
    }

    /// <summary>入力の向きと印の動きが食い違っていないか（配線の検査）。</summary>
    [Test]
    public void 入力の向きの割り当てが逆になっていない()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerController.cs");
        StringAssert.Contains("move.y > 0 ? -FloorPicker.Columns : FloorPicker.Columns", src,
                              "上で小さい階、下で大きい階");
        StringAssert.Contains("Game.FlightMove(move.x > 0 ? 1 : -1)", src,
                              "右で次の階、左で前の階");
    }

    [Test]
    public void やめるときは今いる階を選ぶ()
    {
        var g = Game(5, 10);
        g.BeginFlight();
        Assert.IsFalse(g.ConfirmFlight(), "同じ階なら飛ばない");
        Assert.IsFalse(g.IsChoosingFloor, "画面は閉じる");
        Assert.AreEqual(5, g.Player.Floor);
    }

    [Test]
    public void 途中でやめられる()
    {
        var g = Game(5, 10);
        g.BeginFlight();
        g.FlightMove(1);
        g.CancelFlight();
        Assert.IsFalse(g.IsChoosingFloor);
        Assert.AreEqual(5, g.Player.Floor);
    }
}

/// <summary>
/// 縦画面の並べ方。上の帯・盤面・下の帯が**隙間なく、重ならずに**並ぶこと。
/// 枠を下へ伸ばしたとき、カメラの基準（画布の中心）と盤面の基準（枠の中心）が
/// ずれていて、盤面が上の帯に潜り込んでいた（作者の報告）。
/// </summary>
public class PhoneLayoutTests
{
    /// <summary>その画布の高さでの、上の帯・盤面・下の帯の高さ（画布の単位）。</summary>
    static void Lay(float canvasH, out float frameH, out float drop,
                    out float side, out float center)
    {
        float slack = Mathf.Max(0f, canvasH - PhoneScreen.ReferenceHeight) / 2f;
        float extra = slack * PhoneScreen.BottomSlackShare;
        frameH = PhoneScreen.ReferenceHeight + extra;
        drop = extra / 2f;
        side = PhoneScreen.MapSide(PhoneScreen.ReferenceWidth, frameH);
        center = frameH / 2f - (PhoneScreen.TopHeight + side / 2f);
    }

    [TestCase(1920f)]              // 9:16
    [TestCase(2133f)]              // 9:18
    [TestCase(2348f)]              // iPhone 17（9:19.5 くらい）
    [TestCase(2600f)]              // もっと縦長
    public void 上の帯と盤面と下の帯が隙間なく並ぶ(float canvasH)
    {
        float frameH, drop, side, center;
        Lay(canvasH, out frameH, out drop, out side, out center);

        float top = frameH / 2f - center - side / 2f;
        float bottom = frameH / 2f + center - side / 2f;
        Assert.AreEqual(PhoneScreen.TopHeight, top, 0.01f, "上の帯は原典の欄のまま");
        Assert.GreaterOrEqual(bottom, PhoneScreen.BottomHeight - 0.01f,
                              "下の帯は 9:16 のときより狭くならない");
        Assert.AreEqual(frameH, top + side + bottom, 0.01f, "隙間も重なりも無い");
        Assert.LessOrEqual(frameH + drop * 0f, canvasH + 0.01f, "画布に収まる");
    }

    /// <summary>
    /// カメラの基準は**画布の中心**。枠を下げたぶんを引かないと、盤面が上の帯に潜る。
    /// </summary>
    [Test]
    public void カメラの基準は画布の中心にそろえる()
    {
        float frameH, drop, side, center;
        Lay(2348f, out frameH, out drop, out side, out center);

        // 画布の上端から見た、上の帯の下端と盤面の上端がぴったり合うこと
        float frameTop = (2348f - frameH) / 2f + drop;
        float bandBottom = frameTop + PhoneScreen.TopHeight;

        float perUnit = PhoneScreen.MapCells / side;
        float camY = -(center - drop) * perUnit;              // 直したあとの式
        float unitsPerWorld = side / PhoneScreen.MapCells;
        float boardTop = 2348f / 2f
                       - (PhoneScreen.MapCells / 2f + camY / perUnit * perUnit) * 0f
                       - ((PhoneScreen.MapCells / 2f) - camY) * unitsPerWorld;
        Assert.AreEqual(bandBottom, boardTop, 0.5f, "帯の下端と盤面の上端が合う");

        // 直す前の式（枠のずれを引かない）だと、ちょうど drop ぶん上にずれる
        float camYOld = -center * perUnit;
        float boardTopOld = 2348f / 2f
                          - ((PhoneScreen.MapCells / 2f) - camYOld) * unitsPerWorld;
        Assert.AreEqual(bandBottom - drop, boardTopOld, 0.5f, "以前はここがずれていた");
    }

    [Test]
    public void 下に空いたぶんの半分だけ使う()
    {
        Assert.AreEqual(0.5f, PhoneScreen.BottomSlackShare, 1e-4f);
        float frameH, drop, side, center;
        Lay(2348f, out frameH, out drop, out side, out center);
        float frameTop = (2348f - frameH) / 2f + drop;
        Assert.AreEqual((2348f - PhoneScreen.ReferenceHeight) / 2f, frameTop, 0.01f,
                        "上の余白は 1080x1920 を中央に置いたときのまま");
    }
}
