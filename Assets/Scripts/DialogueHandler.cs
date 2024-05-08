using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class DialogueHandler : MonoBehaviour
{
    [SerializeField] private GameObject dialogue;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button nextDialogueButton;

    private List<string> dialogueList = new List<string>(); // ダイアログのリスト
    private int currentDialogueIndex = 0; // 現在のダイアログのインデックス

    void Start()
    {
        // ボタンにリスナーを追加
        nextDialogueButton.onClick.AddListener(HandleNextDialogue);
    }

    // 外部からダイアログリストを設定し、ダイアログを表示するメソッド
    public void ShowDialogue(List<string> newDialogueList)
    {
        nextDialogueButton.gameObject.SetActive(true);
        dialogueList = newDialogueList; // 新しいダイアログリストを設定
        currentDialogueIndex = 0; // インデックスをリセット
        ShowCurrentDialogue(); // 最初のダイアログを表示
    }

    private void ShowCurrentDialogue()
    {
        if (dialogueList != null && dialogueList.Count > 0 && currentDialogueIndex < dialogueList.Count)
        {
            // dialogueがnullでないことを確認
            if (dialogue != null)
            {
                dialogue.SetActive(true);
            }
            else
            {
                Debug.LogError("Dialogue GameObject is null.");
                return; // dialogueがnullの場合、ここで処理を中断
            }

            // dialogueTextがnullでないことを確認
            if (dialogueText != null)
            {
                dialogueText.text = dialogueList[currentDialogueIndex];
            }
            else
            {
                Debug.LogError("DialogueText is null.");
                // dialogueTextがnullの場合、dialogueを非アクティブにして表示されないようにする
                dialogue.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("Dialogue list is not initialized properly or current index is out of range.");
            // 条件を満たさない場合はdialogueを非アクティブにする
            if (dialogue != null)
            {
                dialogue.SetActive(false);
            }
        }
    }

    public void HandleNextDialogue()
    {
        Debug.Log("HandleNextDialogue had run");
        if (currentDialogueIndex < dialogueList.Count - 1)
        {
            currentDialogueIndex++;
            Debug.Log("CurrentDialogueIndex: " + currentDialogueIndex);
            ShowCurrentDialogue();
        }
        else
        {
            dialogue.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // リスナーを削除
        nextDialogueButton.onClick.RemoveListener(HandleNextDialogue);
    }
}
