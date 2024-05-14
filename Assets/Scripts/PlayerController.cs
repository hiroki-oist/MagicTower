using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine.Events;
using System.Reflection;
using System;
using TMPro;
using UnityEngine.UI;
using System.Security.Cryptography;
public class PlayerController: MonoBehaviour {
    public static PlayerController Instance { get; private set; }
    public float speed = 60.0f;
    private Animator animator;
    bool forwardmove;
    bool backmove;
    bool rightmove;
    bool leftmove;
    private Vector2 targetPosition;
    private bool isMoving = false;
    private Rigidbody2D rb;
    public delegate void EncountsHandler(Battler enemyBattler);

    // イベントの定義
    public event EncountsHandler OnEncounts;
     [SerializeField] BattleUnit playerStats;
    public ItemManager itemManager;
    public FloorManager floorManager;

    public GameController gameController;
    [SerializeField] TextMeshProUGUI swordName;
    [SerializeField] TextMeshProUGUI shieldName;
    public GameObject shieldObject;
    public GameObject swordObject;
    [SerializeField] TextMeshProUGUI floorNumber;

    // インスタンスが一つであることを確認
    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else if (Instance != this) {
            Destroy(gameObject);
        }
    }

    void Start () {
        gameController.PlayerBattler.Init();
        gameController.PlayerBattler.HP = 1000;
        playerStats.Setup(gameController.PlayerBattler);
        playerStats.UpdateUI();
        playerStats.UpdateCoinUI();
        itemManager.UpdateKeyValue();
        
        Application.targetFrameRate = 30;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update () {
        // Debug.Log("My Position is: " + rb.position);
        if (isMoving) {
            int layerMask = ~LayerMask.GetMask("Player", "Enemy", "Item");
            RaycastHit2D hit = Physics2D.Raycast(rb.position, targetPosition - rb.position, Vector3.Distance(rb.position, targetPosition), layerMask);
            // Debug.Log("Hit: " + hit.collider);
            if (hit.collider != null && hit.collider.tag.StartsWith("Wall_")) {
                if (hit.collider.tag == "Wall_Secret"){
                    gameController.DestroyObject(hit.collider.gameObject);
                }
                isMoving = false;
                ResetMovementFlags();
            } else if (hit.collider != null && hit.collider.tag.StartsWith("Lock_")){
                isMoving = false;
                ResetMovementFlags();
                HandleOpenLock(hit.collider);
            } else {
                MoveToTarget();
            }
        } else {
            CheckForNewMove();
        }
        UpdateAnimation();
    }

    void HandleOpenLock(Collider2D hitCollider){
        gameController.DestroyObject(hitCollider.gameObject);
        if (hitCollider.tag == "Lock_Yellow"){
            int currentYellowKey = itemManager.GetYellowKey();
            if (currentYellowKey >= 1){
                itemManager.SetYellowKey(currentYellowKey - 1);
                // hitCollider.gameObject.SetActive(false);
            } 
        } else if (hitCollider.tag == "Lock_Blue"){
            int currentBlueKey = itemManager.GetBlueKey();
            if (currentBlueKey >= 1){
                itemManager.SetBlueKey(currentBlueKey - 1);
                // hitCollider.gameObject.SetActive(false);
            }
        } else if (hitCollider.tag == "Lock_Red"){
            int currentRedKey = itemManager.GetRedKey();
            if (currentRedKey >= 1){
                itemManager.SetRedKey(currentRedKey - 1);
                // hitCollider.gameObject.SetActive(false);
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
        if (!isMoving) { 
            targetPosition = transform.position + direction;
            targetPosition.x = Mathf.Round(targetPosition.x);
            targetPosition.y = Mathf.Round(targetPosition.y);
            isMoving = true;
        }
    }
    public Battler CreateBattler(string tag)
    {
        BattlerBase battlerBase = Resources.Load<BattlerBase>("Battlers/" + tag);

        if (battlerBase == null)
        {
            Debug.LogError("BattlerBase not found for tag: " + tag);
            return null;
        }

        Battler newBattler = new Battler();
        
        FieldInfo baseField = typeof(Battler).GetField("_base", BindingFlags.NonPublic | BindingFlags.Instance);
        baseField.SetValue(newBattler, battlerBase);

        newBattler.Init();

        return newBattler;
    }

    void MoveToTarget() {
        Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, speed * Time.deltaTime);
        rb.MovePosition(newPosition);
        if (Vector3.Distance(newPosition, targetPosition) < 0.01f) {  
            isMoving = false;  // 移動を停止
            int layerMask = ~LayerMask.GetMask("Player");
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(newPosition, 0.1f, layerMask);
            foreach (var hitCollider in hitColliders) {
                HandleCollision(hitCollider, newPosition, targetPosition);
            }
        }
    }

    void HandleCollision(Collider2D hitCollider, Vector3 newPosition, Vector3 targetPosition){
        if (hitCollider.tag.StartsWith("Enemy_")) {
            ResetMovementFlags();
            HandleCollisionEnemy(hitCollider);
        } else if (hitCollider.tag.StartsWith("Key_")){
            HandleGetKey(hitCollider);
        } else if (hitCollider.tag.StartsWith("Lock_")){
            ResetMovementFlags();
            HandleOpenLock(hitCollider);
        } else if (hitCollider.tag.StartsWith("Potion_")){
            HandleGetPotion(hitCollider);
        } else if (hitCollider.tag.StartsWith("Gem_")){
            HandleGetGem(hitCollider);
        } else if (hitCollider.tag.StartsWith("Stairs_")){
            ResetMovementFlags();
            HandleStairs(hitCollider);
        } else if (hitCollider.tag.StartsWith("Sword")){
            HandleGetSword(hitCollider);
        } else if (hitCollider.tag.StartsWith("Shield")){
            HandleGetShield(hitCollider);
        }
    }

    void HandleStairs(Collider2D hitCollider){
        int currentFloor = floorManager.GetFloorNumber();
        if (hitCollider.tag == "Stairs_Up"){
            floorManager.SetFloorNumber(currentFloor + 1);
            floorNumber.text = $"{currentFloor + 1} 階";
        } else if (hitCollider.tag == "Stairs_Down"){
            floorManager.SetFloorNumber(currentFloor - 1);
            floorNumber.text = $"{currentFloor - 1} 階";
        }
    }

    void HandleGetSword(Collider2D hitCollider){
        Sprite swordSprite = Resources.Load<Sprite>("Weapons/" + hitCollider.gameObject.name);
        SpriteRenderer spriteRenderer = swordObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = swordSprite;
        // hitCollider.gameObject.SetActive(false);
        gameController.DestroyObject(hitCollider.gameObject);
        if (hitCollider.gameObject.name == "Sword_1"){
            gameController.PlayerBattler.Attack += 10;
            swordName.text = "鉄の剣";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Sword_2"){
            gameController.PlayerBattler.Attack += 20;
            swordName.text = "鋼の剣";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Sword_3"){
            gameController.PlayerBattler.Attack += 30;
            swordName.text = "騎士の剣";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Sword_4"){
            gameController.PlayerBattler.Attack += 50;
            swordName.text = "聖なる剣";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Sword_5"){
            gameController.PlayerBattler.Attack += 100;
            swordName.text = "神剣";
            playerStats.UpdateUI();
        }
    }

    void HandleGetShield(Collider2D hitCollider){
        Sprite shieldSprite = Resources.Load<Sprite>("Weapons/" + hitCollider.gameObject.name);
        SpriteRenderer spriteRenderer = shieldObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = shieldSprite;
        gameController.DestroyObject(hitCollider.gameObject);
        if (hitCollider.gameObject.name == "Shield_1"){
            gameController.PlayerBattler.Defense += 10;
            shieldName.text = "鉄の盾";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Shield_2"){
            gameController.PlayerBattler.Defense += 20;
            shieldName.text = "鋼の剣";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Shield_3"){
            gameController.PlayerBattler.Defense += 30;
            shieldName.text = "騎士の盾";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Shield_4"){
            gameController.PlayerBattler.Defense += 50;
            shieldName.text = "聖なる盾";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Shield_5"){
            gameController.PlayerBattler.Defense += 100;
            shieldName.text = "神盾";
            playerStats.UpdateUI();
        }
    }

    void HandleGetPotion(Collider2D hitCollider){
        // hitCollider.gameObject.SetActive(false);
        gameController.DestroyObject(hitCollider.gameObject);
        if (hitCollider.tag == "Potion_Red"){
            gameController.PlayerBattler.HP += 200;
            playerStats.UpdateUI();
        } else if (hitCollider.tag == "Potion_Blue"){
            gameController.PlayerBattler.HP += 400;
            playerStats.UpdateUI();
        }
    }

    void HandleGetGem(Collider2D hitCollider){
        // hitCollider.gameObject.SetActive(false);
        gameController.DestroyObject(hitCollider.gameObject);
        if (hitCollider.tag == "Gem_Attack"){
            gameController.PlayerBattler.Attack += 2;
            playerStats.UpdateUI();
        } else if (hitCollider.tag == "Gem_Defense"){
            gameController.PlayerBattler.Defense += 4;
            playerStats.UpdateUI();
        }
    }

    void HandleCollisionEnemy(Collider2D hitCollider){
        // 敵との接触が確認された場合、戦闘UIを起動
        // hitCollider.gameObject.SetActive(false);
        Debug.Log($"Player Stats\tHP: {gameController.PlayerBattler.HP}\tAttack: {gameController.PlayerBattler.Attack}\tDefense: {gameController.PlayerBattler.Defense}");
        gameController.DestroyObject(hitCollider.gameObject);
        Battler battler = CreateBattler(hitCollider.tag);
        OnEncounts?.Invoke(battler);
    }

    void HandleGetKey(Collider2D hitCollider){
        gameController.DestroyObject(hitCollider.gameObject);
        if (hitCollider.tag == "Key_Yellow"){
            int currentYellowKey = itemManager.GetYellowKey();
            itemManager.SetYellowKey(currentYellowKey + 1);
            // hitCollider.gameObject.SetActive(false);
        } else if (hitCollider.tag == "Key_Blue"){
            int currentBlueKey = itemManager.GetBlueKey();
            itemManager.SetBlueKey(currentBlueKey + 1);
            // hitCollider.gameObject.SetActive(false);
        } else if (hitCollider.tag == "Key_Red"){
            int currentRedKey = itemManager.GetRedKey();
            itemManager.SetRedKey(currentRedKey + 1);
            // hitCollider.gameObject.SetActive(false);
        }
    }



    public void ResetMovementFlags() {
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