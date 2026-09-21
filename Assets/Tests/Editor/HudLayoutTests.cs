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
                                  ScreenLayout.MessageBox, ScreenLayout.BattleNumberA })
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
                                  ScreenLayout.MessageBox, ScreenLayout.BattleNumberA,
                                  ScreenLayout.BattleNumberB })
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
            RectTransform hp = null;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var rt in go.GetComponentsInChildren<RectTransform>(true))
                    if (rt.name == "Hp") hp = rt;

            Assert.IsNotNull(hp, "Hp の欄が無い");
            Assert.AreEqual(ScreenLayout.ToCanvas(ScreenLayout.HpBox), hp.anchoredPosition);
            Assert.AreEqual(ScreenLayout.SizeOnCanvas(ScreenLayout.HpBox), hp.sizeDelta);
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
            RectTransform frame = null;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var rt in go.GetComponentsInChildren<RectTransform>(true))
                    if (rt.name == "Frame") frame = rt;

            Assert.IsNotNull(frame, "Frame が無い");
            Assert.AreEqual(0, frame.GetSiblingIndex(), "文字より後ろに置く");
            Assert.AreEqual(new Vector2(1152f, 720f), frame.sizeDelta, "画面いっぱい");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }
}
