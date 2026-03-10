using System.Collections.Generic;
using Procedural;
using UnityEditor;
using UnityEngine;

namespace ProceduralEditor
{
    [CustomEditor(typeof(BlockArea))]
    public class BlockAreaEditor : ProceduralMeshEditor<BlockArea>
    {
        private bool m_AddBuilding;
        private bool m_ReselectArea;
        private Vector3 m_PreviewPos;

        private BuildingSemantic m_SemanticsPreset;

        protected override void Setup()
        {
            EditorPrefs.SetBool("AddBuilding", false);

            string savedPath = EditorPrefs.GetString("SemanticPresetPath", "");
            if (!string.IsNullOrEmpty(savedPath))
            {
                m_SemanticsPreset = AssetDatabase.LoadAssetAtPath<BuildingSemantic>(savedPath);
            }
        }

        protected override void DrawWindow()
        {
            if (GUILayout.Button(EditorPrefs.GetBool("AddBuilding") ? "Cancel" : "Add Building"))
            {
                m_AddBuilding = !m_AddBuilding;
                EditorPrefs.SetBool("AddBuilding", m_AddBuilding);
            }

            if (EditorPrefs.GetBool("AddBuilding"))
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Semantics", GUILayout.Width(63));
                m_SemanticsPreset = (BuildingSemantic)EditorGUILayout.ObjectField(m_SemanticsPreset, typeof(BuildingSemantic), false, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    string path = AssetDatabase.GetAssetPath(m_SemanticsPreset);
                    EditorPrefs.SetString("SemanticPresetPath", path);
                }
            }

            if (GUILayout.Button(EditorPrefs.GetBool("ReselectArea") ? "Cancel" : "Select New Area"))
            {
                m_ReselectArea = !m_ReselectArea;
                EditorPrefs.SetBool("ReselectArea", m_ReselectArea);

                if (m_ReselectArea)
                {
                    EditorPrefs.SetString("ReselectAreaTarget", m_TargetProcedural.Guid.ToString());
                    DeselectEditor();
                }
            }

            if (GUILayout.Button("Remove Block Area"))
            {
                DeselectEditor();
                Undo.DestroyObjectImmediate(m_TargetProcedural.gameObject);
            }
        }

        protected override void UpdateEditor(Event currentEvent)
        {
            // Handle building edit
            if (EditorPrefs.GetBool("AddBuilding"))
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    // Preview
                    m_PreviewPos = ray.GetPoint(enter);
                    //m_PreviewPos.y = 0.0f;

                    EditorTools.DrawPreviewKnot(m_PreviewPos);

                    if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
                    {
                        if (m_ProceduralManager.AddBuilding(m_TargetProcedural, out GameObject buildingObject))
                        {
                            Undo.RecordObject(buildingObject, "Add building");

                            EditorPrefs.SetBool("CreateBuilding", true);
                            EditorPrefs.SetFloat("BuildingPositionX", m_PreviewPos.x);
                            EditorPrefs.SetFloat("BuildingPositionZ", m_PreviewPos.z);
                            Selection.activeGameObject = buildingObject;

                            if (buildingObject.TryGetComponent(out Building building))
                            {
                                // Set semantics
                                building.Semantics = m_SemanticsPreset;
                            }

                            EditorUtility.SetDirty(buildingObject);
                        }

                        currentEvent.Use();
                    }
                }

                SceneView.RepaintAll();
            }

            // Draw area edges
            List<Vector3> vertices = m_TargetProcedural.CachedVertices;
            for (int i = 0; i < vertices.Count; i++)
            {
                EditorTools.DrawStaticKnot(vertices[i]);

                // Draw index label
                //Vector3 labelPos = vertices[i] + Vector3.up * 0.2f;
                //GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
                //{
                //    normal = { textColor = Color.white },
                //    fontSize = 11,
                //    alignment = TextAnchor.MiddleCenter
                //};
                //Handles.Label(labelPos, i.ToString(), style);
            }

            // Draw lots
            int lotCount = 0;
            foreach (var buildingEntry in m_TargetProcedural.Buildings)
            {
                Building building = buildingEntry.Value;

                if (building.Lot.Count == 0) continue;

                Color lotColor = Color.HSVToRGB((lotCount++ * 0.61803398875f) % 1.0f, 0.6f, 0.95f);

                // Draw transparent polygon
                lotColor.a = 0.4f;
                Handles.color = lotColor;
                Handles.DrawAAConvexPolygon(building.Lot.ToArray());

                // Draw edges
                for (int j = 0; j < building.Lot.Count; j++)
                {
                    Handles.DrawLine(building.Lot[j], building.Lot[(j + 1) % building.Lot.Count]);
                }
            }
        }
    }
}