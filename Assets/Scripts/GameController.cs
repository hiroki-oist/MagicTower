using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Battler playerBattler;
    public CanvasGroup endBattleUI;
    [SerializeField] BattleUnit playerStats;

    // public Battler EnemyBattler { get => enemyBattler; }
    public Battler PlayerBattler { get => playerBattler; }
    public static GameController Instance { get; private set; }
    private string objectKey;

    private void Awake()
    {
        Debug.Log("Deleted PlayerPrefs");
        PlayerPrefs.DeleteAll(); // すべてのキーを削除
        PlayerPrefs.Save(); // 変更を保存
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // private void Start(){
    //     Debug.Log("Deleted PlayerPrefs");
    //     PlayerPrefs.DeleteAll(); // すべてのキーを削除
    //     PlayerPrefs.Save(); // 変更を保存
    // }
    private void OnEnable(){
        player.OnEncounts += StartBattle;
        battleSystem.OnBattleOver += EndBattle;
    }
    
    public void StartBattle(Battler enemyBattler){
        Debug.Log("Started battle");
        enemyBattler.Init();
  
        player.gameObject.SetActive(false);
        battleSystem.gameObject.SetActive(true);
        battleSystem.BattleStart(playerBattler, enemyBattler);
    }

    public void EndBattle(){
        player.gameObject.SetActive(true);
        battleSystem.gameObject.SetActive(false);
        endBattleUI.gameObject.SetActive(true);
    }
    public void DestroyObject(Collider2D hitCollider)
    {
        objectKey = SceneManager.GetActiveScene().name + "_" + hitCollider.gameObject.name;
        Destroy(hitCollider.gameObject);
        PlayerPrefs.SetInt(objectKey, 1); 
        PlayerPrefs.Save(); 
        Debug.Log($"Added {objectKey} to PlayerPrefs");
    }
}
