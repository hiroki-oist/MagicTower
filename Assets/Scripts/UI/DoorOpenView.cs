using System.Collections;
using UnityEngine;

/// <summary>
/// 鍵で扉を開けたときの9コマの動きを出す役。規則は <see cref="DoorAnimation"/>
/// （原典 PC98版 MTE.EXE の `0x07be6`）。
///
/// 原典は地図を先に床へ書き換えてから、そのマスに9コマを1つずつ貼る。
/// こちらも同じで、盤面の側はもう床になっているところへ**上から**9コマを重ねる。
/// 閉まるとき（原典 0x07b26）は同じ絵を**逆から**貼る。地図を先に門へ書き換えるのも同じ。
/// </summary>
public class DoorOpenView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] string[] colors = new string[0];
    [SerializeField] Sprite[] frames = new Sprite[0];     // colors.Length x DoorAnimation.Frames

    /// <summary>今出しているか。</summary>
    public bool IsPlaying { get { return running > 0 || queue.Count > 0; } }

    int running;
    GameObject cel;

    /// <summary>
    /// これから出す扉の待ち行列。**1枚ずつ順に**出す。
    /// 原典の解錠（0x4762）は門1枚ごとに 0x7a68 を呼んで9コマを出し切るので、
    /// 何枚も同時に開くことはない（10階で金の骸骨を倒したとき、まとめて消えて
    /// 見えるという報告があった）。
    /// </summary>
    readonly System.Collections.Generic.Queue<TowerGame.DoorOpen> queue =
        new System.Collections.Generic.Queue<TowerGame.DoorOpen>();

    bool draining;

    void Awake()
    {
        cel = new GameObject("DoorFrame", typeof(SpriteRenderer));
        cel.transform.SetParent(transform, false);
        cel.GetComponent<SpriteRenderer>().sortingOrder = 120;
        cel.SetActive(false);
    }

    void LateUpdate()
    {
        if (controller == null || controller.Game == null) return;
        var list = controller.Game.ConsumeDoorOpens();
        for (int i = 0; i < list.Count; i++) queue.Enqueue(list[i]);
        if (!draining && queue.Count > 0 && isActiveAndEnabled) StartCoroutine(Drain());
    }

    /// <summary>待ち行列を頭から1枚ずつ。</summary>
    IEnumerator Drain()
    {
        draining = true;
        while (queue.Count > 0) yield return Play(queue.Dequeue());
        draining = false;
    }

    IEnumerator Play(TowerGame.DoorOpen door)
    {
        int at = IndexOf(DoorAnimation.ColorOf(door.Cell));
        if (at < 0 || cel == null) yield break;
        running++;
        var sr = cel.GetComponent<SpriteRenderer>();
        cel.transform.position = new Vector3(door.At.x, door.At.y, -1f);
        cel.SetActive(true);
        for (int f = 0; f < DoorAnimation.Frames; f++)
        {
            // 閉まるときは同じ9コマを逆から貼る（原典 0x07b26 は 0xa00 ずつ引く）
            int k = at * DoorAnimation.Frames
                    + (door.Closing ? DoorAnimation.Frames - 1 - f : f);
            if (k < frames.Length) sr.sprite = frames[k];
            yield return new WaitForSeconds(DoorAnimation.FrameSeconds);
        }
        cel.SetActive(false);
        // 原典は9コマのあと盤面の動きだけを回す（鍵の扉は 0x9288(8)、門は 0x9288(0x14)）
        yield return new WaitForSeconds(
            DoorAnimation.TailOf(door.Cell) * DoorAnimation.FrameSeconds);
        running--;
    }

    int IndexOf(string color)
    {
        if (string.IsNullOrEmpty(color)) return -1;
        for (int i = 0; i < colors.Length; i++)
            if (colors[i] == color) return i;
        return -1;
    }
}
