using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// .floor データ中の名前から実際のプレハブを引くための登録簿。
/// Assets/Resources/FloorPrefabRegistry.asset として保存され、
/// docs/make_registry.py で自動生成される。
/// </summary>
[CreateAssetMenu(menuName = "MagicTower/Floor Prefab Registry")]
public class FloorPrefabRegistry : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public string key;
        public GameObject prefab;
    }

    [SerializeField] List<Entry> entries = new List<Entry>();

    Dictionary<string, GameObject> _map;

    public GameObject Get(string key)
    {
        if (_map == null)
        {
            _map = new Dictionary<string, GameObject>(entries.Count);
            foreach (var e in entries)
                if (!string.IsNullOrEmpty(e.key) && e.prefab != null) _map[e.key] = e.prefab;
        }
        return _map.TryGetValue(key, out var p) ? p : null;
    }

    public IEnumerable<string> Keys
    {
        get { foreach (var e in entries) yield return e.key; }
    }
}
