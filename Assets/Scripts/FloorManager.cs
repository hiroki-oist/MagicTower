using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class FloorManager : MonoBehaviour
{
    public int floorNumber 　{ get; set;}
    
    [SerializeField] TextMeshProUGUI floorNumberText;
    
    void Awake() {
        floorNumber = 1;
        DontDestroyOnLoad(gameObject);
    }

    public void UpdateFloorNumberUI(){
        floorNumberText.text = $"{floorNumber} 階";
    }

    public int GetFloorNumber() {
        return floorNumber;
    }

    // Yellow Key の値を設定
    public void SetFloorNumber(int value) {
        floorNumber = value;
        string sceneName = "Floor_" + floorNumber;
        SceneManager.LoadScene(sceneName); 
        UpdateFloorNumberUI();
    }
}
