using System;
using HutongGames.PlayMaker;
using UnityEngine;

namespace FSM.Scripts
{
    public class HasGotEnemyCondition : MonoBehaviour
    {
        [SerializeField] private FsmEvent _fsmEvent;
        
        private void Start()
        {
            PlayMakerFSM playMakerFsm = GetComponent<PlayMakerFSM>();
            playMakerFsm.Fsm.Event(_fsmEvent);
        }
    }
}