using MeshGeneration.Interface;
using MeshGeneration.Utility;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
namespace WirePoleSystem.Generators{
    public struct Wire : IMeshGenerator{
        public int Resolution{ get; set; }
        public int VertexCount => VertexRows * VertexSegments;
        public int IndexCount => 6 * VertexRows * (VertexSegments - 1);
        public int JobLength => VertexSegments;
        public Bounds Bounds => new(
            Min + (Max - Min) * .5f,
            Max - Min + Radius * 2
        );
        
        public int Subdivisions;
        public float Radius;
        public float3 Max;
        public float3 Min;
        
        [ReadOnly]
        public NativeArray<float3> Positions;
        
        private int VertexRows => Resolution + 2;
        private int VertexSegments => Subdivisions + 2;

        public void Execute<TStream>(int index, TStream stream) where TStream : struct, IMeshStream{
            int vertexIndex = index * VertexRows;
            
            // We only start defining the quads from the second vertex loop, so we need to subtract one from the index
            int triangleIndex = (index - 1) * VertexRows * 2;
            
            GenerateVertices(index, stream, vertexIndex);

            if (triangleIndex < 0) return;
            GenerateQuads(stream, vertexIndex, triangleIndex);
        }
        
        // Get the length from beginning to end of the line
        public float GetPositionLength(){
            float3 startPosition = Positions[0];
            float3 endPosition = Positions[^1];
            return math.length(endPosition - startPosition);
        }

        private void GenerateVertices<TStream>(int index, TStream stream, int vertexIndex) where TStream : struct, IMeshStream{
            // Set the parameters that are the same for all vertices
            // Since we're generating the vertices per loop, we know these won't change
            Vertex vertex = new(){
                Tangent = new float4(0, 0, 0, -1f),
                TextureCoordinate0 = new float2((float) index / (VertexSegments - 1), 0)
            };

            float3x3 loopRotationMatrix = GetLoopRotationMatrix(index);
            
            // Generate a loop of vertices
            for (int i = 0; i < VertexRows; i++){
                Vertex loopVertex = GenerateVertex(vertex, i, loopRotationMatrix);
                loopVertex.Position += Positions[index];
                stream.SetVertex(vertexIndex + i, loopVertex);
            }
        }

        private float3x3 GetLoopRotationMatrix(int index){
            bool isLastLoop = index >= VertexSegments - 1;
            bool isFirstLoop = index <= 0;
            
            float2 loopAngleZY;
            float3 loopDelta = new();
            
            if (!isLastLoop) loopDelta += Positions[index + 1] - Positions[index];
            
            if (!isFirstLoop) loopDelta += Positions[index] - Positions[index - 1];

            float horizontalDistance = math.sqrt(math.pow(loopDelta.x, 2) + math.pow(loopDelta.z, 2));
            
            loopAngleZY = new float2(
                -math.atan2(loopDelta.y, horizontalDistance),
                math.atan2(loopDelta.z, loopDelta.x)
            );
                
            math.sincos(loopAngleZY.x, out float zSin, out float zCos);
            float3x3 zRotationMatrix = new(
                zCos, zSin, 0,
                -zSin, zCos, 0,
                0, 0, 1
            );
                
            math.sincos(loopAngleZY.y, out float ySin, out float yCos);
            float3x3 yRotationMatrix = new(
                yCos, 0, -ySin,
                0, 1, 0,
                ySin, 0, yCos
            );
            
            // Combine the matrices
            return math.mul(yRotationMatrix, zRotationMatrix);
        }

        private Vertex GenerateVertex(Vertex baseVertex, int loopIndex, float3x3 loopRotationMatrix){
            // We don't have to define an X position here because this is already taken care of in GenerateVertices()
            Vertex newVertex = baseVertex;
            float edgeAngle = math.TAU / VertexRows * loopIndex;
            // By using math.sincos() we can calculate the sine and cosine at the same time
            math.sincos(edgeAngle, out newVertex.Position.y, out newVertex.Position.z);

            newVertex.Position = math.mul(loopRotationMatrix, newVertex.Position);
            
            // The normal is directly equal to the position of the point since this is in local space
            newVertex.Normal = newVertex.Position;
            
            // Scale the vertices to match the radius of our cylinder
            newVertex.Position *= Radius;
            
            // Rotate the normal to use as the tangent
            newVertex.Tangent.x = -newVertex.Normal.z;
            newVertex.Tangent.z = newVertex.Normal.x;
            
            // UV the cylinder by wrapping the texture around the round part
            newVertex.TextureCoordinate0.y = (float) loopIndex / (VertexRows - 1);
            return newVertex;
        }

        private void GenerateQuads<TStream>(TStream stream, int vertexIndex, int triangleIndex) where TStream : struct, IMeshStream{
            for (int i = 0; i < VertexRows; i++){
                // Define offsets to define our triangles with
                int3 triangle0 = new(0, -VertexRows + 1, -VertexRows);
                int3 triangle1 = new(0, 1, -VertexRows + 1);
                
                // Add the triangle offset from the loop
                triangle0 += i;
                triangle1 += i;
                
                // Make sure that the triangles at the end of the cylinder segment loop around to the beginning
                triangle1.y %= VertexRows; // Make sure we loop around the current ring
                
                // Snap back the previous ring if we end up at 0
                triangle0.y = (triangle0.y + VertexRows) % VertexRows - VertexRows;
                triangle1.z = (triangle1.z + VertexRows) % VertexRows - VertexRows;
                
                // Add the offsets to the vertex index
                stream.SetTriangle(triangleIndex++, triangle0 + vertexIndex);
                stream.SetTriangle(triangleIndex++, triangle1 + vertexIndex);
            }
        }
    }
}