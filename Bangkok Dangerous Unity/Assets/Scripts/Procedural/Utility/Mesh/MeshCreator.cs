using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    public class MeshCreator
    {
        private List<Vector3> m_Vertices = new List<Vector3>();
        private List<int> m_Triangles = new List<int>();
        private List<Vector2> m_Uvs = new List<Vector2>();
        private int m_VertexOffset = 0;
        
        private int m_StripStartIndex = 0;
        private bool m_StripFlip = false;

        public void AddQuad(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 vertex4,
            float uvMinX = 0.0f, float uvMaxX = 1.0f, float uvMinY = 0.0f, float uvMaxY = 1.0f)
        {
            AddQuadBase(vertex1, vertex2, vertex3, vertex4);

            // Set uvs
            m_Uvs.Add(new Vector2(uvMinX, uvMinY)); // Bottom - Left
            m_Uvs.Add(new Vector2(uvMaxX, uvMinY)); // Bottom - Right
            m_Uvs.Add(new Vector2(uvMinX, uvMaxY)); // Top - Left
            m_Uvs.Add(new Vector2(uvMaxX, uvMaxY)); // Top - Right
        }

        public void AddQuad(Quad3D quad, float uvMinX = 0.0f, float uvMaxX = 1.0f, float uvMinY = 0.0f, float uvMaxY = 1.0f)
        {
            AddQuadBase(quad.BottomLeft, quad.BottomRight, quad.TopLeft, quad.TopRight);

            // Set uvs
            m_Uvs.Add(new Vector2(uvMinX, uvMinY)); // Bottom - Left
            m_Uvs.Add(new Vector2(uvMaxX, uvMinY)); // Bottom - Right
            m_Uvs.Add(new Vector2(uvMinX, uvMaxY)); // Top - Left
            m_Uvs.Add(new Vector2(uvMaxX, uvMaxY)); // Top - Right
        }

        public void AddQuadWorldSpace(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 vertex4)
        {
            AddQuadBase(vertex1, vertex2, vertex3, vertex4);

            // Set uvs
            m_Uvs.Add(new Vector2(vertex1.z, vertex1.x));
            m_Uvs.Add(new Vector2(vertex2.z, vertex2.x));
            m_Uvs.Add(new Vector2(vertex3.z, vertex3.x));
            m_Uvs.Add(new Vector2(vertex4.z, vertex4.x));
        }
        
        private void AddQuadBase(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 vertex4)
        {
            // Set vertices
            m_Vertices.Add(vertex1);
            m_Vertices.Add(vertex2);
            m_Vertices.Add(vertex3);
            m_Vertices.Add(vertex4);
            
            // Set triangles
            m_Triangles.Add(m_VertexOffset + 0);
            m_Triangles.Add(m_VertexOffset + 2);
            m_Triangles.Add(m_VertexOffset + 3);
            m_Triangles.Add(m_VertexOffset + 3);
            m_Triangles.Add(m_VertexOffset + 1);
            m_Triangles.Add(m_VertexOffset + 0);
            m_VertexOffset += 4;
        }

        public void Move(Vector3 delta)
        {
            for (int i = 0; i < m_Vertices.Count; i++)
            {
                m_Vertices[i] += delta;
            }
        }

        public void AddTriangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            AddTriangleBase(vertex1, vertex2, vertex3);
            
            // Set uvs
            m_Uvs.Add(uv1);
            m_Uvs.Add(uv2);
            m_Uvs.Add(uv3);
        }

        public void AddTriangleWorldSpace(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            AddTriangleBase(vertex1, vertex2, vertex3);
            
            // Set uvs
            m_Uvs.Add(new Vector2(vertex1.z, vertex1.x));
            m_Uvs.Add(new Vector2(vertex2.z, vertex2.x));
            m_Uvs.Add(new Vector2(vertex3.z, vertex3.x));
        }

        private void AddTriangleBase(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            // Set vertices
            m_Vertices.Add(vertex1);
            m_Vertices.Add(vertex2);
            m_Vertices.Add(vertex3);

            // Set triangles
            m_Triangles.Add(m_VertexOffset + 0);
            m_Triangles.Add(m_VertexOffset + 1);
            m_Triangles.Add(m_VertexOffset + 2);
            m_VertexOffset += 3;
        }

        public void StartTriangleStrip(Vector3 vertex1, Vector3 vertex2, Vector2 uv1, Vector2 uv2, bool inverted = false)
        {
            m_StripStartIndex = m_Vertices.Count;
            m_Vertices.Add(vertex1);
            m_Vertices.Add(vertex2);
    
            m_Uvs.Add(uv1);
            m_Uvs.Add(uv2);

            m_VertexOffset += 2;
            m_StripFlip = inverted;
        }

        public void AddStripPoint(Vector3 vertex, Vector2 uv)
        {
            int currentIndex = m_Vertices.Count;
    
            // Need at least 2 previous vertices to form a triangle
            if (currentIndex - m_StripStartIndex < 2) return;

            m_Vertices.Add(vertex);
            m_Uvs.Add(uv);

            int i0 = currentIndex - 2;
            int i1 = currentIndex - 1;
            int i2 = currentIndex;

            if (m_StripFlip)
            {
                m_Triangles.Add(i0);
                m_Triangles.Add(i2);
                m_Triangles.Add(i1);
            }
            else
            {
                m_Triangles.Add(i0);
                m_Triangles.Add(i1);
                m_Triangles.Add(i2);
            }

            m_StripFlip = !m_StripFlip;
            m_VertexOffset += 1;
        }

        /// <summary>
        /// Only uses the last strip point
        /// </summary>
        public void AddStripPointWithLast(Vector3 vertex, Vector2 uv)
        {
            int currentIndex = m_Vertices.Count;

            // Need at least 2 previous vertices to form a triangle
            if (currentIndex - m_StripStartIndex < 3) return;

            m_Vertices.Add(vertex);
            m_Uvs.Add(uv);

            int i0 = currentIndex - 3;
            int i1 = currentIndex - 1;
            int i2 = currentIndex;

            if (m_StripFlip)
            {
                m_Triangles.Add(i0);
                m_Triangles.Add(i1);
                m_Triangles.Add(i2);
            }
            else
            {
                m_Triangles.Add(i0);
                m_Triangles.Add(i2);
                m_Triangles.Add(i1);
            }

            m_VertexOffset += 1;
        }
        
        public void FinishTriangleStrip(bool closeLoop = false)
        {
            if (closeLoop)
            {
                int count = m_Vertices.Count - m_StripStartIndex;
                if (count >= 3)
                {
                    // Add two more triangles to close the strip loop
                    // AddStripPoint(_vertices[_stripStartIndex], _uvs[_stripStartIndex]);
                    // AddStripPoint(_vertices[_stripStartIndex + 1], _uvs[_stripStartIndex + 1]);
                }
            }

            m_StripFlip = false;
            m_StripStartIndex = 0;
        }

        public void AddStrip(List<Vector3> vertices, int[] indices)
        {
            if (vertices == null || indices == null || vertices.Count < 3 || indices.Length < 3) return;

            int startIndex = m_Vertices.Count;

            m_Vertices.AddRange(vertices);

            // Simple uvs
            for (int i = 0; i < vertices.Count; i++)
            {
                m_Uvs.Add(new Vector2(vertices[i].x, vertices[i].z));
            }

            for (int i = 0; i < indices.Length; i += 3)
            {
                m_Triangles.Add(startIndex + indices[i]);
                m_Triangles.Add(startIndex + indices[i + 1]);
                m_Triangles.Add(startIndex + indices[i + 2]);
            }

            m_VertexOffset += vertices.Count;
        }

        public Mesh GetMesh()
        {
            Mesh mesh = new Mesh();
            mesh.SetVertices(m_Vertices);
            mesh.SetTriangles(m_Triangles, 0);
            mesh.RecalculateNormals();
            mesh.SetUVs(0, m_Uvs);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}