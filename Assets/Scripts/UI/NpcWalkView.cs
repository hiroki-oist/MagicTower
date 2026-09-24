using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 場面の中で**人が1マスずつ歩いていく**のを出す役。
///
/// 原典 PC98版は、序章の盗賊が牢を出ていくところ（MTE.EXE 0x070dc〜0x07129）でも、
/// 10階で骸骨が寄ってくるところ（0x03e0a）でも、向きの表を読みながら
/// **主人公と同じ1マス滑らせる処理 `0xa276`** を呼んでいる。
/// だから歩く速さ（16コマ／マス）も主人公とまったく同じ。
/// 歩き終えると最後のマスに物体が書かれるので、そこに立ったまま残る。
///
/// 本線（<see cref="TowerGame"/>）は地図をすぐ書き換えてしまうので、
/// ここでは**行き先に置かれた絵をいったん隠し**、仮の絵を道順どおりに滑らせてから、
/// 着いたところで本物を出す。盤面に載らない人（10階の盗賊）は
/// <see cref="TowerGame.NpcWalk.Stay"/> が立っていて、仮の絵のまま立たせておく。
///
/// **一度に1人ずつ**しか歩かない。原典は骸骨を1体ずつ寄せてくる（0x4096 の輪）ので、
/// 積まれた順に並べて流す。
///
/// ★**絵も2コマで動かす**（作者の報告「10階の骸骨が動かない」「32階の騎士の画像が
/// 止まってる」）。原典 0xa276 は1マス滑らせる間に 0x92a4 を6回呼ぶので、
/// 歩いている本人も盤面の物と同じように**1コマ目と2コマ目が入れ替わる**。
/// こちらは主人公と同じ見せ方（1マスの半ばで入れ替える。<see cref="TowerController"/>）に
/// 揃えてある。立ち止まっている間も、盤面と同じ拍子で入れ替える
/// （原典では立ち止まった相手は地図に書かれていて、盤面の仕掛けが塗り直すため）。
/// </summary>
public class NpcWalkView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] FloorLoader loader;
    [SerializeField] FloorPrefabRegistry registry;
    /// <summary>2コマの絵を引くところ（盤面の敵と人と同じ表）。</summary>
    [SerializeField] MonsterAnimator monsters;

    /// <summary>歩いている最中か。会話の窓はこれが終わるまで待つ。</summary>
    public bool IsPlaying { get { return running || queue.Count > 0; } }

    bool running;
    readonly Queue<TowerGame.NpcWalk> queue = new Queue<TowerGame.NpcWalk>();

    /// <summary>着いたあとも立たせてある人（マス → 絵）。</summary>
    readonly Dictionary<Vector2Int, GameObject> standing =
        new Dictionary<Vector2Int, GameObject>();

    /// <summary>その絵がどの名前のものか（2コマ目を引くのに要る）。</summary>
    readonly Dictionary<GameObject, string> keyOf = new Dictionary<GameObject, string>();

    void LateUpdate()
    {
        if (controller == null || controller.Game == null || loader == null) return;

        var list = controller.Game.ConsumeWalks();
        for (int i = 0; i < list.Count; i++) queue.Enqueue(list[i]);

        foreach (var at in controller.Game.ConsumeVanishes()) Vanish(at);

        if (!running && queue.Count > 0 && isActiveAndEnabled)
            StartCoroutine(Walk(queue.Dequeue()));

        // 立ち止まっている人も、盤面の物と同じ拍子で2コマを入れ替える。
        // 原典では立った相手は地図に書かれていて 0x92a4 が塗り直すので、
        // ここだけ止まっていると「1人だけ絵が固まっている」ように見える。
        if (monsters != null)
            foreach (var pair in standing)
                Paint(pair.Value, monsters.FrameAt(pair.Key) ? 0 : 1);
    }

    /// <summary>その絵を1コマ目(0)／2コマ目(1)に貼り替える。</summary>
    void Paint(GameObject who, int frame)
    {
        if (who == null || monsters == null) return;
        string key;
        if (!keyOf.TryGetValue(who, out key)) return;
        Sprite first, second;
        if (!monsters.TryFrames(key, out first, out second)) return;
        var sr = who.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        var want = frame == 0 ? first : second;
        if (want != null && sr.sprite != want) sr.sprite = want;
    }

    /// <summary>そのマスに立たせてある人（場面で歩いてきた相手）。無ければ null。</summary>
    public GameObject StandingAt(Vector2Int at)
    {
        GameObject who;
        return standing.TryGetValue(at, out who) ? who : null;
    }

    /// <summary>立たせてある人を消す（原典 0x45be はそのマスを地図どおりに描き直す）。</summary>
    void Vanish(Vector2Int at)
    {
        GameObject who;
        if (!standing.TryGetValue(at, out who)) return;
        standing.Remove(at);
        if (who != null) { keyOf.Remove(who); Destroy(who); }
    }

    IEnumerator Walk(TowerGame.NpcWalk walk)
    {
        if (walk == null || walk.Path == null || walk.Path.Count == 0) yield break;
        running = true;

        var goal = walk.Path[walk.Path.Count - 1];
        // 行き先に置かれた本物は、着くまで見せない
        var real = walk.Stay ? null : loader.ObjectAt(goal);
        if (real != null) real.SetActive(false);

        // すでにそこに立たせてある人がいれば、その絵をそのまま動かす
        // （32階の金騎士は「降りてくる → 戦う → 逃げる」と2回に分けて歩く）
        GameObject mover;
        if (standing.TryGetValue(walk.From, out mover)) standing.Remove(walk.From);
        else mover = Spawn(walk.Prefab, walk.From);
        if (mover != null) keyOf[mover] = walk.Prefab;
        var at = walk.From;
        foreach (var to in walk.Path)
        {
            yield return Slide(mover, at, to, walk.Slow);
            at = to;
        }
        if (walk.Stay)
        {
            if (mover != null) standing[goal] = mover;
        }
        else
        {
            if (mover != null) { keyOf.Remove(mover); Destroy(mover); }
            if (real != null) real.SetActive(true);
        }
        running = false;
    }

    /// <summary>1マスぶん滑らせる。速さは主人公と同じ（原典 0xa276 の16コマ）。</summary>
    IEnumerator Slide(GameObject who, Vector2Int from, Vector2Int to, bool slow = false)
    {
        if (who == null) yield break;
        var a = new Vector3(from.x, from.y, -1f);
        var b = new Vector3(to.x, to.y, -1f);
        float dur = MonsterAnimation.StepSeconds(false) * (slow ? 2f : 1f);
        // 主人公と同じで、1マスの**半ばで2コマ目に入れ替える**（原典 0xa276 は
        // 滑らせる間に 0x92a4 を6回呼ぶ）。横に歩くときだけ入れ替えるのが原典だが、
        // 縦でも入れ替えるのは作者の指定（<see cref="AuthorStyle.WalkAnimationBothWays"/>）。
        bool swap = AuthorStyle.WalkAnimationBothWays || (from.y == to.y && from.x != to.x);
        float t = 0f;
        while (t < dur)
        {
            if (who == null) yield break;
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            who.transform.position = Vector3.Lerp(a, b, k);
            if (swap) Paint(who, k <= 0.5f ? 0 : 1);
            yield return null;
        }
        if (who != null) { who.transform.position = b; Paint(who, 0); }
    }

    GameObject Spawn(string key, Vector2Int at)
    {
        if (registry == null) return null;
        var prefab = registry.Get(key);
        if (prefab == null) return null;
        var go = Instantiate(prefab, new Vector3(at.x, at.y, -1f), Quaternion.identity, transform);
        go.name = "Walking_" + key;
        return go;
    }
}
