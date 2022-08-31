using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;


//public class CircleMask : MonoBehaviour {
//    private MeshFilter meshFilter;
//    private Mesh mesh;

//    private void Awake() {
//        meshFilter = GetComponent<MeshFilter>();
//        mesh = new Mesh();
//        mesh.name = "Circle";
//        meshFilter.mesh = mesh;

//        mesh.vertices = new Vector3[4] { new Vector3(0, 0, 0), new Vector3(0, 10, 0), new Vector3(10, 10, 0), new Vector3(10, 0, 0) };
//        int[] triangles = new int[] {
//            0,1,2,0,2,3
//        };
//        mesh.triangles = triangles;

//    }

//    private void Update() {

//    }
//}

public class BaseImage : Image {

}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CircleImage : BaseImage {
    private int segements = 300; //总共需要分的块数
    private float fillPercent = 1f; //填充比例

    private Vector2 currentVertice; //

    private List<Vector2> outerVertices = new List<Vector2>();

    //private float DegreeDelta {
    //    get {
    //        return 2 * Mathf.PI / segements;
    //    }
    //}

    //private int CurrentSegement { //当前需要生成的三角形数量
    //    get {
    //        return (int)(segements * fillPercent);
    //    }
    //}

    protected override void Awake() {
    }

    protected override void OnPopulateMesh(VertexHelper vh) { // vh用于记录顶点信息
        vh.Clear();

        float tw = rectTransform.rect.width;
        float th = rectTransform.rect.height;
        float outerRadius = rectTransform.pivot.x * tw;

        Vector4 uv = overrideSprite != null ? DataUtility.GetOuterUV(overrideSprite) : Vector4.zero; //如果overrideSprite不为空，则获取其uv
        //Debug.Log(DataUtility.GetOuterUV(overrideSprite) + " " + DataUtility.GetInnerUV(overrideSprite));

        float uvCenterX = (uv.x + uv.z) * 0.5f;
        float uvCenterY = (uv.y + uv.w) * 0.5f;
        float uvScaleX = (uv.z - uv.x) / tw;
        float uvScaleY = (uv.y - uv.w) / th;

        float degreeDelta = 2 * Mathf.PI / segements;
        int currentSegement = (int)(segements * fillPercent);

        float currentDegree = 0; //当前填充角度
        UIVertex uiVertex;
        int verticeCount;
        int triangleCount;
        
        // 圆心信息
        currentVertice = Vector2.zero; //圆心
        verticeCount = currentSegement + 1; //切割数 + 圆心
        uiVertex = new UIVertex();
        uiVertex.color = color;
        uiVertex.position = currentVertice;
        uiVertex.uv0 = new Vector2(currentVertice.x * uvScaleX + uvCenterX, currentVertice.y * uvScaleY + uvCenterY); //贴图点坐标
        vh.AddVert(uiVertex);
        //

        for(int i = 1; i < verticeCount; i++) {
            float cosA = Mathf.Cos(currentDegree);
            float sinA = Mathf.Sin(currentDegree);
            currentVertice = new Vector2(cosA * outerRadius, sinA * outerRadius);
            currentDegree += degreeDelta;

            uiVertex = new UIVertex();
            uiVertex.color = color;
            uiVertex.position = currentVertice;
            uiVertex.uv0 = new Vector2(currentVertice.x * uvScaleX + uvCenterX, currentVertice.y * uvScaleY + uvCenterY);
            vh.AddVert(uiVertex);

            outerVertices.Add(currentVertice);
        }

        // 每次传入三个顶点就确定一个三角形
        // 当传入gpu时，若该三角形顶点是顺时针摆放，则表示正对屏幕

        triangleCount = currentSegement * 3;

        for(int i = 0, vIdx = 1; i < triangleCount - 3; i += 3, vIdx++) {
            vh.AddTriangle(vIdx, 0, vIdx + 1);
        }

        if(fillPercent == 1) {
            vh.AddTriangle(verticeCount - 1, 0, 1);
        }
    }
}
