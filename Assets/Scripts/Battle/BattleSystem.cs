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
        StartTurns,
        RunningTurns,
        BattleOver
    }

    State state;
    [SerializeField] BattleUnit playerStats;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] EnemyStats enemyStats;
    [SerializeField] TextMeshProUGUI earnedCoinDialogue;

    public UnityAction OnBattleOver;
    public float fadeInTime = 1.0f;

    /// <summary>直前の戦闘にプレーヤーが勝ったか。決着後に GameController が参照する。</summary>
    public bool PlayerWon { get; private set; }

    public void BattleStart(Battler player, Battler enemy){
        PlayerWon = false;
        SetupBattle(player, enemy);
        state = State.StartTurns;
    }
    private void SetupBattle(Battler player, Battler enemy){
        playerStats.Setup(player);
        enemyUnit.Setup(enemy);
        enemyStats.Setup(enemy);
    }

    public void BattleOver(){
        OnBattleOver?.Invoke();
    }

    public IEnumerator RunTurn(BattleUnit playerBattleUnit, BattleUnit enemyTargetUnit)
    {
        state = State.RunningTurns;
        Debug.Log("Attacking enemyUnit");
        enemyTargetUnit.Battler.TakeDamage(playerBattleUnit.Battler);
        yield return new WaitForSeconds(0.1f); // 最初のアクション後に0.2秒待機
        enemyTargetUnit.UpdateUI();

        if (enemyTargetUnit.Battler.HP <= 0)
        {
            HandlePlayerWin(playerBattleUnit, enemyTargetUnit);
        }
        else
        {
            yield return new WaitForSeconds(0.1f); // 二つ目のアクション前に0.2秒待機
            Debug.Log("EnemyUnit Attacking");
            playerBattleUnit.Battler.TakeDamage(enemyTargetUnit.Battler);
            playerBattleUnit.UpdateUI();

            if (playerBattleUnit.Battler.HP <= 0)
            {
                // 原典のメッセージ: 「生命力がなくなりました。再挑戦してください。」
                PlayerWon = false;
                state = State.BattleOver;
            } else {
                state = State.StartTurns;
            }
        }
        
    }
    private void HandlePlayerWin(BattleUnit playerBattleUnit, BattleUnit enemyTargetUnit){
        PlayerWon = true;
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
                state = State.StartTurns;
                break;
            case State.StartTurns:
                StartCoroutine(RunTurn(playerStats, enemyStats));
                break;
            case State.RunningTurns:
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
