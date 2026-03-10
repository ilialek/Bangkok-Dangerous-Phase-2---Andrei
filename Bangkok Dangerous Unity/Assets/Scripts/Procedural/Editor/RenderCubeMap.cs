using UnityEngine;
using UnityEditor;
using System.IO;

namespace ProceduralEditor
{
    public class RenderCubeMap : ScriptableWizard
    {
        public Camera Camera;

        private void OnWizardUpdate()
        {
            helpString = "Select a camera position to render a Cubemap from";
            isValid = (Camera != null);
        }

        private void Render()
        {
            Cubemap cubemap = new Cubemap(512, TextureFormat.ARGB32, false);
            Camera.RenderToCubemap(cubemap);

            string directory = "Cubemaps";
            string fullPath = Path.Combine(Application.dataPath, directory);

            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            string path = $"Assets/{directory}/{Camera.name}.cubemap";

            Cubemap existing = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (existing == null)
                AssetDatabase.CreateAsset(cubemap, path);
            else
            {
                Graphics.CopyTexture(cubemap, existing);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
        }

        private void OnWizardCreate()
        {
        }

        private void OnWizardOtherButton()
        {
            Render();
        }

        [MenuItem("Tools/Cubemap Wizard")]
        static void RenderCubeMapWizard()
        {
            DisplayWizard<RenderCubeMap>("Render Cubemap", "Close", "Render");
        }
    }
}
