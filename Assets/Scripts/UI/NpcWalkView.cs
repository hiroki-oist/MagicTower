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
/// </summary>
public class NpcWalkView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] FloorLoader loader;
    [SerializeField] FloorPrefabRegistry registry;

    /// <summary>歩いている最中か。会話の窓はこれが終わるまで待つ。</summary>
    public bool IsPlaying { get { return running || queue.Count > 0; } }

    bool running;
    readonly Queue<TowerGame.NpcWalk> queue = new Queue<TowerGame.NpcWalk>();

    /// <summary>着いたあとも立たせてある人（マス → 絵）。</summary>
    readonly Dictionary<Vector2Int, GameObject> standing =
        new Dictionary<Vector2Int, GameObject>();

    void LateUpdate()
    {
        if (controller == null || controller.Game == null || loader == null) return;

        var list = controller.Game.ConsumeWalks();
        for (int i = 0; i < list.Count; i++) queue.Enqueue(list[i]);

        foreach (var at in controller.Game.ConsumeVanishes()) Vanish(at);

        if (!running && queue.Count > 0 && isActiveAndEnabled)
            StartCoroutine(Walk(queue.Dequeue()));
    }

    /// <summary>立たせてある人を消す（原典 0x45be はそのマスを地図どおりに描き直す）。</summary>
    void Vanish(Vector2Int at)
    {
        GameObject who;
        if (!standing.TryGetValue(at, out who)) return;
        standing.Remove(at);
        if (who != null) Destroy(who);
    }

    IEnumerator Walk(TowerGame.NpcWalk walk)
    {
        if (walk == null || walk.Path == null || walk.Path.Count == 0) yield break;
        running = true;

        var goal = walk.Path[walk.Path.Count - 1];
        // 行き先に置かれた本物は、着くまで見せない
        var real = walk.Stay ? null : loader.ObjectAt(goal);
        if (real != null) real.SetActive(false);

        var mover = Spawn(walk.Prefab, walk.From);
        var at = walk.From;
        foreach (var to in walk.Path)
        {
            yield return Slide(mover, at, to);
            at = to;
        }
        if (walk.Stay)
        {
            if (mover != null) standing[goal] = mover;
        }
        else
        {
            if (mover != null) Destroy(mover);
            if (real != null) real.SetActive(true);
        }
        running = false;
    }

    /// <summary>1マスぶん滑らせる。速さは主人公と同じ（原典 0xa276 の16コマ）。</summary>
    IEnumerator Slide(GameObject who, Vector2Int from, Vector2Int to)
    {
        if (who == null) yield break;
        var a = new Vector3(from.x, from.y, -1f);
        var b = new Vector3(to.x, to.y, -1f);
        float dur = MonsterAnimation.StepSeconds(false);
        float t = 0f;
        while (t < dur)
        {
            if (who == null) yield break;
            t += Time.deltaTime;
            who.transform.position = Vector3.Lerp(a, b, Mathf.Clamp01(t / dur));
            yield return null;
        }
        if (who != null) who.transform.position = b;
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
