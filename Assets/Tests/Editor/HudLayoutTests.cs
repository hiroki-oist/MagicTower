using NUnit.Framework;
using UnityEngine;

/// <summary>左右の帯の位置が原典（MTE.EXE 0xb96c ほか）どおりかを確かめる。</summary>
public class HudLayoutTests
{
    [Test]
    public void 盤面の領域が原典の塗り潰しと一致する()
    {
        Assert.AreEqual(new RectInt(144, 24, 352, 352), ScreenLayout.MapArea);
    }

    [Test]
    public void 左の帯は縦二十四おきに並ぶ()
    {
        Assert.AreEqual(48, ScreenLayout.FloorBox.yMin);
        Assert.AreEqual(80, ScreenLayout.HpBox.yMin);
        Assert.AreEqual(104, ScreenLayout.AttackBox.yMin);
        Assert.AreEqual(128, ScreenLayout.DefenseBox.yMin);
        Assert.AreEqual(152, ScreenLayout.GoldBox.yMin);

        Assert.AreEqual(24, ScreenLayout.AttackBox.yMin - ScreenLayout.HpBox.yMin);
        Assert.AreEqual(24, ScreenLayout.DefenseBox.yMin - ScreenLayout.AttackBox.yMin);
        Assert.AreEqual(24, ScreenLayout.GoldBox.yMin - ScreenLayout.DefenseBox.yMin);
    }

    [Test]
    public void 左の帯は盤面より左にある()
    {
        foreach (var r in new[] { ScreenLayout.FloorBox, ScreenLayout.HpBox,
                                  ScreenLayout.AttackBox, ScreenLayout.DefenseBox,
                                  ScreenLayout.GoldBox })
            Assert.LessOrEqual(r.xMax, ScreenLayout.MapLeft, "盤面(144)にかからない");
    }

    [Test]
    public void 右の帯は盤面より右にある()
    {
        foreach (var r in new[] { ScreenLayout.WeaponIcon, ScreenLayout.WeaponBox,
                                  ScreenLayout.ShieldIcon, ScreenLayout.ShieldBox,
                                  ScreenLayout.MessageBox, ScreenLayout.MonsterHpBox })
            Assert.GreaterOrEqual(r.xMin, ScreenLayout.MapLeft + ScreenLayout.MapWidth,
                                  "盤面(496まで)にかからない");
    }

    [Test]
    public void 装備の絵は三十二角で画面に収まる()
    {
        Assert.AreEqual(32, ScreenLayout.WeaponIcon.width);
        Assert.AreEqual(32, ScreenLayout.WeaponIcon.height);
        Assert.LessOrEqual(ScreenLayout.WeaponIcon.xMax, ScreenLayout.ScreenWidth);
        Assert.LessOrEqual(ScreenLayout.ShieldIcon.xMax, ScreenLayout.ScreenWidth);
        Assert.AreEqual(48, ScreenLayout.ShieldIcon.yMin - ScreenLayout.WeaponIcon.yMin);
    }

    [Test]
    public void どの欄も画面に収まる()
    {
        foreach (var r in new[] { ScreenLayout.MapArea, ScreenLayout.FloorBox,
                                  ScreenLayout.HpBox, ScreenLayout.GoldBox,
                                  ScreenLayout.WeaponIcon, ScreenLayout.ShieldIcon,
                                  ScreenLayout.MessageBox, ScreenLayout.MonsterHpBox,
                                  ScreenLayout.MonsterIcon, ScreenLayout.MonsterName,
                                  ScreenLayout.MonsterDefenseBox })
        {
            Assert.GreaterOrEqual(r.xMin, 0);
            Assert.GreaterOrEqual(r.yMin, 0);
            Assert.LessOrEqual(r.xMax, ScreenLayout.ScreenWidth, r.ToString());
            Assert.LessOrEqual(r.yMax, ScreenLayout.ScreenHeight, r.ToString());
        }
    }

    [Test]
    public void Canvasへの置き換えが会話ウィンドウと同じ倍率()
    {
        var r = ScreenLayout.HpBox;
        Assert.AreEqual(new Vector2(48f * 1.8f, -80f * 1.8f), ScreenLayout.ToCanvas(r));
        Assert.AreEqual(new Vector2(64f * 1.8f, 16f * 1.8f), ScreenLayout.SizeOnCanvas(r));
    }

    [Test]
    public void 組み立てたシーンの欄が原典の位置にある()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            // 縦画面（作者の指定）では、生命の欄は上の帯の**2段目**にある。
            RectTransform hp = null, top = null;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var rt in go.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt.name == "Hp") hp = rt;
                    if (rt.name == "TopPanel") top = rt;
                }

            Assert.IsNotNull(hp, "Hp の欄が無い");
            Assert.IsNotNull(top, "上の帯が無い");
            Assert.AreSame(top, hp.parent, "上の帯の中にある");
            Assert.AreEqual(-PhoneScreen.RowHeight - PhoneScreen.Pad, hp.anchoredPosition.y,
                            0.01f, "2段目");
            Assert.Greater(hp.anchoredPosition.x, 0f, "階の旗の右");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void 画面の枠は原典のLCG一枚目で盤面の穴が開いている()
    {
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Sprites/Screens/Frame.png");
        Assert.IsNotNull(tex, "Frame.png が無い");
        Assert.AreEqual(640, tex.width);
        Assert.AreEqual(400, tex.height);
    }

    [Test]
    public void 組み立てたシーンに枠が敷いてある()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            // 縦画面（作者の指定）。原典の横長の枠は敷かず、
            // 上下の帯に**枠と同じ石積み**を敷く。
            RectTransform top = null, bottom = null;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var rt in go.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt.name == "TopPanel") top = rt;
                    if (rt.name == "BottomPanel") bottom = rt;
                }

            Assert.IsNotNull(top, "上の帯が無い");
            Assert.IsNotNull(bottom, "下の帯が無い");
            // 高さは端末の縦横比で変わる（PhoneLayoutView が盤面に接するまで伸ばす）ので、
            // ここでは**上と下に貼り付いていて厚みがある**ことだけを見る。
            Assert.AreEqual(1f, top.anchorMin.y, 0.001f, "上に貼り付く");
            Assert.AreEqual(0f, bottom.anchorMax.y, 0.001f, "下に貼り付く");
            Assert.Greater(top.sizeDelta.y, 0f);
            Assert.Greater(bottom.sizeDelta.y, 0f);
            foreach (var band in new[] { top, bottom })
            {
                var img = band.GetComponent<UnityEngine.UI.Image>();
                Assert.IsNotNull(img, "地が無い");
                Assert.IsNotNull(img.sprite, "石積みが敷いていない");
                StringAssert.Contains("Brick", img.sprite.name);
            }
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }
}

/// <summary>
/// スマホ縦画面の並べ方（<see cref="PhoneScreen"/>。作者の指定）。
/// 見せてもらった中国語版の画面に合わせて、上に3段・真ん中に盤面・下に
/// 持ち物の帯とボタン、という積み方になっているか。
/// </summary>
public class PhoneScreenTests
{
    [Test]
    public void 盤面は幅いっぱいの正方形で上下の帯の間に入る()
    {
        float w = PhoneScreen.ReferenceWidth, h = PhoneScreen.ReferenceHeight;
        float side = PhoneScreen.MapSide(w, h);
        Assert.AreEqual(w, side, 0.01f, "9:16 の画面なら幅いっぱい");
        Assert.LessOrEqual(PhoneScreen.TopHeight + side + PhoneScreen.BottomHeight, h,
                           "上の帯＋盤面＋下の帯が画面に収まる");
    }

    [Test]
    public void 縦に細い画面では盤面が縮む()
    {
        // 上下の帯を引いた高さのほうが狭ければ、そちらに合わせる
        float side = PhoneScreen.MapSide(1080f, 1400f);
        Assert.AreEqual(1400f - PhoneScreen.TopHeight - PhoneScreen.BottomHeight, side, 0.01f);
    }

    [Test]
    public void カメラは正方形の枠に十一マス()
    {
        Assert.AreEqual(11, PhoneScreen.MapCells);
        Assert.AreEqual(5.5f, PhoneScreen.CameraSize, 1e-4f, "11マス ÷ 2");
    }

    [Test]
    public void 下のボタンは八つと十字で全部飾り()
    {
        Assert.AreEqual(4, PhoneScreen.LeftButtons.Length);
        Assert.AreEqual(4, PhoneScreen.RightButtons.Length);
        foreach (var s in PhoneScreen.LeftButtons) Assert.IsNotEmpty(s);
        foreach (var s in PhoneScreen.RightButtons) Assert.IsNotEmpty(s);
    }

    [Test]
    public void 上下の帯に敷く絵と欄の絵が書き出してある()
    {
        foreach (var path in new[] { PhoneScreen.BrickPath, PhoneScreen.HpIconPath,
                                     PhoneScreen.AtkIconPath, PhoneScreen.DefIconPath,
                                     PhoneScreen.GoldIconPath })
            Assert.IsNotNull(UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path), path);
    }
}
