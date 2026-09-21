using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 「参照が外れているのに黙って動く」たぐいの穴をまとめて見る。
///
/// 3x3のボス（ドラゴン・大ダコ）と店の祭壇は、絵が 96x96 / 96x32 と2の冪でないのに
/// 取り込み設定が「2の冪に丸める」になっていて **Sprite として取り込めず**、
/// プレハブの絵の参照が外れたまま盤面から消えていた。
/// 何も言わずに消えるので、遊んでみるまで気づけない。ここで機械的に潰す。
/// </summary>
public class AssetLinkTests
{
    [Test]
    public void 登録簿のプレハブはぜんぶ絵が繋がっている()
    {
        var reg = Resources.Load<FloorPrefabRegistry>("FloorPrefabRegistry");
        Assert.IsNotNull(reg, "FloorPrefabRegistry が読めない");
        var missing = new System.Collections.Generic.List<string>();
        foreach (var key in reg.Keys)
        {
            var prefab = reg.Get(key);
            if (prefab == null) { missing.Add($"{key}: プレハブ無し"); continue; }
            var sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null) { missing.Add($"{key}: 絵の役が無い"); continue; }
            if (sr.sprite == null) missing.Add($"{key}: 絵が外れている");
        }
        Assert.IsEmpty(missing, string.Join(" / ", missing));
    }

    [Test]
    public void タイルの絵はぜんぶスプライトとして読める()
    {
        var bad = new System.Collections.Generic.List<string>();
        foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(path);
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null || imp.textureType != TextureImporterType.Sprite) continue;
            if (imp.spriteImportMode != SpriteImportMode.Single) continue;
            if (AssetDatabase.LoadAssetAtPath<Sprite>(p) == null) bad.Add(p);
        }
        Assert.IsEmpty(bad, "Sprite にならない絵: " + string.Join(" / ", bad));
    }

    [Test]
    public void 二の冪でない絵は丸めない設定になっている()
    {
        var bad = new System.Collections.Generic.List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null || imp.textureType != TextureImporterType.Sprite) continue;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (tex == null) continue;
            if (IsPowerOfTwo(tex.width) && IsPowerOfTwo(tex.height)) continue;
            if (imp.npotScale != TextureImporterNPOTScale.None) bad.Add(p);
        }
        Assert.IsEmpty(bad, "2の冪に丸める設定のままの絵: " + string.Join(" / ", bad));
    }

    static bool IsPowerOfTwo(int n) { return n > 0 && (n & (n - 1)) == 0; }

    [Test]
    public void シーンの参照に抜けが無い()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Tower.unity");
        var bad = new System.Collections.Generic.List<string>();
        foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>(true))
        {
            var type = mb.GetType();
            // 自分たちで書いた役だけを見る（Unity や TextMeshPro の中身は対象外）
            if (type.Namespace != null) continue;
            if (type.Assembly.GetName().Name != "Assembly-CSharp") continue;
            var so = new SerializedObject(mb);
            var it = so.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (it.objectReferenceValue != null) continue;
                if (it.objectReferenceInstanceIDValue != 0)
                    bad.Add($"{mb.gameObject.name}.{type.Name}.{it.propertyPath}: 参照先が消えている");
            }
        }
        Assert.IsEmpty(bad, string.Join(" / ", bad));
    }
}
