using UnityEngine;
using System.Collections;
public class MainCharacterManager : MonoBehaviour {
    public float speed = 5.0f;
    private Animator animator;
    bool forwardmove;
    bool backmove;
    bool rightmove;
    bool leftmove;
    // Use this for initialization
    private Vector3 targetPosition;
    private bool isMoving = false;
    void Start () {
        animator = GetComponent<Animator>();
    }
    // Update is called once per frame
    void Update () {
        if(forwardmove == true){
            transform.position += new Vector3(0, speed * Time.deltaTime, 0);
        }
        if(backmove == true){
            transform.position += new Vector3(0, -speed * Time.deltaTime, 0);
        }
        if(rightmove == true){
            transform.position += new Vector3(speed * Time.deltaTime, 0, 0);
        }
        if(leftmove == true){
            transform.position += new Vector3(-speed * Time.deltaTime, 0, 0);
        }
        UpdateAnimation();
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