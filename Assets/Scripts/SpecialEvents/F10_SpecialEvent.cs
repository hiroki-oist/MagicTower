using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using TMPro;
using UnityEditor;
using UnityEngine.SceneManagement;

public class F10_SpecialEvent : MonoBehaviour
{
    private DialogueHandler dialogueHandler;
    private PlayerController playerController;
    
    private GameController gameController;
    private PlayerStats playerStats;
    private GameObject battleUI;
    private Rigidbody2D rb;
    // EventEnemyフォルダのパス
    private string folderPath = "EventEnemy";
    private bool eventTriggered = false;
    private bool bossEventTriggered = false;
    private bool bossEventFinishedTriggered = false;
    [SerializeField] CanvasGroup cg;
    [SerializeField] GameObject stairs_up;
    private GameObject swordObject;
    private GameObject shieldObject;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private string swordName = "Sword_Name";
    [SerializeField] private string shieldName = "Shield_Name";
    [SerializeField] private string floorNumber = "FloorNo";
    private FloorManager floorManager;
    private GameObject bossSkeletonObject;
    private GameObject skeletonWorrior2Object;
    private GameObject skeletonWorrior3Object;
    private GameObject skeleton4Object;
    private GameObject skeleton5Object;
    private GameObject skeleton6Object;
    private GameObject skeleton7Object;
    private GameObject skeleton8Object;
    private GameObject skeleton9Object;
    private GameObject mikata;
    private string objectKey;
    private Vector3[] bossPositions = {
        new Vector3(0, 4, 1),
        new Vector3(0, 5, 1),
        new Vector3(0, 6, 0)
    };
    private GameObject secretLock;
    private GameObject secretLock2;
    private GameObject secretLock3;
    private GameObject secretLock4;
    private GameObject yellowKey1;
    private GameObject yellowKey2;
    private GameObject yellowKey3;
    private GameObject bluePotion;
    private GameObject bluePotion2;
    private GameObject bluePotion3;
    private GameObject attackGem;
    private GameObject attackGem2;
    private GameObject attackGem3;
    private GameObject defenseGem;
    private GameObject defenseGem2;
    private GameObject defenseGem3;

    private GameObject playerObject;

    private Vector3[] skeletonWorrior3Positions = {
        new Vector3(-3, 3, 1),
        new Vector3(-2, 3, 1),
        new Vector3(-1, 3, 1),
        new Vector3(-1, 2, 0),
        new Vector3(-1, 1, 0),
        new Vector3(0, 1, 0)
    };

    private Vector3[] skeletonWorrior2Positions = {
        new Vector3(3, 3, 1),
        new Vector3(2, 3, 1),
        new Vector3(1, 3, 1),
        new Vector3(0, 3, 0)
    };

    private Vector3[] skeleton4Positions = {
        new Vector3(4, 4, 1),
        new Vector3(3, 4, 1),
        new Vector3(3, 3, 1),
        new Vector3(2, 3, 1),
        new Vector3(1, 3, 1)
    };

    private Vector3[] skeleton5Positions = {
        new Vector3(3, 4, 1),
        new Vector3(3, 3, 1),
        new Vector3(2, 3, 1),
        new Vector3(1, 3, 1),
        new Vector3(1, 2, 1)
    };

    private Vector3[] skeleton6Positions = {
        new Vector3(3, 3, 1),
        new Vector3(2, 3, 1),
        new Vector3(1, 3, 1),
        new Vector3(1, 2, 1),
        new Vector3(1, 1, 1)
    };
    private Vector3[] skeleton7Positions = {
        new Vector3(-3, 3, 1),
        new Vector3(-2, 3, 1),
        new Vector3(-1, 3, 1),
        new Vector3(-1, 2, 0),
        new Vector3(-1, 1, 0)
    };
    private Vector3[] skeleton8Positions = {
        new Vector3(-4, 3, 1),
        new Vector3(-3, 3, 1),
        new Vector3(-2, 3, 1),
        new Vector3(-1, 3, 1),
        new Vector3(-1, 2, 0)
    };
    private Vector3[] skeleton9Positions = {
        new Vector3(-5, 3, 1),
        new Vector3(-4, 3, 1),
        new Vector3(-3, 3, 1),
        new Vector3(-2, 3, 1),
        new Vector3(-1, 3, 1)
    };

    private Vector3[] mikataPositions1 = {
        new Vector3(-5, -3, 0),
        new Vector3(-5, -2, 0),
        new Vector3(-5, -1, 0),
        new Vector3(-4, -1, 0),
        new Vector3(-3, -1, 0),
        new Vector3(-3, -2, 0),
        new Vector3(-3, -3, 0),
        new Vector3(-3, -4, 0),
        new Vector3(-2, -4, 0),
        new Vector3(-1, -4, 0),
        new Vector3(-1, -3, 0),
        new Vector3(0, -3, 0),
        new Vector3(0, -2, 0),
        new Vector3(0, -1, 0),
        new Vector3(0, 0, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 2, 0),
        new Vector3(0, 3, 0),
        new Vector3(0, 4, 0)
    };

    private Vector3[] mikataPositions2 = {
        new Vector3(0, 3, 0),
        new Vector3(0, 2, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 0, 0),
        new Vector3(0, -1, 0),
        new Vector3(0, -2, 0),
        new Vector3(0, -3, 0),
        new Vector3(0, -4, 0)
    };



    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        // PlayerController コンポーネントを持つオブジェクトを検索して取得
        playerController = FindObjectOfType<PlayerController>();
        dialogueHandler = FindObjectOfType<DialogueHandler>();
        gameController = FindObjectOfType<GameController>();
        battleUI = FindInactiveObjectByName("BattleUI");
        secretLock2 = FindInactiveObjectByName("Lock_Sealed (2)");
        secretLock3 = FindInactiveObjectByName("Lock_Sealed (3)");
        secretLock = GameObject.Find("Lock_Sealed");
        secretLock4 = GameObject.Find("Lock_Sealed (1)");
        skeleton6Object = GameObject.Find("Enemy_Skeleton (6)");
        skeleton9Object = GameObject.Find("Enemy_Skeleton (9)");
        skeleton7Object = GameObject.Find("Enemy_Skeleton (7)");
        skeleton4Object = GameObject.Find("Enemy_Skeleton (4)");
        skeleton8Object = GameObject.Find("Enemy_Skeleton (8)");
        skeleton5Object = GameObject.Find("Enemy_Skeleton (5)");
        yellowKey1 = FindInactiveObjectByName("Key_Yellow2");
        yellowKey2 = FindInactiveObjectByName("Key_Yellow (1)");
        yellowKey3 = FindInactiveObjectByName("Key_Yellow (2)");
        bluePotion = FindInactiveObjectByName("Potion_Blue (1)");
        bluePotion2 = FindInactiveObjectByName("Potion_Blue (2)");
        bluePotion3 = FindInactiveObjectByName("Potion_Blue (3)");
        attackGem = FindInactiveObjectByName("Gem_Attack (1)");
        attackGem2 = FindInactiveObjectByName("Gem_Attack (2)");
        attackGem3 = FindInactiveObjectByName("Gem_Attack (3)");
        defenseGem = FindInactiveObjectByName("Gem_Defense (1)");
        defenseGem2 = FindInactiveObjectByName("Gem_Defense (2)");
        defenseGem3 = FindInactiveObjectByName("Gem_Defense (3)");
        mikata = FindInactiveObjectByName("Mikata");


        skeletonWorrior2Object = GameObject.Find("Enemy_SkeletonWorrior (2)");
        skeletonWorrior3Object = GameObject.Find("Enemy_SkeletonWorrior (3)");
        bossSkeletonObject = GameObject.Find("Enemy_BossSkeleton");
        playerObject = GameObject.Find("Player");


        stairs_up.gameObject.SetActive(false);
        Debug.Log("Has set the stairsup to false");


        if (playerController != null)
        {
            rb = playerController.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Debug.Log("Player position: " + rb.position);
            }
        }
    }

    void Update()
    {
        if (!eventTriggered && (rb.position.x >= -0.01f && rb.position.x <= 0.01f) && 
            (rb.position.y >= 1.99f && rb.position.y <= 2.01f))
        {
            eventTriggered = true;

            DisableAllButtonsInCanvas("Controller");
            playerController.ResetMovementFlags();

            StartCoroutine(ActivateObjectsSequentially());
        }
        // 敵を全部倒したか
        if (skeleton6Object == null &&
            skeleton9Object == null &&
            skeleton7Object == null &&
            skeleton4Object == null &&
            skeleton8Object == null &&
            skeleton5Object == null &&
            skeletonWorrior2Object == null &&
            skeletonWorrior3Object == null &&
            !bossEventTriggered &&
            !battleUI.activeSelf)
        {
            bossEventTriggered = true;
            StartCoroutine(BossBattle());
        }
        if (bossSkeletonObject == null &&
            !bossEventFinishedTriggered　&&
            !battleUI.activeSelf)
        {
            bossEventFinishedTriggered = true;
            playerObject.transform.position = new Vector3(0, 5, 1);
            stairs_up.SetActive(true);
            StartCoroutine(BossEventFinished());
        }
    }

    IEnumerator BossEventFinished(){
        List<string> thirdDialogues = new List<string> {
            "ボスケルトン「ううう・・・よくぞこの俺様を倒せたものだ・・・しかし、これから先の階はこれまでとは比べものにならんほど強力な敵とワナが待っているぞ、せいぜい気をつけることだな・・・」"
        };

        DisableAllButtonsInCanvas("Controller");

        dialogueHandler.ShowDialogue(thirdDialogues);
        yield return new WaitUntil(() => dialogueHandler.IsDialogueFinished());
        
        secretLock.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        secretLock4.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        secretLock3.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        attackGem.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        attackGem2.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        attackGem3.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        defenseGem.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        defenseGem2.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        defenseGem3.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        bluePotion.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        bluePotion2.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        bluePotion3.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        // オブジェクトがNullでないことを確認
        if (yellowKey1 != null)
        {
            bool isActiveSelf1 = yellowKey1.activeSelf;
            Debug.Log("The object's activeSelf is: " + isActiveSelf1);
            // Transformコンポーネントを取得
            Transform objectTransform = yellowKey1.transform;

            // 位置情報を確認
            Vector3 position = objectTransform.position;
            Debug.Log("The object's position is: " + position);

            // その他のコンポーネントを確認
            Rigidbody rb = yellowKey1.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Debug.Log("The object has a Rigidbody component with mass: " + rb.mass);
            }
            else
            {
                Debug.Log("The object does not have a Rigidbody component.");
            }
        }
        else
        {
            Debug.Log("The object is null.");
        }

        yellowKey1.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        bool isActiveSelf = yellowKey1.activeSelf;
        Debug.Log("The object's activeSelf is: " + isActiveSelf);

        yellowKey2.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        yellowKey3.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        mikata.SetActive(true);
        yield return StartCoroutine(MoveObject(mikata, mikataPositions1));

        List<string> fourthDialogues = new List<string> {
            "「よっ！　また会ったな。　すげえじゃねえか、ここのブロックのボスを倒すとはよ。　俺もこれ以上、先の階に行けなくて困ってたんだ。　ああ、あと聞いた話だが、１１階に盾、１７階に剣があるそうだ。　盾よりも剣の方を先に取りに行った方がいいそうだから、うまく敵をかわして剣を先に取りに行くんだな。　じゃ、お先に！」"
        };

        dialogueHandler.ShowDialogue(fourthDialogues);
        yield return new WaitUntil(() => dialogueHandler.IsDialogueFinished());

        yield return StartCoroutine(MoveObject(mikata, mikataPositions2));

        mikata.SetActive(false);

        EnableAllButtonsInCanvas("Controller");
    }

    IEnumerator BossBattle(){
        List<string> secondDialogues = new List<string> {
                "「ほほう、なかなか・・・。　だが、ここから先へは行かせん！　ここで葬ってくれるわっ！　さあ、こいっ！！！」"
        };

        dialogueHandler.ShowDialogue(secondDialogues);
        yield return new WaitUntil(() => dialogueHandler.IsDialogueFinished());

        secretLock2.SetActive(false);
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
                // Debug.Log($"Button '{button.name}' interactable set to {button.interactable}");
            }

            // 再度ボタンを取得して状態を確認
            Button[] buttons2 = canvas.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons2)
            {
                // Debug.Log($"Button '{button.name}' is still present with interactable = {button.interactable}");
            }

            // Debug.Log("All buttons and their EventTriggers under " + canvasName + " have been disabled.");
        }
        else
        {
            // Debug.LogError("No canvas with name " + canvasName + " found.");
        }
    }


    IEnumerator ActivateObjectsSequentially()
    {

        List<string> firstDialogues = new List<string> {
            "「ふふふ、よくここまで来たな。　かくなる上はこの俺様がじきじきに相手になってやる・・・と言いたいところだが、まずは貴様の力を見せてもらうぞ！　出合え！　スケルトン兵士達よ！」"
        };

        dialogueHandler.ShowDialogue(firstDialogues);

        yield return new WaitUntil(() => dialogueHandler.IsDialogueFinished());

        
        if (bossSkeletonObject != null)
        {
            // 移動処理の開始
            yield return StartCoroutine(MoveObject(bossSkeletonObject, bossPositions));
        }

        secretLock2.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        secretLock3.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        
        
        secretLock.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.1f);
        secretLock4.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.1f);

        if (skeletonWorrior2Object != null)
        {
            // 移動処理の開始
            yield return StartCoroutine(MoveObject(skeletonWorrior2Object, skeletonWorrior2Positions));
        }

        if (skeletonWorrior3Object != null)
        {
            // 移動処理の開始
            yield return StartCoroutine(MoveObject(skeletonWorrior3Object, skeletonWorrior3Positions));
        }

        
        yield return StartCoroutine(MoveObject(skeleton6Object, skeleton6Positions));
        yield return StartCoroutine(MoveObject(skeleton7Object, skeleton7Positions));
        yield return StartCoroutine(MoveObject(skeleton5Object, skeleton5Positions));
        yield return StartCoroutine(MoveObject(skeleton8Object, skeleton8Positions));
        yield return StartCoroutine(MoveObject(skeleton4Object, skeleton4Positions));
        yield return StartCoroutine(MoveObject(skeleton9Object, skeleton9Positions));
        
        secretLock.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        secretLock4.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.1f);

        

        EnableAllButtonsInCanvas("Controller");
    }























    //     // EventEnemyフォルダを検索
    //     Transform eventEnemyFolder = GameObject.Find(folderPath).transform;

    //     if (eventEnemyFolder == null)
    //     {
    //         Debug.LogError("EventEnemy folder not found!");
    //         yield break;
    //     }

    //     // Debug.Log("Fading in the canvas");
    //     cg.gameObject.SetActive(true);
    //     StartCoroutine(FadeCanvasGroupIn(cg, 1f));


    //     // オープニングへ
    //     gameController.PlayerBattler.Attack -= 100;
    //     gameController.PlayerBattler.Defense -= 100;
    //     battleUI.SetActive(true);
    //     playerStats = FindObjectOfType<PlayerStats>();
    //     if (playerStats != null){
    //         playerStats.UpdateUI();
    //     } else {
    //         Debug.Log("PlayerStats is null");
    //         yield break;
    //     }
    //     battleUI.SetActive(false);
    //     GameObject swordObject = GameObject.Find("Sword_Image");
    //     GameObject shieldObject = GameObject.Find("Shield_Image");
    //     SpriteRenderer swordRenderer = swordObject.GetComponent<SpriteRenderer>();
    //     SpriteRenderer shieldRenderer = shieldObject.GetComponent<SpriteRenderer>();
    //     Color color = swordRenderer.color;
    //     color.a = 0;
    //     swordRenderer.color = color;
    //     shieldRenderer.color = color;
    //     GameObject swordNameObject = GameObject.Find(swordName);
    //     GameObject shieldNameObject = GameObject.Find(shieldName);
    //     TextMeshProUGUI shieldTextMeshProUGUI = shieldNameObject.GetComponent<TextMeshProUGUI>();
    //     shieldTextMeshProUGUI.text = "";
    //     TextMeshProUGUI swordTextMeshProUGUI = swordNameObject.GetComponent<TextMeshProUGUI>();
    //     swordTextMeshProUGUI.text = "";

    //     // 味方のセリフ
    //     List<string> secondDialogues = new List<string> {
    //         "「・・・・・」",
    //         "「・・・・・おいっ」",
    //         "「・・・・・おいっ！　起きろ！」",
    //     };

    //     dialogueHandler.ShowDialogue(secondDialogues);

    //     yield return new WaitUntil(() => dialogueHandler.IsDialogueFinished());
    //     Debug.Log("Second Dialogues finished");



    //     // ここから2階へ

    //     floorManager = FindObjectOfType<FloorManager>();
    //     floorManager.SetFloorNumber(2);
    //     GameObject floorNoObject = GameObject.Find(floorNumber);
    //     TextMeshProUGUI floorNoTextMeshProUGUI = floorNoObject.GetComponent<TextMeshProUGUI>();
    //     floorNoTextMeshProUGUI.text = "2 階";

    //     Transform playerTransform = playerController.transform;

    //     Vector3 newPosition = new Vector3(-3f, -1f, 0f);
    //     playerTransform.position = newPosition;

    //     // 味方のセリフ
    //     List<string> thirdDialogues = new List<string> {
    //         "「おお、無事か！　おまえ、ここの魔導師に捕まって、この牢屋の中に入れられてたんだよ。　で、今まで気を失っていた訳だ。　にしても、おまえ運がいいぜ。　ちょうど抜け穴が完成した時に来るんだからな。　そういう訳で俺は今から脱走するところだ。　おまえもついてこい」"
    //     };

    //     dialogueHandler.ShowDialogue(thirdDialogues);

    //     yield return new WaitUntil(() => dialogueHandler.IsDialogueFinished());

    //     // 味方のセリフ
    //     List<string> fourthDialogues = new List<string> {
    //         "「よし、じゃあ俺は先に行かせてもらうぜ。　ああ、それから、あんたの剣と盾あいつらに取り上げられちまったみたいだな。　この塔の中を丸腰で歩き回るのは、やめた方がいい。　確か５階に剣、９階に盾があったはずだから、まずそれを手に入れることを考えるんだな。　じゃあ、またなっ！」"
    //     };

    //     dialogueHandler.ShowDialogue(fourthDialogues);

    //     yield return new WaitUntil(() => dialogueHandler.IsDialogueFinished());


    //     objectKey = "Floor_3_" + gameObject.name;
    //     Destroy(gameObject);
    //     PlayerPrefs.SetInt(objectKey, 1); 
    //     PlayerPrefs.Save(); 
    // }

    IEnumerator MoveObject(GameObject mikataObject, Vector3[] mikataPositions)
    {
        foreach (Vector3 newPosition in mikataPositions)
        {
            // 現在地点から新しい位置まで滑らかに移動
            yield return StartCoroutine(MoveToPosition(mikataObject, newPosition, 0.1f));  // 1秒かけて移動
        }
    }

    IEnumerator MoveToPosition(GameObject obj, Vector3 newPosition, float duration)
    {
        float time = 0;
        Vector3 startPosition = obj.transform.position;

        while (time < duration)
        {
            obj.transform.position = Vector3.Lerp(startPosition, newPosition, time / duration);
            time += Time.deltaTime;
            yield return null;  // 次のフレームまで待機
        }

        obj.transform.position = newPosition;  // 最終的な位置を保証
    }

    public void EnableAllButtonsInCanvas(string canvasName)
    {
        GameObject canvas = GameObject.Find(canvasName);
        // Debug.Log("Canvas found as:" + canvas);

        // 再度ボタンを取得して状態を確認
        Button[] buttons2 = canvas.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons2)
        {
            // Debug.Log($"Button '{button.name}' is still present with interactable = {button.interactable}");
        }


        // CanvasGroupがある場合、そのinteractableプロパティをtrueに設定
        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            // Debug .Log("Setting interactable to true");
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (canvas != null)
        {
            // Canvasの下のすべてのButtonコンポーネントを取得して有効化
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                button.interactable = true;
                // Debug.Log($"Button '{button.name}' is still present with interactable = {button.interactable}");

                // 同じGameObjectにあるEventTriggerコンポーネントも有効化
                EventTrigger trigger = button.GetComponent<EventTrigger>();
                if (trigger != null)
                {
                    trigger.enabled = true;
                }
            }
            // Debug.Log("All buttons and their EventTriggers under " + canvasName + " have been enabled.");
        }
        else
        {
            // Debug.LogError("No canvas with name " + canvasName + " found.");
        }
    }

    public IEnumerator FadeCanvasGroupIn(CanvasGroup cg, float fadeInTime)
    {
        Debug.Log("Now fading");
        yield return StartCoroutine(FadeAlpha(cg, 0, 1, fadeInTime));
    }

    public IEnumerator FadeAlpha(CanvasGroup cg, float start, float end, float lerpTime)
    {
        float timeStartedLerping = Time.time;
        float timeSinceStarted = 0f;
        float percentageComplete = 0f;

        while (true)
        {
            timeSinceStarted = Time.time - timeStartedLerping;
            percentageComplete = timeSinceStarted / lerpTime;

            float currentValue = Mathf.Lerp(start, end, percentageComplete);
            cg.alpha = currentValue;

            if (percentageComplete >= 1) break;

            yield return new WaitForEndOfFrame();
        }
    }

    GameObject FindInactiveObjectByName(string name)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var transform in allTransforms)
        {
            if (transform.name == name && !transform.gameObject.activeInHierarchy)
            {
                return transform.gameObject;
            }
        }
        return null;
    }
}
