using System;
using UnityEngine;

namespace CNC.Utility
{
    public static class Utils
    {
        /// <summary>
        /// To calculate a rect depend on the given start point and end point.
        /// </summary>
        public static Rect CalculateRect(Vector2 startPoint, Vector2 endPoint)
        {
            float width = Mathf.Abs(startPoint.x - endPoint.x);
            float height = Mathf.Abs(startPoint.y - endPoint.y);
            float xMin = Mathf.Min(startPoint.x, endPoint.x);
            float yMin = Mathf.Min(startPoint.y, endPoint.y);
            return new Rect(xMin, yMin, width, height);
        }

        /// <summary>
        /// To calculate the center of gravity from positions.
        /// </summary>
        public static Vector3 CalculateCenterOfGravity(Vector3[] positions)
        {
            float sumX = 0f, sumY = 0f, sumZ = 0f;
            int count = positions.Length;
            for (int i = 0; i < count; i++)
            {
                sumX += positions[i].x;
                sumY += positions[i].y;
                sumZ += positions[i].z;
            }
            return new Vector3(sumX, sumY, sumZ) / count;
        }

        public static void Swap<T>(ref T item01, ref T item02)
        {
            T temp = item02;
            item02 = item01;
            item01 = temp;
        }

        public static int SqrDistance(Vector2Int origin, Vector2Int other)
        {
            int deltaX = origin.x - other.x;
            int deltaY = origin.y - other.y;
            return deltaX * deltaX + deltaY * deltaY;
        }

        public static float SqrDistance(Vector2 origin, Vector2 other)
        {
            float deltaX = origin.x - other.x;
            float deltaY = origin.y - other.y;
            return deltaX * deltaX + deltaY * deltaY;
        }

        public static int Sqr(int number)
        {
            return number * number;
        }

        public static float Sqr(float number)
        {
            return number * number;
        }

        /// <summary>
        /// ��angle > 0������ʱ��ת��
        /// </summary>
        public static Vector2 Rotate(Vector2 vector, float angle)
        {
            float cosA = Mathf.Cos(angle * Mathf.Deg2Rad);
            float sinA = Mathf.Sin(angle * Mathf.Deg2Rad);
            return new Vector2(vector.x * cosA - vector.y * sinA, vector.y * cosA + vector.x * sinA);
        }

        /// <summary>
        /// ��signΪ����ʱ��������ʱ��ת90�ȵ�������
        /// </summary>
        public static Vector2 Perpendicular(Vector2 vector, float sign)
        {
            return new Vector2(-vector.y, vector.x) * sign;
        }

        /// <summary>
        /// �����ȼ����˶���λ�ơ�
        /// </summary>
        /// <param name="v0">���ٶ�</param>
        /// <param name="v1">ĩ�ٶ�</param>
        /// <param name="acceleration">���ٶ�</param>
        /// <returns>λ��</returns>
        public static float AccelerationRange(float v0, float v1, float acceleration)
        {
            return Mathf.Abs(Sqr(v1) - Sqr(v0)) / (2 * acceleration);
        }

        /// <summary>
        /// ����η��̡�
        /// </summary>
        /// <param name="a">������ϵ��</param>
        /// <param name="b">һ����ϵ��</param>
        /// <param name="c">������</param>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <returns>����ֵΪ���ʵ��������</returns>
        public static int SolveQuadratic(float a, float b, float c, out float x1, out float x2)
        {
            if (a == 0f)
            {
                if (b == 0f)
                {
                    x1 = float.NaN;
                    x2 = float.NaN;
                    return 0;
                }
                x1 = -c / b;
                x2 = float.NaN;
                return 1;
            }

            float delta = Sqr(b) - 4f * a * c;

            if (delta > 0)
            {
                x1 = (-b + Mathf.Sqrt(delta)) / (2f * a);
                x2 = (-b - Mathf.Sqrt(delta)) / (2f * a);
                return 2;
            }
            else if (delta == 0)
            {
                x1 = (-b + Mathf.Sqrt(Sqr(b) - 4f * a * c)) / (2f * a);
                x2 = float.NaN;
                return 1;
            }
            else
            {
                (x1, x2) = (float.NaN, float.NaN);
                return 0;
            }
        }
    }

    public struct Circle
    {
        public Vector2 Center { get; set; }
        public float Radius { get; set; }

        public Circle(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public int IntersectWith(Segment segment, out Vector2 intersection1, out Vector2 intersection2)
        {
            Vector2 seg = segment.Point2 - segment.Point1;
            Vector2 vC = segment.Point1 - Center;

            float a = Utils.Sqr(seg.x) + Utils.Sqr(seg.y);
            float b = 2f * (seg.x * vC.x + seg.y * vC.y);
            float c = Utils.Sqr(vC.x) + Utils.Sqr(vC.y) - Utils.Sqr(Radius);

            // ��Ϊ�ཻ�㴦��seg�����ı�����
            int solutionCount = Utils.SolveQuadratic(a, b, c, out float x1, out float x2);
            if (solutionCount == 0)
            {
                intersection1 = intersection2 = new Vector2(float.NaN, float.NaN);
                return 0;
            }

            if (solutionCount == 1)
            {
                if (x1 >= 0f && x1 <= 1f)
                {
                    intersection1 = segment.Point1 + x1 * seg;
                    intersection2 = new Vector2(float.NaN, float.NaN);
                    return 1;
                }
                intersection1 = intersection2 = new Vector2(float.NaN, float.NaN);
                return 0;
            }
            else // solutionCount == 2
            {
                if (x1 >= 0f && x1 <= 1f)
                {
                    intersection1 = segment.Point1 + x1 * seg;
                    if (x2 >= 0f && x2 <= 1f)
                    {
                        intersection2 = segment.Point1 + x2 * seg;
                        return 2;
                    }
                    intersection2 = new Vector2(float.NaN, float.NaN);
                    return 1;
                }
                else // intersection1���ڷ�Χ�ڡ�
                {
                    if (x2 >= 0f && x2 <= 1f)
                    {
                        intersection1 = segment.Point1 + x2 * seg;
                        intersection2 = new Vector2(float.NaN, float.NaN);
                        return 1;
                    }
                    intersection1 = intersection2 = new Vector2(float.NaN, float.NaN);
                    return 0;
                }
            }
        }
    }

    public class Segment
    {
        private Vector2 _point1;
        private Vector2 _point2;

        public Vector2 Point1 => _point1;
        public Vector2 Point2 => _point2;

        public float SegmentLength => Vector2.Distance(_point1, _point2);
        public Vector2 Direction => _point2 - _point1;
        public Vector2 Normal => new Vector2(_point2.y - _point1.y, _point1.x - _point2.x).normalized;

        public Segment(Vector2 point1, Vector2 point2)
        {
            _point1 = point1;
            _point2 = point2;
        }

        /// <summary>
        /// �ж����߶����ڵ�ֱ���Ƿ��ཻ��
        /// </summary>
        /// <param name="other"></param>
        /// <param name="intersection"></param>
        /// <returns>���ཻ����true��out�������ꡣ</returns>
        public bool LineIntersection(Segment other, out Vector2 intersection)
        {
            Vector2 vector1 = Direction;
            Vector2 vector2 = other.Direction;

            float cross = vector1.x * vector2.y - vector1.y * vector2.x;
            if (cross == 0f)
            {
                intersection = default;
                return false;
            }

            float x1, y1, x2, y2, x3, y3, x4, y4;
            (x1, y1) = (_point1.x, _point1.y);
            (x2, y2) = (_point2.x, _point2.y);
            (x3, y3) = (other.Point1.x, other.Point1.y);
            (x4, y4) = (other.Point2.x, other.Point2.y);

            float deltaX1 = x2 - x1;
            float deltaY1 = y2 - y1;

            float deltaX2 = x4 - x3;
            float deltaY2 = y4 - y3;

            float k1 = deltaY1 / deltaX1;
            float k2 = deltaY2 / deltaX2;

            float b1 = y1 - k1 * x1;
            float b2 = y3 - k2 * x3;

            float solutionX = (b2 - b1) / (k1 - k2);
            float solutionY = k1 * solutionX + b1;

            intersection = new Vector2(solutionX, solutionY);
            return true;
        }

        public bool SegmentIntersection(Segment other, out Vector2 intersection)
        {
            if (!LineIntersection(other, out intersection))
                return false;

            float condition = WhereIsPointOnSegment(intersection);
            float conditionOther = other.WhereIsPointOnSegment(intersection);

            if (condition < 0f || condition > 1f || conditionOther < 0f || conditionOther > 1f)
                return false;

            return true;
        }

        /// <summary>
        /// �ж�ֱ���ϵĵ��Ƿ����߶��ϡ�
        /// </summary>
        /// <param name="point"></param>
        /// <returns>�������߶��ϣ�����0~1��</returns>
        public float WhereIsPointOnSegment(Vector2 point)
        {
            Vector2 pointVector = point - _point1;
            return Vector2.Dot(Direction, pointVector) / Direction.sqrMagnitude;
        }
    }
}