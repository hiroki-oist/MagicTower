using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 新しい本線のシーン `Assets/Scenes/Tower.unity` が
/// ちゃんと組まれていて、階を出し入れできるかを確かめる。
///
/// 旧来の「階ごとに手で作った51本のシーン」を置き換えるのがこのシーンなので、
/// ここが通れば F35 以降も含めて全50階が出せることになる。
/// </summary>
public class TowerSceneTests
{
    TowerRuntime runtime;
    FloorLoader loader;
    GameRules rules;

    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.OpenScene(TowerSceneBuilder.ScenePath, OpenSceneMode.Single);
        runtime = Object.FindObjectOfType<TowerRuntime>();
        loader  = Object.FindObjectOfType<FloorLoader>();
        rules   = Object.FindObjectOfType<GameRules>();
    }

    [Test]
    public void シーンに必要な部品が揃っている()
    {
        Assert.IsNotNull(runtime, "TowerRuntime が無い");
        Assert.IsNotNull(loader,  "FloorLoader が無い");
        Assert.IsNotNull(rules,   "GameRules が無い");
        Assert.IsNotNull(Object.FindObjectOfType<Camera>(), "カメラが無い");
        Assert.IsNotNull(Object.FindObjectOfType<TowerController>(), "TowerController が無い");
    }

    [Test]
    public void 画面の文字が揃っていて参照も繋がっている()
    {
        var view = Object.FindObjectOfType<TowerHudView>();
        Assert.IsNotNull(view, "TowerHudView が無い");
        var so = new UnityEditor.SerializedObject(view);
        foreach (var name in new[] { "controller", "floorLabel", "hp", "attack",
                                     "defense", "gold", "weapon", "shield",
                                     "messageBox", "keyIcons", "weaponIcon", "shieldIcon",
                                     "battleView", "registry", "monsterPanel", "monsterIcon",
                                     "monsterName", "monsterHp", "monsterAttack",
                                     "monsterDefense",
                                     "keyRed", "keyPurple", "keyYellow" })
            Assert.IsNotNull(so.FindProperty(name).objectReferenceValue, $"{name} が未設定");

        // 日本語が出せるフォントが割り当たっているか
        var label = (TMPro.TextMeshProUGUI)so.FindProperty("floorLabel").objectReferenceValue;
        Assert.IsNotNull(label.font, "フォントが未設定");
        StringAssert.Contains("PixelMplus", label.font.name, "日本語のピクセルフォントを使う");
    }

    [Test]
    public void 入力を受ける側にも参照が繋がっている()
    {
        var c = Object.FindObjectOfType<TowerController>();
        var so = new UnityEditor.SerializedObject(c);
        Assert.IsNotNull(so.FindProperty("loader").objectReferenceValue, "FloorLoader が未設定");
        Assert.IsNotNull(so.FindProperty("playerSprite").objectReferenceValue,
                         "プレーヤーのスプライトが未設定");
    }

    [Test]
    public void 一階に入ると盤面が組み上がる()
    {
        runtime.Enter(1, new Vector2Int(0, -5));
        Assert.AreEqual(1, runtime.CurrentFloor);
        Assert.Greater(loader.Container.childCount, 121, "地形と物体が生成されているはず");
        Assert.IsNotNull(rules.Floor);
        Assert.AreEqual(1, rules.Floor.FloorNumber);
    }

    [Test]
    public void 階段で上がると階が入れ替わる()
    {
        runtime.Enter(1, new Vector2Int(0, -5));
        int before = loader.Container.childCount;
        Assert.IsTrue(runtime.GoUp(), "1階から2階へ上がれる");
        Assert.AreEqual(2, runtime.CurrentFloor);
        Assert.Greater(loader.Container.childCount, 121, "2階の盤面が組み上がっている");
        Assert.AreEqual(2, rules.Floor.FloorNumber);
    }

    [Test]
    public void 翼なら幻の階に入れる()
    {
        runtime.Enter(43, new Vector2Int(0, 0));
        Assert.IsTrue(runtime.UseWing(true), "43階から昇華の翼で上がる");
        Assert.AreEqual(44, runtime.CurrentFloor, "階段と違い44階を飛ばさない");
    }

    [Test]
    public void 全50階を出し入れできる()
    {
        for (int n = 1; n <= 50; n++)
        {
            runtime.Enter(n, new Vector2Int(0, 0));
            Assert.AreEqual(n, runtime.CurrentFloor);
            Assert.Greater(loader.Container.childCount, 100, $"F{n} の盤面が空");
        }
    }
}
