using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyStats : BattleUnit
{
        [SerializeField] TextMeshProUGUI HPText;
        [SerializeField] TextMeshProUGUI AttackText;
        [SerializeField] TextMeshProUGUI DefenseText;
        public override void Setup(Battler battler){
        base.Setup(battler);

        HPText.text = $"HP:{battler.MaxHP}";
        AttackText.text = $"ATK:{battler.Attack}";
        DefenseText.text = $"DEF:{battler.Defense}";
        }
        public override void UpdateUI()
        {
            HPText.text = $"HP:{Battler.HP}";
            AttackText.text = $"ATK:{Battler.Attack}";
            DefenseText.text = $"DEF:{Battler.Defense}";
        }
}
