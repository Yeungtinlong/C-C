using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CNC.StateMachine.ScriptableObjects;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class StateMachineView : GraphView
{
    public new class UxmlFactory : UxmlFactory<StateMachineView, UxmlTraits> { }

    private TransitionTableSO _table;

    public event Action<StateView> OnStateSelected;
    public event Action<ConditionEdgeView> OnEdgeSelected;

    public StateMachineView()
    {
        Insert(0, new GridBackground());
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        var styleSheet =
            AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/StateMachine/Editor/StateMachineEditor.uss");
        styleSheets.Add(styleSheet);
    }

    public void PopulateTable(TransitionTableSO table)
    {
        _table = table;

        graphViewChanged -= OnGraphChange;
        DeleteElements(graphElements);
        graphViewChanged += OnGraphChange;

        List<TransitionItem> transitionItems = _table.Transitions;
        Dictionary<StateSO, List<StateSO>> transitions = new Dictionary<StateSO, List<StateSO>>();
        HashSet<StateSO> states = new HashSet<StateSO>();

        transitionItems.ForEach(item =>
        {
            if (item.FromState != null)
            {
                if (!transitions.ContainsKey(item.FromState))
                    transitions.Add(item.FromState, new List<StateSO>());

                states.Add(item.FromState);

                if (item.ToState != null)
                {
                    transitions[item.FromState].Add(item.ToState);
                    states.Add(item.ToState);
                }
            }
        });

        table.States.ForEach(CreateStateView);
        transitionItems.ForEach(CreateEdgeView);

        // table.States.ForEach(fromState =>
        // {
        //     if (transitions.ContainsKey(fromState))
        //     {
        //         StateView fromStateView = FindStateViewByState(fromState);
        //         transitions[fromState].ForEach(toState =>
        //         {
        //             StateView toStateView = FindStateViewByState(toState);
        //             ConditionEdgeView edgeView = CreateEdgeView(fromStateView, toStateView,
        //                 transitionItems.Find(item => item.FromState == fromState && item.ToState == toState));
        //             AddElement(edgeView);
        //         });
        //     }
        // });

        // foreach (var state in states)
        //     CreateStateView(state);
        //
        // foreach (var transitionPair in transitions)
        // {
        //     StateView from = FindStateViewByState(transitionPair.Key);
        //     transitionPair.Value.ForEach(toState =>
        //     {
        //         StateView to = FindStateViewByState(toState);
        //         Edge edge = from.OutputPort.ConnectTo(to.InputPort);
        //         AddElement(edge);
        //     });
        // }
    }

    private void CreateEdgeView(TransitionItem transitionItem)
    {
        StateView fromStateView = FindStateViewByState(transitionItem.FromState);
        StateView toStateView = FindStateViewByState(transitionItem.ToState);
        ConditionEdgeView edge = fromStateView.OutputPort.ConnectTo<ConditionEdgeView>(toStateView.InputPort);
        edge.SetTransitionItem(transitionItem);
        edge.OnEdgeSelected += OnEdgeSelected;
        AddElement(edge);
    }

    private GraphViewChange OnGraphChange(GraphViewChange graphViewChange)
    {
        if (graphViewChange.edgesToCreate != null)
        {
            graphViewChange.edgesToCreate.ForEach(edge =>
            {
                StateView from = edge.output.node as StateView;
                StateView to = edge.input.node as StateView;
                TransitionItem transitionItem = _table.CreateTransition(from.State, to.State);

                if (edge is ConditionEdgeView conditionEdgeView)
                {
                    conditionEdgeView.SetTransitionItem(transitionItem);
                    conditionEdgeView.OnEdgeSelected += OnEdgeSelected;
                }
            });
        }

        if (graphViewChange.elementsToRemove != null)
        {
            graphViewChange.elementsToRemove.ForEach(element =>
            {
                if (element is StateView stateView)
                {
                    _table.RemoveState(stateView.State);
                }

                if (element is ConditionEdgeView edge)
                {
                    _table.RemoveTransition(edge.TransitionItem);
                }
            });
        }
        return graphViewChange;
    }

    public void CreateStateView(StateSO state)
    {
        StateView stateView = new StateView(state);
        stateView.OnStateSelected += OnStateSelected;
        AddElement(stateView);
    }

    private StateView FindStateViewByState(StateSO state)
    {
        return GetNodeByGuid(state.guid) as StateView;
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(endPoint =>
            endPoint.direction != startPort.direction &&
            endPoint.node != startPort.node).ToList();
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        Vector2 mousePos = evt.localMousePosition;
        evt.menu.AppendAction($"[State] State", a => CreateState(typeof(StateSO), mousePos));

        TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<StateActionSO>();
        foreach (var type in types)
        {
            evt.menu.AppendAction($"[{type.BaseType.Name}] {type.Name}", a => { });
        }
    }

    public void CreateState(Type type, Vector2 position)
    {
        StateSO state = _table.CreateState(type);
        state.position = position;
        CreateStateView(state);
    }
}