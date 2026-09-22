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

    /// <summary>
    /// 今この役が絵を持っているマス。**盤面の描き直しを止めておく**ための印。
    ///
    /// 原典は地図を書き換えても**その場では描き直さず**、9コマの動きの中で
    /// そのマスを塗っていく。こちらは書き換えるとすぐ盤面に映るので、
    /// 「扉が一瞬消えてから動きが始まる」「閉まる前に門が現れる」ように見えていた。
    /// </summary>
    readonly System.Collections.Generic.HashSet<Vector2Int> held =
        new System.Collections.Generic.HashSet<Vector2Int>();

    /// <summary>そのマスの描き直しを止めているか（本線の見た目合わせが見る）。</summary>
    public bool Holds(Vector2Int at) { return held.Contains(at); }

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
        for (int i = 0; i < list.Count; i++)
        {
            queue.Enqueue(list[i]);
            // 描き直しを待たせるのは**間をおいて開くもの**（解錠の門）だけ。
            // 鍵の扉は積んだ次のコマで動き出すので待たせる必要がなく、
            // 待たせると別の不具合の元になる（作者から「通常の扉が変」と報告）。
            if (list[i].Delayed) held.Add(list[i].At);
        }
        if (!draining && queue.Count > 0 && isActiveAndEnabled) StartCoroutine(Drain());
    }

    /// <summary>待ち行列を頭から1枚ずつ。</summary>
    IEnumerator Drain()
    {
        draining = true;
        // 戦いのあとに開く扉は、戦闘の箱が消えてからひと呼吸おいてから開く（作者の指定）。
        // その間もマスは**扉のまま**に見せる（下の Play が最初のコマを出す）。
        if (queue.Count > 0 && queue.Peek().Delayed)
        {
            var first = queue.Peek();
            ShowFirstFrame(first);
            yield return new WaitForSeconds(AuthorStyle.SettleSeconds);
        }
        while (queue.Count > 0) yield return Play(queue.Dequeue());
        draining = false;
    }

    /// <summary>待ちの間、そのマスに最初のコマを出しておく。</summary>
    void ShowFirstFrame(TowerGame.DoorOpen door)
    {
        int at = IndexOf(DoorAnimation.ColorOf(door.Cell));
        if (at < 0 || cel == null) return;
        var sr = cel.GetComponent<SpriteRenderer>();
        cel.transform.position = new Vector3(door.At.x, door.At.y, -1f);
        int f = at * DoorAnimation.Frames + (door.Closing ? DoorAnimation.Frames - 1 : 0);
        if (f < frames.Length) sr.sprite = frames[f];
        cel.SetActive(true);
    }

    IEnumerator Play(TowerGame.DoorOpen door)
    {
        int at = IndexOf(DoorAnimation.ColorOf(door.Cell));
        if (at < 0 || cel == null) { held.Remove(door.At); yield break; }
        running++;
        var sr = cel.GetComponent<SpriteRenderer>();
        cel.transform.position = new Vector3(door.At.x, door.At.y, -1f);
        // 最初のコマ（開くなら閉じた扉、閉まるなら開いた扉）をまず出しておく。
        // 待ちの間このマスが空に見えてしまうのを防ぐ。
        int firstFrame = at * DoorAnimation.Frames
                       + (door.Closing ? DoorAnimation.Frames - 1 : 0);
        if (firstFrame < frames.Length) sr.sprite = frames[firstFrame];
        cel.SetActive(true);
        // 原典 0x7be6 / 0x7a68 / 0x7b26 はどれも**9コマの頭で**音11を鳴らす。
        if (controller != null && controller.Sound != null)
            // 門（封印扉）は 0x7a68、鍵の扉は 0x7be6。どちらも音11だが名前を分けてある。
            controller.Sound.PlayCue(door.Cell == "s" ? SoundCues.OpenGate
                                                      : SoundCues.OpenDoor);
        for (int f = 0; f < DoorAnimation.Frames; f++)
        {
            // 閉まるときは同じ9コマを逆から貼る（原典 0x07b26 は 0xa00 ずつ引く）
            int k = at * DoorAnimation.Frames
                    + (door.Closing ? DoorAnimation.Frames - 1 - f : f);
            if (k < frames.Length) sr.sprite = frames[k];
            yield return new WaitForSeconds(DoorAnimation.FrameSeconds);
        }
        cel.SetActive(false);
        held.Remove(door.At);           // ここで初めて盤面の絵に任せる
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
