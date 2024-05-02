using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    // UIの管理
    // Battlerの管理
    public Battler Battler {get; set;}


    public virtual void Setup(Battler battler){
        Battler = battler;
    }

    public virtual void UpdateUI(){

    }
    public virtual void UpdateCoinUI(){
        
    }
}
