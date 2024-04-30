using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BattleSystems : MonoBehaviour
{
    enum State{
        Start,
        RunTurns,
        BattleOver
    }

    State state;

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
                break;
            case State.RunTurns:
                break;
            case State.BattleOver:
                break;
        }
    }

    void HandleRunTurns(){

    }

    void HandleBattleOver(){
        
    }
}
