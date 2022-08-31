using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RectRender : MonoBehaviour {
    private Vector3 startPoint;
    private Vector3 endPoint;
    private bool onDrawingRect;
    private Vector3 currentPoint;

    public GUIStyle rectStyle;

    public Material rectMat = null;

    public Color rectColor;

    private void Update() {
        OnLeftClick();
    }

    private void OnLeftClick() {
        if (Input.GetMouseButtonDown(0)) {
            onDrawingRect = true;
            startPoint = Input.mousePosition;

        }

        if (onDrawingRect) {
            currentPoint = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0)) {
            onDrawingRect = false;

        }
    }

    private void OnPostRender() {
        if (onDrawingRect) {
            endPoint = Input.mousePosition;

            GL.PushMatrix();

            if (!rectMat) {
                return;
            }

            rectMat.SetPass(0);

            GL.LoadPixelMatrix();

            GL.Begin(GL.QUADS);

            GL.Color(new Color(rectColor.r, rectColor.g, rectColor.b, 0.1f));

            GL.Vertex3(startPoint.x, startPoint.y, 0);

            GL.Vertex3(endPoint.x, startPoint.y, 0);

            GL.Vertex3(endPoint.x, endPoint.y, 0);

            GL.Vertex3(startPoint.x, endPoint.y, 0);

            GL.End();

            GL.PopMatrix();
        }
    }
}
