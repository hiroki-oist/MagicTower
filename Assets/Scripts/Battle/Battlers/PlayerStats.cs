using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerStats : BattleUnit
{
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI HPText;
        [SerializeField] TextMeshProUGUI HPTextHeader;
        [SerializeField] TextMeshProUGUI AttackText;
        [SerializeField] TextMeshProUGUI AttackTextHeader;
        
        [SerializeField] TextMeshProUGUI DefenseText;
        [SerializeField] TextMeshProUGUI DefenseTextHeader;
        [SerializeField] TextMeshProUGUI CoinText;
        public override void Setup(Battler battler){
            base.Setup(battler);

            nameText.text = battler.Base.Name;
            HPText.text = $"HP:{battler.MaxHP}";
            AttackText.text = $"ATK:{battler.Attack}";
            DefenseText.text = $"DEF:{battler.Defense}";
        }

    // public override void UpdateUI()
    // {
    //     HPText.text = $"HP:{Battler.HP}";
    //     HPTextHeader.text = $"{Battler.HP}";
    //     AttackTextHeader.text = $"{Battler.Attack}";
    //     DefenseText.text = $"DEF:{Battler.Defense}";
    //     DefenseTextHeader.text = $"{Battler.Defense}";
    // }
    public override void UpdateUI()
    {
    if (HPText != null && Battler != null) {
        HPText.text = $"HP:{Battler.HP}";
    } else {
        Debug.LogError("HPText or Battler is null!");
    }
    if (HPTextHeader != null) {
        HPTextHeader.text = $"{Battler.HP}";
    }
    if (AttackTextHeader != null) {
        AttackTextHeader.text = $"{Battler.Attack}";
    }
    if (DefenseText != null) {
        DefenseText.text = $"DEF:{Battler.Defense}";
    }
    if (DefenseTextHeader != null) {
        DefenseTextHeader.text = $"{Battler.Defense}";
    }
}

    public override void UpdateCoinUI(){
        CoinText.text = $"{Battler.Coin}";
    }
}
