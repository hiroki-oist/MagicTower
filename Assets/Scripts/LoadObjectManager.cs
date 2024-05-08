using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LoadObjectManager : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Checked loadObject or not");
        // シーン内のすべてのEnemyオブジェクトを取得
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        string sceneName = SceneManager.GetActiveScene().name;

        foreach (GameObject eachObject in allObjects)
        {
            // オブジェクトの名前を基にキーを生成
            string objectKey = sceneName + "_" + eachObject.name;
            // Debug.Log("Checking ID: " + objectKey);
            // シーンロード時に状態をチェック
            if (PlayerPrefs.GetInt(objectKey, 0) == 1)
            {
                // Debug.Log($"Enemy ID {objectKey} is resistered as defeated. Deactivating object...");
                eachObject.SetActive(false); // 状態に基づいて非アクティブ化
            }
        }
    }

    // List<GameObject> FindObjectsStartingWithName(string nameStart)
    // {
    //     GameObject[] allObjects = FindObjectsOfType<GameObject>(); // シーン内のすべてのGameObjectを取得
    //     List<GameObject> filteredObjects = new List<GameObject>();

    //     foreach (GameObject obj in allObjects)
    //     {
    //         if (obj.name.StartsWith(nameStart)) // 名前が指定した文字列で始まるかどうかをチェック
    //         {
    //             filteredObjects.Add(obj);
    //         }
    //     }

    //     return filteredObjects;
    // }
}
