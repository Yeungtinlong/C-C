using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackConfigSO", menuName = "Entity Config/Attack Config")]
public class AttackConfigSO : ScriptableObject
{
    [SerializeField] private int _damageToHuman = default;
    [SerializeField] private int _damageToVehicle = default;
    [SerializeField] private int _damageToConstruction = default;
    public int DamageToHuman => _damageToHuman;
    public int DamageToVehicle => _damageToVehicle;
    public int DamageToConstruction => _damageToConstruction;
}
