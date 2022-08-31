using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CNC.StateMachine.ScriptableObjects;
using Object = UnityEngine.Object;

namespace CNC.StateMachine.Editor
{
    [CustomEditor(typeof(TransitionTableSO))]
    internal class TransitionTableEditor : UnityEditor.Editor
    {
        // TransitionItem的序列化版本
        private SerializedProperty _transitions;

        private List<Object> _fromStates;
        // Helper是Transition的封装，内层List是所有相同FromState的Transition，外层List是所有State
        private List<List<TransitionDisplayHelper>> _transitionsByFromStates;

        internal int _toggledIndex = -1;

        private AddTransitionHelper _addTransitionHelper;

        private UnityEditor.Editor _cachedStateEditor;
        private bool _isDisplayingStateEditor;

        private void OnEnable()
        {
            _addTransitionHelper = new AddTransitionHelper(this);
            Undo.undoRedoPerformed += Reset;
            Reset();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Reset;
            _addTransitionHelper?.Dispose();
        }

        /// <summary>
        /// 刷新整个 Editor ，在添加、移除、重排序时调用
        /// </summary>
        internal void Reset()
        {
            serializedObject.Update();
            Object toggledState = _toggledIndex > -1 ? _fromStates[_toggledIndex] : null;
            // 获得所有 TransitionItem
            _transitions = serializedObject.FindProperty("_transitions");
            GroupByFromState();
            // 如果存在打开的 State ，就返回他的 index ，否则设为-1（不存在打开的State）
            _toggledIndex = toggledState ? _fromStates.IndexOf(toggledState) : -1;
        }

        public override void OnInspectorGUI()
        {
            if (!_isDisplayingStateEditor)
                TransitionTableGUI();
            else
                StateEditorGUI();
        }

        private void StateEditorGUI()
        {
            EditorGUILayout.Separator();

            if (GUILayout.Button(EditorGUIUtility.IconContent("scrollleft"), GUILayout.Width(35f), GUILayout.Height(20f))
                || _cachedStateEditor.serializedObject == null)
            {
                _isDisplayingStateEditor = false;
                return;
            }

            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox("编辑该 State 的 Actions，排序决定 Action 的执行顺序。", MessageType.Info);
            EditorGUILayout.Separator();

            // State name
            EditorGUILayout.LabelField(_cachedStateEditor.target.name, EditorStyles.boldLabel);
            EditorGUILayout.Separator();

            _cachedStateEditor.OnInspectorGUI();
        }

        private void TransitionTableGUI()
        {
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox("点击 State 查看 Transitions ，点击 icon 查看 Actions 。", MessageType.Info);
            EditorGUILayout.Separator();

            for (int i = 0; i < _fromStates.Count; i++)
            {
                Rect stateRect = EditorGUILayout.BeginVertical(ContentStyle.WithPaddingAndMargins);
                EditorGUI.DrawRect(stateRect, ContentStyle.LightGray);

                // 缓存当前State的所有transition（这些transition的fromState相同）
                List<TransitionDisplayHelper> transitions = _transitionsByFromStates[i];

                // State的标签
                Rect headerRect = EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    string label = transitions[0].SerializedTransition.FromState.objectReferenceValue.name;
                    if (i == 0)
                        label += " (Initial State)";

                    headerRect.height = EditorGUIUtility.singleLineHeight;
                    GUILayoutUtility.GetRect(headerRect.width, headerRect.height);
                    headerRect.x += 5;

                    // Toggle
                    {
                        Rect toggleRect = headerRect;
                        toggleRect.width -= 140f;
                        _toggledIndex =
                            EditorGUI.BeginFoldoutHeaderGroup(toggleRect, _toggledIndex == i, label, ContentStyle.StateListStyle) ?
                            i : _toggledIndex == i ? -1 : _toggledIndex;
                    }

                    EditorGUILayout.Separator();
                    EditorGUILayout.EndVertical();

                    // State Header Buttons
                    {
                        bool Button(Rect position, string icon) => GUI.Button(position, EditorGUIUtility.IconContent(icon));

                        Rect buttonRect = new Rect(x: headerRect.width - 25, y: headerRect.y, width: 35, height: 20);

                        // Move state down
                        if (i < _fromStates.Count - 1)
                        {
                            if (Button(buttonRect, "scrolldown"))
                            {
                                ReorderState(i, false);
                                EarlyOut();
                                return;
                            }
                            buttonRect.x -= 40;
                        }

                        // Move state up
                        if (i > 0)
                        {
                            if (Button(buttonRect, "scrollup"))
                            {
                                ReorderState(i, true);
                                EarlyOut();
                                return;
                            }
                            buttonRect.x -= 40;
                        }

                        // Switch to state editor
                        if (Button(buttonRect, "SceneViewTools"))
                        {
                            DisplayStateEditor(transitions[0].SerializedTransition.FromState.objectReferenceValue);
                            EarlyOut();
                            return;
                        }

                        void EarlyOut()
                        {
                            EditorGUILayout.EndFoldoutHeaderGroup();
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (_toggledIndex == i)
                {
                    EditorGUI.BeginChangeCheck();
                    stateRect.y += EditorGUIUtility.singleLineHeight * 2;

                    foreach (var transition in transitions)
                    {
                        if (transition.Display(ref stateRect))
                        {
                            EditorGUI.EndChangeCheck();
                            EditorGUILayout.EndFoldoutHeaderGroup();
                            EditorGUILayout.EndVertical();
                            //EditorGUILayout.EndHorizontal();
                            return;
                        }
                        EditorGUILayout.Separator();
                    }
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Separator();
            }

            Rect rect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(rect.width - 55);

            _addTransitionHelper.Display(rect);

            EditorGUILayout.EndHorizontal();
        }

        internal void DisplayStateEditor(Object state)
        {
            if (_cachedStateEditor == null)
                _cachedStateEditor = CreateEditor(state, typeof(StateEditor));
            else
                CreateCachedEditor(state, typeof(StateEditor), ref _cachedStateEditor);

            _isDisplayingStateEditor = true;
        }

        private void GroupByFromState()
        {
            var groupedTransitions = new Dictionary<Object, List<TransitionDisplayHelper>>();
            int count = _transitions.arraySize;
            for (int i = 0; i < count; i++)
            {
                var serializedTransition = new SerializedTransition(_transitions, i);
                if (serializedTransition.FromState.objectReferenceValue == null)
                {
                    Debug.LogWarning("该Transition的\"From State\"值不可用" + serializedObject.targetObject.name);
                    _transitions.DeleteArrayElementAtIndex(i);
                    ApplyModifications("Invalid transition deleted");
                    return;
                }
                if (serializedTransition.ToState.objectReferenceValue == null)
                {
                    Debug.LogWarning("该Transition的\"To State\"值不可用" + serializedObject.targetObject.name);
                    _transitions.DeleteArrayElementAtIndex(i);
                    ApplyModifications("Invalid transition deleted");
                    return;
                }

                if (!groupedTransitions.TryGetValue(serializedTransition.FromState.objectReferenceValue, out List<TransitionDisplayHelper> groupedProp))
                {
                    groupedProp = new List<TransitionDisplayHelper>();
                    groupedTransitions.Add(serializedTransition.FromState.objectReferenceValue, groupedProp);
                }
                groupedProp.Add(new TransitionDisplayHelper(serializedTransition, this));
            }

            _fromStates = groupedTransitions.Keys.ToList();
            _transitionsByFromStates = new List<List<TransitionDisplayHelper>>();
            foreach (var fromState in _fromStates)
                _transitionsByFromStates.Add(groupedTransitions[fromState]);
        }

        private void ApplyModifications(string msg)
        {
            Undo.RecordObject(serializedObject.targetObject, msg);
            serializedObject.ApplyModifiedProperties();
            Reset();
        }

        internal void ReorderState(int index, bool isUp)
        {
            Object toggledState = _toggledIndex > -1 ? _fromStates[_toggledIndex] : null;

            if (!isUp)
                index++;

            List<TransitionDisplayHelper> transitions = _transitionsByFromStates[index];
            // 首个以该 state 为 fromState 的 transition 的索引
            int transitionIndex = transitions[0].SerializedTransition.Index;

            List<TransitionDisplayHelper> targetTransitions = _transitionsByFromStates[index - 1];
            int targetIndex = targetTransitions[0].SerializedTransition.Index;

            _transitions.MoveArrayElement(transitionIndex, targetIndex);

            ApplyModifications($"Moved {_fromStates[index].name} State {(isUp ? "up" : "down")}");

            if (toggledState != null)
                _toggledIndex = _fromStates.IndexOf(toggledState);
        }

        internal void ReorderTransition(SerializedTransition serializedTransition, bool isUp)
        {
            int stateIndex = _fromStates.IndexOf(serializedTransition.FromState.objectReferenceValue);
            List<TransitionDisplayHelper> stateTransitions = _transitionsByFromStates[stateIndex];
            int transitionIndex = stateTransitions.FindIndex(x => x.SerializedTransition.Index == serializedTransition.Index);

            (int currentIndex, int targetIndex) = isUp ?
                (serializedTransition.Index, stateTransitions[transitionIndex - 1].SerializedTransition.Index)
                : (stateTransitions[transitionIndex + 1].SerializedTransition.Index, serializedTransition.Index);

            _transitions.MoveArrayElement(currentIndex, targetIndex);

            ApplyModifications($"Moved transition to {serializedTransition.ToState}");

            _toggledIndex = stateIndex;
        }

        internal List<SerializedTransition> GetStateTransitions(Object state)
        {
            return _transitionsByFromStates[_fromStates.IndexOf(state)].Select(x => x.SerializedTransition).ToList();
        }

        internal void AddTransition(SerializedTransition source)
        {
            SerializedTransition transition;
            if (TryGetExistingTransition(source.FromState, source.ToState, out int fromIndex, out int toIndex))
            {
                transition = _transitionsByFromStates[fromIndex][toIndex].SerializedTransition;
            }
            else
            {
                int count = _transitions.arraySize;
                _transitions.InsertArrayElementAtIndex(count);
                transition = new SerializedTransition(_transitions.GetArrayElementAtIndex(count));
                transition.ClearProperties();
                transition.FromState.objectReferenceValue = source.FromState.objectReferenceValue;
                transition.ToState.objectReferenceValue = source.ToState.objectReferenceValue;
            }

            CopyConditions(transition.Conditions, source.Conditions);

            ApplyModifications($"Added Transition from {transition.FromState} to {transition.ToState}");

            // 如果存在相同 FromState 的 Transition，就跳转到该 Transition，否则就跳转到新建的 Transiiton
            _toggledIndex = fromIndex > -1 ? fromIndex : _fromStates.Count - 1;
        }

        internal void RemoveTransition(SerializedTransition serializedTransition)
        {
            int stateIndex = _fromStates.IndexOf(serializedTransition.FromState.objectReferenceValue);
            List<TransitionDisplayHelper> stateTransiitons = _transitionsByFromStates[stateIndex];
            int count = stateTransiitons.Count;
            int transitionIndex = stateTransiitons.FindIndex(x => x.SerializedTransition.Index == serializedTransition.Index);
            int deleteIndex = serializedTransition.Index;
            string fromStateName = serializedTransition.FromState.objectReferenceValue.name;
            string toStateName = serializedTransition.ToState.objectReferenceValue.name;

            if (transitionIndex == 0 && count > 1)
                _transitions.MoveArrayElement(stateTransiitons[1].SerializedTransition.Index, deleteIndex++); // 先传值再自增

            _transitions.DeleteArrayElementAtIndex(deleteIndex);

            ApplyModifications($"Deleted transition from {fromStateName} " +
                $"to {toStateName}");

            // 确保 ApplyModifications 之后，_toggleIndex 是现在打开的 state 的 index
            if (count > 1)
                _toggledIndex = stateIndex;
        }

        /// <summary>
        /// 获取指定 FromState 的 Transiiton
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        /// <returns>如果指定 FromState 存在则返回 true，否则返回 false</returns>
        private bool TryGetExistingTransition(SerializedProperty from, SerializedProperty to, out int fromIndex, out int toIndex)
        {
            fromIndex = _fromStates.IndexOf(from.objectReferenceValue);
            toIndex = -1;
            if (fromIndex < 0)
                return false;

            toIndex = _transitionsByFromStates[fromIndex].FindIndex(
                transitionHelper => transitionHelper.SerializedTransition.ToState.objectReferenceValue == to.objectReferenceValue);

            return toIndex >= 0;
        }

        /// <summary>
        /// 将新添加的 Transition 的 Conditions 复制到已有的 Transition 中
        /// </summary>
        /// <param name="copyTo">已存在的 Transition</param>
        /// <param name="copyFrom">来源的 Transition</param>
        private void CopyConditions(SerializedProperty copyTo, SerializedProperty copyFrom)
        {
            for (int i = 0, j = copyTo.arraySize; i < copyFrom.arraySize; i++, j++)
            {
                copyTo.InsertArrayElementAtIndex(j);
                SerializedProperty cond = copyTo.GetArrayElementAtIndex(j);
                SerializedProperty srcCond = copyFrom.GetArrayElementAtIndex(i);
                cond.FindPropertyRelative("ExpectedResult").enumValueIndex = srcCond.FindPropertyRelative("ExpectedResult").enumValueIndex;
                cond.FindPropertyRelative("Operator").enumValueIndex = srcCond.FindPropertyRelative("Operator").enumValueIndex;
                cond.FindPropertyRelative("StateCondition").objectReferenceValue = srcCond.FindPropertyRelative("StateCondition").objectReferenceValue;
            }
        }
    }
}
