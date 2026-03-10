using MeshGeneration.Interface;
using MeshGeneration.Utility;
using Unity.Mathematics;
using UnityEngine;
namespace GenericMeshGeneration.Generators{
    public struct SquareGrid : IMeshGenerator{
        
        public int Resolution{ get; set; }
        public int VertexCount => 4 * Resolution * Resolution;
        public int IndexCount => 6 * Resolution * Resolution;
        public int JobLength => Resolution;
        
        public Bounds Bounds => new(
            new Vector3(.5f, 0, .5f),
            new Vector3(1f, 0, 1f)
        );

        public void Execute<TStream>(int index, TStream stream) where TStream : struct, IMeshStream{
            // Create a row of quads
            for (int i = 0; i < Resolution; i++){
                // The position of our quad
                float2 quadPosition = new(i, index);
                
                // Row index times the index of the quad in the row
                int quadIndex = index * Resolution + i;
                
                // Generate the quad
                GenerateQuad(quadPosition, quadIndex, stream);
            }
        }

        private void GenerateQuad<TStream>(float2 position, int quadIndex, TStream stream) where TStream : struct, IMeshStream{
            int vertexIndex = quadIndex * 4;
            int triangleIndex = quadIndex * 2;
            
            // The size of our quad before scaling
            float2 size = new(1f, 1f);
            
            // All the numbers needed for our vertex positions
            float2 xPositions = new(position.x, position.x + size.x);
            float2 zPositions = new(position.y, position.y + size.y);
            
            // Fit everything in 1x0x1
            xPositions /= Resolution;
            zPositions /= Resolution;
            
            // Create a new Vertex struct
            Vertex vertex = new();
            
            // Set global parameters for all our vertices
            vertex.Normal.y = 1f;
            vertex.Tangent.xw = new float2(1f, -1f);
            
            // Vertex 0
            vertex.Position.xz = new float2(xPositions.x, zPositions.x);
            stream.SetVertex(vertexIndex, vertex);
            
            // Vertex 1
            vertex.Position.xz = new float2(xPositions.x, zPositions.y);
            vertex.TextureCoordinate0.y = 1f;
            stream.SetVertex(vertexIndex + 1, vertex);
            
            // Vertex 2
            vertex.Position.xz = new float2(xPositions.y, zPositions.x);
            vertex.TextureCoordinate0 = new float2(1f, 0f);
            stream.SetVertex(vertexIndex + 2, vertex);
            
            // Vertex 3
            vertex.Position.xz = new float2(xPositions.y, zPositions.y);
            vertex.TextureCoordinate0 = 1f;
            stream.SetVertex(vertexIndex + 3, vertex);
            
            // Set triangles
            stream.SetTriangle(triangleIndex++, vertexIndex + new int3(0, 1, 2));
            stream.SetTriangle(triangleIndex, vertexIndex + new int3(1, 3, 2));
        }
    }
}