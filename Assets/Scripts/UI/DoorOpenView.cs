using System.Collections;
using UnityEngine;

/// <summary>
/// 鍵で扉を開けたときの9コマの動きを出す役。規則は <see cref="DoorAnimation"/>
/// （原典 PC98版 MTE.EXE の `0x07be6`）。
///
/// 原典は地図を先に床へ書き換えてから、そのマスに9コマを1つずつ貼る。
/// こちらも同じで、盤面の側はもう床になっているところへ**上から**9コマを重ねる。
/// </summary>
public class DoorOpenView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] string[] colors = new string[0];
    [SerializeField] Sprite[] frames = new Sprite[0];     // colors.Length x DoorAnimation.Frames

    /// <summary>今出しているか。</summary>
    public bool IsPlaying { get { return running > 0; } }

    int running;
    GameObject cel;

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
            if (isActiveAndEnabled) StartCoroutine(Play(list[i]));
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
            int k = at * DoorAnimation.Frames + f;
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
