using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;
public class MainCharacterManager : MonoBehaviour {
    private CharacterStats stats;
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
    private string colliding;
    public GameObject battleUI;
    void Start () {
        Application.targetFrameRate = 30;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // battleUIが見つかったか確認
        if (battleUI == null)
        {
            Debug.LogError("BattleUI object is not found!");
            return;
        }
    }
    // Update is called once per frame
    void Update () {
        if ( forwardmove || backmove || rightmove || leftmove) {
            // Debug.Log("This is in Update");
            // Debug.Log("Checking for new move. Direction flags - Forward: " + forwardmove + ", Back: " + backmove + ", Right: " + rightmove + ", Left: " + leftmove);
            // Debug.Log("isMoving: " + isMoving);
        }
        if (isMoving) {
            int layerMask = ~LayerMask.GetMask("Player", "Enemy");
            RaycastHit2D hit = Physics2D.Raycast(rb.position, targetPosition - rb.position, Vector3.Distance(rb.position, targetPosition), layerMask);
            
            if (hit.collider != null) {
                            int hitLayer = hit.collider.gameObject.layer;
            string layerName = LayerMask.LayerToName(hitLayer);
            Debug.Log("Hit collider is showing: " + hit.collider);
            // Debug.Log("Collided with layer: " + layerName);
                colliding = hit.collider.name;
                isMoving = false;
                ResetMovementFlags();
            }
                
            if (hit.collider != null && hit.collider.CompareTag("Wall")) {
                // Debug.Log("Collision detected ahead with: " + hit.collider.name);
                ResetMovementFlags();  // 移動フラグをリセット
                isMoving = false;  // 移動を停止
            } else {
                MoveToTarget();
            }
        } else {
            // Debug.Log("So I am entergin here");
            CheckForNewMove();
        }
        UpdateAnimation();
    }

    void CheckForNewMove() {
        if ( forwardmove || backmove || rightmove || leftmove) {
            // Debug.Log("This is CheckForNewMove");
            // Debug.Log("Checking for new move. Direction flags - Forward: " + forwardmove + ", Back: " + backmove + ", Right: " + rightmove + ", Left: " + leftmove);
        }
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
        // Debug.Log("I have entered StartMove");
        if (!isMoving) { // すでに移動中でなければ
            // Debug.Log("Starting new move to direction: " + direction);
            // Debug.Log("transform.position: " + transform.position + " direction: " + direction);
            targetPosition = transform.position + direction;
            targetPosition.x = Mathf.Round(targetPosition.x);
            targetPosition.y = Mathf.Round(targetPosition.y);
            isMoving = true;
        }
    }

    void MoveToTarget() {
        // Debug.Log("Moving to target. Current position: " + rb.position + ", Target position: " + targetPosition);
        // speed = 60.0f;
        Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, speed * Time.deltaTime);
        // ApplyDamage();
        // Debug.Log("Applied Damage. The remaining Hit Point" + stats.health);
        rb.MovePosition(newPosition);
        // Debug.Log("Close enough: " + (Vector3.Distance(newPosition, targetPosition) < 0.01f));
    if (Vector3.Distance(newPosition, targetPosition) < 0.01f) {  // 目標に十分近づいたかチェック
            
            // Debug.Log("Reached target position.");
            isMoving = false;  // 移動を停止
            int layerMask = ~LayerMask.GetMask("Player");
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(newPosition, 0.1f, layerMask);
            Debug.Log("Number of HitColliders: " + hitColliders.Length);
            foreach (var hitCollider in hitColliders) {
                Debug.Log("HitCollider: " + hitCollider);
                if (hitCollider.CompareTag("Enemy")) {
                    Debug.Log("Yes it has hit enemy");
                    // 敵との接触が確認された場合、戦闘UIを起動
                    TriggerCombatUI(hitCollider.gameObject);
                    break;
                }
            }
            // ResetMovementFlags();
            // Debug.Log("MoveToTarget\nisMoving: " + isMoving);
        }
    }

    void TriggerCombatUI(GameObject enemy) {
        // 戦闘UIをアクティブにする
        // 敵情報をUIに渡すなど、必要な処理をここで行う
        battleUI.SetActive(true);
        // battleUICanvas.gameObject.Setup(enemy);  // 仮の関数でUIをセットアップ
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
        // Debug.Log("You pressed Up");
        forwardmove = true; 
    }
    public void forwardButtonUp(){
        forwardmove = false;
    }
    public void backButtonDown(){
        // Debug.Log("You pressed Down");
        backmove = true;
    }
    public void backButtonUp(){
        backmove = false;
    }
    public void rightButtonDown(){
        // Debug.Log("You pressed Right");
        rightmove = true;
    }
    public void rightButtonUp(){
        rightmove = false;
    }
    public void leftButtonDown(){
        // Debug.Log("You pressed Left");
        leftmove = true;
    }
    public void leftButtonUp(){
        leftmove = false;
    }
}