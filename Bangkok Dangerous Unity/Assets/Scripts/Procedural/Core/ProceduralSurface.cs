using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    public struct ProceduralSurface
    {
        public Quad3D Quad;
        public SurfaceType SurfaceType;
        public FacadeHandle Handle;
        public FacadeSemantic Semantics;

        public ProceduralSurface(Quad3D quad, SurfaceType surfaceType, FacadeHandle handle, FacadeSemantic semantics)
        {
            Quad = quad;
            SurfaceType = surfaceType;
            Handle = handle;
            Semantics = semantics;
        }

        public float GetArea()
        {
            float area1 = Vector3.Cross(Quad.BottomRight - Quad.BottomLeft, Quad.TopRight - Quad.BottomLeft).magnitude * 0.5f;
            float area2 = Vector3.Cross(Quad.TopLeft - Quad.BottomLeft, Quad.TopRight - Quad.BottomLeft).magnitude * 0.5f;
            return area1 + area2;
        }
    }

    public struct ProceduralSurface2
    {
        public List<Vector3> Vertices;
        public Vector3 Normal;
        public SurfaceType SurfaceType;
        public FacadeHandle Handle;

        public ProceduralSurface2(List<Vector3> vertices, SurfaceType surfaceType, FacadeHandle handle)
        {
            Vertices = vertices;
            SurfaceType = surfaceType;
            Handle = handle;

            // Compute normal
            Vector3 normal = Vector3.zero;
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vector3 current = Vertices[i];
                Vector3 next = Vertices[(i + 1) % Vertices.Count];
                normal += Vector3.Cross(current, next);
            }

            Normal = normal.normalized;
        }

        public float GetArea()
        {
            if (Vertices == null || Vertices.Count < 3) return 0.0f;

            float area = 0f;
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vector3 vi = Vertices[i];
                Vector3 vj = Vertices[(i + 1) % Vertices.Count];
                area += Vector3.Dot(Vector3.Cross(vi, vj), Normal);
            }

            return Mathf.Abs(area) * 0.5f;
        }
    }
}