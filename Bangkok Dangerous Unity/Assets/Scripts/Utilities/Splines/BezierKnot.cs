using UnityEngine;

namespace Utilities.Splines
{
    [System.Serializable]
    public class BezierKnot
    {
        public Vector3 Position;
        public Vector3 HandleIn;
        public Vector3 HandleOut;

        public KnotMode Mode;
        public float Progress;

        public GUID Guid;

        public BezierKnot(Vector3 position, KnotMode mode = KnotMode.Auto)
        {
            Position = position;
            HandleIn = position + Vector3.left;
            HandleOut = position + Vector3.right;
            Mode = mode;
            Progress = 0.0f;
            Guid = GUID.Create();
        }

        public BezierKnot(BezierKnot other)
        {
            Position = other.Position;
            HandleIn = other.HandleIn;
            HandleOut = other.HandleOut;
            Mode = other.Mode;
            Progress = other.Progress;
            Guid = GUID.Create();
        }
        
        public static bool operator ==(BezierKnot left, BezierKnot right)
        {
            if (left is null) return right is null;
            if (right is null) return false;

            return left.Position == right.Position && left.HandleIn == right.HandleIn && left.HandleOut == right.HandleOut && left.Mode == right.Mode;
        }
        
        public static bool operator !=(BezierKnot a, BezierKnot b) => !(a == b);
        
        public override bool Equals(object obj)
        {
            if (obj is BezierKnot other) return this == other;
            return false;
        }

        public override int GetHashCode()
        {
            return (Position, HandleIn, HandleOut, Mode).GetHashCode();
        }
    }

    public enum KnotMode
    {
        Linear,
        Bezier,
        Auto
    }
}