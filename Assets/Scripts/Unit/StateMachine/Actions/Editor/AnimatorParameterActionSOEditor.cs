using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimatorParameterActionSO)), CanEditMultipleObjects]
public class AnimatorParameterActionSOEditor : CustomBaseEditor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawNonEditiableScriptReference<AnimatorParameterActionSO>();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("_specificMoment"), new GUIContent("When To Run"));
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Animator Parameter", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_parameterName"), new GUIContent("Name"));

        SerializedProperty parameterType = serializedObject.FindProperty("_parameterType");
        EditorGUILayout.PropertyField(parameterType, new GUIContent("Type"));

        switch (parameterType.intValue)
        {
            case (int)ParameterType.Bool:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_boolValue"), new GUIContent("Desired value"));
                break;
            case (int)ParameterType.Int:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_intValue"), new GUIContent("Desired value"));
                break;
            case (int)ParameterType.Float:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_floatValue"), new GUIContent("Desired value"));
                break;
        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("_isUseOtherAnimator"), new GUIContent("Use Manual Animator"));
        
        serializedObject.ApplyModifiedProperties();
    }
}
