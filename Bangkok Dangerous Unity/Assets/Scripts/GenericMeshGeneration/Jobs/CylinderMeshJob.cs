using GenericMeshGeneration.Generators;
using MeshGeneration.Interface;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
namespace GenericMeshGeneration.Jobs{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast,  CompileSynchronously = true)]
    public struct CylinderMeshJob<TStream> : IJobFor where TStream : struct, IMeshStream{
        
        private Cylinder generator;

        [WriteOnly]
        private TStream stream;
        
        public void Execute(int index) => generator.Execute(index, stream);

        public static JobHandle ScheduleParallel(Mesh mesh, Mesh.MeshData meshData, int resolution, float length,
            int subdivisions, float radius, JobHandle dependency){
            CylinderMeshJob<TStream> meshJob = new();
            mesh.bounds = meshJob.generator.Bounds;
            meshJob.generator.Resolution = resolution;
            meshJob.generator.Length = length;
            meshJob.generator.Subdivisions = subdivisions;
            meshJob.generator.Radius = radius;
            meshJob.stream.Setup(meshData, mesh.bounds, meshJob.generator.VertexCount, meshJob.generator.IndexCount);
            return meshJob.ScheduleParallel(meshJob.generator.JobLength, 1, dependency);
        }
    }
}