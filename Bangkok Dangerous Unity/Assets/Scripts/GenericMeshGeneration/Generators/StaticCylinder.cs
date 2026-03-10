using System;
using MeshGeneration.Interface;
using MeshGeneration.Utility;
using Unity.Mathematics;
using UnityEngine;
namespace GenericMeshGeneration.Generators{
    public struct StaticCylinder : IMeshGenerator{
        
        public int Resolution{ get; set; }
        public int VertexCount => (Resolution + 1) * (Resolution + 1);
        public int IndexCount => 6 * Resolution * Resolution;
        public int JobLength => Resolution + 1;
        
        public Bounds Bounds => new(
            Vector3.zero,
            new Vector3(2f, 2f, 2f)
        );

        public void Execute<TStream>(int index, TStream stream) where TStream : struct, IMeshStream{
            // Starting vertex index
            int vertexIndex = index * (Resolution + 1);
            // Starting triangle index
            int triangleIndex = (index - 1) * Resolution * 2;

            float normalisedX = (float) index / Resolution;
            float edgeAngle = 2 * Mathf.PI / Resolution * index;
            
            Vertex vertex = new(){
                Position = new float3(Mathf.Sin(edgeAngle), 0, -Mathf.Cos(edgeAngle)),
                TextureCoordinate0 = new float2(normalisedX, 0)
            };
            vertex.Normal = vertex.Position;
            vertex.Tangent = new float4(1f, 0, 0, -1f);
            
            for (int i = 0; i < Resolution + 1; i++, vertexIndex++){
                // Set all the vertices
                float normalisedY = (float) i / Resolution;
                vertex.Position.y = normalisedY;
                vertex.TextureCoordinate0.y = normalisedY;
                stream.SetVertex(vertexIndex, vertex);
                
                // We don't want to place triangles where it doesn't make sense:
                // 1. We won't have enough vertices to make triangles in the first row
                // 2. We can't make any triangles at the last vertex of the row
                // 3. There won't be any vertices left to make triangles at the last vertex of the mesh
                if (index <= 0 || i == Resolution || vertexIndex + 1 == VertexCount) continue;
                SetQuadTriangles(ref triangleIndex, vertexIndex, stream);
            }
        }

        private void SetQuadTriangles<TStream>(ref int triangleIndex, int vertexIndex, TStream stream)
            where TStream : struct, IMeshStream{
            
            stream.SetTriangle(triangleIndex++, vertexIndex + new int3(-Resolution - 1, -Resolution, 0));
            stream.SetTriangle(triangleIndex++, vertexIndex + new int3(1, 0, -Resolution));
        }
    }
}