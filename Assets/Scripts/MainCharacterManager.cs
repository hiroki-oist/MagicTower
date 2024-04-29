using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;
public class MainCharacterManager : MonoBehaviour {
    public float speed = 5.0f;
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
    void Start () {
        animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
    }
    // Update is called once per frame
    void Update () {
        if ( forwardmove || backmove || rightmove || leftmove) {
            Debug.Log("This is in Update");
            Debug.Log("Checking for new move. Direction flags - Forward: " + forwardmove + ", Back: " + backmove + ", Right: " + rightmove + ", Left: " + leftmove);
            Debug.Log("isMoving: " + isMoving);
        }
        if (isMoving) {
            int layerMask = ~LayerMask.GetMask("Player");
            RaycastHit2D hit = Physics2D.Raycast(rb.position, targetPosition - rb.position, Vector3.Distance(rb.position, targetPosition), layerMask);
            
            if (hit.collider != null) {
                            int hitLayer = hit.collider.gameObject.layer;
            string layerName = LayerMask.LayerToName(hitLayer);
            Debug.Log("Hit collider is showing: " + hit.collider);
            Debug.Log("Collided with layer: " + layerName);
                colliding = hit.collider.name;
                isMoving = false;
                ResetMovementFlags();
            }
                
            if (hit.collider != null && hit.collider.CompareTag("Wall")) {
                Debug.Log("Collision detected ahead with: " + hit.collider.name);
                ResetMovementFlags();  // 移動フラグをリセット
                isMoving = false;  // 移動を停止
            } else {
                Debug.Log("rb.position: " + rb.position + " targetPosition: " + targetPosition);
                MoveToTarget();
            }
        } else {
            Debug.Log("So I am entergin here");
            CheckForNewMove();
        }
        UpdateAnimation();
    }

    void CheckForNewMove() {
        if ( forwardmove || backmove || rightmove || leftmove) {
            Debug.Log("This is CheckForNewMove");
            Debug.Log("Checking for new move. Direction flags - Forward: " + forwardmove + ", Back: " + backmove + ", Right: " + rightmove + ", Left: " + leftmove);
        }
        if(forwardmove){
            StartMove(new Vector3(0, 2, 0));
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
        Debug.Log("I have entered StartMove");
        if (!isMoving) { // すでに移動中でなければ
            // Debug.Log("Starting new move to direction: " + direction);
            Debug.Log("transform.position: " + transform.position + " direction: " + direction);
            targetPosition = transform.position + direction;
            isMoving = true;
        }
    }

    void MoveToTarget() {
        // Debug.Log("Moving to target. Current position: " + rb.position + ", Target position: " + targetPosition);
        Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, speed * Time.deltaTime);
        rb.MovePosition(newPosition);
        Debug.Log("Close enough: " + (Vector3.Distance(newPosition, targetPosition) < 0.01f));
    if (Vector3.Distance(newPosition, targetPosition) < 0.01f) {  // 目標に十分近づいたかチェック
            
            // Debug.Log("Reached target position.");
            isMoving = false;  // 移動を停止
            // ResetMovementFlags();
            Debug.Log("MoveToTarget\nisMoving: " + isMoving);
        }
    }

    void ResetMovementFlags() {
        forwardmove = backmove = rightmove = leftmove = isMoving = false;
    }

    void OnCollisionEnter2D(Collision2D collision) {
        if (collision.collider.CompareTag("Wall")) {

            // Debug.Log("Collision with wall detected. Normal of collision: " + collision.contacts[0].normal);
            isMoving = false;  // 移動を停止

            // 衝突した方向に応じてフラグをリセット
            ResetSpecificMovementFlag(collision.contacts[0].normal);
            AdjustPositionAfterCollision(collision);
        }
    }

    void ResetSpecificMovementFlag(Vector3 hitNormal) {
        // Debug.Log("Resetting movement flag based on collision normal: " + hitNormal);
        if (Mathf.Approximately(hitNormal.y, 1)) {  // 下向きに衝突
            forwardmove = false;
        } else if (Mathf.Approximately(hitNormal.y, -1)) {  // 上向きに衝突
            backmove = false;
        } else if (Mathf.Approximately(hitNormal.x, 1)) {  // 左向きに衝突
            rightmove = false;
        } else if (Mathf.Approximately(hitNormal.x, -1)) {  // 右向きに衝突
            leftmove = false;
        }
    }

    void AdjustPositionAfterCollision(Collision2D collision) {
        Vector3 hitNormal = collision.contacts[0].normal;
        // Debug.Log("Adjusting position after collision. Moving away from wall by: " + hitNormal * 0.1f);
        transform.position -= hitNormal * 0.1f;  // 0.1は調整必要に応じて変更してください
        isMoving = false;
        CheckForContinuedMovement();
    }

    void CheckForContinuedMovement() {
        if (!isMoving && (forwardmove || backmove || rightmove || leftmove)) {
            Vector3 moveDirection = Vector3.zero;
            if (forwardmove) moveDirection = Vector3.up;
            if (backmove) moveDirection = Vector3.down;
            if (rightmove) moveDirection = Vector3.right;
            if (leftmove) moveDirection = Vector3.left;

            StartMove(moveDirection);  // 移動再開
        }
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
            // animator.SetFloat("MoveX", movement.x);
            // animator.SetFloat("MoveY", movement.y);
    }
    public void forwardButtonDown(){
        Debug.Log("You pressed Up");
        forwardmove = true; 
    }
    public void forwardButtonUp(){
        forwardmove = false;
    }
    public void backButtonDown(){
        Debug.Log("You pressed Down");
        backmove = true;
    }
    public void backButtonUp(){
        backmove = false;
    }
    public void rightButtonDown(){
        Debug.Log("You pressed Right");
        rightmove = true;
    }
    public void rightButtonUp(){
        rightmove = false;
    }
    public void leftButtonDown(){
        Debug.Log("You pressed Left");
        leftmove = true;
    }
    public void leftButtonUp(){
        leftmove = false;
    }
}