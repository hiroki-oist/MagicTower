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
    public bool IsPlaying { get { return running > 0; } }

    int running;

    void LateUpdate()
    {
        if (controller == null || controller.Game == null || loader == null) return;
        var list = controller.Game.ConsumeBlinks();
        for (int i = 0; i < list.Count; i++)
            if (isActiveAndEnabled) StartCoroutine(Play(list[i]));
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
