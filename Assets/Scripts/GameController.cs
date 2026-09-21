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

    /// <summary>戦闘中の敵オブジェクト。勝ったときだけ盤面から消す。</summary>
    private GameObject pendingEnemyObject;

    /// <summary>生命力が0になったときに発火する。原典のメッセージは
    /// 「生命力がなくなりました。再挑戦してください。」</summary>
    public event System.Action OnGameOver;

    private void Awake()
    {
        // 起動毎に PlayerPrefs.DeleteAll() していたためセーブが毎回消えていた。除去。
        // 進行状況をリセットしたい場合は明示的に ResetProgress() を呼ぶ。
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
    
    public void StartBattle(Battler enemyBattler, GameObject enemyObject){
        Debug.Log("Started battle");
        pendingEnemyObject = enemyObject;
        enemyBattler.Init();
  
        player.gameObject.SetActive(false);
        battleSystem.gameObject.SetActive(true);
        battleSystem.BattleStart(playerBattler, enemyBattler);
    }

    public void EndBattle(){
        bool won = battleSystem.PlayerWon;
        if (won && pendingEnemyObject != null)
        {
            // 原典では敵を倒すとそのマスが床になる。門の解錠判定はそれを見ている
            var rules = GameRules.Active;
            if (rules != null && rules.Floor != null)
                rules.OnEnemyDefeated(GameRules.ToCell(pendingEnemyObject.transform.position));

            DestroyObject(pendingEnemyObject);   // 勝ったときだけ盤面から消す
        }
        pendingEnemyObject = null;

        player.gameObject.SetActive(true);
        battleSystem.gameObject.SetActive(false);
        endBattleUI.gameObject.SetActive(true);

        if (!won)
        {
            Debug.Log("生命力がなくなりました。再挑戦してください。");
            OnGameOver?.Invoke();
        }
    }
    /// <summary>取得済み・撃破済みの記録を全消去する（デバッグ用）。</summary>
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("進行状況をリセットしました");
    }

    public void DestroyObject(GameObject gameObject)
    {
        objectKey = SceneManager.GetActiveScene().name + "_" + gameObject.name;
        Destroy(gameObject);
        PlayerPrefs.SetInt(objectKey, 1); 
        PlayerPrefs.Save(); 
        Debug.Log($"Added {objectKey} to PlayerPrefs with value {PlayerPrefs.GetInt(objectKey)}");
    }
}
