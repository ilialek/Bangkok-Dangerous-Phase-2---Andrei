using UnityEngine;

// Instructions converted to LineRenderer-based implementation:
// - Create empty GameObject and add a LineRenderer component.
// - Add points and set Loop to close the shape if required.
// - Create another empty GameObject and add this script.
// - Assign "Path Renderer" (the LineRenderer) and "Player" in the inspector.
// - Add sound to the object (unchanged).

namespace Cinemachine
{
    public class AmbientZone : MonoBehaviour
    {
        [Tooltip("Line Renderer that defines the path")]
        public LineRenderer PathRenderer;

        [Tooltip("Character to track")]
        public GameObject Player;

        // Cached path data
        Vector3[] m_Positions = System.Array.Empty<Vector3>();
        float[] m_SegmentLengths = System.Array.Empty<float>();
        float[] m_CumulativeLengths = System.Array.Empty<float>();
        float m_TotalLength;

        // Position along path in world units (distance from start)
        float m_Position;

        void Start()
        {
            RebuildPath();
        }

        void Update()
        {
            if (PathRenderer == null || Player == null)
                return;

            // If someone modified the LineRenderer points at runtime, rebuild cache
            if (PathRenderer.positionCount != m_Positions.Length)
                RebuildPath();

            // Find closest point on the polyline to the player
            if (FindClosestPointOnPath(Player.transform.position, out float distanceAlongPath, out Vector3 closestPoint, out Vector3 tangent))
            {
                SetCartPosition(distanceAlongPath, closestPoint, tangent);
            }

            // Define vectors for the dot product (preserve original attach-on-enter logic)
            Vector3 Sub = transform.position - Player.transform.position;
            Vector3 Spline = transform.right; // after SetCartPosition this will reflect path orientation

            // Attach object to player on enter
            if (Vector3.Dot(Sub, Spline) > 0)
            {
                transform.position = Player.transform.position;
                transform.rotation = Player.transform.rotation;
            }
        }

        void RebuildPath()
        {
            if (PathRenderer == null)
            {
                m_Positions = System.Array.Empty<Vector3>();
                m_SegmentLengths = System.Array.Empty<float>();
                m_CumulativeLengths = System.Array.Empty<float>();
                m_TotalLength = 0f;
                return;
            }

            int count = PathRenderer.positionCount;
            if (count == 0)
            {
                RebuildPath(); // will set empties
                return;
            }

            m_Positions = new Vector3[count];
            PathRenderer.GetPositions(m_Positions);

            bool looped = PathRenderer.loop;
            int segCount = (count - 1) + (looped ? 1 : 0);
            if (segCount <= 0)
            {
                m_SegmentLengths = System.Array.Empty<float>();
                m_CumulativeLengths = System.Array.Empty<float>();
                m_TotalLength = 0f;
                return;
            }

            m_SegmentLengths = new float[segCount];
            m_CumulativeLengths = new float[segCount + 1];
            m_CumulativeLengths[0] = 0f;
            float cum = 0f;

            for (int i = 0; i < segCount; ++i)
            {
                Vector3 a = m_Positions[i];
                Vector3 b = (i == count - 1) ? m_Positions[0] : m_Positions[i + 1];
                float len = Vector3.Distance(a, b);
                m_SegmentLengths[i] = len;
                cum += len;
                m_CumulativeLengths[i + 1] = cum;
            }

            m_TotalLength = cum;
        }

        // Returns true if a closest point was found; outputs distance along path, point and tangent.
        bool FindClosestPointOnPath(Vector3 point, out float distanceAlongPath, out Vector3 closestPoint, out Vector3 tangent)
        {
            distanceAlongPath = 0f;
            closestPoint = transform.position;
            tangent = Vector3.forward;

            if (m_Positions == null || m_Positions.Length < 2 || m_SegmentLengths == null || m_SegmentLengths.Length == 0)
                return false;

            int count = m_Positions.Length;
            bool looped = PathRenderer != null && PathRenderer.loop;
            int segCount = m_SegmentLengths.Length;

            float bestSqr = float.MaxValue;
            int bestSeg = 0;
            float bestT = 0f;
            Vector3 bestPoint = Vector3.zero;

            for (int i = 0; i < segCount; ++i)
            {
                Vector3 a = m_Positions[i];
                Vector3 b = (i == count - 1) ? m_Positions[0] : m_Positions[i + 1];
                Vector3 ab = b - a;
                float abLen2 = ab.sqrMagnitude;
                if (abLen2 <= Mathf.Epsilon)
                {
                    // Degenerate segment
                    float d2 = (point - a).sqrMagnitude;
                    if (d2 < bestSqr)
                    {
                        bestSqr = d2;
                        bestSeg = i;
                        bestT = 0f;
                        bestPoint = a;
                    }
                    continue;
                }

                float t = Vector3.Dot(point - a, ab) / abLen2;
                t = Mathf.Clamp01(t);
                Vector3 proj = a + ab * t;
                float sqr = (point - proj).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    bestSeg = i;
                    bestT = t;
                    bestPoint = proj;
                }
            }

            // Compute distance along path to the closest point
            float segStartDistance = m_CumulativeLengths[bestSeg];
            float segLen = m_SegmentLengths[bestSeg];
            distanceAlongPath = segStartDistance + bestT * segLen;
            closestPoint = bestPoint;

            // Compute tangent: prefer segment direction; if vertex and interior, average adjacent segments for smoothness
            Vector3 aSeg = m_Positions[bestSeg];
            Vector3 bSeg = (bestSeg == count - 1) ? m_Positions[0] : m_Positions[bestSeg + 1];
            Vector3 segDir = (bSeg - aSeg).normalized;

            // If closest point is near a vertex (t ~ 0 or t ~ 1), try to smooth tangent using neighbors (when not degenerate)
            const float vertexThreshold = 1e-3f;
            if (bestT <= vertexThreshold)
            {
                // Use previous segment direction if available
                int prev = (bestSeg - 1 + count) % count;
                if (!looped && bestSeg == 0)
                {
                    tangent = segDir;
                }
                else
                {
                    Vector3 prevA = m_Positions[prev];
                    Vector3 prevB = aSeg;
                    Vector3 prevDir = (prevB - prevA).normalized;
                    tangent = (prevDir + segDir).normalized;
                    if (tangent.sqrMagnitude < Mathf.Epsilon)
                        tangent = segDir;
                }
            }
            else if (bestT >= 1f - vertexThreshold)
            {
                // Use next segment direction if available
                int next = (bestSeg + 1) % count;
                if (!looped && bestSeg == segCount - 1 && next == 0 && count - 1 == bestSeg)
                {
                    tangent = segDir;
                }
                else
                {
                    Vector3 nextA = bSeg;
                    Vector3 nextB = (next == count - 1) ? m_Positions[0] : m_Positions[next + 1];
                    Vector3 nextDir = (nextB - nextA).normalized;
                    tangent = (segDir + nextDir).normalized;
                    if (tangent.sqrMagnitude < Mathf.Epsilon)
                        tangent = segDir;
                }
            }
            else
            {
                tangent = segDir;
            }

            if (tangent.sqrMagnitude < Mathf.Epsilon)
                tangent = Vector3.forward;

            return true;
        }

        // Set object to closest point and orient along tangent
        void SetCartPosition(float distanceAlongPath, Vector3 pointOnPath, Vector3 tangent)
        {
            m_Position = distanceAlongPath;
            transform.position = pointOnPath;
            if (tangent.sqrMagnitude > Mathf.Epsilon)
            {
                // Preserve similar orientation behavior as Cinemachine path: look along tangent
                transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
            }
        }
    }
}