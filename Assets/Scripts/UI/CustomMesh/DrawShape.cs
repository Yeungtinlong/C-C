using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DrawShape : MonoBehaviour {
    private Vector3 startPoint = Vector3.zero;
    private Vector3 endPoint = Vector3.zero;
    public float height;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Vector3[] vertices;
    private int[] triangles;
    //private List<Vector2> uvs = new List<Vector2>();

    public bool drawShape = false;

    private void Awake() {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Update() {
        if (drawShape)
            meshFilter.mesh = DrawRectangle();
    }

    private Mesh DrawRectangle() {
        Mesh mesh = new Mesh();

        startPoint.y = height;
        endPoint.y = height;

        vertices = new Vector3[] {
            startPoint, new Vector3(endPoint.x, height, startPoint.z), endPoint, new Vector3(startPoint.x, height, endPoint.z)
        };

        if ((startPoint.x < endPoint.x && startPoint.z > endPoint.z) || (startPoint.x > endPoint.x && startPoint.z < endPoint.z)) {
            triangles = new int[] {
                0, 1, 3,
                1, 2, 3
            };
        } else {
            triangles = new int[] {
                0, 3, 1,
                1, 3, 2
            };
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    }

    public void StartDrawShape() {
        drawShape = true;
    }

    public void UpdateShape(Vector3 startPoint, Vector3 endPoint) {
        this.startPoint = startPoint;
        this.endPoint = endPoint;
    }

    public void StopDrawShape() {
        drawShape = false;
        meshFilter.mesh = null;
        startPoint = Vector3.zero;
        endPoint = Vector3.zero;
    }
}
