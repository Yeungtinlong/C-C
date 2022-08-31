using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), ExecuteInEditMode]
public class DrawCircleRing : MonoBehaviour
{
    [Range(3, 100)]
    [SerializeField] private int _segmentCount;
    [Range(0, 100)]
    [SerializeField] private int _percentage;
    [SerializeField] private float _radiusIn = default;
    [SerializeField] private float _radiusOut = default;

    private MeshFilter _meshFilter;
    private Mesh _mesh;
    private Vector3[] _vertices;
    private Vector2[] _uvs;
    private int[] _triangles;
    private float _degree;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
    }

    private void Start()
    {
        //_mesh = new Mesh();
        //int verticesCount = Mathf.FloorToInt(_segmentCount * 0.01f * _percentage) * 2 + 2;
        //// 存放所有顶点
        //_vertices = new Vector3[verticesCount];
        //// 每个segment所占角度
        //_degree = Mathf.PI * 2 / _segmentCount;
        //// 此处triangle是为vertices标记顺序，指定第几个三角形用哪几个点构成
        //_triangles = new int[Mathf.FloorToInt(_segmentCount * 0.01f * _percentage) * 2 * 3];
        //_uvs = new Vector2[_vertices.Length];
    }

    private void Update()
    {
        _mesh = new Mesh();
        int verticesCount = Mathf.FloorToInt(_segmentCount * 0.01f * _percentage) * 2 + 2;
        // 存放所有顶点
        _vertices = new Vector3[verticesCount];
        // 每个segment所占角度
        _degree = Mathf.PI * 2 / _segmentCount;
        // 此处triangle是为vertices标记顺序，指定第几个三角形用哪几个点构成
        _triangles = new int[Mathf.FloorToInt(_segmentCount * 0.01f * _percentage) * 2 * 3];
        _uvs = new Vector2[_vertices.Length];
        DrawCircleMesh();
    }

    private void DrawCircleMesh()
    {
        //if (_percentage == 0)
        //    return;

        for (int i = 0, j = 0; i < _vertices.Length - 1; i += 2, j++)
        {
            //Vector3 point1 = new Vector3(Mathf.Cos(i * degree) * rIn, 0, Mathf.Sin(i * degree) * rIn);
            //Vector3 point2 = new Vector3(Mathf.Cos(i * degree) * rOut, 0, Mathf.Sin(i * degree) * rOut);

            Vector3 point1 = new Vector3(Mathf.Cos(j * _degree) * _radiusIn, 0f, Mathf.Sin(j * _degree) * _radiusIn);
            Vector3 point2 = new Vector3(Mathf.Cos(j * _degree) * _radiusOut, 0f, Mathf.Sin(j * _degree) * _radiusOut);

            _vertices[i] = point1;
            _vertices[i + 1] = point2;
        }

        // vi表示每隔2个顶点就是下一个segment的起点。在一个segment内，三角形用点顺序是031、023，最后一个segment单独处理
        for (int i = 0, vi = 0; i < _triangles.Length - 5; i += 6, vi += 2)
        {
            if (i == _triangles.Length - 6 && _percentage == 100)
            {
                //Debug.Log("!");
                _triangles[i] = vi;
                _triangles[i + 1] = 1;
                _triangles[i + 2] = vi + 1;

                _triangles[i + 3] = vi;
                _triangles[i + 4] = 0;
                _triangles[i + 5] = 1;
            }
            else
            {
                _triangles[i] = vi;
                _triangles[i + 1] = vi + 3;
                _triangles[i + 2] = vi + 1;

                _triangles[i + 3] = vi;
                _triangles[i + 4] = vi + 2;
                _triangles[i + 5] = vi + 3;
            }
        }

        //for (int i = 0; i < _uvs.Count; i++)
        //{
        //    _uvs[i] = new Vector2(_vertices[i].x / _radiusOut, _vertices[i].y / _radiusIn);
        //}

        _mesh.SetVertices(_vertices);
        _mesh.SetTriangles(_triangles, 0);
        //_mesh.SetUVs(0, _uvs);
        //_mesh.RecalculateNormals();

        _meshFilter.mesh = _mesh;
    }
}
