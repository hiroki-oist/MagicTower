using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Battler playerBattler;

    [SerializeField] Battler enemyBattler;
    public CanvasGroup endBattleUI;


    public Battler EnemyBattler { get => enemyBattler; }
    public Battler PlayerBattler { get => playerBattler; }

    private void Start(){
        player.OnEncounts += StartBattle;
        battleSystem.OnBattleOver += EndBattle;
    }
    public void StartBattle(){
        // playerBattler.Init();
        enemyBattler.Init();
  
        // player.gameObject.SetActive(false);
        battleSystem.gameObject.SetActive(true);
        battleSystem.BattleStart(playerBattler, enemyBattler);
    }

    public void EndBattle(){
        player.gameObject.SetActive(true);
        battleSystem.gameObject.SetActive(false);
        endBattleUI.gameObject.SetActive(true);

        StartCoroutine(WaitOneSecond("EndBattle"));

    }

    IEnumerator WaitOneSecond(string id)
    {
        Debug.Log($"ID: {id}, Wait started at " + Time.time);
        yield return new WaitForSeconds(1); // 1秒間待機
        Debug.Log($"ID: {id}, Wait ended at " + Time.time);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
