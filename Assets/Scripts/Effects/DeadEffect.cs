using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadEffect : MonoBehaviour {
    private AudioSource audioSource;
    public AudioClip tankDeadAudio;
    public AudioClip manmmothDeadAudio;
    public AudioClip buildingDeadAudio;

    private void Awake() {
        
    }

    //public void PlayDeadAudio(UnitType unitType) {
    //    if(unitType == UnitType.MiddleTank || unitType == UnitType.RocketLauncher || 
    //        unitType == UnitType.LightTank || unitType == UnitType.ReconBike ||
    //        unitType == UnitType.FlameTank || unitType == UnitType.StealthTank ||
    //        unitType == UnitType.SSMLauncher || unitType == UnitType.NotFightUnit || 
    //        unitType == UnitType.Aircraft) {
    //        audioSource.clip = tankDeadAudio;
    //    } else if(unitType == UnitType.ManmmothTank || unitType == UnitType.Artillery) {
    //        audioSource.clip = manmmothDeadAudio;
    //    } else if(unitType == UnitType.Building || unitType == UnitType.Turret ||
    //        unitType == UnitType.ObeliskOfLight || unitType == UnitType.GuardTower ||
    //        unitType == UnitType.AdvancedGuardTower) {
    //        audioSource.clip = buildingDeadAudio;
    //    } else if(unitType == UnitType.Soldier) {

    //    }
    //    audioSource.Play();
    //    Destroy(gameObject, 3f);
    //}

    public void PlayDeadAudio(AudioClip clip) {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
