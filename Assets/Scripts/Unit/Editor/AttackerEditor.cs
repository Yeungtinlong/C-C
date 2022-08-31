using CNC.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;


[CustomEditor(typeof(Attacker))]
public class AttackerEditor : UnityEditor.Editor
{
    private ReorderableList _list;
    private SerializedProperty _weaponItems;

    private void OnEnable()
    {
        _weaponItems = serializedObject.FindProperty("_weaponItems");
        _list = new ReorderableList(serializedObject, _weaponItems, true, true, true, true);
        SetupWeaponItemList(_list);
    }

    public override void OnInspectorGUI()
    {
        _list.DoLayoutList();
        //serializedObject.Update();
        serializedObject.ApplyModifiedProperties();
    }

    private static void SetupWeaponItemList(ReorderableList reorderablelist)
    {
        reorderablelist.elementHeight *= 2.5f;
        reorderablelist.drawHeaderCallback = (Rect rect) => GUI.Label(rect, "Weapons");
        reorderablelist.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            Rect r = rect;
            SerializedProperty weaponItem = reorderablelist.serializedProperty.GetArrayElementAtIndex(index);
            r.y += 5f;
            r.height = EditorGUIUtility.singleLineHeight;

            SerializedProperty weaponSO = weaponItem.FindPropertyRelative("WeaponSO");
            SerializedProperty weaponAnchor = weaponItem.FindPropertyRelative("WeaponAnchor");

            if (weaponSO.objectReferenceValue != null)
            {
                r.width = 50f;
                EditorGUI.PropertyField(r, weaponSO, GUIContent.none);
                r.width = rect.width - 50f;
                r.x += 55f;
                GUI.Label(r, weaponSO.objectReferenceValue.name, EditorStyles.boldLabel);
            }
            else
            {
                EditorGUI.PropertyField(r, weaponSO, GUIContent.none);
            }

            r.x = rect.x;
            r.y += EditorGUIUtility.singleLineHeight + 5f;

            if (weaponAnchor.objectReferenceValue != null)
            {
                r.width = 50f;
                EditorGUI.PropertyField(r, weaponAnchor, GUIContent.none);
                r.width = rect.width - r.x;
                r.x += 55f;
                GUI.Label(r, weaponAnchor.objectReferenceValue.name, EditorStyles.boldLabel);
            }
            else
            {
                EditorGUI.PropertyField(r, weaponAnchor, GUIContent.none);
            }
        };
        reorderablelist.onAddCallback = (list) =>
        {
            int index = list.count;
            list.serializedProperty.InsertArrayElementAtIndex(index);
            SerializedProperty prop = list.serializedProperty.GetArrayElementAtIndex(index);

            SerializedProperty weaponSO = prop.FindPropertyRelative("WeaponSO");
            weaponSO.objectReferenceValue = null;
            SerializedProperty weaponAnchor = prop.FindPropertyRelative("WeaponAnchor");
            weaponAnchor.objectReferenceValue = null;

        };
        //reorderablelist.onChangedCallback = list => list.serializedProperty.serializedObject.ApplyModifiedProperties();
    }
}


