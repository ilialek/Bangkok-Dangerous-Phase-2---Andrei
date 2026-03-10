using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utilities.Splines
{
    [Serializable]
    public class Spline
    {
        [SerializeField] private List<BezierKnot> m_Knots = new List<BezierKnot>();
        [SerializeField, HideInInspector] private List<float> m_CurveLengths = new List<float>();
        [SerializeField, HideInInspector] private float m_TotalLength;
        [SerializeField, HideInInspector] private bool m_UpdateRequired;
        public Action OnChange;

        public int Count => m_Knots.Count;
        public float Length => m_TotalLength;

        public Spline() { }
        
        public Spline(Spline other)
        {
            if (other == null) return;
            
            m_Knots = new List<BezierKnot>(other.m_Knots.Count);
            foreach (BezierKnot knot in other.m_Knots)
            {
                m_Knots.Add(new BezierKnot(knot));
            }
            
            m_CurveLengths = new List<float>(other.m_CurveLengths);
            m_TotalLength = other.m_TotalLength;
            m_UpdateRequired = other.m_UpdateRequired;
            OnChange = null;
        }
        
        /// <summary>
        /// Add a new knot at the end of the spline
        /// </summary>
        public BezierKnot Add(Vector3 position, KnotMode mode = KnotMode.Auto)
        {
            BezierKnot knot = new BezierKnot(position, mode);
            m_Knots.Add(knot);

            if (Count > 1)
            {
                BezierCurve curve = new BezierCurve(m_Knots[Count - 2], m_Knots[Count - 1]);
                float curveLength = curve.GetLength();
                m_CurveLengths.Add(curveLength);
                m_TotalLength += curveLength;
            }

            CalculateKnotProgress();
            return knot;
        }

        /// <summary>
        /// Inserts the knot at the index
        /// </summary>
        public BezierKnot Insert(int index, Vector3 position, KnotMode mode = KnotMode.Auto)
        {
            if (index < 0 || index > m_Knots.Count) throw new ArgumentOutOfRangeException(nameof(m_Knots), "Index to insert bezier knot into spline is invalid");

            BezierKnot knot = new BezierKnot(position, mode);
            m_Knots.Insert(index, knot);
            Recalculate();
            return knot;
        }

        /// <summary>
        /// Removes the knot at the index
        /// </summary>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= m_Knots.Count) return;

            m_Knots.RemoveAt(index);
            Recalculate();
        }

        public void Clear()
        {
            m_Knots.Clear();
            m_CurveLengths.Clear();
            m_TotalLength = 0.0f;
            m_UpdateRequired = true;
        }

        public BezierKnot this[int index]
        {
            get { return m_Knots[index]; }
            set { m_Knots[index] = value; }
        }

        public IEnumerator<BezierKnot> GetEnumerator() => m_Knots.GetEnumerator();

        public int GetKnotIndex(BezierKnot knot)
        {
            return m_Knots.IndexOf(knot);
        }

        public void Recalculate()
        {
            CalculateLengths();
            CalculateKnotProgress();
        }

        public void CalculateLengths()
        {
            m_CurveLengths.Clear();
            m_TotalLength = 0.0f;

            for (int i = 0; i < Count - 1; i++)
            {
                BezierCurve curve = new BezierCurve(m_Knots[i], m_Knots[i + 1]);
                float curveLength = curve.GetLength();
                m_CurveLengths.Add(curveLength);
                m_TotalLength += curveLength;
            }

            m_UpdateRequired = true;
        }

        /// <summary>
        /// Assigns progress to each knot
        /// </summary>
        public void CalculateKnotProgress()
        {
            if (Count == 0) return;
            
            float totalProgress = 0.0f;
            
            for (int i = 0; i < Count; i++)
            {
                if (i == 0) m_Knots[i].Progress = 0.0f;
                else if (i == Count - 1) m_Knots[i].Progress = 1.0f;
                else
                {
                    totalProgress += m_CurveLengths[i - 1];
                    m_Knots[i].Progress = m_TotalLength > 0.0f ? totalProgress / m_TotalLength : 0.0f;
                }
            }
            
            m_UpdateRequired = true;
        }

        public void CalculateLengthsAt(int knotIndex, bool computeTotalLength)
        {
            if (Count < 2)
            {
                m_CurveLengths.Clear();
                m_TotalLength = 0f;
                return;
            }

            if (m_CurveLengths.Count != Count - 1)
            {
                CalculateLengths();
                return;
            }

            if (knotIndex > 0)
            {
                BezierCurve curve = new BezierCurve(m_Knots[knotIndex - 1], m_Knots[knotIndex]);
                m_CurveLengths[knotIndex - 1] = curve.GetLength();
            }

            if (knotIndex < Count - 1)
            {
                BezierCurve curve = new BezierCurve(m_Knots[knotIndex], m_Knots[knotIndex + 1]);
                m_CurveLengths[knotIndex] = curve.GetLength();
            }

            // Recalculate total spline length from cached values
            if (computeTotalLength)
            {
                m_TotalLength = 0.0f;

                foreach (float length in m_CurveLengths)
                {
                    m_TotalLength += length;
                }
            }

            CalculateKnotProgress();
            m_UpdateRequired = true;
        }

        public void CheckForUpdate(){
            if (!m_UpdateRequired) return;
            OnChange?.Invoke();
            m_UpdateRequired = false;
        }
        
        public void UpdateHandles()
        {
            if (Count < 2) return;

            for (int i = 0; i < Count; i++)
            {
                BezierKnot knot = this[i];
                Vector3 prev = i > 0 ? this[i - 1].Position : knot.Position;
                Vector3 next = i < Count - 1 ? this[i + 1].Position : knot.Position;

                ComputeAutoHandles(knot.Mode, prev, knot.Position, next, knot.HandleIn, knot.HandleOut, out knot.HandleIn, out knot.HandleOut);
                this[i] = knot;
            }

            Recalculate();
        }

        public bool Evaluate(float progress, out Vector3 position, out Vector3 tangent, out Vector3 up)
        {
            if (Count < 2)
            {
                position = Vector3.zero;
                tangent = Vector3.forward;
                up = Vector3.up;
                return false;
            }

            int curveIndex = SplineToCurve(progress, out float curveProgress);
            BezierCurve curve = GetCurve(curveIndex);

            position = EvaluatePosition(curve, curveProgress);
            tangent = EvaluateTangent(curve, curveProgress);
            up = EvaluateUp(curveIndex, curveProgress);
            return true;
        }
        
        public bool EvaluateFromPosition(Vector3 approximatedPosition, out Vector3 position, out Vector3 tangent, out Vector3 up, out float progress)
        {
            if (Count < 2)
            {
                position = Vector3.zero;
                tangent = Vector3.forward;
                up = Vector3.up;
                progress = 0.0f;
                return false;
            }

            // Find nearest curve progress
            position = GetClosestPointOnSpline(approximatedPosition, out _, out int curveIndex, out float curveProgress);
            
            // Evaluate the curve
            BezierCurve curve = GetCurve(curveIndex);
            
            tangent = EvaluateTangent(curve, curveProgress);
            up = EvaluateUp(curveIndex, curveProgress);
            progress = GetProgressFromCurve(curveIndex, curveProgress);
            return true;
        }

        public BezierCurve GetCurve(int curveIndex)
        {
            return new BezierCurve(m_Knots[curveIndex], m_Knots[curveIndex + 1]);
        }

        private Vector3 EvaluatePosition(BezierCurve curve, float progress)
        {
            progress = Mathf.Clamp01(progress);
            float t2 = progress * progress;
            float t3 = t2 * progress;
            Vector3 position =
                curve.P0 * (-1f * t3 + 3f * t2 - 3f * progress + 1f) +
                curve.P1 * (3f * t3 - 6f * t2 + 3f * progress) +
                curve.P2 * (-3f * t3 + 3f * t2) +
                curve.P3 * (t3);

            return position;
        }

        private Vector3 EvaluateTangent(BezierCurve curve, float progress)
        {
            progress = Mathf.Clamp01(progress);
            
            // Handle end points
            if (Mathf.Approximately(progress, 0.0f)) progress = 0.01f;
            if (Mathf.Approximately(progress, 1.0f)) progress = 0.99f;
            
            float t2 = progress * progress;

            Vector3 tangent =
                curve.P0 * (-3f * t2 + 6f * progress - 3f) +
                curve.P1 * (9f * t2 - 12f * progress + 3f) +
                curve.P2 * (-9f * t2 + 6f * progress) +
                curve.P3 * (3f * t2);

            return tangent.normalized;
        }

        private Vector3 EvaluateUp(int curveIndex, float progress)
        {
            return Vector3.up;
        }

        /// <summary>
        /// Calculates curve that is at t
        /// </summary>
        private int SplineToCurve(float progress, out float curveProgress)
        {
            if (Count <= 1)
            {
                curveProgress = 0.0f;
                return 0;
            }

            progress = Mathf.Clamp(progress, 0.0f, 1.0f);
            float length = progress * m_TotalLength;

            float start = 0.0f;
            for (int i = 0; i < Count - 1; i++)
            {
                float curveLength = m_CurveLengths[i];

                if (length <= (start + curveLength))
                {
                    curveProgress = (length - start) / curveLength;
                    return i;
                }

                start += curveLength;
            }

            curveProgress = 1.0f;
            return Count - 2;
        }

        private float GetProgressFromCurve(int curveIndex, float curveProgress)
        {
            float accumulated = 0.0f;
            for (int i = 0; i < curveIndex; i++)
            {
                accumulated += m_CurveLengths[i];
            }

            return (accumulated + curveProgress * m_CurveLengths[curveIndex]) / m_TotalLength;
        }

        public Vector3 GetClosestPointOnSpline(Vector3 point, out float distance, out int curveIndex, out float curveProgress)
        {
            curveIndex = -1;
            curveProgress = 0.0f;
            
            if (Count < 2)
            {
                distance = Mathf.Infinity;
                return Vector3.zero;
            }
                
            Vector3 closestPoint = Vector3.zero;
            distance = float.MaxValue;

            for (int i = 0; i < Count - 1; i++)
            {
                BezierCurve curve = GetCurve(i);

                int curveSamples = Mathf.CeilToInt(4.0f * m_CurveLengths[i]);

                for (int j = 0; j <= curveSamples; j++)
                {
                    float t = j / (float)curveSamples;
                    Vector3 sample = EvaluatePosition(curve, t);

                    float newDistance = (sample - point).sqrMagnitude;
                    if (!(newDistance < distance)) continue;
                    distance = newDistance;
                    closestPoint = sample;
                    curveIndex = i;
                    curveProgress = t;
                }
            }

            distance = Mathf.Sqrt(distance);
            return closestPoint;
        }

        public void GetRoadPositions(float step, float width, out Vector3 position1, out Vector3 position2, out Vector3 forward)
        {
            Evaluate(step, out Vector3 position, out forward, out Vector3 up);
            Vector3 right = Vector3.Cross(forward, up).normalized;
            position1 = right * width + position;
            position2 = -right * width + position;
        }
        
        public static void ComputeAutoHandles(KnotMode mode, Vector3 previous, Vector3 current, Vector3 next, Vector3 lastHandleIn, Vector3 lastHandleOut, out Vector3 handleIn, out Vector3 handleOut)
        {
            switch (mode)
            {
                case KnotMode.Linear:
                    handleIn = current;
                    handleOut = current;
                    break;

                case KnotMode.Auto:
                    // Tangent direction from Catmull�Rom
                    Vector3 tangent = (next - previous).normalized;

                    // Scale handle length based on local distances
                    float d0 = Vector3.Distance(previous, current);
                    float d1 = Vector3.Distance(current, next);
                    float scale = Mathf.Min(d0, d1) / 3.0f;

                    handleIn = current - tangent * scale;
                    handleOut = current + tangent * scale;
                    break;

                case KnotMode.Bezier:
                default:
                    handleIn = lastHandleIn;
                    handleOut = lastHandleOut;
                    break;
            }
        }
    }
}