using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class BattleSystem : MonoBehaviour
{
    enum State{
        Start,
        RunTurns,
        BattleOver
    }

    State state;
    [SerializeField] BattleUnit playerStats;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] EnemyStats enemyStats;
    [SerializeField] TextMeshProUGUI earnedCoinDialogue;

    public UnityAction OnBattleOver;
    public float fadeInTime = 1.0f;

    public void BattleStart(Battler player, Battler enemy){
        state = State.Start;
        Debug.Log("バトル開始！");
        SetupBattle(player, enemy);
        state = State.RunTurns;
        StartCoroutine(WaitOneSecond());

    }
    private void SetupBattle(Battler player, Battler enemy){
        playerStats.Setup(player);
        enemyUnit.Setup(enemy);
        enemyStats.Setup(enemy);
    }

    public void BattleOver(){
        OnBattleOver?.Invoke();
    }

    public void RunTurn(BattleUnit playerBattleUnit, BattleUnit enemyTargetUnit){

        enemyTargetUnit.Battler.TakeDamage(playerBattleUnit.Battler);
        // Debug.Log("Attacker attack is " + playerBattleUnit.Battler.Attack);
        // Debug.Log("EnemyTargetUnit attack is " + enemyTargetUnit.Battler.Attack);
        // Debug.Log("Enemy coin: " + enemyTargetUnit.Battler.Coin);
        enemyTargetUnit.UpdateUI();

        if (enemyTargetUnit.Battler.HP <= 0){
            HandlePlayerWin(playerBattleUnit, enemyTargetUnit);
        }
        
        playerBattleUnit.Battler.TakeDamage(enemyTargetUnit.Battler);
        playerBattleUnit.UpdateUI();

        if (playerBattleUnit.Battler.HP <= 0){
            Debug.Log("あなたはやられてしまった。。");
            state = State.BattleOver;
        }


    }

    private void HandlePlayerWin(BattleUnit playerBattleUnit, BattleUnit enemyTargetUnit){
        Debug.Log("敵を倒した！");
        playerBattleUnit.Battler.EarnCoin(enemyTargetUnit.Battler);
        Debug.Log("敵のステータスは、HP: " + enemyTargetUnit.Battler.HP + "\nCoin: " + enemyTargetUnit.Battler.Coin);
        Debug.Log(enemyTargetUnit.Battler.Coin + "ゴールドを得た！");
        playerBattleUnit.UpdateCoinUI();
        state = State.BattleOver;

        earnedCoinDialogue.text = $"{enemyTargetUnit.Battler.Coin}ゴールド獲得";
    }

    IEnumerator WaitOneSecond()
    {
        Debug.Log("Wait started at " + Time.time);
        yield return new WaitForSeconds(1); // 1秒間待機
        Debug.Log("Wait ended at " + Time.time);
    }

    // Start is called before the first frame update
    void Start()
    {
        state = State.Start;
    }

    // Update is called once per frame
    void Update()
    {
        switch (state){
            case State.Start:
                state = State.RunTurns;
                break;
            case State.RunTurns:
                RunTurn(playerStats, enemyStats);
                break;
            case State.BattleOver:
                BattleOver();
                break;
        }
    }

    void HandleRunTurns(){

    }

    void HandleBattleOver(){

    }
}
