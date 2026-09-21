using TMPro;
using UnityEngine;

/// <summary>
/// 画面の文字を実際に描く役。中身は <see cref="TowerHud"/> が作り、ここは貼るだけ。
///
/// 原典の画面は左右に細い帯があり、中央が盤面という構成:
///   左  「塔　Ｎ階」／生命力／攻撃力／防御力／所持金
///   右  WEAPON 欄／SHIELD 欄／鍵の絵／モンスター情報とメッセージを兼ねる枠
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
    [SerializeField] TextMeshProUGUI messageBox;

    /// <summary>
    /// 装備の絵。原典（0x9b80 付近）は欄を塗ってから、
    /// **剣は物体ID 50+段位、盾は 55+段位**の絵をそのまま貼る
    /// （属性表 es:[bx+0x3b2] / es:[bx+0x3d0] ＝ 表の +4 の絵の位置）。
    /// 段位が0のときは塗ったまま何も貼らない。
    /// </summary>
    [SerializeField] UnityEngine.UI.Image weaponIcon;
    [SerializeField] UnityEngine.UI.Image shieldIcon;
    [SerializeField] Sprite[] swordIcons = new Sprite[5];
    [SerializeField] Sprite[] shieldIcons = new Sprite[5];

    /// <summary>鍵の絵を並べる入れ物。原典は数字ではなく絵を本数ぶん出す（0x9ff8）。</summary>
    [SerializeField] RectTransform keyIcons;
    [SerializeField] Sprite keyRed;
    [SerializeField] Sprite keyPurple;
    [SerializeField] Sprite keyYellow;

    /// <summary>いま並べている絵。使い回す。</summary>
    readonly System.Collections.Generic.List<UnityEngine.UI.Image> keyImages =
        new System.Collections.Generic.List<UnityEngine.UI.Image>();


    void LateUpdate()
    {
        if (controller == null || controller.Game == null) return;
        var p = controller.Game.Player;

        Set(floorLabel, TowerHud.FloorLabel(climbing > 0 ? climbing : p.Floor));
        Set(hp,      TowerHud.Hp(p));
        Set(attack,  TowerHud.Attack(p));
        Set(defense, TowerHud.Defense(p));
        Set(gold,    TowerHud.Gold(p));
        // WEAPON / SHIELD の見出しは枠の絵（LCG.P58 の1枚目）に入っているので、
        // ここで書くのは名前だけ。
        Set(weapon,  TowerHud.WeaponName(p));
        Set(shield,  TowerHud.ShieldName(p));
        // 原典は欄を塗り直して**その時のメッセージ1つだけ**を6桁で折り返して書く。
        Set(messageBox, TowerHud.InfoBox(Last(controller.Game.Log)));
        ShowIcon(weaponIcon, swordIcons,  p.SwordRank);
        ShowIcon(shieldIcon, shieldIcons, p.ShieldRank);
        ShowKeys(p);
    }

    static void Set(TextMeshProUGUI t, string s) { if (t != null) t.text = s; }

    /// <summary>装備の絵。段位が0なら何も出さない。</summary>
    static void ShowIcon(UnityEngine.UI.Image img, Sprite[] icons, int rank)
    {
        if (img == null) return;
        var ok = rank >= 1 && rank <= icons.Length && icons[rank - 1] != null;
        img.enabled = ok;
        if (ok) img.sprite = icons[rank - 1];
    }

    /// <summary>鍵の絵を本数ぶん並べる。並べ方は <see cref="ScreenLayout.KeyIcons"/>。</summary>
    void ShowKeys(PlayerState p)
    {
        if (keyIcons == null) return;
        var icons = ScreenLayout.KeyIcons(p.RedKeys, p.PurpleKeys, p.YellowKeys);

        while (keyImages.Count < icons.Count)
        {
            var go = new GameObject("Key" + keyImages.Count,
                                    typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(keyIcons, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
            keyImages.Add(go.GetComponent<UnityEngine.UI.Image>());
        }

        for (int i = 0; i < keyImages.Count; i++)
        {
            var img = keyImages[i];
            if (i >= icons.Count) { img.gameObject.SetActive(false); continue; }
            img.gameObject.SetActive(true);
            img.sprite = SpriteOf(icons[i].Color);
            var r = img.rectTransform;
            // 入れ物は画面の左上に置いてあるので、原典の座標をそのまま使える。
            r.anchoredPosition = ScreenLayout.ToCanvas(icons[i].Rect);
            r.sizeDelta = ScreenLayout.SizeOnCanvas(icons[i].Rect);
        }
    }

    Sprite SpriteOf(ScreenLayout.KeyColor c)
    {
        switch (c)
        {
            case ScreenLayout.KeyColor.Red:    return keyRed;
            case ScreenLayout.KeyColor.Purple: return keyPurple;
            default:                           return keyYellow;
        }
    }

    static string Last(System.Collections.Generic.IReadOnlyList<string> log)
    {
        return log == null || log.Count == 0 ? "" : log[log.Count - 1];
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
