using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 鍵の絵の並べ方（原典 0x9ff8）。数字ではなく絵を本数ぶん出す。
/// </summary>
public class KeyIconTests
{
    [Test]
    public void 一本目は原典と同じ位置に出る()
    {
        var icons = ScreenLayout.KeyIcons(1, 0, 0);
        Assert.AreEqual(1, icons.Count);
        Assert.AreEqual(ScreenLayout.KeyColor.Red, icons[0].Color);
        // 変位 0x2d00 + 0x42 = 行144・バイト列66
        Assert.AreEqual(new RectInt(528, 144, 16, 16), icons[0].Rect);
    }

    [Test]
    public void 十六ドットずつ右に並ぶ()
    {
        var icons = ScreenLayout.KeyIcons(3, 0, 0);
        Assert.AreEqual(528, icons[0].Rect.xMin);
        Assert.AreEqual(544, icons[1].Rect.xMin);
        Assert.AreEqual(560, icons[2].Rect.xMin);
        foreach (var i in icons) Assert.AreEqual(144, i.Rect.yMin);
    }

    [Test]
    public void 六本で折り返して十六ドット下がる()
    {
        var icons = ScreenLayout.KeyIcons(7, 0, 0);
        Assert.AreEqual(144, icons[5].Rect.yMin);
        Assert.AreEqual(new RectInt(528, 160, 16, 16), icons[6].Rect);
    }

    [Test]
    public void 色は赤紫黄の順で位置は続けて詰める()
    {
        // 赤2・紫1・黄3 → 6本が1行に並ぶ
        var icons = ScreenLayout.KeyIcons(2, 1, 3);
        Assert.AreEqual(6, icons.Count);
        var order = new[] { ScreenLayout.KeyColor.Red, ScreenLayout.KeyColor.Red,
                            ScreenLayout.KeyColor.Purple,
                            ScreenLayout.KeyColor.Yellow, ScreenLayout.KeyColor.Yellow,
                            ScreenLayout.KeyColor.Yellow };
        for (int i = 0; i < order.Length; i++)
        {
            Assert.AreEqual(order[i], icons[i].Color, $"{i}本目の色");
            Assert.AreEqual(528 + i * 16, icons[i].Rect.xMin, $"{i}本目の位置");
            Assert.AreEqual(144, icons[i].Rect.yMin);
        }
    }

    [Test]
    public void 二十四本で打ち切る()
    {
        var icons = ScreenLayout.KeyIcons(20, 20, 20);
        Assert.AreEqual(24, icons.Count);
        Assert.AreEqual(new RectInt(528 + 5 * 16, 144 + 3 * 16, 16, 16), icons[23].Rect);
    }

    [Test]
    public void 鍵の絵が三色ぶん書き出してある()
    {
        foreach (var n in new[] { "Key_Red", "Key_Purple", "Key_Yellow" })
        {
            var path = $"Assets/Sprites/Hud/{n}.png";
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.IsNotNull(s, $"{path} が無い");
            Assert.AreEqual(16, (int)s.rect.width, $"{n} の幅");
            Assert.AreEqual(16, (int)s.rect.height, $"{n} の高さ");
        }
    }
}

/// <summary>
/// 装備の絵（原典 0x9b80 付近）。剣は物体ID 50+段位、盾は 55+段位 の絵を貼る。
/// </summary>
public class EquipIconTests
{
    [Test]
    public void 装備の欄は原典の座標にある()
    {
        // 塗り潰し 0xf4a = 行48・バイト列74、2ワード×32行
        Assert.AreEqual(new RectInt(592, 48, 32, 32), ScreenLayout.WeaponIcon);
        // 0x1e4a = 行96・バイト列74
        Assert.AreEqual(new RectInt(592, 96, 32, 32), ScreenLayout.ShieldIcon);
        // 名前の欄 0x1442 = 行64・バイト列66、4ワード×16行
        Assert.AreEqual(new RectInt(528, 64, 64, 16), ScreenLayout.WeaponBox);
        // 0x2342 = 行112・バイト列66
        Assert.AreEqual(new RectInt(528, 112, 64, 16), ScreenLayout.ShieldBox);
    }

    [Test]
    public void 剣と盾の物体IDが原典の表と合う()
    {
        // 属性表の +0x3b2 / +0x3d0 は、表の頭 0x282 から
        // 6バイト×50+4 / 6バイト×55+4 ＝ 物体ID 50/55 の「絵の位置」欄
        Assert.AreEqual(0x282 + 6 * 50 + 4, 0x3b2);
        Assert.AreEqual(0x282 + 6 * 55 + 4, 0x3d0);

        var ids = ObjectIds.Instance;
        for (int rank = 1; rank <= 5; rank++)
        {
            Assert.AreEqual(50 + rank, ids.Of("Sword_" + rank),  $"剣{rank}の物体ID");
            Assert.AreEqual(55 + rank, ids.Of("Shield_" + rank), $"盾{rank}の物体ID");
        }
    }
}
