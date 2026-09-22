using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 知らせ（「鍵を手に入れた！」など）の見せ方。**作者の指定**で、
/// 画面の中央に出して少し置いてから薄くして消す（旧版がこの形だった）。
/// 原典は右下の欄に書きっぱなし（<see cref="TowerHud.InfoBox"/>）。
/// </summary>
public class ToastTests
{
    [Test]
    public void 拾っても窓は開かず知らせだけが積まれる()
    {
        var p = new PlayerState
        {
            Hp = 1000, Attack = 100, Defense = 100,
            Floor = 1, HighestFloor = 50, Position = new Vector2Int(0, 0),
        };
        var g = new TowerGame(p);
        g.Floor.Put(new Vector2Int(0, 1), "Key_Yellow");
        int before = g.Log.Count;

        g.Step(Vector2Int.up);

        Assert.IsFalse(g.IsShowingPage, "窓は開かない（原典 0x23ee も開かない）");
        Assert.AreEqual(before + 1, g.Log.Count, "知らせは積まれる");
        StringAssert.Contains("手に入れた", g.Log[g.Log.Count - 1]);
        Assert.AreEqual(1, p.YellowKeys, "ちゃんと拾えている");
    }

    [Test]
    public void 出しておく時間と薄くなる時間()
    {
        Assert.AreEqual(0.5f, PhoneScreen.ToastHold, 1e-4f, "0.5秒そのまま出す");
        Assert.AreEqual(0.5f, PhoneScreen.ToastFade, 1e-4f, "そのあと薄くして消す");
    }

    [Test]
    public void シーンに知らせの枠がある()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            RectTransform toast = null;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var rt in go.GetComponentsInChildren<RectTransform>(true))
                    if (rt.name == "Toast") toast = rt;
            Assert.IsNotNull(toast, "Toast が無い");
            Assert.IsFalse(toast.gameObject.activeSelf, "ふだんは出ていない");
            // 画面の**中央**（盤面の真ん中）に置く
            Assert.AreEqual(0f, toast.anchoredPosition.x, 0.01f);
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }
}

/// <summary>移動の速さ（作者の指定で4倍速に）。</summary>
public class MoveSpeedTests
{
    [Test]
    public void 一マスは十分の一秒()
    {
        Assert.AreEqual(0.1f, MonsterAnimation.StepSeconds(false), 1e-4f,
                        "16コマ x 1/160秒 = 0.1秒");
        Assert.AreEqual(0.05f, MonsterAnimation.StepSeconds(true), 1e-4f,
                        "高速モードはその半分（コマ数8）");
    }

    [Test]
    public void コマ数は原典のまま()
    {
        Assert.AreEqual(16, MonsterAnimation.TicksPerStep, "原典 0xa276 は16コマ");
        Assert.AreEqual(8, MonsterAnimation.FastTicksPerStep, "高速モードは8コマ");
    }

    [Test]
    public void 滑らせる処理は経過時間で進める()
    {
        // コマ送りで待つと、画面が60コマ/秒のとき1コマ1/60秒になって指定より遅くなる
        var src = System.IO.File.ReadAllText("Assets/Scripts/TowerController.cs");
        StringAssert.Contains("t += Time.deltaTime;", src);
        StringAssert.DoesNotContain("WaitForSeconds(MonsterAnimation.SlideFrameSeconds)", src);
    }
}

/// <summary>
/// 主人公の絵（作者の指定で Java版の4方向×2コマに差し替え）。
/// 原典（PC98版）は PLAYER1/PLAYER2 の2枚だけで、後ろ姿も横向きも無い。
/// </summary>
public class HeroSpriteTests
{
    [Test]
    public void 四方向かける二コマの名前が並んでいる()
    {
        Assert.AreEqual(8, AuthorStyle.HeroSpriteNames.Length);
        CollectionAssert.AreEqual(
            new[] { "MTower_1", "MTower_2", "MTower_3", "MTower_4",
                    "MTower_5", "MTower_6", "MTower_7", "MTower_8" },
            AuthorStyle.HeroSpriteNames);
    }

    [Test]
    public void アトラスからその八枚が引ける()
    {
        var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(AuthorStyle.HeroAtlasPath);
        foreach (var name in AuthorStyle.HeroSpriteNames)
        {
            bool found = false;
            foreach (var a in all)
            {
                var sp = a as Sprite;
                if (sp != null && sp.name == name) { found = true; break; }
            }
            Assert.IsTrue(found, name + " がアトラスに無い");
        }
    }

    [Test]
    public void どの階も一マスに収める()
    {
        // 作者が「1マスに収める方」を選んだので、どの階も同じ
        Assert.IsFalse(AuthorStyle.FullSizeHeroOn(3));
        Assert.IsFalse(AuthorStyle.FullSizeHeroOn(2));
        // 40ドットの絵を 100ドット=1単位 で取り込んでいるので 0.4単位
        Assert.AreEqual(1f, 0.4f * AuthorStyle.HeroScaleFit, 1e-4f, "1マスちょうど");
    }

    [Test]
    public void シーンに八枚とも繋がっている()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            TowerController c = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                var found = go.GetComponentInChildren<TowerController>(true);
                if (found != null) c = found;
            }
            Assert.IsNotNull(c);
            var so = new UnityEditor.SerializedObject(c);
            var arr = so.FindProperty("heroSprites");
            Assert.AreEqual(8, arr.arraySize);
            for (int i = 0; i < 8; i++)
                Assert.IsNotNull(arr.GetArrayElementAtIndex(i).objectReferenceValue,
                                 AuthorStyle.HeroSpriteNames[i]);
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }
}
