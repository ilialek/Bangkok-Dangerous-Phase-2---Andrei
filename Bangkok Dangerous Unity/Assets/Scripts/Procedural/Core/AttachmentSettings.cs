using System;
using UnityEngine;

namespace Procedural
{
    [CreateAssetMenu(fileName = "AttachmentSettings", menuName = "Procedural/Attachment Settings")]
    public class AttachmentSettings : ScriptableObject
    {
        public SurfaceType Target = SurfaceType.None;
        public bool AllowOverlap = false;
        [Tooltip("Distribution from edge (-1.0) to center (1.0)"), Range(-1.0f, 1.0f)] public float Weight = 0.0f;
        [Tooltip("Spawn chance per square meter"), Min(0.0f)] public float Density = 0.1f;
    }

    [Flags]
    public enum SurfaceType
    {
        None = 0,
        Wall = 1 << 0,
        Front = 1 << 1,
        Roof = 1 << 2,
    }
}