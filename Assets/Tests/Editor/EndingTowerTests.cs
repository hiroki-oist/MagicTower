using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 終幕の塔の絵が流れていく様子。原典 PC98版 MTE.EXE の `0x0ce83` の輪。
/// </summary>
public class EndingTowerTests
{
    [Test]
    public void 原典の回数と待ち()
    {
        Assert.AreEqual(0x1a, Ending.TowerSteps, "26回（原典 cmp [bp-0x24e], 0x1a）");
        Assert.AreEqual(0x1e, Ending.TowerStepWait, "1回 0x1e(30) 待つ");
        Assert.AreEqual(15, Ending.TowerScrollPerStep, "高さが 15*i で縮む");
        Assert.AreEqual(7, Ending.TowerSound, "1回ごとに音7");
        Assert.AreEqual(13f, Ending.TowerDuration, 1e-4f, "26 x 30/60 = 13秒");
        // 送り合計は画面（400ドット）をほぼ流し切る
        Assert.AreEqual(390, Ending.TowerSteps * Ending.TowerScrollPerStep);
        Assert.LessOrEqual(Ending.TowerSteps * Ending.TowerScrollPerStep,
                           DialogueWindow.ScreenHeight);
    }

    [Test]
    public void 明るさは入り七段と出七段()
    {
        // 原典は i<7 で入りの表、i>0x12 で出の表のパレットに差し替える
        Assert.AreEqual(7, Ending.TowerFadeInSteps);
        Assert.AreEqual(0x12, Ending.TowerFadeOutAfter);
        Assert.Less(Ending.TowerBrightness(0), 1f, "はじめは暗い");
        Assert.AreEqual(1f, Ending.TowerBrightness(6), 1e-5f, "7回目で出そろう");
        for (int i = 7; i <= Ending.TowerFadeOutAfter; i++)
            Assert.AreEqual(1f, Ending.TowerBrightness(i), 1e-5f, $"{i}回目はそのまま");
        Assert.Less(Ending.TowerBrightness(Ending.TowerFadeOutAfter + 1), 1f, "そのあと暗くなる");
        Assert.AreEqual(0f, Ending.TowerBrightness(Ending.TowerSteps - 1), 1e-5f, "終わりは真っ暗");
    }

    [Test]
    public void 濃さの変わり方が近似であることを明記してある()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/Ending.cs");
        StringAssert.Contains("直線で近似", src, "パレットの表はまだ読めていない");
    }

    [Test]
    public void 見た目の側が流している()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/UI/FullScreenView.cs");
        StringAssert.Contains("Ending.TowerSteps", src);
        StringAssert.Contains("Ending.TowerScrollPerStep", src);
        StringAssert.Contains("SoundCues.SceneBeat", src);
    }

    [Test]
    public void シーンの塔の絵だけが流れる()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            int scrolling = 0;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var v in go.GetComponentsInChildren<FullScreenView>(true))
                {
                    var so = new UnityEditor.SerializedObject(v);
                    if (so.FindProperty("scrollTower").boolValue)
                    {
                        scrolling++;
                        Assert.AreEqual("EndingTowerScreen", v.name);
                    }
                }
            Assert.AreEqual(1, scrolling, "流れるのは終幕の塔の絵だけ");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }
}
