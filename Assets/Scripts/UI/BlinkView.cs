using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 場面でものが現れるときの**点滅**を出す役。原典 PC98版 MTE.EXE の 3階の場面（0x6ae0）。
///
/// <code>
///   do {  マスに 床(6) を書いて そのマスを描く      （0xae6e）
///         マスに ゼノ(0x5b) を書いて そのマスを描く
///   } while (++i &lt; 0x32);            ; 50回。**待ちは入らない**
///   0x1e(30) 待つ                      （lcall 0xf5f,0x3f2）
/// </code>
///
/// 魔導師（0x5d）は 4マスに各 0xf(15) 回、1マスごとに 0xa(10) 待つ。
///
/// ⚠ **速さは原典と同じにできない**。原典は待ちを入れずに描けるだけ速く入れ替えるので、
/// 1回ぶんは画面の1コマ（1/60秒）より短い。こちらは1コマに1回しか描けないので、
/// **1回＝1コマ**にしてある。回数と待ちは原典のまま、速さだけが違う。
/// </summary>
public class BlinkView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] FloorLoader loader;

    /// <summary>点滅の最中か。会話の窓はこれが終わるまで待つ（原典も点滅のあとに出す）。</summary>
    public bool IsPlaying { get { return running > 0 || queue.Count > 0; } }

    int running;

    /// <summary>
    /// これから点滅させるものの待ち行列。**1つずつ順に**出す。
    ///
    /// 原典（0x6ae0）は魔導師4体を「1体ぶん点滅させて 0xa 待つ」を4回くり返すので、
    /// **4体が同時に現れることはない**。ここで一斉に走らせていたため
    /// 「魔導師も一個ずつ順番に現れてほしい」と報告された。
    /// </summary>
    readonly Queue<Cutscenes.MapStep> queue = new Queue<Cutscenes.MapStep>();

    bool draining;

    void LateUpdate()
    {
        if (controller == null || controller.Game == null || loader == null) return;
        var list = controller.Game.ConsumeBlinks();
        for (int i = 0; i < list.Count; i++)
        {
            // 順番が来るまで**見せない**。原典は点滅を始めるときに初めて地図へ書くので、
            // 待っている間は床のまま（先に4体とも見えてしまってはいけない）。
            var waiting = loader.ObjectAt(list[i].Pos);
            if (waiting != null) waiting.SetActive(false);
            queue.Enqueue(list[i]);
        }
        if (!draining && queue.Count > 0 && isActiveAndEnabled)
            StartCoroutine(Drain());
    }

    /// <summary>待ち行列を頭から1つずつ。</summary>
    IEnumerator Drain()
    {
        draining = true;
        // **歩き終わってから**出す（歩いている途中に現れると、着く前に始まって見える）
        while (controller != null && controller.IsSliding) yield return null;
        while (queue.Count > 0) yield return Play(queue.Dequeue());
        draining = false;
    }

    IEnumerator Play(Cutscenes.MapStep step)
    {
        running++;
        var target = loader.ObjectAt(step.Pos);
        if (target != null)
        {
            // 原典は「床を描く → ものを描く」の順。見えない状態から始まる。
            for (int i = 0; i < step.Blinks; i++)
            {
                target.SetActive(false);
                yield return null;
                target.SetActive(true);
                yield return null;
                if (target == null) break;
            }
            if (target != null) target.SetActive(true);
        }
        if (step.Wait > 0)
            yield return new WaitForSeconds(step.Wait * MonsterAnimation.FrameSeconds);
        running--;
    }
}
