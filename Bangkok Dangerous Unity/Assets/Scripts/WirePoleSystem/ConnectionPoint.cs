using System;
using UnityEngine;
using Utilities;

namespace WirePoleSystem{
    [Serializable]
    public class ConnectionPoint{
        public WireConnector Parent;
        
        public Vector3 Position;

        public Vector3 WorldSpacePosition => Parent.transform.TransformPoint(Position);
        
        [SerializeField]
        private GUID PointGuid = GUID.Create();

        public GUID Guid => PointGuid;

        public override string ToString(){
            return $"ConnectionPoint {Guid}";
        }
    }
}