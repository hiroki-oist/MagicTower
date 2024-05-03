using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class ItemManager : MonoBehaviour
{
    public int yellowKey　{ get; set;}
    public int blueKey { get; set;}
    public int redKey { get; set;}
    
    [SerializeField] TextMeshProUGUI yellow_key_value;
    [SerializeField] TextMeshProUGUI blue_key_value;
    [SerializeField] TextMeshProUGUI red_key_value;
    
    void Awake() {
        yellowKey = 0;
        blueKey = 0;
        redKey = 0;
    }

    public void UpdateKeyValue(){
        yellow_key_value.text = $"{yellowKey}";
        blue_key_value.text   = $"{blueKey}";
        red_key_value.text    = $"{redKey}";
    }

    public int GetYellowKey() {
        return yellowKey;
    }
    public int GetBlueKey() {
        return blueKey;
    }
    public int GetRedKey() {
        return redKey;
    }

    // public void UseYellowKey() {
    //     yellowKey -= 1;
    //     UpdateKeyValue();
    // }

    // public void UseBlueKey() {
    //     blueKey -= 1;
    //     UpdateKeyValue();
    // }

    // public void UseRedKey() {
    //     redKey -= 1;
    //     UpdateKeyValue();
    // }

    // Yellow Key の値を設定
    public void SetYellowKey(int value) {
        yellowKey = value;
        UpdateKeyValue();
    }

    public void SetBlueKey(int value) {
        blueKey = value;
        UpdateKeyValue(); 
    }
    public void SetRedKey(int value) {
        redKey = value;
        UpdateKeyValue();
    }

}
