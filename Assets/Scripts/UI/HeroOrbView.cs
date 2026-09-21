using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 勇者のオーブの一覧を出す役。並べ方は <see cref="HeroOrbPanel"/>（作者の指定）。
/// 中身は原典どおり <see cref="HeroOrb.Rows"/> から取る。
/// </summary>
public class HeroOrbView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] FloorPrefabRegistry registry;
    [SerializeField] RectTransform panel;
    [SerializeField] Image[] icons = new Image[HeroOrb.MaxRows];
    [SerializeField] TextMeshProUGUI[] names = new TextMeshProUGUI[HeroOrb.MaxRows];
    [SerializeField] TextMeshProUGUI[] golds = new TextMeshProUGUI[HeroOrb.MaxRows];
    [SerializeField] TextMeshProUGUI[] turns = new TextMeshProUGUI[HeroOrb.MaxRows];
    [SerializeField] TextMeshProUGUI[] stats = new TextMeshProUGUI[HeroOrb.MaxRows];
    [SerializeField] TextMeshProUGUI[] damages = new TextMeshProUGUI[HeroOrb.MaxRows];

    /// <summary>今出ているか（テストから見る）。</summary>
    public bool IsShowing { get; private set; }

    void LateUpdate()
    {
        if (panel == null || controller == null || controller.Game == null) return;
        bool show = controller.Game.CurrentKind == TowerGame.PageKind.HeroOrb;
        IsShowing = show;
        if (panel.gameObject.activeSelf != show) panel.gameObject.SetActive(show);
        if (!show) return;

        var rows = HeroOrb.Rows(controller.Game.Floor, controller.Game.Player);
        for (int i = 0; i < HeroOrb.MaxRows; i++)
        {
            bool used = i < rows.Count;
            if (!used) { Clear(i); continue; }
            var r = rows[i];
            Show(icons, i, SpriteOf(r.Prefab));
            Set(names, i, r.Name, HeroOrbPanel.Text);
            Set(golds, i, HeroOrbPanel.GoldLine(r.Gold), HeroOrbPanel.Gold);
            Set(turns, i, r.CanAttack ? HeroOrbPanel.TurnLine(r.Turns) : "",
                HeroOrbPanel.Text);
            Set(stats, i, HeroOrbPanel.StatLine(r.Hp, r.Attack, r.Defense),
                HeroOrbPanel.Text);
            Set(damages, i, HeroOrbPanel.DamageLine(r.CanAttack, r.Damage),
                HeroOrbPanel.Damage);
        }
    }

    void Clear(int i)
    {
        Show(icons, i, null);
        Set(names, i, "", HeroOrbPanel.Text);
        Set(golds, i, "", HeroOrbPanel.Gold);
        Set(turns, i, "", HeroOrbPanel.Text);
        Set(stats, i, "", HeroOrbPanel.Text);
        Set(damages, i, "", HeroOrbPanel.Damage);
    }

    static void Set(IList<TextMeshProUGUI> boxes, int i, string s, Color c)
    {
        if (boxes == null || i >= boxes.Count || boxes[i] == null) return;
        boxes[i].text = s;
        boxes[i].color = c;
    }

    static void Show(IList<Image> boxes, int i, Sprite sprite)
    {
        if (boxes == null || i >= boxes.Count || boxes[i] == null) return;
        boxes[i].enabled = sprite != null;
        if (sprite != null) boxes[i].sprite = sprite;
    }

    /// <summary>相手の絵は盤面に置くプレハブのものをそのまま使う。</summary>
    Sprite SpriteOf(string prefabName)
    {
        if (registry == null || string.IsNullOrEmpty(prefabName)) return null;
        var prefab = registry.Get(prefabName);
        if (prefab == null) return null;
        var sr = prefab.GetComponent<SpriteRenderer>();
        return sr != null ? sr.sprite : null;
    }
}
