using GenericMeshGeneration.Jobs;
using MeshGeneration.Runtime;
using MeshGeneration.Streams;
using UnityEngine;
namespace GenericMeshGeneration.Runtime{
    public class CylinderGenerator : RuntimeMeshGenerator{
        public float Radius = .25f;
        public float Length = 1;
        public float SubdivisionDensity;
        [Range(0, 10)]
        public int Subdivisions;

        protected override void GenerateMesh(){
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];

            CylinderMeshJob<SingleStream>.ScheduleParallel(Mesh, meshData, Resolution, Length, Subdivisions, Radius,  default).Complete();
            
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, Mesh);
        }
    }
}