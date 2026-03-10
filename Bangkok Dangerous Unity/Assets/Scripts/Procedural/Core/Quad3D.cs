using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    [System.Serializable]
    public struct Quad3D
    {
        public Vector3 BottomLeft;
        public Vector3 BottomRight;
        public Vector3 TopRight;
        public Vector3 TopLeft;

        public Quad3D(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topRight, Vector3 topLeft)
        {
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
            TopRight = topRight;
            TopLeft = topLeft;
        }

        public Vector3 Normal
        {
            get
            {
                Vector3 edge1 = BottomRight - BottomLeft;
                Vector3 edge2 = TopLeft - BottomLeft;
                Vector3 normal = Vector3.Cross(edge1, edge2);

                return normal.normalized;
            }
        }

        public static Quad3D operator +(Quad3D quad, Vector3 offset)
        {
            return new Quad3D(
                quad.BottomLeft + offset,
                quad.BottomRight + offset,
                quad.TopRight + offset,
                quad.TopLeft + offset
            );
        }

        public static Quad3D operator -(Quad3D quad, Vector3 offset)
        {
            return new Quad3D(
                quad.BottomLeft - offset,
                quad.BottomRight - offset,
                quad.TopRight - offset,
                quad.TopLeft - offset
            );
        }
    }
}