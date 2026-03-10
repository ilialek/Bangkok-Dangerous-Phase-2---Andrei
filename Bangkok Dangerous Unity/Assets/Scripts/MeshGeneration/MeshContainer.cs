using System;
using UnityEngine;
namespace MeshGeneration{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshContainer : MonoBehaviour{
        [Flags]
        public enum GizmoMode{
            Nothing = 0,
            Vertices = 1,
            Normals = 0b10,
            Tangents = 0b100,
            Bounds = 0b100000
        }

        public GizmoMode ActiveGizmos;
        
        [SerializeField]
        protected Mesh Mesh;
        
        private Vector3[] vertexPositions;
        private Vector3[] normals;
        private Vector4[] tangents;

        private void Update() => ResetGizmoData();

        protected void ResetGizmoData(){
            vertexPositions = null;
            normals = null;
            tangents = null;
        }

        private void OnDrawGizmos(){
            if (ActiveGizmos == GizmoMode.Nothing || Mesh == null) return;
            
            vertexPositions ??= Mesh.vertices;
            normals ??= Mesh.normals;
            tangents ??= Mesh.tangents;
            
            Transform ownTransform = transform;
            for (int i = 0; i < vertexPositions.Length; i++){
                // Object to world space
                Vector3 vertexPosition = ownTransform.TransformPoint(vertexPositions[i]);
                
                // Positions
                if ((ActiveGizmos & GizmoMode.Vertices) != 0){
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(vertexPosition, .02f);
                }
                
                // Normals
                if ((ActiveGizmos & GizmoMode.Normals) != 0){
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(vertexPosition, ownTransform.TransformDirection(normals[i]) * .2f);
                }
                
                // Tangents
                if ((ActiveGizmos & GizmoMode.Tangents) != 0){
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(vertexPosition, ownTransform.TransformDirection(tangents[i]) * .2f);
                }
            }

            if ((ActiveGizmos & GizmoMode.Bounds) != 0){
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(Mesh.bounds.center + ownTransform.position, Mesh.bounds.size);
            }
        }
    }
}