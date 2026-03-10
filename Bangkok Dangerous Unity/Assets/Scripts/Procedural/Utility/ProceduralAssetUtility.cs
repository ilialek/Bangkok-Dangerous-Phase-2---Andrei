using UnityEngine;

namespace Procedural
{
    public static class ProceduralAssetUtility
    {
        public static Bounds CalculatePrefabBounds(GameObject target)
        {
            if (target == null)
            {
                Debug.LogWarning("Could not generate boounds for invalid object");
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            Bounds combinedBounds;

            if (renderers.Length > 0)
            {
                combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
            }
            else
            {
                Debug.LogWarning($"Could not generate boounds for an object without a renderer ({target.name})");
                combinedBounds = new Bounds(Vector3.zero, Vector3.one);
            }

            return combinedBounds;
        }
    }
}