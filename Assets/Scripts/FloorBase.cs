using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorBase : MonoBehaviour
{
    // Start is called before the first frame update
    public static FloorBase Instance { get; private set; }

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else if (Instance != this) {
            Destroy(gameObject);
        }
    }
}