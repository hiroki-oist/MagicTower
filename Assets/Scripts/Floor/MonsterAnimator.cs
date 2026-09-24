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
    float clock;
    /// <summary>最後に拾ったときの盤面の版（<see cref="FloorLoader.Version"/>）。</summary>
    int seenVersion = -1;

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
            seenVersion = loader.Version;
            Collect();
            RepaintAll();
            return;
        }

        // **時間で回す**。原典は歩いたときと待ちの間だけ回すが、
        // 「自分が動かないと敵が動かない」と報告されたので常に回す。
        // 速さは <see cref="MonsterAnimation.IdleTickSeconds"/>（約1秒で盤面を一周）。
        if (p.Position != lastPos) { lastPos = p.Position; Collect(); }
        // ★**盤面の中身が変わったら拾い直す**。場面で現れた相手（10階の骸骨、
        // 20階のバンパイア）は、主人公が動くまで拾われないので絵が止まっていた
        // （作者の報告「戦い始めるまで骸骨が動かない」）。
        if (seenVersion != loader.Version) { seenVersion = loader.Version; Collect(); }
        clock += Time.deltaTime;
        while (clock >= MonsterAnimation.IdleTickSeconds)
        {
            clock -= MonsterAnimation.IdleTickSeconds;
            anim.Tick();
        }
        RepaintAll();
    }

    /// <summary>
    /// 1コマぶん進める。移動の演出（原典 0xa276）はコマごとに 0x92a4 を1回呼ぶので、
    /// 滑らかな移動を出している側がこれを毎コマ叩く。
    /// </summary>
    public void TickOnce()
    {
        if (controller == null || controller.Game == null || loader == null) return;
        lastPos = controller.Game.Player.Position;   // まとめ進みの二重取りを防ぐ
        anim.Tick();
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

    /// <summary>
    /// その名前の1コマ目・2コマ目の絵を返す（2コマで動くものでなければ false）。
    /// 場面の中で**歩いている人**（<see cref="NpcWalkView"/>）も、盤面の物と同じ
    /// 2枚で動かすためにここを引く。表は1か所（この役）にまとめてある。
    /// </summary>
    public bool TryFrames(string key, out Sprite first, out Sprite second)
    {
        first = second = null;
        int index;
        if (key == null || !indexOfKey.TryGetValue(key, out index)) return false;
        first = At(firstFrames, index);
        second = At(secondFrames, index);
        return first != null && second != null;
    }

    /// <summary>そのマスが今どちらのコマか（原典 0x92a4 の旗）。</summary>
    public bool FrameAt(Vector2Int cell) { return anim.FrameAt(cell); }

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
