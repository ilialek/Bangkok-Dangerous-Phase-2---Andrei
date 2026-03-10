using UnityEngine;

namespace Procedural
{
    [System.Serializable]
    public class IntersectionSettings
    {
        [Min(2)] public int Resolution = 10;
        [Min(0.0f)] public float DefaultRoadWidth = 0.8f;
    }
}