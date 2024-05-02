using System.Collections;
using System.Collections.Generic;
// using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyUnit : BattleUnit
{
    
    [SerializeField]　Image image;
    [SerializeField] TextMeshProUGUI nameText;
    public override void Setup(Battler battler){
    if (battler.Base.Sprite == null) {
        Debug.LogError("Battler.Base.Sprite is null");
        return;
    }

        base.Setup(battler);
        image.sprite = battler.Base.Sprite;
        nameText.text = battler.Base.Name;
    }
}
