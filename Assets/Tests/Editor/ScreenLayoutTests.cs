using NUnit.Framework;
using UnityEngine;

/// <summary>画面の組み立てが原典（MTE.EXE 0x1b6d）どおりかを確かめる。</summary>
public class ScreenLayoutTests
{
    [Test]
    public void 盤面は左右中央で左上が百四十四と二十四()
    {
        Assert.AreEqual(144, ScreenLayout.MapLeft);
        Assert.AreEqual(24, ScreenLayout.MapTop);
        Assert.AreEqual(352, ScreenLayout.MapWidth);
        Assert.IsTrue(ScreenLayout.MapIsCentered, "144 + 352 + 144 = 640");
    }

    [Test]
    public void 原典の式のとおりに位置が出る()
    {
        // 横 = (X*4 + 18) * 8 、 縦 = Y*32 + 24
        Assert.AreEqual(new Vector2Int(144, 24), ScreenLayout.FromOriginalXY(0, 0));
        Assert.AreEqual(new Vector2Int(176, 56), ScreenLayout.FromOriginalXY(1, 1));
        Assert.AreEqual(new Vector2Int(464, 344), ScreenLayout.FromOriginalXY(10, 10));
    }

    [Test]
    public void 盤面の座標からも同じ位置になる()
    {
        Assert.AreEqual(ScreenLayout.FromOriginalXY(0, 0),
                        ScreenLayout.FromCell(new Vector2Int(-5, 5)), "左上");
        Assert.AreEqual(ScreenLayout.FromOriginalXY(5, 5),
                        ScreenLayout.FromCell(new Vector2Int(0, 0)), "中央");
        Assert.AreEqual(ScreenLayout.FromOriginalXY(10, 10),
                        ScreenLayout.FromCell(new Vector2Int(5, -5)), "右下");
    }

    [Test]
    public void 盤面は画面からはみ出さない()
    {
        var br = ScreenLayout.FromOriginalXY(10, 10);
        Assert.LessOrEqual(br.x + ScreenLayout.TileSize, ScreenLayout.ScreenWidth);
        Assert.LessOrEqual(br.y + ScreenLayout.TileSize, ScreenLayout.ScreenHeight);
        Assert.AreEqual(24, ScreenLayout.ScreenHeight - (br.y + ScreenLayout.TileSize),
                        "下の余白も24で上下も同じ");
    }

    [Test]
    public void VRAM変位が原典の式と一致する()
    {
        // 0x50 * (Y*32 + 0x18) + (X*4 + 0x12)
        Assert.AreEqual(0x50 * 24 + 0x12, ScreenLayout.VramOffset(0, 0));
        Assert.AreEqual(0x50 * (10 * 32 + 24) + (10 * 4 + 18), ScreenLayout.VramOffset(10, 10));
    }

    [Test]
    public void 会話ウィンドウは盤面の上に重なる()
    {
        var w = DialogueWindow.For(6);
        Assert.AreEqual(160, w.xMin);
        Assert.GreaterOrEqual(w.xMin, ScreenLayout.MapLeft, "盤面の内側に開く");
        Assert.LessOrEqual(w.xMax, ScreenLayout.MapLeft + ScreenLayout.MapWidth + 144);
        Assert.AreEqual(ScreenLayout.ScreenWidth, DialogueWindow.ScreenWidth);
    }

    [Test]
    public void カメラの大きさが原典の画面と一致する()
    {
        Assert.AreEqual(6.25f, ScreenLayout.CameraSize, 1e-6f, "400 / 32 / 2");
        Assert.AreEqual(1.6f, ScreenLayout.Aspect, 1e-6f, "640 / 400");
        Assert.AreEqual(20f, ScreenLayout.CameraSize * ScreenLayout.Aspect * 2f, 1e-5f,
                        "横は20マスぶん");
    }

    [Test]
    public void 盤面の四隅が原典の画面位置とぴったり重なる()
    {
        // 一番左上のマス(-5,+5) の左上角
        var w = ScreenLayout.ScreenToWorld(new Vector2(ScreenLayout.MapLeft, ScreenLayout.MapTop));
        Assert.AreEqual(-5.5f, w.x, 1e-5f, "マス(-5)の左端");
        Assert.AreEqual(5.5f, w.y, 1e-5f, "マス(+5)の上端");

        // 一番右下のマス(+5,-5) の右下角
        var br = ScreenLayout.ScreenToWorld(new Vector2(
            ScreenLayout.MapLeft + ScreenLayout.MapWidth,
            ScreenLayout.MapTop + ScreenLayout.MapHeight));
        Assert.AreEqual(5.5f, br.x, 1e-5f);
        Assert.AreEqual(-5.5f, br.y, 1e-5f);
    }

    [Test]
    public void 画面の四隅もワールドの端に対応する()
    {
        var tl = ScreenLayout.ScreenToWorld(Vector2.zero);
        Assert.AreEqual(-10f, tl.x, 1e-5f);
        Assert.AreEqual(6.25f, tl.y, 1e-5f);
        var brc = ScreenLayout.ScreenToWorld(
            new Vector2(ScreenLayout.ScreenWidth, ScreenLayout.ScreenHeight));
        Assert.AreEqual(10f, brc.x, 1e-5f);
        Assert.AreEqual(-6.25f, brc.y, 1e-5f);
    }
}
