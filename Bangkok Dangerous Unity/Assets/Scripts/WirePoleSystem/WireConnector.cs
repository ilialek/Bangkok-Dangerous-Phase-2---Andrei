using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GUID = Utilities.GUID;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WirePoleSystem{
    [ExecuteInEditMode]
    public class WireConnector : MonoBehaviour{
        [SerializeField, HideInInspector]
        protected GUID ConnectorGuid;
        
        public List<ConnectionPoint> ConnectionPoints;
        
        public readonly Dictionary<GUID, WireGenerator> Wires = new();
        public readonly Dictionary<ConnectionPoint, List<ConnectionPoint>> WireConnections = new();

        public Material WireMaterial;

        public Action OnTransformChange = () => {};
        
        [NonSerialized]
        public bool Initialised;
        
        public GUID Guid{
            get => ConnectorGuid;
            set => ConnectorGuid = value;
        }

        protected void Initialise(){
            Wires.Clear();
            WireConnections.Clear();
            
            WireGenerator[] wires = GetComponentsInChildren<WireGenerator>(true);
            foreach (WireGenerator wire in wires){
                Wires.TryAdd(wire.Guid, wire);
                
                if (WireConnections.TryGetValue(wire.StartPoint, out List<ConnectionPoint> connections)) connections.Add(wire.EndPoint);
                else WireConnections.Add(wire.StartPoint, new List<ConnectionPoint>{wire.EndPoint});
            }
        }
        
        protected void Update(){
            if (!Initialised) Initialise();
            
            if (!transform.hasChanged) return;
            UpdateWires();
            OnTransformChange?.Invoke();
            transform.hasChanged = false;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected(){
            Gizmos.color = Color.cyan;
            const float sphereRadius = .1f;
            Vector3 labelOffset = new(0, 0, 0);
            
            foreach (ConnectionPoint connectionPoint in ConnectionPoints){
                Vector3 pointPosition = transform.TransformPoint(connectionPoint.Position);
                Gizmos.DrawSphere(pointPosition, sphereRadius);
                Handles.Label(pointPosition + labelOffset, connectionPoint.Guid.ToString());
            }
        }
#endif
        
        public GUID AddConnectionPoint(Vector3? position = null){
            Vector3 newPointPosition;
            
            if (ConnectionPoints.Count > 0){
                // Get the average
                Vector3 pointsPositionsSigma = ConnectionPoints.Aggregate(Vector3.zero, (current, point) => current + point.Position);
                newPointPosition = pointsPositionsSigma / ConnectionPoints.Count;
            } else newPointPosition = Vector3.one; // We don't want it at zero since the gizmo could interfere with the object's own gizmo
            
            ConnectionPoint newPoint = new(){
                Parent = this,
                Position = position ?? newPointPosition
            };
            
            ConnectionPoints.Add(newPoint);
            return newPoint.Guid;
        }

        public WireGenerator Connect(ConnectionPoint start, ConnectionPoint end){
            if (WireConnections.TryGetValue(start, out List<ConnectionPoint> connections)) connections.Add(end);
            else WireConnections.Add(start, new List<ConnectionPoint>{end});
            return CreateWire(start, end);
        }
        
        private WireGenerator CreateWire(ConnectionPoint start, ConnectionPoint end){
            if (!ConnectionPoints.Contains(start)) return null;
            
            GameObject wireObject = new(){
                isStatic = true,
                transform = {
                    position = start.WorldSpacePosition
                }
            };
            wireObject.transform.SetParent(transform);
            
            WireGenerator generator = wireObject.AddComponent<WireGenerator>();
            MeshRenderer meshRenderer = wireObject.GetComponent<MeshRenderer>();
            
            meshRenderer.material = WireMaterial;
            
            generator.Guid = GUID.Create();
            generator.SetupMesh();
            generator.CreateWire(start, end);
            
            return generator;
        }
        
        public void GenerateWires(){
            if (Wires.Count != 0) PurgeWires();
            
            foreach ((ConnectionPoint startPoint, List<ConnectionPoint> endPoints) in WireConnections)
            foreach (ConnectionPoint endPoint in endPoints){
                WireGenerator wireGenerator = CreateWire(startPoint, endPoint);
                Wires.Add(wireGenerator.Guid, wireGenerator);
            }
        }
        
        public void UpdateWires(){
            if (Wires is not { Count: > 0 }){
                GenerateWires();
                return;
            }

            foreach (WireGenerator wire in Wires.Values){
                if (wire.Spline.Count > 0) wire.Spline[0].Position = wire.StartPoint.WorldSpacePosition;
                wire.Spline[^1].Position = wire.EndPoint.WorldSpacePosition;
                wire.UpdateMiddlePoint();
                wire.UpdateWire();
            }
        }
        
        
        public void PurgeWires(){
            foreach (WireGenerator wire in Wires.Values.Where(wire => wire)) DestroyImmediate(wire.gameObject);
            foreach (GameObject wireObject in GetComponentsInChildren<WireGenerator>().Select(generator => generator.gameObject)) DestroyImmediate(wireObject);
            Wires.Clear();
        }
    }
}