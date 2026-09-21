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
    public void 白への寄せ具合が原典のパレットの表どおり()
    {
        // 原典 0x0c4d8 がスタックに積む8本の表。番号0（下地）の濃さは
        // 15, 11, 9, 7, 5, 3, 1, 0（まっ白 → ふだんの色）。
        Assert.AreEqual(7, Ending.TowerFadeInSteps);
        Assert.AreEqual(0x12, Ending.TowerFadeOutAfter);

        // 入り: [bp-0x228] から後ろへ ＝ 11/15 → 0（白からすうっと現れる）
        var wantIn = new[] { 11, 9, 7, 5, 3, 1, 0 };
        for (int i = 0; i < wantIn.Length; i++)
            Assert.AreEqual(wantIn[i] / 15f, Ending.TowerWhiteMix(i), 1e-5f, $"{i}回目");

        // 途中は差し替えない ＝ ふだんの色のまま
        for (int i = 7; i <= Ending.TowerFadeOutAfter; i++)
            Assert.AreEqual(0f, Ending.TowerWhiteMix(i), 1e-5f, $"{i}回目はそのまま");

        // 出: [bp+0xd8] から 32*i を引く ＝ 1/15 → 15/15（まっ白に消える）
        var wantOut = new[] { 1, 3, 5, 7, 9, 11, 15 };
        for (int k = 0; k < wantOut.Length; k++)
            Assert.AreEqual(wantOut[k] / 15f,
                            Ending.TowerWhiteMix(Ending.TowerFadeOutAfter + 1 + k), 1e-5f,
                            $"{Ending.TowerFadeOutAfter + 1 + k}回目");
        Assert.AreEqual(1f, Ending.TowerWhiteMix(Ending.TowerSteps - 1), 1e-5f,
                        "終わりはまっ白（そのあと終わりの言葉が白地に出る）");
    }

    [Test]
    public void パレットの表を原典から起こしたと書いてある()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/Ending.cs");
        StringAssert.Contains("0fff 1fff 2fff", src, "表そのものを控えてある");
        StringAssert.Contains("逆だった", src, "以前の近似が逆向きだったことを残す");
    }

    [Test]
    public void 見た目の側が流している()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/UI/FullScreenView.cs");
        StringAssert.Contains("Ending.TowerSteps", src);
        StringAssert.Contains("Ending.TowerScrollPerStep", src);
        StringAssert.Contains("SoundCues.SceneBeat", src);
        StringAssert.Contains("Ending.TowerWhiteMix", src);
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
            // 白へ寄せる板は、流れる1枚だけが持っていること
            int veils = 0;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var v in go.GetComponentsInChildren<FullScreenView>(true))
                {
                    var so = new UnityEditor.SerializedObject(v);
                    var veil = so.FindProperty("whiteVeil").objectReferenceValue;
                    if (veil == null) continue;
                    veils++;
                    Assert.AreEqual("EndingTowerScreen", v.name);
                    Assert.IsTrue(so.FindProperty("scrollTower").boolValue,
                                  "白へ寄せるのは流れる絵だけ");
                }
            Assert.AreEqual(1, veils, "白へ寄せる板は1枚だけ");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }
}
