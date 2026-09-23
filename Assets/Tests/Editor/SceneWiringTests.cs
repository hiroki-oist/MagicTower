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
        // 白へ寄せる板は**終幕の塔の絵だけ**が持つ（ほかの1枚絵は流れも寄せもしない）。
        // 塔の絵の側は TowerSceneTests / EndingTowerTests で別に見ている。
        "FullScreenView.whiteVeil",
        // 鍵を**本数ぶん絵で並べる**のは原典（横長）の見せ方。縦画面の並べ方
        // （作者の指定）では鍵の絵ひとつに数字を添えるので、こちらは使わない。
        "TowerHudView.keyIcons",
        // 知らせは画面の中央に出して消える（MessageToastView）ので、
        // 原典の右下の欄は使わない。
        "TowerHudView.messageBox",
        "TowerHudView.keyRed", "TowerHudView.keyPurple", "TowerHudView.keyYellow",
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

    /// <summary>
    /// 画面の中身（枠・欄・窓）が**画布の中央に置いた原典と同じ大きさの入れ物**に入っている。
    /// top-left に直接置くと、窓の横幅が 16:10 より広いときに**枠だけ左に寄って
    /// 盤面（カメラ）とずれる**。手で遊んでもらって見つかったのがこれ。
    /// </summary>
    [Test]
    public void 画面の中身は中央に置いた入れ物の中にある()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            RectTransform screen = null, title = null;
            var found = new Dictionary<string, RectTransform>();
            foreach (var go in scene.GetRootGameObjects())
                foreach (var rt in go.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt.name == "Screen") screen = rt;
                    // 1枚絵（題名など）は**一番上の層**へ移した。上下の帯に隠れて
                    // 見えないという報告があったため（FullScreenLayer）。
                    if (rt.name == "DialogueWindow" || rt.name == "MenuWindow")
                        found[rt.name] = rt;
                    if (rt.name == "TitleScreen") title = rt;
                }

            Assert.IsNotNull(screen, "入れ物 Screen が無い");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), screen.anchorMin, "中央に置く");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), screen.anchorMax);
            Assert.AreEqual(new Vector2(DialogueWindow.CanvasWidth, DialogueWindow.CanvasHeight),
                            screen.sizeDelta, "原典の画面と同じ大きさ（1.8倍）");

            foreach (var kv in found)
                Assert.AreSame(screen, kv.Value.parent, kv.Key + " が入れ物の外にある");

            Assert.IsNotNull(title, "題名の1枚絵が無い");
            Assert.AreEqual("FullScreenLayer", title.parent.name,
                            "1枚絵は一番上の層に置く（帯に隠れないように）");
            Assert.AreSame(screen.parent, title.parent.parent, "層は同じ枠の中");
            // **一番上は起動時の題名画面**（作者の指定）。1枚絵の層はそのすぐ下。
            var titleLayer = screen.parent.Find("TitleLayer");
            Assert.IsNotNull(titleLayer, "起動時の題名の層が無い");
            Assert.AreEqual(screen.parent.childCount - 1, titleLayer.GetSiblingIndex(),
                            "題名の層が一番あと（＝一番上）");
            Assert.AreEqual(screen.parent.childCount - 2,
                            title.parent.GetSiblingIndex(), "1枚絵の層はそのすぐ下");

            // 縦画面（作者の指定）。盤面が画面の幅いっぱいなので**幅で合わせる**。
            var scaler = screen.GetComponentInParent<UnityEngine.UI.CanvasScaler>();
            Assert.IsNotNull(scaler);
            Assert.AreEqual(0f, scaler.matchWidthOrHeight, 0.001f, "幅を基準にする");
            Assert.AreEqual(new Vector2(PhoneScreen.ReferenceWidth, PhoneScreen.ReferenceHeight),
                            scaler.referenceResolution, "縦画面の基準");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }

    /// <summary>
    /// 主人公と当たりの絵は**盤面のマスと同じ大きさ**で取り込む。
    /// 取り込みの設定（1マス＝32ドット）が既定の100のままだと1/3の大きさで出る。
    /// </summary>
    [Test]
    public void 絵は一マス三十二ドットで取り込んである()
    {
        foreach (var path in new[]
        {
            "Assets/Sprites/Effects/Player_0.png",
            "Assets/Sprites/Effects/Player_1.png",
            "Assets/Sprites/Effects/Hit.png",
            "Assets/Sprites/Tiles/006_Floor.png",
        })
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.IsNotNull(sp, path);
            Assert.AreEqual(32f, sp.pixelsPerUnit, 0.01f, path);
            Assert.AreEqual(1f, sp.bounds.size.x, 0.01f, path + " は1マスぶん");
            Assert.AreEqual(1f, sp.bounds.size.y, 0.01f, path + " は1マスぶん");
        }
    }

    /// <summary>
    /// 音を聴く耳がシーンに1つある。**無いと何も鳴らない**（手で組み立てたカメラには
    /// 付いてこないので、実際に鳴らないまま気づかなかった）。
    /// </summary>
    [Test]
    public void 音を聴く耳がシーンに一つある()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            int ears = 0;
            foreach (var go in scene.GetRootGameObjects())
                ears += go.GetComponentsInChildren<AudioListener>(true).Length;
            Assert.AreEqual(1, ears, "AudioListener はちょうど1つ");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
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

    /// <summary>
    /// 1枚絵の入れ物は**生かしたまま**置く。この役は入れ物そのものに付いているので、
    /// 消すと自分の LateUpdate が動かず、出番が来ても出られない
    /// （題名・序章の絵・ゲームオーバーが出なかった原因）。
    /// </summary>
    [Test]
    public void 一枚絵の入れ物は生きていて絵だけ消してある()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            int seen = 0;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var v in go.GetComponentsInChildren<FullScreenView>(true))
                {
                    seen++;
                    Assert.IsTrue(v.gameObject.activeSelf,
                                  v.name + " の入れ物が消えている（出番が来ても出られない）");
                    foreach (var g in v.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                        Assert.IsFalse(g.enabled, v.name + " の " + g.name + " が出たままになっている");
                }
            Assert.AreEqual(6, seen, "題名・序章・終幕の塔・終わりの言葉・ゲームオーバー・最後の画面");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }
}
