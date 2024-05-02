using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine.Events;
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
    public GameObject battleUI;
    // [SerializeField] GameController gameController;
    public UnityAction OnEncounts; // Encountしたときに実行したい関数を登録
     [SerializeField] BattleUnit playerStats;
    // [SerializeField] Battler battler;
    // [SerializeField] Battler enemyBattler;

    // public Battler Battler {get => battler;}
    // public Battler EnemyBattler {get => enemyBattler;}

    public GameController gameController;
    

    void Start () {
        gameController.PlayerBattler.Init();
        Debug.Log("Initialised playbattler");
        playerStats.Setup(gameController.PlayerBattler);
        playerStats.UpdateUI();
        playerStats.UpdateCoinUI();
        
        Application.targetFrameRate = 30;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        // battler.Init();
        Debug.Log("Initialise EnemyBattler");
        // enemyBattler.Init();

        // battleUIが見つかったか確認
        if (battleUI == null)
        {
            Debug.LogError("BattleUI object is not found!");
            return;
        }
        Debug.Log("Start done");
        
    }

    // Update is called once per frame
    void Update () {
        if (isMoving) {
            int layerMask = ~LayerMask.GetMask("Player", "Enemy");
            RaycastHit2D hit = Physics2D.Raycast(rb.position, targetPosition - rb.position, Vector3.Distance(rb.position, targetPosition), layerMask);
            
            if (hit.collider != null) {
                int hitLayer = hit.collider.gameObject.layer;
                string layerName = LayerMask.LayerToName(hitLayer);
                isMoving = false;
                ResetMovementFlags();
            }
                
            if (hit.collider != null && hit.collider.CompareTag("Wall")) {
                ResetMovementFlags();  // 移動フラグをリセット
                isMoving = false;  // 移動を停止
            } else {
                MoveToTarget();
            }
        } else {
            CheckForNewMove();
        }
        UpdateAnimation();
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
    private void WhenCollidedEnemy(Collider2D collision)
    {
        if (collision.tag.StartsWith("Enemy_"))
        {
            LoadBattler(collision.tag);
        }
    }
    void LoadBattler(string tag)
    {
        BattlerBase battler = Resources.Load<BattlerBase>("Battlers/" + tag);
        if (battler != null)
        {
            Debug.Log("Loaded Battler: " + battler.Name);
            // ここでbattlerのデータを使用して何かを行う
        }
        else
        {
            Debug.LogError("Battler not found for tag: " + tag);
        }
    }

    void MoveToTarget() {
        Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, speed * Time.deltaTime);
        rb.MovePosition(newPosition);
        if (Vector3.Distance(newPosition, targetPosition) < 0.01f) {  // 目標に十分近づいたかチェック
                isMoving = false;  // 移動を停止
                // StartCoroutine(WaitTwoSecond());
                int layerMask = ~LayerMask.GetMask("Player");
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(newPosition, 0.1f, layerMask);
                foreach (var hitCollider in hitColliders) {
                    Debug.Log(hitCollider);
                    if (hitCollider.tag.StartsWith("Enemy_")) {
                        // 敵との接触が確認された場合、戦闘UIを起動
                        hitCollider.gameObject.SetActive(false);
                        LoadBattler(hitCollider.tag);
                        OnEncounts?.Invoke();
                        break;
                    }
                }
            }
        }

    void TriggerCombatUI(GameObject enemy) {
        // 戦闘UIをアクティブにする
        // 敵情報をUIに渡すなど、必要な処理をここで行う
        battleUI.SetActive(true);
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