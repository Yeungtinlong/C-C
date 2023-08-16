using System;
using System.Collections;
using System.Collections.Generic;
using CNC.StateMachine.ScriptableObjects;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphs;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Edge = UnityEditor.Experimental.GraphView.Edge;

public class StateView : UnityEditor.Experimental.GraphView.Node
{
    private StateSO _state;
    public StateSO State => _state;

    private Port _inputPort;
    public Port InputPort => _inputPort;

    private Port _outputPort;
    public Port OutputPort => _outputPort;

    public event Action<StateView> OnStateSelected;

    public StateView(StateSO state) : base("Assets/Scripts/StateMachine/Editor/StateView.uxml")
    {
        _state = state;
        this.title = state.name;
        viewDataKey = _state.guid;
        style.left = state.position.x;
        style.top = state.position.y;

        // Label stateName = this.Q<Label>("state-name");
        // stateName.bindingPath = "stateName";
        // stateName.Bind(new SerializedObject(_state));

        TextField stateName = this.Q<TextField>("state-name-input");
        stateName.bindingPath = "stateName";
        stateName.Bind(new SerializedObject(_state));

        Editor actionsEditor = Editor.CreateEditor(_state);
        IMGUIContainer actionsContainer = new IMGUIContainer(() =>
        {
            actionsEditor.OnInspectorGUI();
        });

        Foldout foldout = this.Q<Foldout>("action-list");
        foldout.Add(actionsContainer);
        foldout.value = false;

        CreateInputPorts();
        CreateOutputPorts();
    }

    private void CreateInputPorts()
    {
        _inputPort = Port.Create<ConditionEdgeView>(Orientation.Vertical, Direction.Input, Port.Capacity.Multi, typeof(Port));
        _inputPort.portName = "";
        _inputPort.style.flexDirection = FlexDirection.Column;
        inputContainer.Add(_inputPort);
    }

    private void CreateOutputPorts()
    {
        _outputPort = Port.Create<ConditionEdgeView>(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(Port));
        _outputPort.portName = "";
        _outputPort.style.flexDirection = FlexDirection.ColumnReverse;
        outputContainer.Add(_outputPort);
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        _state.position = new Vector2(newPos.xMin, newPos.yMin);
        EditorUtility.SetDirty(_state);
    }

    public override void OnSelected()
    {
        base.OnSelected();
        OnStateSelected?.Invoke(this);
    }
}