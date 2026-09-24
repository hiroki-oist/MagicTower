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

    /// <summary>
    /// 点滅しながら**消える**もののマス。地図はもう床になっているので、
    /// 点滅が終わるまで <see cref="TowerController"/> に描き直しを待たせる。
    /// </summary>
    readonly HashSet<Vector2Int> held = new HashSet<Vector2Int>();

    /// <summary>そのマスをまだ手放していないか。</summary>
    public bool Holds(Vector2Int at) { return held.Contains(at); }

    void LateUpdate()
    {
        if (controller == null || controller.Game == null || loader == null) return;
        var list = controller.Game.ConsumeBlinks();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Kind == "床")
            {
                // 消える側。**出したまま**で順番を待つ（消してから点滅はできない）。
                held.Add(list[i].Pos);
            }
            else
            {
                // 順番が来るまで**見せない**。原典は点滅を始めるときに初めて地図へ書くので、
                // 待っている間は床のまま（先に4体とも見えてしまってはいけない）。
                var waiting = loader.ObjectAt(list[i].Pos);
                if (waiting != null) waiting.SetActive(false);
            }
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
        if (step.Kind == "床") { yield return Fade(step); yield break; }
        running++;
        var target = loader.ObjectAt(step.Pos);
        // 地図の書き換えが見た目に届くのが1コマ遅れることがある。
        // そこで諦めると**点滅せずに出たまま**になり、セリフと同時に現れて見える。
        for (int i = 0; i < 10 && target == null; i++)
        {
            yield return null;
            target = loader.ObjectAt(step.Pos);
        }
        // **元が床でないなら、そのものと入れ替えながら点滅させる**。
        // 原典は「元の値」と「新しい値」を交互に地図へ書く。3階（0x6ae0）は元が床(6)
        // なので出たり消えたりに見えるが、終幕（0xcb52〜0xcbee）は**元が盗賊(21)**で、
        // 盗賊とゼノ(91)を50回入れ替える＝**盗賊がゼノに変わって見える**。
        // ここを床としてしか扱っていなかったので、50階で誰も出てこなかった（作者の報告）。
        var fromKey = FloorState.ViewKeyOf(step.From);
        if (!string.IsNullOrEmpty(fromKey) && fromKey != FloorState.ViewKeyOf(
                FloorState.SymbolOfKind(step.Kind)))
            yield return Swap(step, fromKey, target);
        else if (target != null)
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

    /// <summary>
    /// **ものが別のものに変わる**（元が床でないときの入れ替え）。
    ///
    /// 原典（終幕 0xcb52〜0xcbee）は盗賊(21)とゼノ(91)を**50回**そのまま交互に描くだけ。
    /// 作者の指定で、<see cref="AuthorStyle.SwapSeconds"/> のあいだ
    /// **新しい方を出す割合を 0 から 1 へ上げていく** — はじめは元のもの、
    /// だんだん新しい方が混ざり、最後は完全に新しい方になって点滅が止まる。
    ///
    /// 割合はコマごとに足し込んで1を超えたら新しい方を出す（誤差を持ち越すので、
    /// どのコマ数でも見た目の割合がその時点の値どおりになる）。
    /// 地図はもう新しい方に書き換わっているので、**元の方を重ねて出して**入れ替える。
    /// </summary>
    IEnumerator Swap(Cutscenes.MapStep step, string fromKey, GameObject to)
    {
        var from = loader.SpawnExtra(fromKey, step.Pos);
        float total = AuthorStyle.SwapSeconds;
        float t = 0f, acc = 0f;
        while (t < total)
        {
            float p = total <= 0f ? 1f : t / total;      // 新しい方を出す割合
            acc += p;
            bool showTo = acc >= 1f;
            if (showTo) acc -= 1f;
            if (to != null) to.SetActive(showTo);
            if (from != null) from.SetActive(!showTo);
            yield return null;
            t += Time.deltaTime;
        }
        if (from != null) Destroy(from);
        if (to != null) to.SetActive(true);
    }

    /// <summary>
    /// 点滅しながら消える（作者の指定）。出てきたときと同じ回数だけ入れ替えて、
    /// 最後は床のまま終わる。42階の場面の終わりでゼノと魔導士を片付けるのに使う
    /// （原典 0x7182 はただ地図を床に書き戻すだけで、消え方は決めていない）。
    /// </summary>
    IEnumerator Fade(Cutscenes.MapStep step)
    {
        running++;
        var target = loader.ObjectAt(step.Pos);
        if (target != null)
        {
            for (int i = 0; i < step.Blinks; i++)
            {
                target.SetActive(false);
                yield return null;
                if (target == null) break;
                target.SetActive(true);
                yield return null;
            }
            if (target != null) target.SetActive(false);
        }
        if (step.Wait > 0)
            yield return new WaitForSeconds(step.Wait * MonsterAnimation.FrameSeconds);
        // ここで手を離すと、本線のとおり床が描き直される
        held.Remove(step.Pos);
        if (controller != null) controller.RefreshCell(step.Pos);
        running--;
    }
}
