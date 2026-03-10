using MeshGeneration.Interface;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
namespace MeshGeneration.Jobs{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MeshJob<TGenerator, TStream> : IJobFor
        where TGenerator : struct, IMeshGenerator
        where TStream : struct, IMeshStream{
        
        private TGenerator generator;
        
        [WriteOnly]
        private TStream stream;

        public void Execute(int index) => generator.Execute(index, stream);

        public static JobHandle ScheduleParallel(Mesh mesh, Mesh.MeshData meshData, int resolution, JobHandle dependency){
            MeshJob<TGenerator, TStream> meshJob = new();
            mesh.bounds = meshJob.generator.Bounds;
            meshJob.generator.Resolution = resolution;
            meshJob.stream.Setup(meshData, mesh.bounds, meshJob.generator.VertexCount, meshJob.generator.IndexCount);
            return meshJob.ScheduleParallel(meshJob.generator.JobLength, 1, dependency);
        }
    }
    
    public delegate JobHandle MeshJobScheduleDelegate(Mesh mesh, Mesh.MeshData meshData, int resolution, JobHandle dependency);
}