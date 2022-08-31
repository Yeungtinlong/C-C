using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CNC.StateMachine.Editor
{
    internal readonly struct SerializedTransition
    {
        internal readonly SerializedProperty Transition;
        internal readonly SerializedProperty FromState;
        internal readonly SerializedProperty ToState;
        internal readonly SerializedProperty Conditions;
        internal readonly int Index; // 指该 transition 在整个 table 中的索引

        internal SerializedTransition(SerializedProperty transition)
        {
            Transition = transition;
            FromState = Transition.FindPropertyRelative("FromState");
            ToState = Transition.FindPropertyRelative("ToState");
            Conditions = Transition.FindPropertyRelative("Conditions");
            Index = -1;
        }

        internal SerializedTransition(SerializedProperty transition, int index)
        {
            Transition = transition.GetArrayElementAtIndex(index);
            FromState = Transition.FindPropertyRelative("FromState");
            ToState = Transition.FindPropertyRelative("ToState");
            Conditions = Transition.FindPropertyRelative("Conditions");
            Index = index;
        }

        internal void ClearProperties()
        {
            FromState.objectReferenceValue = null;
            ToState.objectReferenceValue = null;
            Conditions.ClearArray();
        }
    }
}

