using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine.Events;
using System.Reflection;
using System;
using TMPro;
using UnityEngine.UI;
using System.Security.Cryptography;
using Unity.VisualScripting;
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
    public delegate void EncountsHandler(Battler enemyBattler, GameObject enemyObject);

    // イベントの定義
    public event EncountsHandler OnEncounts;
     [SerializeField] BattleUnit playerStats;
    public ItemManager itemManager;

    /// <summary>原典の特殊アイテム15種の所持状態。</summary>
    public SpecialItems specialItems = new SpecialItems();
    public FloorManager floorManager;

    public GameController gameController;
    [SerializeField] TextMeshProUGUI swordName;
    [SerializeField] TextMeshProUGUI shieldName;
    public GameObject shieldObject;

    /// <summary>
    /// 装備中の盾の段階（0=無し, 1=鉄 … 5=神盾）。
    /// 原典では神盾(5)が魔術士・魔導師の魔法攻撃を無効にするため、数値で持つ必要がある。
    /// </summary>
    public int EquippedShieldRank { get; private set; }
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
                if (hit.collider != null){
                    int hitLayer = hit.collider.gameObject.layer;
                    string layerName = LayerMask.LayerToName(hitLayer);
                    if (layerName == "People"){
                        ResetMovementFlags();
                    }
                } else {
                    MoveToTarget();
                }
            }
        } else {
            CheckForNewMove();
        }
        UpdateAnimation();
    }

    /// <summary>
    /// 扉に触れたときの処理。原典 PC98版 MTE.EXE の扉処理（0x23ee）の規則:
    ///
    /// <code>
    ///   si = マスの値 × 6
    ///   if (属性表[si].種別 == 1) {        // 種別1 は ID1,2,3 の「鍵の扉」だけ
    ///       ID1 → 鍵 / ID2 → 紫の鍵 / ID3 → 赤い鍵 を1本消費して床にする
    ///       本数が0なら何もしない（扉は残る）
    ///   }
    /// </code>
    ///
    /// **門(ID4) と特殊扉(ID5) はこの分岐に入らない。**
    /// 属性表の種別がそれぞれ 2 と 5 で、種別1ではないため。
    /// つまり原典では**触れても開かず、壁と同じ**で、
    /// 別の仕組み（解錠判定 <see cref="SealedDoors"/>）が条件を満たしたときに
    /// 地図側から床に書き換えられて初めて通れるようになる。
    ///
    /// ⚠ 以前は「封印扉・特殊扉は鍵を消費せず開く」と実装していたが、これは誤りだった。
    ///    当時は解錠条件が未解明で、Java版の「無条件で開く」という読みに従っていた。
    ///    原典コードを読んで、門は18マスあり、そのすべてが解錠表の18件と一致することも確認済み。
    /// </summary>
    void HandleOpenLock(Collider2D hitCollider){
        bool opened = false;
        string tag = hitCollider.tag;

        if (tag == "Lock_Yellow"){
            int k = itemManager.GetYellowKey();
            if (k >= 1){ itemManager.SetYellowKey(k - 1); opened = true; }
        } else if (tag == "Lock_Blue"){          // 原典では「紫の鍵」
            int k = itemManager.GetBlueKey();
            if (k >= 1){ itemManager.SetBlueKey(k - 1); opened = true; }
        } else if (tag == "Lock_Red"){
            int k = itemManager.GetRedKey();
            if (k >= 1){ itemManager.SetRedKey(k - 1); opened = true; }
        }
        // 門(Lock_Sealed) と特殊扉(Lock_Special) は触れても開かない。
        // 解錠判定が満たされたときに地図側から床へ書き換えられる。

        if (!opened){
            // 鍵が足りないとき・門や特殊扉のときは壁と同じ。扉は残る。
            ResetMovementFlags();
            return;
        }
        gameController.DestroyObject(hitCollider.gameObject);
    }

    /// <summary>
    /// 1マス動き終わったことを規則側へ伝える。原典の一手はこの順で進む:
    ///   移動 → 隣接マスの魔法攻撃 → 解錠判定。
    /// GameRules がシーンに無ければ何もしない（旧シーンはそのまま動く）。
    /// </summary>
    void NotifyMoved(Vector3 worldPosition)
    {
        var rules = GameRules.Active;
        if (rules == null || rules.Floor == null) return;

        var cell = GameRules.ToCell(worldPosition);
        var battler = gameController != null ? gameController.PlayerBattler : null;
        int hp = battler != null ? battler.HP : 0;

        int after = rules.OnPlayerMoved(cell, hp, EquippedShieldRank);
        if (battler != null && after != hp) battler.HP = after;
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
            NotifyMoved(newPosition);
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
        } else if (hitCollider.tag.StartsWith("Item_")){
            HandleGetSpecialItem(hitCollider);
        } else if (hitCollider.tag == "Shop_Altar"){
            ResetMovementFlags();
            HandleShop(hitCollider);
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
            swordName.text = "銀の剣";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Sword_3"){
            gameController.PlayerBattler.Attack += 40;   // 原典: 攻撃力が40ポイントアップ
            swordName.text = "騎士の剣";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Sword_4"){
            gameController.PlayerBattler.Attack += 50;
            swordName.text = "聖　剣";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Sword_5"){
            gameController.PlayerBattler.Attack += 100;
            swordName.text = "神　剣";
            playerStats.UpdateUI();
        }
    }

    void HandleGetShield(Collider2D hitCollider){
        // 名前の末尾の数字が段階。原典の「装備盾」(1〜5) に対応する
        var shieldKey = hitCollider.gameObject.name;
        int rank;
        if (shieldKey.Length > 0 && int.TryParse(shieldKey.Substring(shieldKey.Length - 1), out rank))
            EquippedShieldRank = rank;

        Sprite shieldSprite = Resources.Load<Sprite>("Weapons/" + shieldKey);
        SpriteRenderer spriteRenderer = shieldObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = shieldSprite;
        gameController.DestroyObject(hitCollider.gameObject);
        if (hitCollider.gameObject.name == "Shield_1"){
            gameController.PlayerBattler.Defense += 10;
            shieldName.text = "鉄の盾";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Shield_2"){
            gameController.PlayerBattler.Defense += 20;
            shieldName.text = "銀の盾";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Shield_3"){
            gameController.PlayerBattler.Defense += 40;   // 原典: 防御力40ポイントアップ
            shieldName.text = "騎士の盾";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Shield_4"){
            gameController.PlayerBattler.Defense += 50;
            shieldName.text = "聖　盾";
            playerStats.UpdateUI();
        } else if (hitCollider.gameObject.name == "Shield_5"){
            gameController.PlayerBattler.Defense += 100;
            shieldName.text = "神　盾";
            playerStats.UpdateUI();
        }
    }

    void HandleGetPotion(Collider2D hitCollider){
        gameController.DestroyObject(hitCollider.gameObject);
        // 原典では階によって効果量が変わる（Resources/ItemPower.txt に根拠を記載）
        var power = ItemPowerTable.Instance.ForFloor(floorManager.GetFloorNumber());
        int amount = hitCollider.tag == "Potion_Red" ? power.PotionRed
                   : hitCollider.tag == "Potion_Blue" ? power.PotionBlue : 0;
        if (amount == 0) return;
        gameController.PlayerBattler.HP += amount;
        playerStats.UpdateUI();
        // 文言は Resources/Messages.txt（PC98版の原文）から引く
        Debug.Log(GameText.Instance.Message("potion", amount));
    }

    void HandleGetGem(Collider2D hitCollider){
        gameController.DestroyObject(hitCollider.gameObject);
        var power = ItemPowerTable.Instance.ForFloor(floorManager.GetFloorNumber());
        if (hitCollider.tag == "Gem_Attack"){
            gameController.PlayerBattler.Attack += power.CrystalAttack;
            playerStats.UpdateUI();
            Debug.Log(GameText.Instance.Message("crystal_atk", power.CrystalAttack));
        } else if (hitCollider.tag == "Gem_Defense"){
            gameController.PlayerBattler.Defense += power.CrystalDefense;
            playerStats.UpdateUI();
            Debug.Log(GameText.Instance.Message("crystal_def", power.CrystalDefense));
        }
    }

    void HandleCollisionEnemy(Collider2D hitCollider){
        // 敵との接触が確認された場合、戦闘UIを起動
        Debug.Log($"Player Stats\tHP: {gameController.PlayerBattler.HP}\tAttack: {gameController.PlayerBattler.Attack}\tDefense: {gameController.PlayerBattler.Defense}");
        Battler battler = CreateBattler(hitCollider.tag);
        if (battler == null) return;

        // 原典: 自分の攻撃力以上の防御力を持った敵は攻撃できない。
        // ガードが無いと 0 ダメージのまま殴り合い、プレーヤーだけが減り続ける。
        // 十字架・ドラゴンスレーヤーの2倍補正もここで効く。
        var kind = BattleRules.KindFromTag(hitCollider.tag);
        var charms = specialItems.ToCharms();
        if (!BattleRules.CanDefeat(gameController.PlayerBattler.Attack, battler.Defense, kind, charms))
        {
            Debug.Log($"攻撃できない（自分の攻撃力 {gameController.PlayerBattler.Attack} / 相手の防御力 {battler.Defense} / 種別 {kind}）");
            ResetMovementFlags();
            return;   // 壁と同じで進めない
        }

        // 敵オブジェクトの破棄は勝敗が決まってから行う。
        // ここで消すと、負けたときに敵だけ消えて盤面が壊れる。
        OnEncounts?.Invoke(battler, hitCollider.gameObject);
    }

    /// <summary>オーブ・マトック・魔法の鍵などの特殊アイテムを取得する。</summary>
    void HandleGetSpecialItem(Collider2D hitCollider){
        var kind = SpecialItems.FromPrefabName(hitCollider.gameObject.name);
        if (kind == SpecialItems.Kind.None){
            Debug.LogWarning($"未対応の特殊アイテム: {hitCollider.gameObject.name}");
            return;
        }
        string message = specialItems.Add(kind);
        Debug.Log(message);
        gameController.DestroyObject(hitCollider.gameObject);
    }

    /// <summary>商店のカウンター。原典では会話で品物を買う。</summary>
    void HandleShop(Collider2D hitCollider){
        // 商店は盤面から消えない。会話UIの実装が済むまではログのみ。
        Debug.Log("商店のカウンターに話しかけた");
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