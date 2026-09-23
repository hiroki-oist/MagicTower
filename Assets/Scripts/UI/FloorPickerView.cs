using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 飛翔のオーブの階選びを画面に出す役（作者の指定）。
/// 並べ方と色は <see cref="FloorPicker"/>。
///
/// 原典は「今どの階を選んでいるか」を数字1つでしか見せていなかったので、
/// 1階〜50階を並べて、飛べる階だけはっきり出し、選んでいる階に印を敷く。
/// </summary>
public class FloorPickerView : MonoBehaviour
{
    [SerializeField] TowerController controller;
    [SerializeField] RectTransform panel;
    [SerializeField] Image background;
    [SerializeField] TextMeshProUGUI prompt;
    /// <summary>1階〜50階のます（敷く印）と、その上の数字。</summary>
    [SerializeField] Image[] cells = new Image[0];
    [SerializeField] TextMeshProUGUI[] labels = new TextMeshProUGUI[0];

    void LateUpdate()
    {
        if (controller == null || controller.Game == null) return;
        var g = controller.Game;

        bool show = g.IsChoosingFloor;
        if (panel != null && panel.gameObject.activeSelf != show)
            panel.gameObject.SetActive(show);
        if (!show) return;

        if (background != null) background.color = DialogueWindowView.Fill;
        if (prompt != null)
        {
            prompt.text = TowerMenu.FlightPrompt();
            prompt.color = DialogueWindowView.Text;
        }

        for (int i = 0; i < cells.Length && i < labels.Length; i++)
        {
            int floor = i + 1;
            bool here = floor == g.FlightFloor;
            bool can = FloorPicker.CanGo(g.Player, floor);
            if (cells[i] != null)
            {
                cells[i].enabled = here;
                cells[i].color = FloorPicker.Highlight;
            }
            if (labels[i] != null)
                labels[i].color = here ? FloorPicker.OnHighlight
                                : can  ? FloorPicker.Reachable
                                       : FloorPicker.Unreachable;
        }
    }
}
