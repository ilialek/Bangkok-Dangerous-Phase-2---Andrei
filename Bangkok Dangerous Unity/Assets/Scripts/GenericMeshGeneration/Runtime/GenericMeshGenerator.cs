using GenericMeshGeneration.Generators;
using MeshGeneration.Jobs;
using MeshGeneration.Runtime;
using MeshGeneration.Streams;
using UnityEngine;
namespace GenericMeshGeneration.Runtime{
    public class GenericMeshGenerator : RuntimeMeshGenerator{
        
        public enum MeshType{
            SquareGrid,
            SharedSquareGrid,
            StaticCylinder
        }

        public MeshType GeneratorMesh;

        private readonly MeshJobScheduleDelegate[] jobs = {
            MeshJob<SquareGrid, SingleStream>.ScheduleParallel,
            MeshJob<SharedSquareGrid, SingleStream>.ScheduleParallel,
            MeshJob<StaticCylinder, SingleStream>.ScheduleParallel
        };

        protected override void GenerateMesh(){
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];
        
            jobs[(int) GeneratorMesh](Mesh, meshData, Resolution, default).Complete();

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, Mesh);
        }
    }
}