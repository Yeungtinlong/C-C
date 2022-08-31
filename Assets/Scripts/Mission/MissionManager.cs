using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    [SerializeField] private MissionSO _mission = default;

    private void Start()
    {
        _mission.OnMissionLoaded();
    }
}
