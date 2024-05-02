using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EndBattleUIController : MonoBehaviour
{
    public CanvasGroup endBattleUI;
    // Start is called before the first frame update
    private float fadeInTime = 0.5f;
    private float waitTime = 0.5f;
    private float fadeOutTime = 0.5f;

    void Start()
    {
        StartCoroutine(FadeCanvasGroupInOut(endBattleUI, fadeInTime, waitTime, fadeOutTime));
    }

    public IEnumerator FadeCanvasGroupInOut(CanvasGroup cg, float fadeInTime, float waitTime, float fadeOutTime)
    {
        // フェードイン処理
        yield return StartCoroutine(FadeAlpha(cg, 0, 1, fadeInTime));

        // 指定秒数待機
        yield return new WaitForSeconds(waitTime);

        // フェードアウト処理
        yield return StartCoroutine(FadeAlpha(cg, 1, 0, fadeOutTime));
    }

    public IEnumerator FadeAlpha(CanvasGroup cg, float start, float end, float lerpTime)
    {
        float timeStartedLerping = Time.time;
        float timeSinceStarted = 0f;
        float percentageComplete = 0f;

        while (true)
        {
            timeSinceStarted = Time.time - timeStartedLerping;
            percentageComplete = timeSinceStarted / lerpTime;

            float currentValue = Mathf.Lerp(start, end, percentageComplete);
            cg.alpha = currentValue;

            if (percentageComplete >= 1) break;

            yield return new WaitForEndOfFrame();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
