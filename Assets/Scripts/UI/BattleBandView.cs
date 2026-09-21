using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 戦闘中の黒い帯。**作者の旧版の見せ方**（<see cref="AuthorStyle"/>）で、
/// 原典 PC98版の再現ではない。<c>AuthorStyle.Enabled</c> が false のときは何も出さない。
///
/// 出すものは旧版 BattleUI と同じ:
/// 左に自分の絵と名前と HP/ATK/DEF、右に相手の絵と名前と HP/ATK/DEF、上に「第 N ターン」。
/// HPは <see cref="BattleView"/> が進めた撃数に合わせて**1ターンごとに減る**ので、
/// 間合いと音は原典どおりのまま見せ方だけが旧版になる。
/// </summary>
public class BattleBandView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] BattleView battleView;
    [SerializeField] FloorPrefabRegistry registry;
    [SerializeField] RectTransform panel;
    [SerializeField] Image background;
    [SerializeField] TextMeshProUGUI turnText;
    [SerializeField] Image playerIcon;
    [SerializeField] TextMeshProUGUI playerName;
    [SerializeField] TextMeshProUGUI[] playerStats = new TextMeshProUGUI[3];
    [SerializeField] Image enemyIcon;
    [SerializeField] TextMeshProUGUI enemyName;
    [SerializeField] TextMeshProUGUI[] enemyStats = new TextMeshProUGUI[3];

    /// <summary>勝ったあと「Ｎ ゴールド 手に入れた」を出しておく時間（秒）。</summary>
    public const float GoldHold = 0.6f;

    float holdUntil;
    bool wasPlaying;

    /// <summary>今出ているか（テストから見る）。</summary>
    public bool IsShowing { get; private set; }

    void LateUpdate()
    {
        if (panel == null) return;
        if (!AuthorStyle.Enabled) { Hide(); return; }
        if (controller == null || controller.Game == null) { Hide(); return; }

        var n = controller.Game.LastBattleNumbers;
        bool playing = battleView != null && battleView.IsPlaying
                       && !string.IsNullOrEmpty(n.Enemy) && n.Hits > 0;

        if (wasPlaying && !playing)
            holdUntil = n.Gold > 0 ? Time.time + GoldHold : 0f;   // 金の一言を少し残す
        wasPlaying = playing;

        bool show = playing || Time.time < holdUntil;
        if (!show) { Hide(); return; }

        IsShowing = true;
        if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);
        if (background != null) background.color = AuthorStyle.BandFill;

        int hits = battleView != null ? battleView.HitsDone : 0;
        int counters = battleView != null ? battleView.CountersDone : 0;
        var tick = BattleBand.At(n.PlayerHp, n.EnemyHp, n.PlayerDamage, n.EnemyDamage,
                                 hits, counters);

        if (turnText != null)
            turnText.text = playing
                ? "第 " + GameText.Num(tick.Turn) + " ターン"
                : BattleBand.GoldLine(n.Gold);

        Set(playerName, AuthorStyle.PlayerName);
        Set(playerStats, tick.PlayerHp, n.PlayerAttack, n.PlayerDefense);
        Show(playerIcon, PlayerSprite());

        var info = TowerHud.Inspect(controller.Game.Player, n.Enemy);
        Set(enemyName, info.Valid ? info.Name : "");
        Set(enemyStats, tick.EnemyHp, n.EnemyAttack, n.EnemyDefense);
        Show(enemyIcon, SpriteOfEnemy(n.Enemy));
    }

    void Hide()
    {
        IsShowing = false;
        if (panel != null && panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
    }

    static void Set(TextMeshProUGUI t, string s) { if (t != null) t.text = s; }

    /// <summary>HP: / ATK: / DEF: の3行。見出しは旧版の文字そのまま。</summary>
    static void Set(TextMeshProUGUI[] boxes, int hp, int attack, int defense)
    {
        if (boxes == null) return;
        var values = new[] { hp, attack, defense };
        for (int i = 0; i < boxes.Length && i < values.Length; i++)
            Set(boxes[i], AuthorStyle.StatLabels[i] + GameText.Num(values[i]));
    }

    static void Show(Image img, Sprite sprite)
    {
        if (img == null) return;
        img.enabled = sprite != null;
        if (sprite != null) img.sprite = sprite;
    }

    Sprite PlayerSprite()
    {
        return controller != null ? controller.PlayerSprite : null;
    }

    /// <summary>相手の絵は盤面に置くプレハブのものをそのまま使う。</summary>
    Sprite SpriteOfEnemy(string prefabName)
    {
        if (registry == null || string.IsNullOrEmpty(prefabName)) return null;
        var prefab = registry.Get(prefabName);
        if (prefab == null) return null;
        var sr = prefab.GetComponent<SpriteRenderer>();
        return sr != null ? sr.sprite : null;
    }
}
