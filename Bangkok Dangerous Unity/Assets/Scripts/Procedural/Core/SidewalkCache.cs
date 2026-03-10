using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    [System.Serializable]
    public struct SidewalkCache
    {
        public SidewalkHandle Handle;
        public List<Vector3> Positions;

        public SidewalkCache(SidewalkHandle handle, List<Vector3> positions)
        {
            Handle = handle;
            Positions = positions;
        }
    }
}