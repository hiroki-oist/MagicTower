using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using TMPro;
using UnityEditor;
using UnityEngine.SceneManagement;

public class F3_SpecialEvent : MonoBehaviour
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
    [SerializeField] CanvasGroup cg;
    private GameObject swordObject;
    private GameObject shieldObject;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private string swordName = "Sword_Name";
    [SerializeField] private string shieldName = "Shield_Name";
    [SerializeField] private string floorNumber = "FloorNo";
    private FloorManager floorManager;
    private GameObject mikataObject;
    private string objectKey;
    private Vector3[] mikataPositions = {
        new Vector3(-4, 0, 1),
        new Vector3(-5, 0, 1),
        new Vector3(-5, -1, 0),
        new Vector3(-5, -2, 0)
    };
    private GameObject secretWall;

    private GameObject playerObject;

    private Vector3[] playerPositions = {
        new Vector3(-3, 0, 1),
        new Vector3(-4, 0, 1),
        new Vector3(-5, 0, 1),
        new Vector3(-5, -1, 0),
    };

    private Vector3[] mikataPositions2 = {
        new Vector3(-5, -3, 1),
        new Vector3(-5, -4, 1),
    };
    

    // アクティブ化するオブジェクトの名前のリスト
    private string[] objectNames = {
        "Enemy_Zeno",
        "Enemy_GreatMagician",
        "Enemy_GreatMagician (1)",
        "Enemy_GreatMagician (3)",
        "Enemy_GreatMagician (2)"
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
                // Debug.Log($"Activated: {objectName}");
            }
            else
            {
                Debug.LogWarning($"Object not found: {objectName}");
            }
            yield return new WaitForSeconds(0.5f);  // 0.5秒待つ
        }

        // Zenoのセリフ
        List<string> zenoDialogues = new List<string> {
            "「ようこそ、『魔法の塔』へ。　お前がちょうど１００人目の侵入者だ。　本来なら手下共に相手をさせるところだが、特別にこの私が自ら相手になってやろう。　己の不運を呪うのだな。」",
            "「なんだっ！？　うわぁーっ！！？」",
        };

        dialogueHandler.ShowDialogue(zenoDialogues);

        yield return new WaitUntil(() => dialogueHandler.IsDialogueFinished());

        // Debug.Log("Fading in the canvas");
        cg.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroupIn(cg, 1f));

        // 敵兵をロードしないものに登録
        foreach ( var objectName in objectNames)
        {
            Transform child = eventEnemyFolder.Find(objectName);
            gameController.DestroyObject(child.gameObject);
        }

        // オープニングへ
        gameController.PlayerBattler.Attack -= 100;
        gameController.PlayerBattler.Defense -= 100;
        battleUI.SetActive(true);
        playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats != null){
            playerStats.UpdateUI();
        } else {
            Debug.Log("PlayerStats is null");
            yield break;
        }
        battleUI.SetActive(false);
        GameObject swordObject = GameObject.Find("Sword_Image");
        GameObject shieldObject = GameObject.Find("Shield_Image");
        SpriteRenderer swordRenderer = swordObject.GetComponent<SpriteRenderer>();
        SpriteRenderer shieldRenderer = shieldObject.GetComponent<SpriteRenderer>();
        Color color = swordRenderer.color;
        color.a = 0;
        swordRenderer.color = color;
        shieldRenderer.color = color;
        GameObject swordNameObject = GameObject.Find(swordName);
        GameObject shieldNameObject = GameObject.Find(shieldName);
        TextMeshProUGUI shieldTextMeshProUGUI = shieldNameObject.GetComponent<TextMeshProUGUI>();
        shieldTextMeshProUGUI.text = "";
        TextMeshProUGUI swordTextMeshProUGUI = swordNameObject.GetComponent<TextMeshProUGUI>();
        swordTextMeshProUGUI.text = "";

        // 味方のセリフ
        List<string> secondDialogues = new List<string> {
            "「・・・・・」",
            "「・・・・・おいっ」",
            "「・・・・・おいっ！　起きろ！」",
        };

        dialogueHandler.ShowDialogue(secondDialogues);

        yield return new WaitUntil(() => dialogueHandler.IsDialogueFinished());
        Debug.Log("Second Dialogues finished");



        // ここから2階へ

        floorManager = FindObjectOfType<FloorManager>();
        floorManager.SetFloorNumber(2);
        GameObject floorNoObject = GameObject.Find(floorNumber);
        TextMeshProUGUI floorNoTextMeshProUGUI = floorNoObject.GetComponent<TextMeshProUGUI>();
        floorNoTextMeshProUGUI.text = "2 階";

        Transform playerTransform = playerController.transform;

        Vector3 newPosition = new Vector3(-3f, -1f, 0f);
        playerTransform.position = newPosition;

        // 味方のセリフ
        List<string> thirdDialogues = new List<string> {
            "「おお、無事か！　おまえ、ここの魔導師に捕まって、この牢屋の中に入れられてたんだよ。　で、今まで気を失っていた訳だ。　にしても、おまえ運がいいぜ。　ちょうど抜け穴が完成した時に来るんだからな。　そういう訳で俺は今から脱走するところだ。　おまえもついてこい」"
        };

        dialogueHandler.ShowDialogue(thirdDialogues);

        yield return new WaitUntil(() => dialogueHandler.IsDialogueFinished());

        secretWall = GameObject.Find("Wall_Secret");
        gameController.DestroyObject(secretWall);
    
        mikataObject = GameObject.Find("Mikata");
        if (mikataObject != null)
        {
            // 移動処理の開始
            yield return StartCoroutine(MoveObject(mikataObject, mikataPositions));
        }

        playerObject = GameObject.Find("Player");
        yield return StartCoroutine(MoveObject(playerObject, playerPositions));

        // 味方のセリフ
        List<string> fourthDialogues = new List<string> {
            "「よし、じゃあ俺は先に行かせてもらうぜ。　ああ、それから、あんたの剣と盾あいつらに取り上げられちまったみたいだな。　この塔の中を丸腰で歩き回るのは、やめた方がいい。　確か５階に剣、９階に盾があったはずだから、まずそれを手に入れることを考えるんだな。　じゃあ、またなっ！」"
        };

        dialogueHandler.ShowDialogue(fourthDialogues);

        yield return new WaitUntil(() => dialogueHandler.IsDialogueFinished());

        yield return StartCoroutine(MoveObject(mikataObject, mikataPositions2));

        gameController.DestroyObject(mikataObject);

        EnableAllButtonsInCanvas("Controller");

        objectKey = "Floor_3_" + gameObject.name;
        Destroy(gameObject);
        PlayerPrefs.SetInt(objectKey, 1); 
        PlayerPrefs.Save(); 
    }

    IEnumerator MoveObject(GameObject mikataObject, Vector3[] mikataPositions)
    {
        foreach (Vector3 newPosition in mikataPositions)
        {
            // 現在地点から新しい位置まで滑らかに移動
            yield return StartCoroutine(MoveToPosition(mikataObject, newPosition, 0.3f));  // 1秒かけて移動
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
