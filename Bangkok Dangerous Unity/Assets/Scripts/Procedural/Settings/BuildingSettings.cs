using UnityEngine;

namespace Procedural
{
    [System.Serializable]
    public class BuildingSettings
    {
        public int StoryCount = 10;

        [Range(0, 1)] public float ShrinkChance = 0.2f;
        public float ShrinkMin = 0.1f;
        public float ShrinkMax = 0.3f;
        public float HeightOffset = -0.3f;
        public float MaxWidth = 5.0f;
        public bool MergeWindows = true;

        public void Validate()
        {
            StoryCount = Mathf.Max(1, StoryCount);
        }
    }
}