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
        // 原典の式: ダメージ = 攻撃側の攻撃力 - 防御側の防御力
        // 防御力が攻撃力以上なら 0 ダメージ（原典では「倒せない敵」になる）。
        // クランプしないと負のダメージで相手を回復させてしまう。
        int damage = Mathf.Max(0, attacker.Attack - Defense);
        HP = Mathf.Clamp(HP - damage, 0, MaxHP);
        Debug.Log($"Attack: {attacker.Attack}");
        Debug.Log($"Defense: {Defense}");
    }

    public void EarnCoin(Battler enemy){
        int amountEarned = enemy.Coin;
        Coin += amountEarned;
    }
}
