using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 装備の絵。原典は **剣 = 物体ID 50+段位 / 盾 = 55+段位** の絵をそのまま貼る（0x9b80 付近）。
///
/// ⚠ タイルのファイル名は動画から切り出したときの呼び名のままで、
/// **`ObjectIds.txt` の名前と食い違っているものがある**（剣は 52〜55 が入れ替わって見える）。
/// 中身は `p58_to_tiles.py` が**IDで**原典から埋め直しているので合っているが、
/// 名前を信じると取り違えるので、ここでIDの対応を押さえておく。
/// </summary>
public class EquipIconSceneTests
{
    [Test]
    public void 剣と盾の絵がIDぶん揃っている()
    {
        for (int rank = 1; rank <= 5; rank++)
        {
            Assert.IsNotNull(Tile(50 + rank), $"剣 {rank} の絵（ID{50 + rank}）が無い");
            Assert.IsNotNull(Tile(55 + rank), $"盾 {rank} の絵（ID{55 + rank}）が無い");
        }
    }

    [Test]
    public void シーンの装備の絵が段位の順に並んでいる()
    {
        var path = "Assets/Scenes/Tower.unity";
        if (!System.IO.File.Exists(path)) Assert.Ignore("シーン未生成");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            TowerHudView view = null;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var v in go.GetComponentsInChildren<TowerHudView>(true)) view = v;
            Assert.IsNotNull(view);
            var so = new UnityEditor.SerializedObject(view);
            for (int rank = 1; rank <= 5; rank++)
            {
                Same(so, "swordIcons", rank - 1, 50 + rank);
                Same(so, "shieldIcons", rank - 1, 55 + rank);
            }
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }

    static void Same(UnityEditor.SerializedObject so, string field, int index, int objectId)
    {
        var got = so.FindProperty(field).GetArrayElementAtIndex(index).objectReferenceValue;
        Assert.IsNotNull(got, $"{field}[{index}] が未設定");
        var want = Tile(objectId);
        Assert.IsNotNull(want, $"ID{objectId} の絵が無い");
        Assert.AreEqual(want.name, got.name, $"{field}[{index}] は物体ID {objectId} の絵のはず");
    }

    /// <summary>物体IDのタイル。名前の頭3桁がIDなので、そこだけで引く。</summary>
    static Sprite Tile(int id)
    {
        foreach (var p in System.IO.Directory.GetFiles("Assets/Sprites/Tiles", "*.png"))
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(p);
            if (name.EndsWith("_b", System.StringComparison.Ordinal)) continue;
            if (name.StartsWith($"{id:000}_"))
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p);
        }
        return null;
    }
}
