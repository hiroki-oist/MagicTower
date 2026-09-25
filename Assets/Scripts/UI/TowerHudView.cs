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
    /// 右上の「実績」の**隣の欄**。妖精にもらった称号を出す（作者の指定）。
    /// まだもらっていなければ「無」。
    /// </summary>
    [SerializeField] TextMeshProUGUI rank;

    /// <summary>
    /// 知らせを**1行で**出すか。原典の欄は6桁×8行で折り返す（<see cref="TowerHud.InfoBox"/>）が、
    /// 縦画面の並べ方（作者の指定）では横に長い1行の帯なので折り返さない。
    /// </summary>
    [SerializeField] bool oneLineMessage;

    /// <summary>
    /// 戦闘中に出す相手の欄（原典 0x9626）。メッセージと同じ枠を使う。
    /// </summary>
    [SerializeField] BattleView battleView;
    [SerializeField] FloorPrefabRegistry registry;
    [SerializeField] RectTransform monsterPanel;
    [SerializeField] UnityEngine.UI.Image monsterIcon;
    [SerializeField] TextMeshProUGUI monsterName;
    [SerializeField] TextMeshProUGUI monsterHp;
    [SerializeField] TextMeshProUGUI monsterAttack;
    [SerializeField] TextMeshProUGUI monsterDefense;

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

    /// <summary>
    /// 持ち物の欄（原典 0x2f8c）。左の帯の下の箱に、持っている道具の絵を3列で並べる。
    /// 15マスぶんで、順番は物体ID 36〜50。持っていないマスは何も出さない。
    /// </summary>
    [SerializeField] UnityEngine.UI.Image[] itemSlots = new UnityEngine.UI.Image[0];

    /// <summary>
    /// ★作者が選んだ「石壁に彫った棚」。ますの縁と地。
    /// **持っていないますも沈んだ石として見せる**ので、物体ID順に並べた空きが
    /// そのまま「まだ手に入れていないものがある」という手がかりになる。
    /// </summary>
    [SerializeField] UnityEngine.UI.Image[] itemSlotEdges = new UnityEngine.UI.Image[0];
    [SerializeField] UnityEngine.UI.Image[] itemSlotFills = new UnityEngine.UI.Image[0];

    /// <summary>鍵の絵を並べる入れ物。原典は数字ではなく絵を本数ぶん出す（0x9ff8）。</summary>
    [SerializeField] RectTransform keyIcons;
    [SerializeField] Sprite keyRed;
    [SerializeField] Sprite keyPurple;
    [SerializeField] Sprite keyYellow;

    /// <summary>
    /// 鍵の**本数**の欄（縦画面の並べ方。作者の指定）。
    /// 原典は本数ぶん絵を並べる（<see cref="keyIcons"/>）が、見せてもらった画面は
    /// 鍵の絵ひとつに数字を添える形なので、そちらのときはこちらを使う。
    /// 繋がっていなければ何もしない。
    /// </summary>
    [SerializeField] TextMeshProUGUI yellowKeys;
    [SerializeField] TextMeshProUGUI purpleKeys;
    [SerializeField] TextMeshProUGUI redKeys;

    /// <summary>いま並べている絵。使い回す。</summary>
    readonly System.Collections.Generic.List<UnityEngine.UI.Image> keyImages =
        new System.Collections.Generic.List<UnityEngine.UI.Image>();


    void LateUpdate()
    {
        if (controller == null || controller.Game == null) return;
        var p = controller.Game.Player;

        Set(floorLabel, TowerHud.FloorLabel(climbing > 0 ? climbing : p.Floor));
        // 生命力は**斬り合いのターンに合わせて**減らす（作者の指定）。
        // 帯と同じ数え方を通すので、左の欄と帯の数字は必ず一致する。
        Set(hp,      TowerHud.Hp(p, controller.Game.LastBattleNumbers,
                                 battleView != null && battleView.IsPlaying,
                                 battleView != null ? battleView.HitsDone : 0,
                                 battleView != null ? battleView.CountersDone : 0));
        Set(attack,  TowerHud.Attack(p));
        Set(defense, TowerHud.Defense(p));
        Set(gold,    TowerHud.Gold(p));
        // WEAPON / SHIELD の見出しは枠の絵（LCG.P58 の1枚目）に入っているので、
        // ここで書くのは名前だけ。
        Set(weapon,  TowerHud.WeaponName(p));
        Set(shield,  TowerHud.ShieldName(p));
        // 右下の枠は「戦闘中は相手の欄、それ以外はメッセージ」。原典は同じ場所を塗り直す。
        var fighting = ShowMonster(p);
        if (messageBox != null) messageBox.enabled = !fighting;
        if (!fighting)
        {
            // 原典は欄を塗り直して**その時のメッセージ1つだけ**を6桁で折り返して書く。
            var last = Last(controller.Game.Log);
            Set(messageBox, oneLineMessage ? (last ?? "") : TowerHud.InfoBox(last));
        }
        if (rank != null)
            rank.text = string.IsNullOrEmpty(p.Rank) ? PhoneScreen.NoRankLabel : p.Rank;
        ShowIcon(weaponIcon, swordIcons,  p.SwordRank);
        ShowIcon(shieldIcon, shieldIcons, p.ShieldRank);
        ShowKeys(p);
        Set(yellowKeys, GameText.Num(p.YellowKeys));
        Set(purpleKeys, GameText.Num(p.PurpleKeys));
        Set(redKeys,    GameText.Num(p.RedKeys));
        ShowItems(p);
    }

    static void Set(TextMeshProUGUI t, string s) { if (t != null) t.text = s; }

    /// <summary>
    /// 戦っている間、右下の枠に相手の絵と能力値を出す（原典 0x9626）。
    /// 命の数字は一撃ごとに減る（0x639b）。出したときだけ true。
    /// </summary>
    bool ShowMonster(PlayerState p)
    {
        if (monsterPanel == null) return false;
        var fighting = battleView != null && battleView.IsPlaying
                       && !string.IsNullOrEmpty(controller.Game.LastBattleEnemy);
        // 作者の見せ方（AuthorStyle）のときは同じ中身を黒い帯（BattleBandView）が
        // 画面の中央に出すので、原典の右下の欄は重ねずに隠す。
        monsterPanel.gameObject.SetActive(fighting && !AuthorStyle.Enabled);
        if (!fighting) return false;

        var info = TowerHud.Inspect(p, controller.Game.LastBattleEnemy);
        if (!info.Valid) { monsterPanel.gameObject.SetActive(false); return false; }

        if (monsterIcon != null)
        {
            var sprite = SpriteOfEnemy(controller.Game.LastBattleEnemy);
            monsterIcon.enabled = sprite != null;
            if (sprite != null) monsterIcon.sprite = sprite;
        }
        Set(monsterName, info.Name);
        Set(monsterHp, GameText.Num(TowerHud.MonsterHpAfter(info, battleView.HitsDone)));
        Set(monsterAttack, info.Attack);
        Set(monsterDefense, info.Defense);
        return true;
    }

    /// <summary>相手の絵は盤面に置くプレハブのものをそのまま使う（原典も同じ絵）。</summary>
    Sprite SpriteOfEnemy(string prefabName)
    {
        if (registry == null) return null;
        var prefab = registry.Get(prefabName);
        if (prefab == null) return null;
        var sr = prefab.GetComponent<SpriteRenderer>();
        return sr != null ? sr.sprite : null;
    }

    /// <summary>持ち物の欄。持っている道具だけ絵を出す（原典 0x2f8c）。</summary>
    void ShowItems(PlayerState p)
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            var kind = SpecialItems.PanelKind(i);
            bool have = kind != SpecialItems.Kind.None && p.Has(kind);
            var img = itemSlots[i];
            if (img != null) img.enabled = have;
            if (i < itemSlotEdges.Length && itemSlotEdges[i] != null)
                itemSlotEdges[i].color = have ? PhoneScreen.SlotEdge
                                              : PhoneScreen.EmptySlotEdge;
            if (i < itemSlotFills.Length && itemSlotFills[i] != null)
                itemSlotFills[i].color = have ? PhoneScreen.SlotFill
                                              : PhoneScreen.EmptySlotFill;
        }
    }

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
