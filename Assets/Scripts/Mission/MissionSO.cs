using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Mission", menuName = "Mission/Mission")]
public class MissionSO : ScriptableObject
{
    [SerializeField] private LevelLimitSO _levelLimit = default;

    public void OnMissionLoaded()
    {
        _levelLimit.OnMissionLoaded();
    }
}
