using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// .floor データから1フロア分の GameObject を組み立てる。
/// 既存のシーン方式（Floor_N.unity をロード）を置き換えるための土台。
/// 現時点では既存の動作には一切介入しない。
/// </summary>
public class FloorLoader : MonoBehaviour
{
    [SerializeField] FloorPrefabRegistry registry;
    [SerializeField] Transform container;

    /// <summary>3x3 を占有するボス。中心マスにのみ1体だけ生成する。</summary>
    static readonly HashSet<string> MultiTileBosses = new HashSet<string>
    {
        "Boss_Dragon", "Boss_GiantOctopus",
    };

    public FloorData Current { get; private set; }

    /// <summary>
    /// 参照を外から差し込む。シーンに置かずに使う場合（テストや動的生成）に用いる。
    /// </summary>
    public void Configure(FloorPrefabRegistry reg, Transform parent)
    {
        registry = reg;
        container = parent;
    }

    /// <summary>生成したオブジェクトの親。</summary>
    public Transform Container { get { return container != null ? container : transform; } }

    /// <summary>
    /// 盤面の物を作ったり消したりするたびに1つ増える番号。
    /// **盤面の中身が変わったことを外から知る**ための印で、
    /// 2コマの動き（<see cref="MonsterAnimator"/>）がこれを見て拾い直す。
    /// これが無いと、場面で現れた相手（10階の骸骨）は
    /// **主人公が次に動くまで絵が止まったまま**だった（作者の報告）。
    /// </summary>
    public int Version { get; private set; }

    public void Load(int floorNumber)
    {
        var data = FloorData.Load(floorNumber);
        if (data == null) return;
        Build(data);
    }

    public void Build(FloorData data)
    {
        Clear();
        Version++;
        Current = data;
        if (registry == null)
        {
            Debug.LogError("FloorLoader: registry が未設定です");
            return;
        }

        for (int y = FloorData.MinY; y <= FloorData.MaxY; y++)
            for (int x = FloorData.MinX; x <= FloorData.MaxX; x++)
                SpawnTerrain(x, y, data.At(x, y));

        var placedBoss = new HashSet<string>();
        foreach (var o in data.Objects)
        {
            if (MultiTileBosses.Contains(o.Prefab))
            {
                // 3x3 の9マスに同じ名前が並ぶので、中心の1マスだけ生成する
                if (!placedBoss.Add(o.Prefab + BossKey(data, o.Prefab))) continue;
                var c = BossCenter(data, o.Prefab);
                Spawn(o.Prefab, c.x, c.y);
                continue;
            }
            Spawn(o.Prefab, o.Pos.x, o.Pos.y);
        }
    }

    void SpawnTerrain(int x, int y, FloorData.Terrain t)
    {
        // 床は下地として常に敷き、その上に地形を重ねる
        Spawn("Floor", x, y);
        if (t == FloorData.Terrain.Floor) return;
        Spawn(t.ToString(), x, y);
    }

    GameObject Spawn(string key, int x, int y)
    {
        var prefab = registry.Get(key);
        if (prefab == null)
        {
            Debug.LogWarning($"FloorLoader: プレハブ未登録 '{key}' at ({x},{y})");
            return null;
        }
        var go = Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity,
                             container != null ? container : transform);
        go.name = $"{key}_{x}_{y}";
        return go;
    }

    static string BossKey(FloorData d, string prefab)
    {
        var c = BossCenter(d, prefab);
        return $"@{c.x},{c.y}";
    }

    /// <summary>同じ名前で並んでいるマス群の中心を返す。</summary>
    static Vector2Int BossCenter(FloorData d, string prefab)
    {
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (var o in d.Objects)
        {
            if (o.Prefab != prefab) continue;
            minX = Mathf.Min(minX, o.Pos.x); maxX = Mathf.Max(maxX, o.Pos.x);
            minY = Mathf.Min(minY, o.Pos.y); maxY = Mathf.Max(maxY, o.Pos.y);
        }
        return new Vector2Int((minX + maxX) / 2, (minY + maxY) / 2);
    }

    /// <summary>
    /// そのマスに**もう1つ重ねて**出す。入れ替えの演出で、元のものと新しいものを
    /// どちらも持っておくために使う（<see cref="BlinkView"/>）。
    ///
    /// 名前の終わりを変えてあるので <see cref="Refresh"/> も <see cref="ObjectAt"/> も
    /// これを見つけない＝本線の描き直しの邪魔をしない。出した側が片付けること。
    /// </summary>
    public GameObject SpawnExtra(string key, Vector2Int pos)
    {
        if (string.IsNullOrEmpty(key)) return null;
        var go = Spawn(key, pos.x, pos.y);
        if (go != null) go.name = $"{key}_{pos.x}_{pos.y}~重ね";
        return go;
    }

    /// <summary>
    /// 1マスだけ作り直す。解錠で門が消えたときなど、
    /// <see cref="FloorState"/> 側の変化を見た目に反映するために使う。
    /// </summary>
    /// <param name="kind">新しい中身。床にするなら null。</param>
    public void Refresh(Vector2Int pos, string kind)
    {
        // そのマスに置いた物を消す（Spawn は "{key}_{x}_{y}" という名前で作る）
        var suffix = $"_{pos.x}_{pos.y}";
        var parent = container != null ? container : transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var c = parent.GetChild(i);
            if (!c.name.EndsWith(suffix)) continue;
            if (c.name.StartsWith("Floor_")) continue;   // 下地の床は残す
            // 遊んでいる最中と、エディタから触るときとで消し方が違う（Clear と同じ）
            if (Application.isPlaying) Destroy(c.gameObject);
            else DestroyImmediate(c.gameObject);
        }
        if (!string.IsNullOrEmpty(kind)) Spawn(kind, pos.x, pos.y);
        Version++;
    }

    /// <summary>
    /// そのマスに置いてある物体を返す（下地の床は除く）。無ければ null。
    /// 点滅の演出（<see cref="BlinkView"/>）が絵を出し入れするのに使う。
    /// </summary>
    public GameObject ObjectAt(Vector2Int pos)
    {
        var suffix = $"_{pos.x}_{pos.y}";
        var parent = container != null ? container : transform;
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (!c.name.EndsWith(suffix)) continue;
            if (c.name.StartsWith("Floor_")) continue;
            return c.gameObject;
        }
        return null;
    }

    public void Clear()
    {
        var t = container != null ? container : transform;
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(t.GetChild(i).gameObject);
            else DestroyImmediate(t.GetChild(i).gameObject);
        }
    }
}
