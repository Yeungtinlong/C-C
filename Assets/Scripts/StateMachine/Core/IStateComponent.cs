using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CNC.StateMachine
{
    interface IStateComponent
    {
        void OnStateEnter();
        void OnStateExit();
    }
}

