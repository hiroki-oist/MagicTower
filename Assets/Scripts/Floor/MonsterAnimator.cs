using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 盤面の敵と人の絵を2コマで動かす（原典 0x92a4）。規則は <see cref="MonsterAnimation"/>。
///
/// 原典は**1歩動いたときだけ**この仕掛けを回す（移動の演出 0xa276 の中に6か所）。
/// 壁にぶつかったときや、拾っただけのときは回らない。
/// </summary>
public class MonsterAnimator : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] FloorLoader loader;

    /// <summary>2コマで動くものの名前と、その1コマ目・2コマ目の絵。</summary>
    [SerializeField] string[] keys = new string[0];
    [SerializeField] Sprite[] firstFrames = new Sprite[0];
    [SerializeField] Sprite[] secondFrames = new Sprite[0];

    readonly MonsterAnimation anim = new MonsterAnimation();
    readonly Dictionary<string, int> indexOfKey = new Dictionary<string, int>();

    /// <summary>マスごとの、そこに居る動くものの絵。</summary>
    readonly Dictionary<Vector2Int, SpriteRenderer> onBoard =
        new Dictionary<Vector2Int, SpriteRenderer>();
    readonly Dictionary<Vector2Int, int> kindOnBoard = new Dictionary<Vector2Int, int>();

    int builtFloor = -1;
    Vector2Int lastPos;

    /// <summary>今の仕掛けの中身（テストから見る）。</summary>
    public MonsterAnimation State { get { return anim; } }

    void Awake()
    {
        indexOfKey.Clear();
        for (int i = 0; i < keys.Length; i++) indexOfKey[keys[i]] = i;
    }

    void LateUpdate()
    {
        if (controller == null || controller.Game == null || loader == null) return;
        var p = controller.Game.Player;

        if (p.Floor != builtFloor)
        {
            builtFloor = p.Floor;
            lastPos = p.Position;
            anim.Reset();
            Collect();
            RepaintAll();
            return;
        }

        if (p.Position == lastPos) return;      // 動いたときだけ
        lastPos = p.Position;
        anim.Step(p.FastMode);
        // 盤面は戦闘や解錠で入れ替わるので拾い直してから、マスごとのコマを貼る。
        Collect();
        RepaintAll();
    }

    /// <summary>盤面ぜんぶを、マスごとに覚えているコマで貼り直す。</summary>
    void RepaintAll()
    {
        foreach (var cell in onBoard.Keys) Repaint(cell);
    }

    /// <summary>盤面に出ている「動くもの」を拾い直す。</summary>
    void Collect()
    {
        onBoard.Clear();
        kindOnBoard.Clear();
        var parent = loader.Container;
        if (parent == null) return;
        foreach (Transform child in parent)
        {
            // FloorLoader は "{名前}_{x}_{y}" で作る
            var name = child.name;
            int cut = name.LastIndexOf('_');
            if (cut <= 0) continue;
            int cut2 = name.LastIndexOf('_', cut - 1);
            if (cut2 <= 0) continue;
            var key = name.Substring(0, cut2);
            int index;
            if (!indexOfKey.TryGetValue(key, out index)) continue;
            int x, y;
            if (!int.TryParse(name.Substring(cut2 + 1, cut - cut2 - 1), out x)) continue;
            if (!int.TryParse(name.Substring(cut + 1), out y)) continue;
            var sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            var cell = new Vector2Int(x, y);
            onBoard[cell] = sr;
            kindOnBoard[cell] = index;
        }
    }

    /// <summary>そのマスの絵を、今のコマに合わせて貼り直す。</summary>
    void Repaint(Vector2Int cell)
    {
        SpriteRenderer sr;
        int index;
        if (!onBoard.TryGetValue(cell, out sr) || !kindOnBoard.TryGetValue(cell, out index)) return;
        if (sr == null) return;                 // 倒された等で消えている
        var sprite = anim.FrameAt(cell) ? At(firstFrames, index) : At(secondFrames, index);
        if (sprite != null) sr.sprite = sprite;
    }

    static Sprite At(Sprite[] a, int i) { return i >= 0 && i < a.Length ? a[i] : null; }
}
