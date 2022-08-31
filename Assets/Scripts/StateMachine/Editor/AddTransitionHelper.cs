using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using CNC.StateMachine.ScriptableObjects;
using System;

namespace CNC.StateMachine.Editor
{
    internal class AddTransitionHelper : IDisposable
    {
        internal SerializedTransition SerializedTransition { get; }
        private readonly SerializedObject _transition;
        private readonly ReorderableList _reorderableList;
        private readonly TransitionTableEditor _editor;
        private bool _toggle = false;

        internal AddTransitionHelper(TransitionTableEditor editor)
        {
            _editor = editor;
            _transition = new SerializedObject(ScriptableObject.CreateInstance<TransitionItemSO>());
            SerializedTransition = new SerializedTransition(_transition.FindProperty("Item"));
            _reorderableList = new ReorderableList(_transition, SerializedTransition.Conditions);
            SetupConditionList(_reorderableList);
        }

        internal void Display(Rect position)
        {
            position.x += 8f;
            position.width -= 16f;
            Rect rect = position;
            float listHeight = _reorderableList.GetHeight();
            float singleLineHeight = EditorGUIUtility.singleLineHeight;

            // 如果添加列表不是正在打开，就显示添加按钮
            if (!_toggle)
            {
                position.height = singleLineHeight;

                // Reserve space
                GUILayoutUtility.GetRect(position.width, position.height);

                if (GUI.Button(position, "Add Transition"))
                {
                    _toggle = true;
                    SerializedTransition.ClearProperties();
                }

                return;
            }

            // Background
            {
                position.height = listHeight + singleLineHeight * 4f;
                EditorGUI.DrawRect(position, ContentStyle.LightGray);
            }

            // Reserve space
            GUILayoutUtility.GetRect(position.width, position.height);

            // State Fields
            {
                position.y += 10f;
                position.x += 20f;
                StatePropField(position, "From", SerializedTransition.FromState);
                position.x = rect.width / 2f + 20f;
                StatePropField(position, "To", SerializedTransition.ToState);
            }

            // Conditions List
            {
                position.y += 30f;
                position.x = rect.x + 5f;
                position.height = listHeight;
                position.width -= 10f;
                _reorderableList.DoList(position);
            }

            // Add and cencel buttons
            {
                position.y += position.height + 5f;
                position.height = singleLineHeight;
                position.width = rect.width / 2f - 20f;
                if (GUI.Button(position, "Add Transition"))
                {
                    if (SerializedTransition.FromState.objectReferenceValue == null)
                        Debug.LogException(new ArgumentNullException("FromState"));
                    else if (SerializedTransition.ToState.objectReferenceValue == null)
                        Debug.LogException(new ArgumentNullException("ToState"));
                    else if (SerializedTransition.FromState.objectReferenceValue == null)
                        Debug.LogException(new InvalidOperationException("FromState and ToState are the same."));
                    else
                    {
                        _editor.AddTransition(SerializedTransition);
                        _toggle = false;
                    }
                }
                position.x += rect.width / 2f;
                if (GUI.Button(position, "Cancel"))
                {
                    _toggle = false;
                }
            }

            void StatePropField(Rect pos, string label, SerializedProperty prop)
            {
                pos.height = singleLineHeight;
                EditorGUI.LabelField(pos, label);
                pos.x += 40f;
                pos.width /= 4f;
                EditorGUI.PropertyField(pos, prop, GUIContent.none);
            }
        }

        private static void SetupConditionList(ReorderableList reorderableList)
        {
            reorderableList.elementHeight *= 2.3f;
            reorderableList.drawHeaderCallback += (rect) => GUI.Label(rect, "Conditions");

            reorderableList.onAddCallback += (list) =>
            {
                int count = list.count;
                list.serializedProperty.InsertArrayElementAtIndex(count);
                SerializedProperty prop = list.serializedProperty.GetArrayElementAtIndex(count);
                prop.FindPropertyRelative("ExpectedResult").enumValueIndex = 0;
                prop.FindPropertyRelative("Operator").enumValueIndex = 0;
                prop.FindPropertyRelative("StateCondition").objectReferenceValue = null;
            };

            reorderableList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty prop = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
                rect = new Rect(rect.x, rect.y + 2.5f, rect.width, EditorGUIUtility.singleLineHeight);
                SerializedProperty condition = prop.FindPropertyRelative("StateCondition");
                if (condition.objectReferenceValue != null)
                {
                    string label = condition.objectReferenceValue.name;
                    GUI.Label(rect, "If");
                    GUI.Label(new Rect(rect.x + 20f, rect.y, rect.width, rect.height), label, EditorStyles.boldLabel);
                    EditorGUI.PropertyField(new Rect(rect.x + rect.width - 180f, rect.y, 20f, rect.height), condition, GUIContent.none);
                }
                else
                {
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, 150f, rect.height), condition, GUIContent.none);
                }
                EditorGUI.LabelField(new Rect(rect.x + rect.width - 120f, rect.y, 20f, rect.height), "Is");
                EditorGUI.PropertyField(new Rect(rect.x + rect.width - 60f, rect.y, 60f, rect.height), prop.FindPropertyRelative("ExpectedResult"), GUIContent.none);
                EditorGUI.PropertyField(new Rect(rect.x + 20f, rect.y + EditorGUIUtility.singleLineHeight + 5f, 60f, rect.height), prop.FindPropertyRelative("Operator"), GUIContent.none);
            };

            reorderableList.onChangedCallback += list => reorderableList.serializedProperty.serializedObject.ApplyModifiedProperties();
            reorderableList.drawElementBackgroundCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (isFocused)
                    EditorGUI.DrawRect(rect, ContentStyle.Focused);

                if (index % 2 == 1)
                    EditorGUI.DrawRect(rect, ContentStyle.ZebraDark);
                else
                    EditorGUI.DrawRect(rect, ContentStyle.ZebraLight);
            };
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(_transition.targetObject);
            _transition.Dispose();
            GC.SuppressFinalize(this);
        }

        internal class TransitionItemSO : ScriptableObject
        {
            public TransitionTableSO.TransitionItem Item = default;
        }
    }
}

