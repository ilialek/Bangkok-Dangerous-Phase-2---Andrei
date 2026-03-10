using System.Runtime.InteropServices;
using MeshGeneration.Interface;
using MeshGeneration.Utility;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
namespace MeshGeneration.Streams{
    public struct SingleStream : IMeshStream{
        [StructLayout(LayoutKind.Sequential)]
        private struct Stream0Vertex{
            public float3 Position;
            public float3 Normal;
            public float4 Tangent;
            public float2 TextureCoordinate0;
        }
        
        // The Unity Job system will throw an error here if we don't disable the container safety restriction
        // This error comes from the fact that NativeArray is essentially a kind of pointer to a block of memory
        // Both of these blocks of memory are part of the same MeshData container, and Unity will proceed to throw
        // an error to avoid multiple jobs writing to the same memory. Because both of these "pointers" refer to
        // different blocks of memory that will *never* overlap, we can safely disable the safety check in this case
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<Stream0Vertex> stream0;
        
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<TriangleUInt16> triangles;
        
        public void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount){
            // The NativeArray to store our vertex attributes in.
            // We're using **four** attributes in this **temporary** array,
            // and we don't have to **initialise** the memory (setting it to 0)
            // as we'll be overwriting it, anyway.
            NativeArray<VertexAttributeDescriptor> vertexAttributes = new(
                4,
                Allocator.Temp,
                NativeArrayOptions.UninitializedMemory
            );
            
            // Position as float3 (Float32) in stream 0
            vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);
            
            // Normal as float3 (Float32) in stream 0
            vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3);
            
            // Tangent as float4 (Float32) in stream 0
            vertexAttributes[2] = new VertexAttributeDescriptor(VertexAttribute.Tangent, dimension: 4);
            
            // Texture coordinate 0 (UV) as float2 (Float32) in stream 0
            vertexAttributes[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2);
            
            // Assign the vertex attributes to the MeshData
            meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
            // We don't need our vertex attribute definition array anymore now that we've passed on the attributes
            vertexAttributes.Dispose();
            
            // Set the parameters for the index buffer (triangle definition)
            // We're also telling it to use UInt16 for the buffer definition
            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
            
            // We're only supporting one sub-mesh for now
            meshData.subMeshCount = 1;
            
            // Since we only have one sub-mesh, the range for the mesh is the full range of all our triangles 
            // We need to prevent the index validation as it'll fail due to us not having added any vertices yet
            // The vertices will be added once the job runs
            // We also need to specify to not recalculate the bounds as we'll be supplying them manually and we
            // need to prevent them from being overwritten again once this is done
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexCount){
                    bounds = bounds,
                    vertexCount = vertexCount
                },
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices
            );
            
            // Store a reference to stream 0
            stream0 = meshData.GetVertexData<Stream0Vertex>();
            // Reinterpret the array of integers as an array of integer triples
            // The parameter "4" here is the size in bytes of the current array type, used for the reinterpretation
            // An integer is a 32-bit signed number type, so 4 bytes big
            triangles = meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);
        }
        
        public void SetVertex(int index, Vertex vertex){
            stream0[index] = new Stream0Vertex(){
                Position = vertex.Position,
                Normal = vertex.Normal,
                Tangent = vertex.Tangent,
                TextureCoordinate0 = vertex.TextureCoordinate0
            };
        }

        public void SetTriangle(int index, int3 triangle) => triangles[index] = triangle;
    }
}