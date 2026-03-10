using UnityEngine;
using Utilities;
namespace Procedural
{
    [System.Serializable]
    public struct IntersectionEdge
    {
        public GUID Knot1;
        public GUID Knot2;

        public Vector3 ControlPoint;

        public override bool Equals(object obj)
        {
            return obj is IntersectionEdge other && Equals(other);
        }

        public bool Equals(IntersectionEdge other)
        {
            return Knot1 == other.Knot1 && Knot2 == other.Knot2;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public static bool operator ==(IntersectionEdge left, IntersectionEdge right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IntersectionEdge left, IntersectionEdge right)
        {
            return !(left == right);
        }
    }
}