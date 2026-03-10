using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    [CreateAssetMenu(fileName = "FacadeSemantic", menuName = "Procedural/Facade Semantic")]
    public class FacadeSemantic : ScriptableObject
    {
        [Header("Base")]

        [Tooltip("Height of the facade, needs to be the same for facades that can spawn next to each other"), Min(0.001f)]
        public float StoryHeight;

        [Tooltip("The thickness of a wall. When value is 0, no inside walls are created"), Min(0.0f)]
        public float WallThickness = 0.0f;

        [Header("Covers")]

        [Tooltip("Size of the gap between the cover and the edge of the facade")]
        public Vector2 EdgeGap = new Vector2(0.3f, 0.3f);

        [Tooltip("Size of the gap between multiple instances of covers"), Min(0.0f)]
        public float CoverGap = 0.3f;

        [Tooltip("The change of a window spawning"), Range(0, 1)]
        public float SpawnChance = 1.0f;


        [Header("Assets")]

        public List<Material> WallMaterials = new List<Material>();
        public List<ProceduralAsset> Covers = new List<ProceduralAsset>();


        public void Validate()
        {
            EdgeGap = Vector2.Max(EdgeGap, Vector2.zero);

            for (int i = WallMaterials.Count - 1; i >= 0; i--)
            {
                if (WallMaterials[i] == null)
                {
                    WallMaterials.RemoveAt(i);
                }
            }

            for (int i = Covers.Count - 1; i >= 0; i--)
            {
                if (Covers[i] == null)
                {
                    Covers.RemoveAt(i);
                }
            }
        }
    }
}