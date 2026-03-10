using UnityEngine;

namespace Procedural
{
    [System.Serializable]
    public struct OrientedBounds
    {
        public Bounds Bounds;
        public Quaternion Rotation;
        public Bounds WorldBounds;

        public OrientedBounds(Bounds bounds, Quaternion rotation)
        {
            Bounds = bounds;
            Rotation = rotation;
            WorldBounds = CalculateWorldBounds(bounds, rotation);
        }

        private static Bounds CalculateWorldBounds(Bounds localBounds, Quaternion rotation)
        {
            Vector3 extents = localBounds.extents;
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 worldExtents = new Vector3(
                Mathf.Abs(right.x) * extents.x + Mathf.Abs(up.x) * extents.y + Mathf.Abs(forward.x) * extents.z,
                Mathf.Abs(right.y) * extents.x + Mathf.Abs(up.y) * extents.y + Mathf.Abs(forward.y) * extents.z,
                Mathf.Abs(right.z) * extents.x + Mathf.Abs(up.z) * extents.y + Mathf.Abs(forward.z) * extents.z
            );

            return new Bounds(localBounds.center, worldExtents * 2.0f);
        }

        public bool Overlaps(OrientedBounds other)
        {
            // First check axis aligned bounding box
            if (!WorldBounds.Intersects(other.WorldBounds)) return false;

            Vector3[] axesA = { Rotation * Vector3.right, Rotation * Vector3.up, Rotation * Vector3.forward };
            Vector3[] axesB = { other.Rotation * Vector3.right, other.Rotation * Vector3.up, other.Rotation * Vector3.forward };

            // Check potential separating axes
            for (int i = 0; i < 3; i++)
            {
                if (IsSeparated(Bounds.center, Bounds.size, axesA, other.Bounds.center, other.Bounds.size, axesB, axesA[i])) return false;
                
                if (IsSeparated(Bounds.center, Bounds.size, axesA, other.Bounds.center, other.Bounds.size, axesB, axesB[i])) return false;
            }

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Vector3 axis = Vector3.Cross(axesA[i], axesB[j]);

                    if (axis.sqrMagnitude > 1e-6f && IsSeparated(Bounds.center, Bounds.size, axesA, other.Bounds.center, other.Bounds.size, axesB, axis.normalized)) return false;
                }
            }

             // No separating axis found
            return true;
        }

        private static bool IsSeparated(Vector3 cA, Vector3 sA, Vector3[] aA, Vector3 cB, Vector3 sB, Vector3[] aB, Vector3 axis)
        {
            float projectionA = ProjectExtent(sA, aA, axis);
            float projectionB = ProjectExtent(sB, aB, axis);
            float distance = Mathf.Abs(Vector3.Dot(cB - cA, axis));
            return distance > (projectionA + projectionB);
        }

        private static float ProjectExtent(Vector3 size, Vector3[] axes, Vector3 axis)
        {
            return
                Mathf.Abs(Vector3.Dot(axes[0], axis)) * size.x * 0.5f +
                Mathf.Abs(Vector3.Dot(axes[1], axis)) * size.y * 0.5f +
                Mathf.Abs(Vector3.Dot(axes[2], axis)) * size.z * 0.5f;
        }
    }
}