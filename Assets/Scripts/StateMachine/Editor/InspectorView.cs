using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class InspectorView : VisualElement
{
    public new class UxmlFactory : UxmlFactory<InspectorView, UxmlTraits> { }
    public InspectorView() { }

    private Editor _editor;

    public void UpdateStateSelection(StateView stateView)
    {
        Clear();
        Object.DestroyImmediate(_editor);
        _editor = Editor.CreateEditor(stateView.State);
        IMGUIContainer container = new IMGUIContainer(() =>
        {
            if (_editor.target != null)
            {
                _editor.OnInspectorGUI();
                if (GUI.changed)
                {
                    stateView.State.name = stateView.State.stateName;
                    EditorUtility.SetDirty(stateView.State);
                }
            }
        });
        Add(container);
    }

    public void UpdateTransitionSelection(ConditionEdgeView edgeView)
    {
        Clear();
        Object.DestroyImmediate(_editor);
        _editor = Editor.CreateEditor(edgeView.TransitionItem);
        IMGUIContainer container = new IMGUIContainer(() =>
        {
            if (_editor.target != null)
            {
                _editor.OnInspectorGUI();
                if (GUI.changed)
                {
                    edgeView.TransitionItem.name =
                        $"{edgeView.TransitionItem.FromState.name} -> {edgeView.TransitionItem.ToState.name}";
                    EditorUtility.SetDirty(edgeView.TransitionItem);
                }
            }
        });
        Add(container);
    }
}