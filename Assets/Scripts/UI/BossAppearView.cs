using System.Collections;
using UnityEngine;

/// <summary>
/// 20階のボスが現れる動きを出す役。規則は <see cref="BossAppear"/>
/// （原典 PC98版 MTE.EXE の `0x03044` の中、0x3119〜0x3194）。
///
/// 96x96（＝3x3マス）の絵を10コマ、盤面の中心に順に貼る。1コマ 0x19(25)/60秒。
/// 原典はこの動きが終わってから地図の三つ組を当てて、セリフ121を出す。
/// </summary>
public class BossAppearView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] Sprite[] frames = new Sprite[0];

    /// <summary>今出しているか。会話の窓はこれが終わるまで待つ。</summary>
    public bool IsPlaying { get { return running > 0; } }

    int running;
    GameObject cel;

    void Awake()
    {
        cel = new GameObject("BossAppearFrame", typeof(SpriteRenderer));
        cel.transform.SetParent(transform, false);
        cel.GetComponent<SpriteRenderer>().sortingOrder = 130;
        cel.SetActive(false);
    }

    void LateUpdate()
    {
        if (controller == null || controller.Game == null) return;
        foreach (var at in controller.Game.ConsumeBossAppears())
            if (isActiveAndEnabled) StartCoroutine(Play(at));
    }

    IEnumerator Play(Vector2Int at)
    {
        if (cel == null || frames.Length == 0) yield break;
        running++;
        var sr = cel.GetComponent<SpriteRenderer>();
        cel.transform.position = new Vector3(at.x, at.y, -1f);
        cel.SetActive(true);
        for (int f = 0; f < BossAppear.Frames; f++)
        {
            if (f < frames.Length) sr.sprite = frames[f];
            yield return new WaitForSeconds(BossAppear.FrameSeconds);
        }
        cel.SetActive(false);
        running--;
    }
}
