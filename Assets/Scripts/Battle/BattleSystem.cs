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
        SetupBattle(player, enemy);
        state = State.RunTurns;
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
        playerBattleUnit.Battler.EarnCoin(enemyTargetUnit.Battler);
        playerBattleUnit.UpdateCoinUI();
        state = State.BattleOver;

        earnedCoinDialogue.text = $"{enemyTargetUnit.Battler.Coin}ゴールド獲得";
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
