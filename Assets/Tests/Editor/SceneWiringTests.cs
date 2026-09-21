using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 組み立てたシーンの**配線**が埋まっているかを見る。
///
/// 参照が1つ抜けていても Unity は黙って動く（その場で何も起きないだけ）ので、
/// 「繋ぎ忘れ」は遊んでみるまで気づけない。ここで自分のスクリプトの
/// `[SerializeField]` を全部見て、空のものが無いことを確かめる。
/// </summary>
public class SceneWiringTests
{
    /// <summary>
    /// 空でよい参照。理由をここに書く。
    ///   TowerRuntime.player … 本線は `TowerController` 側で、主人公の見た目は
    ///                         遊び始めてから作るのでシーンには居ない。
    /// </summary>
    static readonly HashSet<string> MayBeEmpty = new HashSet<string>
    {
        "TowerRuntime.player",
    };

    /// <summary>自分たちが書いたスクリプトか（Unity や TMP のものは見ない）。</summary>
    static bool Ours(MonoBehaviour mb)
    {
        var ns = mb.GetType().Namespace;
        if (!string.IsNullOrEmpty(ns)) return false;          // 自分のはどれも名前空間なし
        var asm = mb.GetType().Assembly.GetName().Name;
        return asm == "Assembly-CSharp";
    }

    [Test]
    public void 本線のシーンは配線が埋まっている()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        var empty = new List<string>();
        int looked = 0;
        try
        {
            foreach (var go in scene.GetRootGameObjects())
                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null || !Ours(mb)) continue;
                    var so = new SerializedObject(mb);
                    var it = so.GetIterator();
                    while (it.NextVisible(true))
                    {
                        if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (it.name == "m_Script") continue;
                        var key = mb.GetType().Name + "." + it.name;
                        looked++;
                        if (it.objectReferenceValue == null && !MayBeEmpty.Contains(key))
                            empty.Add($"{key}（{mb.gameObject.name}）");
                    }
                }
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }

        Assert.Greater(looked, 20, "見た参照が少なすぎる（探し方がおかしい）");
        Assert.IsEmpty(empty, "繋ぎ忘れ: " + string.Join(" / ", empty.ToArray()));
    }

    /// <summary>音を鳴らす役がシーンに居て、本線に繋がっている。</summary>
    [Test]
    public void 音を鳴らす役がシーンに居る()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            SoundPlayer sound = null;
            TowerController controller = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                foreach (var s in go.GetComponentsInChildren<SoundPlayer>(true)) sound = s;
                foreach (var c in go.GetComponentsInChildren<TowerController>(true)) controller = c;
            }
            Assert.IsNotNull(sound, "SoundPlayer がシーンに無い");
            Assert.IsNotNull(controller);
            var so = new SerializedObject(controller);
            Assert.AreSame(sound, so.FindProperty("sound").objectReferenceValue,
                           "本線が同じ SoundPlayer を見ている");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }
}
