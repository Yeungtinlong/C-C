using System;
using CNC.StateMachine.ScriptableObjects;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace CNC.StateMachine.Editor
{
    public class StateMachineEditor : EditorWindow
    {
        private StateMachineView _stateMachineView;
        private InspectorView _inspectorView;

        [MenuItem("StateMachineEditor/Open State Machine Window")]
        public static void OpenStateMachineEditor()
        {
            StateMachineEditor wnd = GetWindow<StateMachineEditor>();
            wnd.titleContent = new GUIContent("StateMachineEditor");
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID)
        {
            if (Selection.activeObject is TransitionTableSO)
            {
                OpenStateMachineEditor();
                return true;
            }

            return false;
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/StateMachine/Editor/StateMachineEditor.uxml");

            visualTree.CloneTree(root);

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/StateMachine/Editor/StateMachineEditor.uss");
            root.styleSheets.Add(styleSheet);

            _stateMachineView = root.Q<StateMachineView>();
            _inspectorView = root.Q<InspectorView>();
            _stateMachineView.OnStateSelected += OnStateSelectionChanged;
            _stateMachineView.OnEdgeSelected += OnTransitionSelectionChanged;

            OnSelectionChange();
        }

        private void OnStateSelectionChanged(StateView stateView)
        {
            _inspectorView?.UpdateStateSelection(stateView);
        }

        private void OnTransitionSelectionChanged(ConditionEdgeView edgeView)
        {
            _inspectorView?.UpdateTransitionSelection(edgeView);
        }

        private void OnSelectionChange()
        {
            TransitionTableSO table = Selection.activeObject as TransitionTableSO;
            if (table != null)
            {
                _stateMachineView?.PopulateTable(table);
            }
        }
    }
}