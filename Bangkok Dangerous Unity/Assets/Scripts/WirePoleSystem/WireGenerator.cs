using System;
using MeshGeneration;
using MeshGeneration.Streams;
using Utilities.Splines;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
using WirePoleSystem.Jobs;

namespace WirePoleSystem{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WireGenerator : MeshGenerator{
        
        [Range(.01f, .5f)]
        public float Radius = .05f;
        
        [Range(.5f, 3)]
        public float SubdivisionDensity = .8f;
        public int MaxSubdivisions = 60;
        public bool LockShape;
        public ConnectionPoint StartPoint => StartAnchor;
        public ConnectionPoint EndPoint => EndAnchor;
        
        public Spline Spline;
        
        [SerializeField, HideInInspector]
        private GUID WireGuid;
        
        [SerializeField, HideInInspector]
        private ConnectionPoint StartAnchor;
        
        [SerializeField, HideInInspector]
        private ConnectionPoint EndAnchor;
        
        private bool unprocessedChanges;

        public GUID Guid{
            get => WireGuid;
            set{
                WireGuid = value;
                gameObject.name = $"Wire {value}";
            }
        }

        public GUID StartGuid => StartAnchor.Guid;
        public GUID EndGuid => EndAnchor.Guid;

        private void OnEnable(){
            Mesh = GetComponent<MeshFilter>().sharedMesh;
        }

        private void OnValidate() => unprocessedChanges = true;

        private void Update(){
            if (!unprocessedChanges) return;
            GenerateMesh();
            unprocessedChanges = false;
        }

        public void SetupMesh(){
            Mesh = new Mesh{
                name = $"{name} mesh"
            };
            GetComponent<MeshFilter>().mesh = Mesh;
        }

        public void CreateWire(ConnectionPoint startAnchor, ConnectionPoint endAnchor){
            StartAnchor = startAnchor;
            EndAnchor = endAnchor;
            
            InitialiseSpline();

            Vector3 middlePoint = SampleSpline(.5f);
            AddPoint(middlePoint);
            UpdateMiddlePoint();
            
            Spline.UpdateHandles();
            GenerateMesh();
        }
        
        [ContextMenu("Update mesh")]
        public void UpdateWire() => GenerateMesh();
        
        public void UpdateSpline() => Spline.UpdateHandles();

        public void UpdateMiddlePoint(){
            if (Spline == null || Spline.Count < 3 || LockShape) return;

            Vector3 startPosition = StartPoint.WorldSpacePosition;
            Vector3 endPosition = EndPoint.WorldSpacePosition;
            
            // Find middle point
            int middleIndex = Mathf.FloorToInt((Spline.Count - 1f) / 2);
            Spline[middleIndex].Position = startPosition + (endPosition - startPosition) * .5f + Vector3.down;
            Spline.UpdateHandles();
        }

        protected override void GenerateMesh(){
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];
            
            if (Spline == null) InitialiseSpline();
            
            // Calculate the subdivision amount based on the length of the wire
            int subdivisions = CalculateSubdivisionAmount();
            
            // Sample the spline to get the cylinder loop positions
            NativeArray<float3> positions = GetWirePositions(subdivisions);

            JobHandle jobHandle = WireMeshJob<SingleStream>.ScheduleParallel(
                Mesh,
                meshData,
                Resolution,
                subdivisions,
                Radius,
                positions,
                default
            );

            positions.Dispose(jobHandle);
            jobHandle.Complete();
            
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, Mesh);
            ResetGizmoData();
        }

        private void InitialiseSpline(){
            Spline = new Spline();
            
            // The order is important
            AddPoint(StartPoint.WorldSpacePosition);
            AddPoint(EndPoint.WorldSpacePosition, Spline.Count); // Make sure to add the end point... at the end
            
            // We need to make sure the spline updates after this
            Spline.UpdateHandles();
        }
        
        // Small wrapper function for cleanliness
        private Vector3 SampleSpline(float progress){
            Spline.Evaluate(progress, out Vector3 sampledPoint, out _, out _);
            return sampledPoint;
        }

        private void SpreadPoints(Spline lastSpline){
            for (int i = 0; i < Spline.Count; i++){
                float progress = i / ((float) Spline.Count - 1);
                
                if (i == Spline.Count - 1) progress = 1f;

                lastSpline.Evaluate(progress, out Vector3 sampledPoint, out _, out _);
                Spline[i].Position = sampledPoint;
            }
            
            Spline.UpdateHandles();
        }

        public void AddPoint(Vector3? position = null, int? index = null){
            if (Spline == null) return;
            if (index != null && (index < 0 || index > Spline.Count))
                throw new ArgumentOutOfRangeException(nameof(index), "Wire point index out of range.");
            
            int insertionIndex;
            if (index != null) insertionIndex = (int) index;
            else insertionIndex = Spline.Count > 0 ? Spline.Count - 1 : 0;

            Spline lastSpline = new(Spline);
            
            float progress = insertionIndex / (float) Spline.Count;
            Vector3 newPointPosition = position ?? SampleSpline(progress);
            
            Spline.Insert(insertionIndex, newPointPosition);
            
            if (position == null) SpreadPoints(lastSpline);
        }

        private int CalculateSubdivisionAmount(){
            int subdivisions = Mathf.RoundToInt(Spline.Length * SubdivisionDensity);
            return Mathf.Clamp(subdivisions, 0, MaxSubdivisions);
        }

        private NativeArray<float3> GetWirePositions(int subdivisions){
            // We need a position for every subdivision, as well as for the start and the end point
            NativeArray<float3> positions = new(subdivisions + 2, Allocator.TempJob);
            for (int i = 0; i < positions.Length; i++){
                // Get a point on the spline
                Vector3 position = SampleSpline((float) i / (positions.Length - 1));
                
                // World to local space
                positions[i] = transform.InverseTransformPoint(position);
            }
            return positions;
        }

        protected void OnDrawGizmosSelected(){
            if (!Mesh) return;
            
            Transform ownTransform = transform;
            
            Vector3[] vertexPositions = Mesh.vertices;
            for (int i = 0; i < vertexPositions.Length; i++){
                // Object to world space
                Vector3 vertexPosition = ownTransform.TransformPoint(vertexPositions[i]);
                
                // We want to avoid drawing lines between loops
                // Here we draw a line back to the first vertex of the loop once we're at the last loop vertex
                int otherVertexIndex = (i + 1) % (Resolution + 2) == 0 ? i - (Resolution + 1) : i + 1;
                
                // We have to check if we're in the bounds of the array because the mesh might not have been updated yet
                if (otherVertexIndex > vertexPositions.Length - 1) return;
                
                Vector3 otherVertex = vertexPositions[otherVertexIndex];
                
                // We need to get the point in world space
                otherVertex = ownTransform.TransformPoint(otherVertex);

                Gizmos.color = Color.white;
                Gizmos.DrawLine(vertexPosition, otherVertex);
            }

            Gizmos.color = Color.blue;
            foreach (BezierKnot splineKnot in Spline) Gizmos.DrawSphere(splineKnot.Position, .1f);
        }
    }
}