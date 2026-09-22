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
    /// <summary>相手の絵を出し入れするため（盤面に置かれているもの）。</summary>
    [SerializeField] FloorLoader loader;
    /// <summary>倒れたときに少しずつ暗くする黒い板（画面いっぱい）。</summary>
    [SerializeField] UnityEngine.UI.Image fade;
    [SerializeField] Sprite hitSprite;
    // 金騎士の絵は使わなくなった。40階は**部下が1体ずつ歩いてくる**作りで、
    // 歩かせるのは NpcWalkView（盤面のプレハブをそのまま動かす）。
    [SerializeField] Color flash = new Color(1f, 1f, 1f, 0.75f);

    /// <summary>今再生しているか。</summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// ここまでに入れた斬りの数。原典は一撃ごとに相手の残りの命を書き換える（0x639b）ので、
    /// 右下の欄がこれを見て数字を減らす。
    /// </summary>
    public int HitsDone { get; private set; }

    /// <summary>
    /// ここまでに受けた反撃の数。作者の旧版の帯（<see cref="AuthorStyle"/>）が
    /// 自分の命を1ターンごとに減らすのに見る。
    /// </summary>
    public int CountersDone { get; private set; }

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

    /// <summary>
    /// 斬り合いの見せ方（作者の指定）。
    ///
    /// **相手のマスに主人公を重ねてから**、
    /// 主人公 → 閃光 → 相手 → 閃光 → 主人公 … と入れ替えて見せる。
    /// 勝ったら主人公はそのマスに立ったまま（あとで本線も同じ場所へ動く）なので、
    /// 戦いのあとに瞬間移動して見えることがない。
    /// </summary>
    IEnumerator Play(List<BattlePresentation.Step> steps)
    {
        IsPlaying = true;
        HitsDone = 0;
        CountersDone = 0;
        var at = Vector2Int.zero;
        foreach (var s in steps)
        {
            at = s.At;
            switch (s.Kind)
            {
                case "踏み込む":
                    // **普通の移動と同じように歩いて**相手のマスへ入る（作者の指定）。
                    // 瞬間移動して見えるのが不自然、という報告への手当て。
                    if (controller != null) yield return controller.SlidePlayerViewTo(s.At);
                    ShowActor(s.At, true);            // まず主人公
                    break;
                case "待つ":
                    yield return new WaitForSeconds(BattlePresentation.WaitSeconds(s.Value));
                    break;
                case "斬る":
                case "反撃":
                    if (s.Kind == "斬る") HitsDone++; else CountersDone++;
                    ShowEffect(s.At);
                    yield return new WaitForSeconds(BattlePresentation.WaitSeconds(s.Value));
                    effect.SetActive(false);
                    // 斬ったら相手、反撃されたら主人公を出す
                    ShowActor(s.At, s.Kind != "斬る");
                    break;
                case "相手が消える":
                    effect.SetActive(false);
                    ShowActor(s.At, true);
                    break;
            }
        }
        // 倒れるときは**相手を残して**主人公が点滅して消える
        if (controller != null && controller.Game != null && controller.Game.ConsumeDeath())
            yield return Dying(at);
        IsPlaying = false;
    }

    /// <summary>そのマスに出すのが主人公か相手か。</summary>
    void ShowActor(Vector2Int at, bool player)
    {
        if (controller != null) controller.SetPlayerViewVisible(player);
        // 盤面に居る相手か、場面で歩いてきて立っている相手（32階・40階）
        var enemy = loader != null ? loader.ObjectAt(at) : null;
        if (enemy == null && walkView != null) enemy = walkView.StandingAt(at);
        if (enemy != null) enemy.SetActive(!player);
    }

    /// <summary>場面で歩いてきた相手（盤面には載っていない立ち絵）を探すため。</summary>
    [SerializeField] NpcWalkView walkView;

    /// <summary>点滅する回数（倒れるとき）。</summary>
    public const int DeathBlinks = 6;
    /// <summary>暗くなるまでの長さ（秒）。</summary>
    public const float FadeSeconds = 1.2f;

    /// <summary>
    /// 倒れる様子。相手はそのまま、主人公だけが点滅して消え、少しずつ暗くなる。
    /// 暗くなり切ってから ＧＡＭＥ　ＯＶＥＲ の画面が出る（本線がそう並べている）。
    /// </summary>
    IEnumerator Dying(Vector2Int at)
    {
        var enemy = loader != null ? loader.ObjectAt(at) : null;
        if (enemy == null && walkView != null) enemy = walkView.StandingAt(at);
        if (enemy != null) enemy.SetActive(true);         // 相手は残る
        for (int i = 0; i < DeathBlinks; i++)
        {
            if (controller != null) controller.SetPlayerViewVisible(false);
            yield return new WaitForSeconds(0.12f);
            if (controller != null) controller.SetPlayerViewVisible(true);
            yield return new WaitForSeconds(0.12f);
        }
        if (controller != null) controller.SetPlayerViewVisible(false);

        if (fade != null)
        {
            fade.gameObject.SetActive(true);
            float t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.deltaTime;
                var c = fade.color;
                fade.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(t / FadeSeconds));
                yield return null;
            }
        }
    }

    /// <summary>
    /// 金騎士が道順どおりに動く。1歩ごとの間合いは戦闘の踏み込みと同じ 10/60秒にしてある
    /// （原典は描画の中で待っていて、値は追えていない）。
    /// </summary>
    void ShowEffect(Vector2Int at)
    {
        if (effect == null) return;
        effect.transform.position = new Vector3(at.x, at.y, -2f);
        effect.SetActive(true);
    }
}
