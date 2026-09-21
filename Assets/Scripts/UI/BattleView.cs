using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戦闘の演出を画面で再生する役。並びは <see cref="BattlePresentation.Steps"/>
/// （原典 0x6082 の流れ）をそのまま順に流すだけ。
///
/// 原典の待ちは垂直帰線の回数なので 1回 = 1/60秒。音は本線が積んだものを
/// <see cref="SoundPlayer"/> が鳴らすので、ここでは**絵だけ**を受け持つ。
///
/// 当たりの絵は原典が buffer 変位 **0x87a** から貼るもので、これは
/// **GROUND.P58 の11枚目（GROUND_010）の先頭とぴったり一致**する。
/// 属性表（物体ID → 絵）のどこからも指されていない絵で、当たりの演出専用。
/// 原典は戦闘・魔法攻撃・場面の節目の**7か所すべてで同じこの絵**を貼っている。
/// 書き出しは `docs/p58_to_effect.py`（Assets/Sprites/Effects/Hit.png）。
/// </summary>
public class BattleView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] Sprite hitSprite;
    [SerializeField] Color flash = new Color(1f, 1f, 1f, 0.75f);

    /// <summary>今再生しているか。</summary>
    public bool IsPlaying { get; private set; }

    List<BattlePresentation.Step> playing;
    GameObject effect;

    void Awake()
    {
        effect = new GameObject("BattleEffect", typeof(SpriteRenderer));
        effect.transform.SetParent(transform, false);
        var sr = effect.GetComponent<SpriteRenderer>();
        sr.sprite = hitSprite;
        sr.color = flash;
        sr.sortingOrder = 200;
        effect.SetActive(false);
    }

    void LateUpdate()
    {
        if (controller == null || controller.Game == null) return;

        // 戦闘以外（魔法攻撃など）の閃光。原典は同じ絵を同じように1回出すだけ。
        foreach (var at in controller.Game.ConsumeEffects())
            StartCoroutine(Flash(at));

        if (IsPlaying) return;
        var steps = controller.Game.LastBattle;
        if (steps == null || steps == playing) return;
        playing = steps;
        StartCoroutine(Play(steps));
    }

    /// <summary>その場に閃光を1回出す。</summary>
    IEnumerator Flash(Vector2Int at)
    {
        ShowEffect(at);
        yield return new WaitForSeconds(
            BattlePresentation.WaitSeconds(BattlePresentation.HitWait));
        effect.SetActive(false);
    }

    IEnumerator Play(List<BattlePresentation.Step> steps)
    {
        IsPlaying = true;
        foreach (var s in steps)
        {
            switch (s.Kind)
            {
                case "待つ":
                    yield return new WaitForSeconds(BattlePresentation.WaitSeconds(s.Value));
                    break;
                case "斬る":
                case "反撃":
                    ShowEffect(s.At);
                    yield return new WaitForSeconds(BattlePresentation.WaitSeconds(s.Value));
                    effect.SetActive(false);
                    break;
                case "相手が消える":
                    effect.SetActive(false);
                    break;
            }
        }
        IsPlaying = false;
    }

    void ShowEffect(Vector2Int at)
    {
        if (effect == null) return;
        effect.transform.position = new Vector3(at.x, at.y, -2f);
        effect.SetActive(true);
    }
}
