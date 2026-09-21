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

/// <summary>
/// 持ち物の欄（原典 0x2f8c）。左の帯の下の箱に3列で並べる。
/// </summary>
public class ItemPanelTests
{
    [Test]
    public void 並べ方が原典どおり()
    {
        Assert.AreEqual(new Vector2Int(16, 192), ScreenLayout.ItemPanelOrigin,
                        "0x3c02 = 行192・バイト列2");
        Assert.AreEqual(3, ScreenLayout.ItemPanelColumns);
        Assert.AreEqual(32, ScreenLayout.ItemPanelStep);
        Assert.AreEqual(15, ScreenLayout.ItemPanelSlots, "表は 6バイト×15件");

        Assert.AreEqual(new RectInt(16, 192, 32, 32), ScreenLayout.ItemPanelSlot(0));
        Assert.AreEqual(new RectInt(80, 192, 32, 32), ScreenLayout.ItemPanelSlot(2));
        Assert.AreEqual(new RectInt(16, 224, 32, 32), ScreenLayout.ItemPanelSlot(3),
                        "4つ目で折り返して32ドット下");
        Assert.AreEqual(new RectInt(80, 320, 32, 32), ScreenLayout.ItemPanelSlot(14));
    }

    [Test]
    public void 並ぶ順は物体ID三十六から五十()
    {
        for (int i = 0; i < ScreenLayout.ItemPanelSlots; i++)
            Assert.AreEqual(36 + i, ScreenLayout.ItemPanelObjectId(i));
    }

    [Test]
    public void どのマスも持ち物に結び付いている()
    {
        for (int i = 0; i < ScreenLayout.ItemPanelSlots; i++)
            Assert.AreNotEqual(SpecialItems.Kind.None, SpecialItems.PanelKind(i),
                               $"{i}番目（ID{ScreenLayout.ItemPanelObjectId(i)}）が結び付いていない");
    }

    [Test]
    public void 十五種がすべて別の持ち物()
    {
        var seen = new System.Collections.Generic.HashSet<SpecialItems.Kind>();
        for (int i = 0; i < ScreenLayout.ItemPanelSlots; i++)
            Assert.IsTrue(seen.Add(SpecialItems.PanelKind(i)), $"{i}番目が重複");
        Assert.AreEqual(15, seen.Count);
    }

    [Test]
    public void 欄はぜんぶ左の帯の箱の中にある()
    {
        // 枠の絵の灰色の箱は (13,190) 102x164
        for (int i = 0; i < ScreenLayout.ItemPanelSlots; i++)
        {
            var r = ScreenLayout.ItemPanelSlot(i);
            Assert.GreaterOrEqual(r.xMin, 13);
            Assert.LessOrEqual(r.xMax, 13 + 102);
            Assert.GreaterOrEqual(r.yMin, 190);
            Assert.LessOrEqual(r.yMax, 190 + 164);
        }
    }

    [Test]
    public void シーンに十五マスぶん繋がっている()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Tower.unity");
        var view = Object.FindObjectOfType<TowerHudView>();
        var so = new UnityEditor.SerializedObject(view);
        var slots = so.FindProperty("itemSlots");
        Assert.AreEqual(15, slots.arraySize);
        for (int i = 0; i < slots.arraySize; i++)
        {
            var img = (UnityEngine.UI.Image)slots.GetArrayElementAtIndex(i).objectReferenceValue;
            Assert.IsNotNull(img, $"{i}番目が未設定");
            Assert.IsNotNull(img.sprite, $"{i}番目の絵が無い");
            Assert.IsFalse(img.enabled, "持っていない間は出さない");
        }
    }
}
