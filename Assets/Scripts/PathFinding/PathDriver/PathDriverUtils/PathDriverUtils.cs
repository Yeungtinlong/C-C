using UnityEngine;

namespace CNC.PathFinding
{
    public static class PathDriverUtils
    {
        public static Vector2 TransformToWorldPoint(Vector3 transformPoint)
        {
            return new Vector2(transformPoint.x, transformPoint.z);
        }

        public static Vector3 WorldToTransformPoint(Vector2 worldPoint)
        {
            return new Vector3(worldPoint.x, 0, worldPoint.y);
        }
    }
}