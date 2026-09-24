using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Java版（2001年の公式移植）の終幕の動き（作者の指定。<see cref="AuthorStyle.JavaEnding"/>）。
///
/// Java版 `MTower.class` の `OpenningZeno(4)` は `data2/edata01.gif`〜`edata15.gif` を
/// 順に出し、**1枚ごとに 40x40 のマス目のモザイクで溶かして**切り替える
/// （PixelGrabber で 40x40 ずつ画素を取り、組み直して描き直す。`twait(100)` ずつ待つ）。
/// 絵は塔が地面へ沈んでいく15枚で、1枚ずつ描いてある。
///
/// ここでは次の1枚を **16x10＝160マス**の小さな板に分けて持っておき、
/// でたらめな順に1マスずつ出していく。全部出たらその1枚を地にして次へ進む。
/// </summary>
public class EndingMovieView : MonoBehaviour
{
    [SerializeField] TowerController controller;

    /// <summary>いま出ている1枚（下地）。</summary>
    [SerializeField] Image back;

    /// <summary>次の1枚を分けて出すマス（左上から横に 16x10）。</summary>
    [SerializeField] RawImage[] cells = new RawImage[0];

    /// <summary>塔が沈んでいく絵（edata01〜15）。</summary>
    [SerializeField] Sprite[] frames = new Sprite[0];

    bool playing, done;
    Coroutine run;

    public bool IsPlaying { get { return playing; } }

    void Awake() { Hide(); }

    void Hide()
    {
        if (back != null) back.enabled = false;
        for (int i = 0; i < cells.Length; i++)
            if (cells[i] != null) cells[i].enabled = false;
    }

    void LateUpdate()
    {
        if (controller == null || controller.Game == null || !AuthorStyle.JavaEnding) return;
        bool mine = controller.Game.CurrentKind == TowerGame.PageKind.EndingTower;
        if (mine && !done && isActiveAndEnabled)
        {
            done = true;
            run = StartCoroutine(Play());
        }
        if (!mine && done)
        {
            done = false;
            if (run != null) { StopCoroutine(run); run = null; }
            playing = false;
            Hide();
        }
    }

    IEnumerator Play()
    {
        playing = true;
        if (frames.Length == 0 || back == null) { playing = false; yield break; }

        // でたらめな順にマスを出す。順番は毎回作り直す（同じ模様が続くと目につく）
        var order = new int[cells.Length];
        for (int i = 0; i < order.Length; i++) order[i] = i;

        back.enabled = true;
        back.sprite = frames[0];
        for (int i = 0; i < cells.Length; i++)
            if (cells[i] != null) cells[i].enabled = false;

        for (int f = 1; f < frames.Length; f++)
        {
            Shuffle(order);
            var tex = frames[f] != null ? frames[f].texture : null;
            for (int i = 0; i < cells.Length; i++)
                if (cells[i] != null) { cells[i].texture = tex; cells[i].enabled = false; }

            float step = Ending.JavaFrameSeconds;
            float t = 0f;
            int shown = 0;
            while (t < step)
            {
                int want = Mathf.Clamp(Mathf.CeilToInt(order.Length * (t / step)),
                                       0, order.Length);
                for (; shown < want; shown++)
                    if (cells[order[shown]] != null) cells[order[shown]].enabled = true;
                yield return null;
                t += Time.deltaTime;
            }
            for (; shown < order.Length; shown++)
                if (cells[order[shown]] != null) cells[order[shown]].enabled = true;

            // 出し終わったらその1枚を地にして、マスは片付ける
            back.sprite = frames[f];
            for (int i = 0; i < cells.Length; i++)
                if (cells[i] != null) cells[i].enabled = false;
        }
        playing = false;
    }

    static void Shuffle(int[] v)
    {
        for (int i = v.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int t = v[i]; v[i] = v[j]; v[j] = t;
        }
    }
}
