using MeshGeneration.Interface;
using MeshGeneration.Utility;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
namespace MeshGeneration.Streams{
    // ReSharper disable once InconsistentNaming
    public struct Multi4x1Stream : IMeshStream{
        
        // The Unity Job system will throw an error here if we don't disable the container safety restriction
        // This error comes from the fact that NativeArray is essentially a kind of pointer to a block of memory
        // Both of these blocks of memory are part of the same MeshData container, and Unity will proceed to throw
        // an error to avoid multiple jobs writing to the same memory. Because both of these "pointers" refer to
        // different blocks of memory that will *never* overlap, we can safely disable the safety check in this case
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float3> stream0;
        
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float3> stream1;
        
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float4> stream2;
        
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float2> stream3;
        
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
            vertexAttributes[0] = new VertexAttributeDescriptor(stream: 0, dimension: 3);
            
            // Normal as float3 (Float32) in stream 1
            vertexAttributes[1] = new VertexAttributeDescriptor(stream: 1, attribute: VertexAttribute.Normal, dimension: 3);
            
            // Tangent as float4 (Float32) in stream 2
            vertexAttributes[2] = new VertexAttributeDescriptor(stream: 2, attribute: VertexAttribute.Tangent, dimension: 4);
            
            // Texture coordinate 0 (UV) as float2 (Float32) in stream 3
            vertexAttributes[3] = new VertexAttributeDescriptor(stream: 3, attribute: VertexAttribute.TexCoord0, dimension: 2);
            
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
            stream0 = meshData.GetVertexData<float3>();
            stream1 = meshData.GetVertexData<float3>(1);
            stream2 = meshData.GetVertexData<float4>(2);
            stream3 = meshData.GetVertexData<float2>(3);
            
            // Reinterpret the array of integers as an array of integer triples
            // The parameter "4" here is the size in bytes of the current array type, used for the reinterpretation
            // An integer is a 32-bit signed number type, so 4 bytes big
            triangles = meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);
        }
        
        public void SetVertex(int index, Vertex vertex){
            stream0[index] = vertex.Position;
            stream1[index] = vertex.Normal;
            stream2[index] = vertex.Tangent;
            stream3[index] = vertex.TextureCoordinate0;
        }

        public void SetTriangle(int index, int3 triangle) => triangles[index] = triangle;
    }
}