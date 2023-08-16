using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class MissionManager : MonoBehaviour
{
    [SerializeField] private MissionSO _mission = default;
    [SerializeField] private PlayableDirector _director;

    private void Start()
    {
        _mission.OnMissionLoaded();
        _director.Play();
    }
}
