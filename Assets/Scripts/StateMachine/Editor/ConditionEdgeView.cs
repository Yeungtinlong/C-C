using System;
using System.Collections;
using System.Collections.Generic;
using CNC.StateMachine.ScriptableObjects;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ConditionEdgeView : Edge
{
    private TransitionItem _transitionItem;
    public TransitionItem TransitionItem => _transitionItem;

    public event Action<ConditionEdgeView> OnEdgeSelected;

    public ConditionEdgeView() { }

    public void SetTransitionItem(TransitionItem transitionItem)
    {
        _transitionItem = transitionItem;
        this.viewDataKey = transitionItem.guid;
    }

    public override void OnSelected()
    {
        base.OnSelected();
        OnEdgeSelected?.Invoke(this);
    }
}