using Unity.Mathematics;
using UnityEngine;
namespace MeshGeneration.Interface{
    public interface IMeshStream{
        public void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount);

        public void SetVertex(int index, Utility.Vertex data);

        public void SetTriangle(int index, int3 triangle);
    }
}