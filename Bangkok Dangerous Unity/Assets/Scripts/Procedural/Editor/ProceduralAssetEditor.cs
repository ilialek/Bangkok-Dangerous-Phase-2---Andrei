using UnityEngine;
using UnityEditor;
using Procedural;

namespace ProceduralEditor
{
    [CustomEditor(typeof(ProceduralAsset))]
    public class ProceduralAssetEditor : Editor
    {
        private ProceduralAsset m_TargetAsset;
        private GameObject previewInstance;
        private PreviewRenderUtility previewUtility;
        private Vector2 previewDir = new Vector2(120, -20);
        private float previewDistance = 3.0f;
        private Bounds previewBounds;

        private void OnEnable()
        {
            m_TargetAsset = target as ProceduralAsset;
            InitPreview();
        }

        private void InitPreview()
        {
            if (m_TargetAsset == null || m_TargetAsset.Prefab == null) return;

            CleanupPreview();

            previewUtility = new PreviewRenderUtility(true);
            previewUtility.cameraFieldOfView = 30.0f;

            previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(m_TargetAsset.Prefab);
            previewInstance.name = m_TargetAsset.Prefab.name;
            previewInstance.hideFlags = HideFlags.HideAndDontSave;

            foreach (var renderer in previewInstance.GetComponentsInChildren<Renderer>())
            {
                // Create a copy of a standard material just for preview
                Material previewMat = new Material(Shader.Find("Standard"));
                if (renderer.sharedMaterial != null)
                    previewMat.color = renderer.sharedMaterial.color;

                renderer.sharedMaterial = previewMat;
            }

            previewUtility.AddSingleGO(previewInstance);


            Renderer[] renderers = previewInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                previewBounds = renderers[0].bounds;
                foreach (var r in renderers)
                    previewBounds.Encapsulate(r.bounds);
            }
            else
            {
                previewBounds = new Bounds(previewInstance.transform.position, Vector3.one);
            }

            // Set default preview distance based on object size
            float radius = Mathf.Max(previewBounds.extents.x, Mathf.Max(previewBounds.extents.y, previewBounds.extents.z));
            previewDistance = radius * 2.5f;
        }

        public override void OnInspectorGUI()
        {
            if (m_TargetAsset == null) return;

            string name1 = previewInstance != null ? previewInstance.name : "";
            string name2 = m_TargetAsset.Prefab != null ? m_TargetAsset.Prefab.name : "";
            if (name1 != name2)
            {
                InitPreview();
            }

            if (m_TargetAsset.Prefab != null)
            {
                // Draw preview
                Rect previewRect = GUILayoutUtility.GetRect(400, 300, GUILayout.ExpandWidth(true));
                DrawPreviewPrefab(previewRect);
            }

            serializedObject.Update();
            DrawDefaultInspector();

            ProceduralAsset asset = (ProceduralAsset)target;
            if (GUILayout.Button("Recalculate Bounding Box"))
            {
                if (asset.Prefab != null)
                {
                    asset.BoundingBox = ProceduralAssetUtility.CalculatePrefabBounds(asset.Prefab);
                    EditorUtility.SetDirty(asset);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPreviewPrefab(Rect rect)
        {
            if (previewUtility == null || previewInstance == null) return;

            Event currentEvent = Event.current;

            if (rect.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0)
                {
                    previewDir -= currentEvent.delta * 0.5f;
                    Repaint();
                }

                if (currentEvent.type == EventType.ScrollWheel)
                {
                    previewDistance *= 1.0f + currentEvent.delta.y * 0.05f;
                    Repaint();
                }
            }

            previewUtility.BeginPreview(rect, GUIStyle.none);

            var camera = previewUtility.camera;
            camera.clearFlags = CameraClearFlags.Color;
            camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            Vector3 pivot = previewBounds.center;
            Quaternion rotation = Quaternion.Euler(-previewDir.y, -previewDir.x, 0);
            Vector3 cameraPosition = pivot + rotation * (Vector3.back * previewDistance);

            camera.transform.position = cameraPosition;
            camera.transform.rotation = rotation;
            camera.transform.LookAt(pivot);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            previewUtility.lights[0].intensity = 1.2f;
            previewUtility.lights[0].transform.rotation = Quaternion.Euler(30f, 60f, 0);
            previewUtility.lights[1].intensity = 1.6f;

            previewUtility.Render();

            // Properly draw bounding box using the same camera matrices
            Material lineMat = new Material(Shader.Find("Hidden/Internal-Colored"));
            lineMat.hideFlags = HideFlags.HideAndDontSave;
            lineMat.SetInt("_ZWrite", 0);
            lineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            lineMat.SetPass(0);

            GL.PushMatrix();
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            GL.modelview = camera.worldToCameraMatrix;
            DrawBoundingBox(m_TargetAsset.BoundingBox, m_TargetAsset.Direction);
            GL.PopMatrix();

            Texture tex = previewUtility.EndPreview();
            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, false);
        }
        
        private void DrawBoundingBox(Bounds bounds, Vector3 normal)
        {
            Vector3 extends = bounds.extents;

            Vector3[] corners = new Vector3[8]
            {
                bounds.center + new Vector3(-extends.x, -extends.y, -extends.z),
                bounds.center + new Vector3(extends.x, -extends.y, -extends.z),
                bounds.center + new Vector3(extends.x, -extends.y, extends.z),
                bounds.center + new Vector3(-extends.x, -extends.y, extends.z),
                bounds.center + new Vector3(-extends.x, extends.y, -extends.z),
                bounds.center + new Vector3(extends.x, extends.y, -extends.z),
                bounds.center + new Vector3(extends.x, extends.y, extends.z),
                bounds.center + new Vector3(-extends.x, extends.y, extends.z)
            };

            GL.Begin(GL.LINES);
            GL.Color(Color.yellow);

            // Bottom square
            GL.Vertex(corners[0]); GL.Vertex(corners[1]);
            GL.Vertex(corners[1]); GL.Vertex(corners[2]);
            GL.Vertex(corners[2]); GL.Vertex(corners[3]);
            GL.Vertex(corners[3]); GL.Vertex(corners[0]);

            // Top square
            GL.Vertex(corners[4]); GL.Vertex(corners[5]);
            GL.Vertex(corners[5]); GL.Vertex(corners[6]);
            GL.Vertex(corners[6]); GL.Vertex(corners[7]);
            GL.Vertex(corners[7]); GL.Vertex(corners[4]);

            // Vertical lines
            GL.Vertex(corners[0]); GL.Vertex(corners[4]);
            GL.Vertex(corners[1]); GL.Vertex(corners[5]);
            GL.Vertex(corners[2]); GL.Vertex(corners[6]);
            GL.Vertex(corners[3]); GL.Vertex(corners[7]);

            GL.End();

            Vector3 dir = normal.normalized;
            Vector3 startPoint = bounds.center;
            float maxExtentAlongNormal = Mathf.Abs(Vector3.Dot(dir, Vector3.right)) * extends.x +
                                         Mathf.Abs(Vector3.Dot(dir, Vector3.up)) * extends.y +
                                         Mathf.Abs(Vector3.Dot(dir, Vector3.forward)) * extends.z;

            startPoint += dir * maxExtentAlongNormal;

            GL.Begin(GL.LINES);
            GL.Color(Color.cyan);
            GL.Vertex(startPoint);
            GL.Vertex(startPoint + dir);
            GL.End();
        }

        void OnDisable() => CleanupPreview();

        private void CleanupPreview()
        {
            if (previewInstance != null)
            {
                DestroyImmediate(previewInstance);
            }

            previewInstance = null;

            if (previewUtility != null)
            {
                previewUtility.Cleanup();
            }

            previewUtility = null;
        }
    }
}