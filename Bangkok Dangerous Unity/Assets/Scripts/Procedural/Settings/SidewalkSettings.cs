using UnityEngine;

namespace Procedural
{
    [System.Serializable]
    public class SidewalkSettings
    {
        public float Width = 0.2f;
        public float Height = 0.02f;

        public void Validate()
        {
            Width = Mathf.Max(0.01f, Width);
        }
    }
}