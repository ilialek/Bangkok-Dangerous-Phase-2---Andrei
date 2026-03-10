using UnityEngine;

namespace Procedural
{
    [System.Serializable]
    public struct SplinePosition
    {
        public Vector3 Position;
        public float Progress;

        public SplinePosition (Vector3 position, float progress)
        {
            Position = position;
            Progress = progress;
        }
    }
}