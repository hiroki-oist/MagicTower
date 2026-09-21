using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレハブ名 → 原典 PC98版の物体ID。データは Resources/ObjectIds.txt。
///
/// 原典は多くの規則をIDで書いている（魔術士Ａ=151・魔導師=93・
/// 破壊の玉が効かない 91/104/122/145 など）ので、
/// リメイク側でも実行時にIDを引けるようにしておく。
/// </summary>
public class ObjectIds
{
    readonly Dictionary<string, int> byName = new Dictionary<string, int>();

    static ObjectIds _instance;
    public static ObjectIds Instance
    {
        get
        {
            if (_instance == null) _instance = LoadDefault();
            return _instance;
        }
    }

    static ObjectIds LoadDefault()
    {
        var o = new ObjectIds();
        var asset = Resources.Load<TextAsset>("ObjectIds");
        if (asset == null) Debug.LogError("ObjectIds: Resources/ObjectIds.txt が見つかりません");
        else o.Parse(asset.text);
        return o;
    }

    void Parse(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('|');
            if (p.Length < 2) continue;
            int id;
            if (int.TryParse(p[1].Trim(), out id)) byName[p[0].Trim()] = id;
        }
    }

    /// <summary>プレハブ名から原典のIDを引く。地形など対応が無いものは0。</summary>
    public int Of(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return 0;
        int id;
        return byName.TryGetValue(prefabName, out id) ? id : 0;
    }

    /// <summary>その名前が敵かどうか（属性表の種別が8〜11に相当）。</summary>
    public bool IsEnemy(string prefabName)
    {
        return !string.IsNullOrEmpty(prefabName)
            && (prefabName.StartsWith("Enemy_") || prefabName.StartsWith("Boss_"));
    }

    public int Count { get { return byName.Count; } }

    /// <summary>
    /// 物体IDからプレハブ名を引く。解錠で置かれるマス（原典は数値で指定する）を
    /// 盤面の名前に直すのに使う。見つからなければ null。
    /// </summary>
    public string NameOf(int id)
    {
        foreach (var kv in byName) if (kv.Value == id) return kv.Key;
        return null;
    }

    /// <summary>"ID30" のような書き方なら名前に直す。そうでなければそのまま返す。</summary>
    public string Resolve(string what)
    {
        if (string.IsNullOrEmpty(what) || !what.StartsWith("ID")) return what;
        int id;
        if (!int.TryParse(what.Substring(2), out id)) return what;
        return NameOf(id) ?? what;
    }
}
