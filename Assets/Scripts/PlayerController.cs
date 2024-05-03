using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine.Events;
using System.Reflection;
public class PlayerController: MonoBehaviour {
    public float speed = 60.0f;
    private Animator animator;
    bool forwardmove;
    bool backmove;
    bool rightmove;
    bool leftmove;
    // Use this for initialization
    private Vector2 targetPosition;
    private bool isMoving = false;
    private Rigidbody2D rb;
    // public GameObject battleUI;
    // デリゲートの定義
    public delegate void EncountsHandler(Battler enemyBattler);

    // イベントの定義
    public event EncountsHandler OnEncounts;
     [SerializeField] BattleUnit playerStats;
    public ItemManager itemManager;

    public GameController gameController;
    

    void Start () {
        gameController.PlayerBattler.Init();
        playerStats.Setup(gameController.PlayerBattler);
        playerStats.UpdateUI();
        playerStats.UpdateCoinUI();
        itemManager.UpdateKeyValue();
        
        Application.targetFrameRate = 30;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update () {
        // Debug.Log("Update is moving");
        if (isMoving) {
            int layerMask = ~LayerMask.GetMask("Player", "Enemy", "Item");
            RaycastHit2D hit = Physics2D.Raycast(rb.position, targetPosition - rb.position, Vector3.Distance(rb.position, targetPosition), layerMask);
            Debug.Log("Hit collider is: " + hit.collider);
            if (hit.collider != null && hit.collider.CompareTag("Wall")) {
                int hitLayer = hit.collider.gameObject.layer;
                string layerName = LayerMask.LayerToName(hitLayer);
                isMoving = false;
                ResetMovementFlags();
            } else if (hit.collider != null && hit.collider.tag.StartsWith("Lock_")){
                isMoving = false;
                ResetMovementFlags();
                HandleCollision(hit.collider);
            
            // }
                
            // if (hit.collider != null && hit.collider.CompareTag("Wall")) {
            //     // Debug.Log("Collided wall!");
            //     ResetMovementFlags();  // 移動フラグをリセット
            //     isMoving = false;  // 移動を停止
                // Debug.Log("hit.collider.tag" + hit.collider.tag);
            // } else if (hit.collider.tag.StartsWith("Lock_")){
                // Debug.Log("Yes I have entered here");
                // isMoving = false;
                // HandleOpenLock(hit.collider);
            } else {
                MoveToTarget();
            }
        } else {
            CheckForNewMove();
        }
        UpdateAnimation();
    }

    void HandleOpenLock(Collider2D hitCollider){
        if (hitCollider.tag == "Lock_Yellow"){
            int currentYellowKey = itemManager.GetYellowKey();
            if (currentYellowKey >= 1){
                itemManager.SetYellowKey(currentYellowKey - 1);
                hitCollider.gameObject.SetActive(false);
            } else {
                Debug.Log("You dont have a key!");
            }
        }
    }

    void CheckForNewMove() {
        if(forwardmove){
            StartMove(new Vector3(0, 1, 0));
        }
        else if(backmove){
            StartMove(new Vector3(0, -1, 0));
        }
        else if(rightmove){
            StartMove(new Vector3(1, 0, 0));
        }
        else if(leftmove){
            StartMove(new Vector3(-1, 0, 0));
        }
    }

    void StartMove(Vector3 direction) {
        if (!isMoving) { // すでに移動中でなければ
            targetPosition = transform.position + direction;
            targetPosition.x = Mathf.Round(targetPosition.x);
            targetPosition.y = Mathf.Round(targetPosition.y);
            isMoving = true;
        }
    }
    public Battler CreateBattler(string tag)
    {
        // ResourcesフォルダからBattlerBaseをロードする
        BattlerBase battlerBase = Resources.Load<BattlerBase>("Battlers/" + tag);

        if (battlerBase == null)
        {
            Debug.LogError("BattlerBase not found for tag: " + tag);
            return null;
        }

        // 新しいBattlerインスタンスを作成
        Battler newBattler = new Battler();
        
        // リフレクションを使用してプライベートフィールドにアクセスし、BattlerBaseを設定
        FieldInfo baseField = typeof(Battler).GetField("_base", BindingFlags.NonPublic | BindingFlags.Instance);
        baseField.SetValue(newBattler, battlerBase);

        // Battlerを初期化
        newBattler.Init();

        return newBattler;
    }

    void MoveToTarget() {
        Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, speed * Time.deltaTime);
        rb.MovePosition(newPosition);
        if (Vector3.Distance(newPosition, targetPosition) < 0.01f) {  // 目標に十分近づいたかチェック
            isMoving = false;  // 移動を停止
            int layerMask = ~LayerMask.GetMask("Player");
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(newPosition, 0.1f, layerMask);
            foreach (var hitCollider in hitColliders) {
                // Debug.Log("Hit Collider: " + hitCollider);
                HandleCollision(hitCollider);
            }
        }
    }

    void HandleCollision(Collider2D hitCollider){
        if (hitCollider.tag.StartsWith("Enemy_")) {
            HandleCollisionEnemy(hitCollider);
        } else if (hitCollider.tag.StartsWith("Key_")){
            HandleGetKey(hitCollider);
        } else if (hitCollider.tag.StartsWith("Lock_")){
            HandleOpenLock(hitCollider);
        }
    }

    void HandleCollisionEnemy(Collider2D hitCollider){
        // 敵との接触が確認された場合、戦闘UIを起動
        hitCollider.gameObject.SetActive(false);
        Battler battler = CreateBattler(hitCollider.tag);
        OnEncounts?.Invoke(battler);
    }

    void HandleGetKey(Collider2D hitCollider){
        if (hitCollider.tag == "Key_Yellow"){
            int currentYellowKey = itemManager.GetYellowKey();
            itemManager.SetYellowKey(currentYellowKey + 1);
            hitCollider.gameObject.SetActive(false);
        }
    }



    void ResetMovementFlags() {
        forwardmove = backmove = rightmove = leftmove = isMoving = false;
    }

    void UpdateAnimation()
    {
            if (rightmove == true){
                    animator.Play("MainCharacterRight");
                }
            else if (leftmove == true){
                    animator.Play("MainCharacterLeft");
                }
            else if (forwardmove == true){
                    animator.Play("MainCharacterUp");
                }
            else if (backmove == true){
                    animator.Play("MainCharacterDown");
                }
    }
    public void forwardButtonDown(){
        forwardmove = true; 
    }
    public void forwardButtonUp(){
        forwardmove = false;
    }
    public void backButtonDown(){
        backmove = true;
    }
    public void backButtonUp(){
        backmove = false;
    }
    public void rightButtonDown(){
        rightmove = true;
    }
    public void rightButtonUp(){
        rightmove = false;
    }
    public void leftButtonDown(){
        leftmove = true;
    }
    public void leftButtonUp(){
        leftmove = false;
    }
}