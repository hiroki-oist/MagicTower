using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 移動の十字キー（作者が6案から選んだ B）。原典はキーボードで遊ぶので、これは作者の指定。
/// </summary>
public class DPadTests
{
    [Test]
    public void 押した場所が向きになる()
    {
        float r = 100f;
        Assert.AreEqual(new Vector2Int(0, 1),  PhoneScreen.DirFromTap(new Vector2(0, 60), r));
        Assert.AreEqual(new Vector2Int(0, -1), PhoneScreen.DirFromTap(new Vector2(0, -60), r));
        Assert.AreEqual(new Vector2Int(1, 0),  PhoneScreen.DirFromTap(new Vector2(60, 0), r));
        Assert.AreEqual(new Vector2Int(-1, 0), PhoneScreen.DirFromTap(new Vector2(-60, 0), r));
    }

    [Test]
    public void 真ん中と円の外は動かない()
    {
        float r = 100f;
        Assert.AreEqual(Vector2Int.zero, PhoneScreen.DirFromTap(Vector2.zero, r), "つまみ");
        Assert.AreEqual(Vector2Int.zero, PhoneScreen.DirFromTap(new Vector2(10, 10), r),
                        "真ん中あたり");
        Assert.AreEqual(Vector2Int.zero, PhoneScreen.DirFromTap(new Vector2(0, 140), r), "外");
    }

    [Test]
    public void 斜めはずれの大きいほうに倒す()
    {
        float r = 100f;
        Assert.AreEqual(new Vector2Int(1, 0), PhoneScreen.DirFromTap(new Vector2(50, 40), r));
        Assert.AreEqual(new Vector2Int(0, -1), PhoneScreen.DirFromTap(new Vector2(-30, -50), r));
    }

    [Test]
    public void 選んだ絵が書き出してある()
    {
        Assert.AreEqual("Assets/Sprites/Hud/DPad_B.png", PhoneScreen.DPadSpritePath);
        Assert.IsNotNull(
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(PhoneScreen.DPadSpritePath));
    }

    [Test]
    public void シーンの十字キーにその絵が貼ってある()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            RectTransform pad = null;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var rt in go.GetComponentsInChildren<RectTransform>(true))
                    if (rt.name == "DPad") pad = rt;
            Assert.IsNotNull(pad, "十字キーが無い");
            var img = pad.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(img.sprite, "絵が貼っていない");
            StringAssert.Contains("DPad_B", img.sprite.name);
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }
}
