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
    public void 戦闘の黒い帯がシーンに組まれている()
    {
        // 作者の旧版の見せ方（AuthorStyle）。原典に戻すときは Enabled を false にする。
        var view = Object.FindObjectOfType<BattleBandView>(true);
        if (!AuthorStyle.Enabled) { Assert.Pass("原典どおりの見せ方なので帯は無い"); return; }
        Assert.IsNotNull(view, "BattleBandView が無い");
        var so = new UnityEditor.SerializedObject(view);
        foreach (var name in new[] { "controller", "battleView", "registry", "panel",
                                     "background", "turnText",
                                     "playerIcon", "playerName",
                                     "enemyIcon", "enemyName" })
            Assert.IsNotNull(so.FindProperty(name).objectReferenceValue, $"{name} が未設定");
        foreach (var side in new[] { "playerStats", "enemyStats" })
        {
            var arr = so.FindProperty(side);
            Assert.AreEqual(AuthorStyle.StatTops.Length, arr.arraySize, side);
            for (int i = 0; i < arr.arraySize; i++)
                Assert.IsNotNull(arr.GetArrayElementAtIndex(i).objectReferenceValue,
                                 $"{side}[{i}] が未設定");
        }
        // 帯は始め隠れていること
        var panel = (RectTransform)so.FindProperty("panel").objectReferenceValue;
        Assert.IsFalse(panel.gameObject.activeSelf, "帯は始め隠しておく");
    }

    [Test]
    public void 点滅の役がシーンに組まれている()
    {
        var blink = Object.FindObjectOfType<BlinkView>(true);
        Assert.IsNotNull(blink, "BlinkView が無い");
        var bso = new UnityEditor.SerializedObject(blink);
        foreach (var name in new[] { "controller", "loader" })
            Assert.IsNotNull(bso.FindProperty(name).objectReferenceValue, $"{name} が未設定");
        // 会話の窓は点滅が終わってから出す（原典 0x6ae0 も点滅→待ち→0x94ba）
        var win = Object.FindObjectOfType<DialogueWindowView>(true);
        Assert.IsNotNull(win);
        var wso = new UnityEditor.SerializedObject(win);
        Assert.IsNotNull(wso.FindProperty("blink").objectReferenceValue,
                         "会話の窓に点滅の役が繋がっていない");
    }

    [Test]
    public void 扉とボスの動きの役もシーンに組まれている()
    {
        // 鍵の扉と門が開く9コマ（原典 0x7be6 / 0x7a68）
        var door = Object.FindObjectOfType<DoorOpenView>(true);
        Assert.IsNotNull(door, "DoorOpenView が無い");
        var dso = new UnityEditor.SerializedObject(door);
        Assert.IsNotNull(dso.FindProperty("controller").objectReferenceValue, "controller が未設定");
        var colors = dso.FindProperty("colors");
        var frames = dso.FindProperty("frames");
        CollectionAssert.AreEqual(new[] { "Yellow", "Purple", "Red", "Sealed" },
            Names(colors), "黄・紫・赤・門の4色");
        Assert.AreEqual(colors.arraySize * DoorAnimation.Frames, frames.arraySize,
                        "4色 x 9コマ");
        for (int i = 0; i < frames.arraySize; i++)
            Assert.IsNotNull(frames.GetArrayElementAtIndex(i).objectReferenceValue,
                             $"扉のコマ {i} が未設定");

        // 20階のボスが現れる10コマ（原典 0x3044）
        var boss = Object.FindObjectOfType<BossAppearView>(true);
        Assert.IsNotNull(boss, "BossAppearView が無い");
        var bso = new UnityEditor.SerializedObject(boss);
        Assert.IsNotNull(bso.FindProperty("controller").objectReferenceValue, "controller が未設定");
        var bf = bso.FindProperty("frames");
        Assert.AreEqual(BossAppear.Frames, bf.arraySize, "10コマ");
        for (int i = 0; i < bf.arraySize; i++)
            Assert.IsNotNull(bf.GetArrayElementAtIndex(i).objectReferenceValue,
                             $"ボスのコマ {i} が未設定");
    }

    static string[] Names(UnityEditor.SerializedProperty arr)
    {
        var outp = new string[arr.arraySize];
        for (int i = 0; i < arr.arraySize; i++)
            outp[i] = arr.GetArrayElementAtIndex(i).stringValue;
        return outp;
    }

    [Test]
    public void 入力を受ける側にも参照が繋がっている()
    {
        var c = Object.FindObjectOfType<TowerController>();
        var so = new UnityEditor.SerializedObject(c);
        Assert.IsNotNull(so.FindProperty("loader").objectReferenceValue, "FloorLoader が未設定");
        Assert.IsNotNull(so.FindProperty("monsters").objectReferenceValue,
                         "MonsterAnimator が未設定");
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
