using MeshGeneration.Interface;
using MeshGeneration.Utility;
using Unity.Mathematics;
using UnityEngine;
namespace GenericMeshGeneration.Generators{
    public struct SharedSquareGrid : IMeshGenerator{
        
        public int Resolution{ get; set; }
        public int VertexCount => (Resolution + 1) * (Resolution + 1);
        public int IndexCount => 6 * Resolution * Resolution;
        public int JobLength => Resolution + 1;
        
        public Bounds Bounds => new(
            new Vector3(.5f, 0, .5f),
            new Vector3(1f, 0, 1f)
        );

        public void Execute<TStream>(int index, TStream stream) where TStream : struct, IMeshStream{
            // Starting vertex index
            int vertexIndex = index * (Resolution + 1);
            // Starting triangle index
            int triangleIndex = (index - 1) * Resolution * 2;

            float normalisedY = (float) index / Resolution;
            
            Vertex vertex = new(){
                Position = new float3(0, 0, normalisedY),
                Normal = new float3(0, 1f, 0),
                Tangent = new float4(1f, 0, 0, -1f),
                TextureCoordinate0 = new float2(0, normalisedY)
            };
            
            for (int i = 0; i < Resolution + 1; i++, vertexIndex++){
                // Set all the vertices
                float normalisedX = (float) i / Resolution;
                vertex.Position.x = normalisedX;
                vertex.TextureCoordinate0.x = normalisedX;
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
            
            stream.SetTriangle(triangleIndex++, vertexIndex + new int3(-Resolution - 1, 0, -Resolution));
            stream.SetTriangle(triangleIndex++, vertexIndex + new int3(0, 1, -Resolution));
        }
    }
}