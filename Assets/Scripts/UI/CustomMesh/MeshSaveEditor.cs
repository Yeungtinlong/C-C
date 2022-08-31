using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshSave))]
public class MeshSaveEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        MeshSave meshSave = (MeshSave)serializedObject.targetObject;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("_name"));

        if (GUILayout.Button("Save Mesh"))
        {
            meshSave.SaveMesh();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
