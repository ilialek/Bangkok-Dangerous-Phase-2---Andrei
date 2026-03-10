using System;
using Utilities;

namespace Procedural
{
    [Serializable]
    public struct RoadAttachment
    {
        public GUID RoadGuid;
        public GUID KnotGuid;

        public RoadAttachment(GUID roadGuid, GUID knotGuid)
        {
            RoadGuid = roadGuid;
            KnotGuid = knotGuid;
        }
        
        public static bool operator ==(RoadAttachment left, RoadAttachment right)
        {
            return left.RoadGuid == right.RoadGuid && left.KnotGuid == right.KnotGuid;
        }
        
        public static bool operator !=(RoadAttachment a, RoadAttachment b) => !(a == b);
        
        public override bool Equals(object obj)
        {
            if (obj is RoadAttachment other)
            {
                return RoadGuid == other.RoadGuid && KnotGuid == other.KnotGuid;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (RoadGuid, KnotGuid).GetHashCode();
        }

        public static readonly RoadAttachment Zero = default;
    }
}