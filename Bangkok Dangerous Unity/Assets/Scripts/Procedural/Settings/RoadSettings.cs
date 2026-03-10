using UnityEngine;

namespace Procedural
{
    [System.Serializable]
    public class RoadSettings
    {
        public float Width = 0.4f;
        public float MaxRoadTilling = 1.0f;
        public int MaxRoadDivisions = 7;
        public float CurveScale = 10.0f;

        public void Validate()
        {
            Width = Mathf.Max(0.01f, Width);
            MaxRoadTilling = Mathf.Max(0.01f, MaxRoadTilling);
            MaxRoadDivisions = Mathf.Max(1, MaxRoadDivisions);
            CurveScale = Mathf.Max(0f, CurveScale);
        }
    }
}