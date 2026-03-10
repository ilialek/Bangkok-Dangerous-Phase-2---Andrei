using System.Linq;
using WirePoleSystem.Generators;
using MeshGeneration.Interface;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using Unity.Jobs;
namespace WirePoleSystem.Jobs{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct WireMeshJob<TStream> : IJobFor where TStream : struct, IMeshStream{
        
        private Wire generator;
        
        [WriteOnly]
        private TStream stream;

        public void Execute(int index) => generator.Execute(index, stream);

        public static JobHandle ScheduleParallel(Mesh mesh, Mesh.MeshData meshData, int resolution, int subdivisions,
            float radius, NativeArray<float3> positions, JobHandle dependency){
            WireMeshJob<TStream> meshJob = new();
            meshJob.generator.Resolution = resolution;
            meshJob.generator.Subdivisions = subdivisions;
            meshJob.generator.Radius = radius;
            meshJob.generator.Positions = positions;
            meshJob.generator.Max = positions.Aggregate(new float3(), math.max);
            meshJob.generator.Min = positions.Aggregate(new float3(), math.min);
            mesh.bounds = meshJob.generator.Bounds;
            meshJob.stream.Setup(meshData, mesh.bounds, meshJob.generator.VertexCount, meshJob.generator.IndexCount);
            return meshJob.ScheduleParallel(meshJob.generator.JobLength, 1, dependency);
        }
    }
}