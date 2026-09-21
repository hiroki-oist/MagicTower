using TMPro;
using UnityEngine;

/// <summary>
/// 画面の文字を実際に描く役。中身は <see cref="TowerHud"/> が作り、ここは貼るだけ。
///
/// 原典の画面は左右に細い帯があり、中央が盤面という構成:
///   左  「塔　Ｎ階」／生命力／攻撃力／防御力／所持金
///   右  WEAPON 欄／SHIELD 欄／鍵の本数／モンスター情報とメッセージを兼ねる枠
/// </summary>
public class TowerHudView : MonoBehaviour
{
    [SerializeField] TowerController controller;

    [SerializeField] TextMeshProUGUI floorLabel;

    /// <summary>駆け上がりの最中に出している階。出していなければ0。</summary>
    int climbing;
    [SerializeField] TextMeshProUGUI hp;
    [SerializeField] TextMeshProUGUI attack;
    [SerializeField] TextMeshProUGUI defense;
    [SerializeField] TextMeshProUGUI gold;
    [SerializeField] TextMeshProUGUI weapon;
    [SerializeField] TextMeshProUGUI shield;
    [SerializeField] TextMeshProUGUI keys;
    [SerializeField] TextMeshProUGUI messageBox;

    /// <summary>原典の右下の欄は履歴が積み上がる。表示する行数。</summary>
    const int LogLines = 8;

    void LateUpdate()
    {
        if (controller == null || controller.Game == null) return;
        var p = controller.Game.Player;

        Set(floorLabel, TowerHud.FloorLabel(climbing > 0 ? climbing : p.Floor));
        Set(hp,      TowerHud.Hp(p));
        Set(attack,  TowerHud.Attack(p));
        Set(defense, TowerHud.Defense(p));
        Set(gold,    TowerHud.Gold(p));
        Set(weapon,  "WEAPON\n" + TowerHud.WeaponName(p));
        Set(shield,  "SHIELD\n" + TowerHud.ShieldName(p));
        Set(keys, $"鍵 {GameText.Num(p.YellowKeys)}\n"
                + $"紫 {GameText.Num(p.PurpleKeys)}\n"
                + $"赤 {GameText.Num(p.RedKeys)}");
        Set(messageBox, Tail(controller.Game.Log, LogLines));
    }

    static void Set(TextMeshProUGUI t, string s) { if (t != null) t.text = s; }

    static string Tail(System.Collections.Generic.IReadOnlyList<string> log, int n)
    {
        if (log == null || log.Count == 0) return "";
        var sb = new System.Text.StringBuilder();
        for (int i = Mathf.Max(0, log.Count - n); i < log.Count; i++)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(log[i]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 終幕の駆け上がり。原典（0x2386〜0x23a6）は地図を変えず、
    /// **階の数字だけ**を1つずつ上げて 5/60秒ずつ待つ。
    /// </summary>
    System.Collections.IEnumerator Climb(System.Collections.Generic.List<int> floors)
    {
        foreach (var f in floors)
        {
            climbing = f;
            yield return new WaitForSeconds(
                Ending.ClimbWait * BattlePresentation.FrameSeconds);
        }
        climbing = 0;
    }

    System.Collections.Generic.List<int> playedClimb;

    void Update()
    {
        if (controller == null || controller.Game == null) return;
        var climb = controller.Game.LastClimb;
        if (climb == null || climb == playedClimb) return;
        playedClimb = climb;
        StartCoroutine(Climb(climb));
    }
}
