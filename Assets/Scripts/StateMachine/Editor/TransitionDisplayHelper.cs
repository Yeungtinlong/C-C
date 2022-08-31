using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CNC.StateMachine.Editor
{
    internal class TransitionDisplayHelper
    {
        // 封装 TransitionItem
        internal SerializedTransition SerializedTransition { get; }
        private readonly ReorderableList _reorderableList;
        private readonly TransitionTableEditor _editor;

        internal TransitionDisplayHelper(SerializedTransition serializedTransition, TransitionTableEditor editor)
        {
            SerializedTransition = serializedTransition;
            _reorderableList = new ReorderableList(SerializedTransition.Transition.serializedObject, SerializedTransition.Conditions, true, false, true, true);
            SetupConditionList(_reorderableList);
            _editor = editor;
        }

        internal bool Display(ref Rect position)
        {
            Rect rect = position;
            float listHeight = _reorderableList.GetHeight();
            float singleLineHeight = EditorGUIUtility.singleLineHeight;

            // Reserve space
            {
                rect.height = singleLineHeight + 10f + listHeight;
                GUILayoutUtility.GetRect(rect.width, rect.height);
                position.y += rect.height + 5;
            }

            // Background
            {
                rect.x += 5f;
                rect.width -= 10f;
                rect.height -= listHeight;
                EditorGUI.DrawRect(rect, ContentStyle.DarkGray);
            }

            // Transition header
            {
                rect.x += 3f;
                EditorGUI.LabelField(rect, "To");

                rect.x += 20f;
                EditorGUI.LabelField(rect, SerializedTransition.ToState.objectReferenceValue.name, EditorStyles.boldLabel);
            }

            // Buttons
            {
                bool Button(Rect position, string icon) => GUI.Button(position, EditorGUIUtility.IconContent(icon));

                Rect buttonRect = new Rect(x: rect.width - 25f, y: rect.y + 5f, width: 30f, height: 18f);

                int i, l;
                {
                    // 获得该 Transition 所在的 State 的 Transition 数量
                    List<SerializedTransition> transitions = _editor.GetStateTransitions(SerializedTransition.FromState.objectReferenceValue);
                    l = transitions.Count - 1;
                    i = transitions.FindIndex(x => x.Index == SerializedTransition.Index);
                }

                // Remove transition
                if (Button(buttonRect, "Toolbar Minus"))
                {
                    _editor.RemoveTransition(SerializedTransition);
                    return true;
                }
                buttonRect.x -= 35f;

                // Move transition down
                if (i < l)
                {
                    if (Button(buttonRect, "scrolldown"))
                    {
                        _editor.ReorderTransition(SerializedTransition, false);
                        return true;
                    }
                    buttonRect.x -= 35f;
                }

                // Move transition up
                if (i > 0)
                {
                    if (Button(buttonRect, "scrollup"))
                    {
                        _editor.ReorderTransition(SerializedTransition, true);
                        return true;
                    }
                    buttonRect.x -= 35f;
                }

                // State editor
                if (Button(buttonRect, "SceneViewTools"))
                {
                    _editor.DisplayStateEditor(SerializedTransition.ToState.objectReferenceValue);
                    return true;
                }
            }

            rect.x = position.x + 5f;
            rect.y += rect.height;
            rect.width = position.width - 10f;
            rect.height = listHeight;

            _reorderableList.DoList(rect);

            return false;
        }

        private static void SetupConditionList(ReorderableList reorderableList)
        {
            reorderableList.elementHeight *= 2.3f;
            reorderableList.headerHeight = 1f;

            reorderableList.onAddCallback = (list) =>
            {
                int count = list.count;
                list.serializedProperty.InsertArrayElementAtIndex(count);
                SerializedProperty prop = list.serializedProperty.GetArrayElementAtIndex(count);
                prop.FindPropertyRelative("StateCondition").objectReferenceValue = null;
                prop.FindPropertyRelative("ExpectedResult").enumValueIndex = 0;
                prop.FindPropertyRelative("Operator").enumValueIndex = 0;
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
                    Rect r = rect;
                    r.x += 20f;
                    r.width = 35f;
                    EditorGUI.PropertyField(r, condition, GUIContent.none);
                    r.x += 40f;
                    r.width = rect.width - 120;
                    GUI.Label(r, label, EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, 150f, rect.height), condition, GUIContent.none);
                }

                EditorGUI.LabelField(new Rect(rect.x + rect.width - 80f, rect.y, 20f, rect.height), "Is");
                EditorGUI.PropertyField(new Rect(rect.x + rect.width - 60f, rect.y, 60f, rect.height), prop.FindPropertyRelative("ExpectedResult"), GUIContent.none);

                // 显示 Operator 选择栏
                if (index < reorderableList.count - 1)
                    EditorGUI.PropertyField(new Rect(rect.x + 20f, rect.y + EditorGUIUtility.singleLineHeight + 5f, 60f, rect.height), prop.FindPropertyRelative("Operator"), GUIContent.none);
            };

            reorderableList.onChangedCallback += (list) => list.serializedProperty.serializedObject.ApplyModifiedProperties();

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
    }
}

