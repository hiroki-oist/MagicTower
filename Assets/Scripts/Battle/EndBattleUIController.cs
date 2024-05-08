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

    void OnEnable()
    {
        StartCoroutine(FadeCanvasGroupInOut(endBattleUI, fadeInTime, waitTime, fadeOutTime));
    }

    void Update(){
    }

    public IEnumerator FadeCanvasGroupInOut(CanvasGroup cg, float fadeInTime, float waitTime, float fadeOutTime)
    {
        yield return StartCoroutine(FadeAlpha(cg, 0, 1, fadeInTime));
        yield return new WaitForSeconds(waitTime);
        yield return StartCoroutine(FadeAlpha(cg, 1, 0, fadeOutTime));
        gameObject.SetActive(false);
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
}
