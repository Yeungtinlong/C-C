using UnityEditor;
using UnityEngine;

public class MeshSave : MonoBehaviour
{
    [SerializeField] private string _name = default;
    private Mesh _mesh;

    public void SaveMesh()
    {
        try
        {
            _mesh = GetComponent<MeshFilter>().mesh;
            if (_mesh != null)
            {
                AssetDatabase.CreateAsset(_mesh, "Assets/Resources/" + _name + ".asset");
                Debug.Log("Mesh is successfully saved at " + AssetDatabase.GetAssetPath(this) + _name + ".asset.");
            }
            else
            {
                Debug.LogWarning("Mesh can't be saved since there are no MeshFilter.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Mesh can't be saved " + e.ToString());
        }

    }
}
