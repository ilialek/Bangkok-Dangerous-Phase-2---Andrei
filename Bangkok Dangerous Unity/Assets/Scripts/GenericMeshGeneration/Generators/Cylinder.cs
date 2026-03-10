using MeshGeneration.Interface;
using MeshGeneration.Utility;
using Unity.Mathematics;
using UnityEngine;
namespace GenericMeshGeneration.Generators{
    public struct Cylinder : IMeshGenerator{
        
        public int Resolution{ get; set; }
        public int VertexCount => VertexRows * VertexSegments;
        public int IndexCount => 6 * VertexRows * (VertexSegments - 1);
        public int JobLength => VertexRows;

        public float Length;
        public int Subdivisions;
        public float Radius;

        private int VertexRows => Resolution + 2;
        private int VertexSegments => Subdivisions + 2;
        
        public Bounds Bounds => new(
            new Vector3(.5f, 0, .5f),
            new Vector3(1f, 0, 1f)
        );

        public void Execute<TStream>(int index, TStream stream) where TStream : struct, IMeshStream{
            int vertexIndex = index * VertexSegments;
            int triangleIndex = index * (Subdivisions + 1) * 2;
            
            float edgeAngle = math.TAU / VertexRows * index;
            Vertex vertex = new(){
                Tangent = new float4(0, 0, 0, -1f),
                TextureCoordinate0 = new float2(0, (float) index / VertexRows),
            };
            math.sincos(edgeAngle, out vertex.Position.y, out vertex.Position.z);
            vertex.Position.yz *= new float2(1, -1) * Radius;
            vertex.Normal = vertex.Position;
            vertex.Tangent.x = -vertex.Normal.z;
            vertex.Tangent.z = vertex.Normal.x;
            
            for (int i = 0; i < VertexSegments; i++, vertexIndex++){
                vertex.Position.x = Length / (VertexSegments - 1) * i;
                vertex.TextureCoordinate0.x = (float) 1 / (VertexSegments - 1) * i;
                stream.SetVertex(vertexIndex, vertex);

                if (i >= VertexSegments - 1) continue;
                stream.SetTriangle(triangleIndex++, (vertexIndex + new int3(0, VertexSegments, 1)) % VertexCount);
                stream.SetTriangle(triangleIndex++, (vertexIndex + new int3(VertexSegments, VertexSegments + 1, 1)) % VertexCount);
            }
        }
    }
}