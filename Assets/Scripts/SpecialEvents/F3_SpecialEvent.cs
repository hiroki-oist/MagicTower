using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using TMPro;

public class F3_SpecialEvent : MonoBehaviour
{
    private DialogueHandler dialogueHandler;
    private PlayerController playerController;
    private Rigidbody2D rb;
    // EventEnemyフォルダのパス
    private string folderPath = "EventEnemy";
    private bool eventTriggered = false;
    

    // アクティブ化するオブジェクトの名前のリスト
    private string[] objectNames = {
        "Enemy_Zeno",
        "Enemy_GreatMagician",
        "Enemy_GreatMagician",
        "Enemy_GreatMagician (1)",
        "Enemy_GreatMagician (3)",
        "Enemy_GreatMagician (2)"
    };

    // Start is called before the first frame update
    void Start()
    {
        // PlayerController コンポーネントを持つオブジェクトを検索して取得
        playerController = FindObjectOfType<PlayerController>();
        dialogueHandler = FindObjectOfType<DialogueHandler>();
        
        if (playerController != null)
        {
            rb = playerController.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Debug.Log("Player position: " + rb.position);
            }
        }
    }

    void Update()
    {
        if (!eventTriggered && (rb.position.x >= -1.01f && rb.position.x <= -0.99f) && 
            (rb.position.y >= -2.01f && rb.position.y <= -1.99f))
        {
            eventTriggered = true;

            // 条件に合致した場合の処理
            DisableAllButtonsInCanvas("Controller");
            playerController.ResetMovementFlags();

            StartCoroutine(ActivateObjectsSequentially());
        }
    }

    public void DisableAllButtonsInCanvas(string canvasName)
    {
        GameObject canvas = GameObject.Find(canvasName);
        if (canvas != null)
        {
            // Canvasの下のすべてのButtonコンポーネントを取得して無効化
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                button.interactable = false;
                // 同じGameObjectにあるEventTriggerコンポーネントも無効化
                EventTrigger trigger = button.GetComponent<EventTrigger>();
                if (trigger != null)
                {
                    trigger.enabled = false;
                }
            }
            Debug.Log("All buttons and their EventTriggers under " + canvasName + " have been disabled.");
        }
        else
        {
            Debug.LogError("No canvas with name " + canvasName + " found.");
        }
    }

    IEnumerator ActivateObjectsSequentially()
    {
        // EventEnemyフォルダを検索
        Transform eventEnemyFolder = GameObject.Find(folderPath).transform;

        if (eventEnemyFolder == null)
        {
            Debug.LogError("EventEnemy folder not found!");
            yield break;
        }

        // 指定されたオブジェクトを順番にアクティブにする
        foreach (var objectName in objectNames)
        {
            Transform child = eventEnemyFolder.Find(objectName);
            if (child != null)
            {
                child.gameObject.SetActive(true);
                Debug.Log($"Activated: {objectName}");
            }
            else
            {
                Debug.LogWarning($"Object not found: {objectName}");
            }
            yield return new WaitForSeconds(0.5f);  // 0.5秒待つ
        }

        // DialogueUI GameObjectを検索
        // GameObject dialogueUI = GameObject.Find("DialogueUI");
        // if (dialogueUI != null)
        // {
        //     // DialogueTextへのパスでTextMeshProコンポーネントを取得
        //     TextMeshProUGUI dialogueText = dialogueUI.transform.Find("DialogueCanvas/Dialogue/DialogueText")
        //                                 .GetComponent<TextMeshProUGUI>();
            
        //     if (dialogueText != null)
        //     {
        //         // テキストを更新
        //         dialogueText.text = "「ようこそ、『魔法の塔』へ。　お前がちょうど１００人目の侵入者だ。　本来なら手下共に相手をさせるところだが、特別にこの私が自ら相手になってやろう。　己の不運を呪うのだな。」";
        //         Debug.Log("Dialogue text updated.");
                
        //         // Dialogueもアクティブにする
        //         dialogueText.transform.parent.gameObject.SetActive(true);
        //         Debug.Log("Dialogue object activated.");
        //     }
        //     else
        //     {
        //         Debug.LogError("DialogueText component not found!");
        //     }
        // }
        // else
        // {
        //     Debug.LogError("DialogueUI object not found!");
        // }
        List<string> myDialogues = new List<string> {
            "こんにちは、世界！",
            "これはテストダイアログです。",
            "最後のメッセージです。",
            "嘘です、最後のメッセージです。",
            "5",
            "6"
        };

        dialogueHandler.ShowDialogue(myDialogues);
    }
}
