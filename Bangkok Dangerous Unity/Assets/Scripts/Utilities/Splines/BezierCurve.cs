using UnityEngine;

namespace Utilities.Splines
{
    [System.Serializable]
    public class BezierCurve
    {
        public Vector3 P0;
        public Vector3 P1;
        public Vector3 P2;
        public Vector3 P3;

        public BezierCurve(BezierKnot knot1, BezierKnot knot2)
        {
            P0 = knot1.Position;
            P1 = knot1.HandleOut;
            P2 = knot2.HandleIn;
            P3 = knot2.Position;
        }

        public float GetLength(int subdivisions = 20)
        {
            float length = 0.0f;
            Vector3 lastPoint = P0;

            for (int i = 1; i <= subdivisions; i++)
            {
                float t = i / (float)subdivisions;
                Vector3 point = CalculateBezierPoint(t, P0, P1, P2, P3);
                length += Vector3.Distance(lastPoint, point);
                lastPoint = point;
            }

            return length;
        }

        private static Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;

            return p;
        }
    }
}