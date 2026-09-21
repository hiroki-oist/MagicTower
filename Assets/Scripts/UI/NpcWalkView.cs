using System.Collections;
using UnityEngine;

/// <summary>
/// 場面の中で**人が1マスずつ歩いていく**のを出す役。
///
/// 原典 PC98版は、序章の盗賊が牢を出ていくところ（MTE.EXE 0x070dc〜0x07129）で
/// 向きの表を読みながら **主人公と同じ1マス滑らせる処理 `0xa276`** を呼んでいる。
/// だから歩く速さ（16コマ／マス）も主人公とまったく同じ。
/// 歩き終えると 0x07153 が最後のマスに ID21 を書くので、盗賊はそこに立ったまま残る。
///
/// 本線（<see cref="TowerGame"/>）は地図をすぐ書き換えてしまうので、
/// ここでは**行き先に置かれた絵をいったん隠し**、仮の絵を道順どおりに滑らせてから、
/// 着いたところで本物を出す。
/// </summary>
public class NpcWalkView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] FloorLoader loader;
    [SerializeField] FloorPrefabRegistry registry;

    /// <summary>歩いている最中か。会話の窓はこれが終わるまで待つ。</summary>
    public bool IsPlaying { get { return running > 0; } }

    int running;

    void LateUpdate()
    {
        if (controller == null || controller.Game == null || loader == null) return;
        var list = controller.Game.ConsumeWalks();
        for (int i = 0; i < list.Count; i++)
            if (isActiveAndEnabled) StartCoroutine(Walk(list[i]));
    }

    IEnumerator Walk(TowerGame.NpcWalk walk)
    {
        if (walk == null || walk.Path == null || walk.Path.Count == 0) yield break;
        running++;

        // 行き先に置かれた本物は、着くまで見せない
        var goal = loader.ObjectAt(walk.Path[walk.Path.Count - 1]);
        if (goal != null) goal.SetActive(false);

        var mover = Spawn(walk.Prefab, walk.From);
        var at = walk.From;
        foreach (var to in walk.Path)
        {
            yield return Slide(mover, at, to);
            at = to;
        }
        if (mover != null) Destroy(mover);
        if (goal != null) goal.SetActive(true);
        running--;
    }

    /// <summary>1マスぶん滑らせる。速さは主人公と同じ（原典 0xa276 の16コマ）。</summary>
    IEnumerator Slide(GameObject who, Vector2Int from, Vector2Int to)
    {
        if (who == null) yield break;
        var a = new Vector3(from.x, from.y, -1f);
        var b = new Vector3(to.x, to.y, -1f);
        int n = MonsterAnimation.TicksPerStep;
        for (int i = 1; i <= n; i++)
        {
            if (who == null) yield break;
            who.transform.position = Vector3.Lerp(a, b, i / (float)n);
            yield return new WaitForSeconds(MonsterAnimation.SlideFrameSeconds);
        }
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
