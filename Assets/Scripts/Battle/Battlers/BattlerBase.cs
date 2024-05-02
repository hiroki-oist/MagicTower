using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu]
public class BattlerBase : ScriptableObject
{
    // Battler の基礎データ
    [SerializeField] new string name;
    [SerializeField] int maxHP;
    [SerializeField] int attack;
    [SerializeField] int defense;
    [SerializeField] int coin;
    [SerializeField] Sprite sprite;

    public string Name { get => name;}
    public int MaxHP { get => maxHP;}
    public int Attack {get => attack;}
    public int Coin {get => coin;}
    public int Defense {get => defense;}
    public Sprite Sprite {get => sprite;}
}
