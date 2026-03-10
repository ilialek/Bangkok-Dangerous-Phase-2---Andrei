using UnityEngine;
using UnityEditor.Callbacks;
using Procedural;

namespace ProceduralEditor
{
    public static class PostProcessingSceneUtilities
    {
        [PostProcessSceneAttribute(2)]
        public static void OnPostprocessScene()
        {
            ProceduralMesh[] procedurals = Object.FindObjectsByType<ProceduralMesh>(FindObjectsSortMode.None);

            for (int i = procedurals.Length - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(procedurals[i]);
            }
        }
    }
}