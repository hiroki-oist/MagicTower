using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[System.Serializable]
public class Battler
{
    [SerializeField] BattlerBase _base;

    public BattlerBase Base { get => _base;}

    //　ステータス
    public int MaxHP { get; set;}
    public int HP {get; set;}
    public int Attack {get; set;}
    public int Defense {get; set;}
    public int Coin {get; set;}

    // 初期化
    public void Init(){
        MaxHP = _base.MaxHP;
        HP = MaxHP;
        Attack = _base.Attack;
        Defense = _base.Defense;
        Coin = _base.Coin;
    }

    public void TakeDamage(Battler attacker){
        int damage = attacker.Attack - Defense;
        HP = Mathf.Clamp(HP - damage, 0, MaxHP);
        Debug.Log($"Attack: {attacker.Attack}");
        Debug.Log($"Defense: {Defense}");
    }

    public void EarnCoin(Battler enemy){
        int amountEarned = enemy.Coin;
        Coin += amountEarned;
    }
}
