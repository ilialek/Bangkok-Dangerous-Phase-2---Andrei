using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Procedural;
using Utilities.Splines;
using WirePoleSystem;
using GUID = Utilities.GUID;

namespace ProceduralEditor
{
    [CustomEditor(typeof(ProceduralManager))]
    public class ProceduralManagerEditor : Editor
    {
        private enum PlacingMode
        {
            None,
            Road,
            Intersection,
            Area,
            Decoration,
            Wire
        }

        private enum DecorationMode
        {
            WirePole,
            Prefab
        }
        
        private string[] m_DecorationModeOptions = {"Wirepole", "Prefab"};
        
        private ProceduralManager m_TargetProceduralManager;
        
        // Window
        private static int m_DefaultWidth = 200;
        private Rect m_WindowRect = new(100, 100, m_DefaultWidth, 100);
        
        private static string m_DefaultWindowTitle = "Procedural Manager";
        private string m_WindowTitle = m_DefaultWindowTitle;
        
        // Preview and mode
        private PlacingMode m_CurrentPlacingMode = PlacingMode.None;
        private Vector3 m_PreviewPos;
        private Vector2 m_MousePosition = Vector2.zero;
        
        // Decoration 
        private DecorationMode m_CurrentDecorationMode = DecorationMode.WirePole;
        private int m_DecorationPrefabIndex;
        private Mesh m_DecorationPreviewMesh;
        private Vector3 m_DecorationPlacementOffset = Vector3.zero;
        private Quaternion m_DecorationRotationOffset = Quaternion.identity;
        
        // Wirepole
        private bool m_ConnectToLastPole = true;
        private bool m_RandomPolePrefab;
        private int m_PolePrefabIndex;

        private const float ConnectionPointHitboxModifier = 80;

        private bool m_ReselectArea;

        private List<RoadAttachment> m_SelectedIntersectionPoints = new();
        private List<SidewalkHandle> m_SelectedSidewalks = new();
        private List<ConnectionPoint> m_SelectedConnectionPoints = new();

        private SceneView m_SceneView;

        public override bool HasPreviewGUI() => false;

        private void OnEnable()
        {
            m_TargetProceduralManager = target as ProceduralManager;
            SceneView.duringSceneGui += OnSceneGUI;

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView)
            {
                m_SceneView = sceneView;
                UpdateWindowRect();
            }

            m_TargetProceduralManager?.Selected();
            EditorTools.SetTransformHandleVisibility(true);
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            m_TargetProceduralManager?.Deselected();
            EditorTools.SetTransformHandleVisibility(false);
        }

        private void UpdateWindowRect()
        {
            Rect sceneViewRect = m_SceneView.position;
            m_WindowRect.x = sceneViewRect.width - m_WindowRect.width - 5;
            m_WindowRect.y = sceneViewRect.height - m_WindowRect.height - 30;
        }

        private void UpdateRectHeight()
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            m_WindowRect.height = lastRect.yMax + 8.0f;
        }

        private void UpdateRectWidth()
        {
            
        }

        private void DrawWindow(int id)
        {
            const float buttonSize = 20.0f;
            if (GUI.Button(new Rect(m_WindowRect.width - buttonSize - 4, 4, buttonSize, buttonSize), "X")) DeselectEditor();

            if (m_CurrentPlacingMode is PlacingMode.Road or PlacingMode.Decoration && GUILayout.Button("Cancel"))
            {
                m_CurrentPlacingMode = PlacingMode.None;
                m_WindowTitle = m_DefaultWindowTitle;
                m_WindowRect.width = m_DefaultWidth;
            }

            if (m_CurrentPlacingMode == PlacingMode.None)
            {
                if (GUILayout.Button("Add Road")) m_CurrentPlacingMode = PlacingMode.Road;
                if (GUILayout.Button("Add decoration")) m_CurrentPlacingMode = PlacingMode.Decoration;
                if (GUILayout.Button("Connect wire")) m_CurrentPlacingMode = PlacingMode.Wire;
                
                if (GUILayout.Button("Add Intersection"))
                {
                    m_CurrentPlacingMode = PlacingMode.Intersection;
                    ClearSelection();
                }
                
                if (GUILayout.Button("Add Area"))
                {
                    m_CurrentPlacingMode = PlacingMode.Area;
                    m_ReselectArea = false;
                    ClearSelection();
                }
            }

            if (m_CurrentPlacingMode == PlacingMode.Decoration)
            {
                m_WindowTitle = "Procedural Decoration Placer";
                m_WindowRect.width = 300;
                UpdateDecorationMeshPreview();
                ShowDecorationPlacingWindow();
            }

            if (m_CurrentPlacingMode == PlacingMode.Intersection)
            {
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Connect", GUILayout.Width(m_WindowRect.width / 2.0f - 6)))
                {
                    ConnectIntersection();
                    ClearSelection();
                }

                if (GUILayout.Button("Cancel", GUILayout.Width(m_WindowRect.width / 2.0f - 6)))
                {
                    m_CurrentPlacingMode = PlacingMode.None;
                    ClearSelection();
                }

                GUILayout.EndHorizontal();
            }

            if (m_CurrentPlacingMode == PlacingMode.Area)
            {
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Connect", GUILayout.Width(m_WindowRect.width / 2.0f - 6)))
                {
                    ConnectArea();
                    ClearSelection();
                }

                if (GUILayout.Button("Cancel", GUILayout.Width(m_WindowRect.width / 2.0f - 6)))
                {
                    m_ReselectArea = false;
                    m_CurrentPlacingMode = PlacingMode.None;
                    ClearSelection();
                }

                GUILayout.EndHorizontal();
            }

            if (m_CurrentPlacingMode == PlacingMode.Wire && GUILayout.Button("Cancel"))
            {
                m_CurrentPlacingMode = PlacingMode.None;
                m_SelectedConnectionPoints.Clear();
            }

            if (Event.current.type != EventType.Repaint) return;
            UpdateRectHeight();
        }

        private void ShowDecorationPlacingWindow()
        {
            if (!m_TargetProceduralManager.DecorationCollection)
            {
                GUILayout.Label("No decoration collection assigned in Procedural\nManager.");
                return;
            }
            
            EditorGUI.BeginChangeCheck();
            m_CurrentDecorationMode = (DecorationMode) EditorGUILayout.Popup("Decoration mode", (int) m_CurrentDecorationMode, m_DecorationModeOptions);
            if(EditorGUI.EndChangeCheck()) UpdateDecorationMeshPreview();

            m_DecorationPlacementOffset = EditorGUILayout.Vector3Field("Position offset", m_DecorationPlacementOffset);
            m_DecorationRotationOffset = Quaternion.Euler
            (
                EditorGUILayout.Vector3Field
                (
                    "Rotation offset",
                    m_DecorationRotationOffset.eulerAngles
                )
            );
            
            GUILayout.Space(18);
            
            switch (m_CurrentDecorationMode)
            {
                case DecorationMode.WirePole:
                    GUILayout.Label("Wirepole settings");
                    m_ConnectToLastPole = GUILayout.Toggle(m_ConnectToLastPole, "Automatically connect");
                    m_RandomPolePrefab = GUILayout.Toggle(m_RandomPolePrefab, "Use random pole prefab");

                    if (m_RandomPolePrefab) break;
                    
                    string[] polePrefabNames = m_TargetProceduralManager.DecorationCollection.WirePoles
                        .Select(wirepolePrefab => wirepolePrefab.ToString()).ToArray();
                    
                    EditorGUI.BeginChangeCheck();
                    m_PolePrefabIndex = EditorGUILayout.Popup("Pole prefab", m_PolePrefabIndex, polePrefabNames);
                    if(EditorGUI.EndChangeCheck()) UpdateDecorationMeshPreview();
                    break;
                case DecorationMode.Prefab:
                    GUILayout.Label("Prefab settings");
                    
                    string[] prefabOptions = m_TargetProceduralManager.DecorationCollection.DecorationPrefabs
                        .Select(decorationPrefab => decorationPrefab.ToString()).ToArray();
            
                    EditorGUI.BeginChangeCheck();
                    m_DecorationPrefabIndex = EditorGUILayout.Popup("Prefab", m_DecorationPrefabIndex, prefabOptions);
                    if(EditorGUI.EndChangeCheck()) UpdateDecorationMeshPreview();
                    break;
                default: return;
            }
        }

        private void UpdateDecorationMeshPreview()
        {
            ProceduralAsset previewAsset;
            switch (m_CurrentDecorationMode)
            {
                case DecorationMode.WirePole:
                    previewAsset = m_TargetProceduralManager.DecorationCollection.WirePoles[0];
                    break;
                case DecorationMode.Prefab:
                    previewAsset =
                        m_TargetProceduralManager.DecorationCollection.DecorationPrefabs[m_DecorationPrefabIndex];
                    break;
                default: return;
            }
            if (!previewAsset) return;
             m_DecorationPreviewMesh = previewAsset.Prefab.GetComponent<MeshFilter>().sharedMesh;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            Event currentEvent = Event.current;

            switch (m_CurrentPlacingMode)
            {
                case PlacingMode.Road:
                    DoRoadPlacement(currentEvent);
                    break;
                case PlacingMode.Intersection:
                    DoIntersectionPlacement(currentEvent);
                    break;
                case PlacingMode.Area:
                    DoAreaCreation(currentEvent);
                    break;
                case PlacingMode.Decoration:
                    DoDecorationPlacement(currentEvent);
                    break;
                case PlacingMode.Wire:
                    DoWireConnection(Event.current);
                    break;
            }

            if (EditorPrefs.GetBool("ReselectArea"))
            {
                GUID areaGuid = new(EditorPrefs.GetString("ReselectAreaTarget"));

                if (m_TargetProceduralManager.BlockAreas.TryGetValue(areaGuid, out BlockArea blockArea))
                {
                    m_SelectedSidewalks.Clear();
                    m_SelectedSidewalks = new List<SidewalkHandle>(blockArea.Guids);

                    m_CurrentPlacingMode = PlacingMode.Area;
                    m_ReselectArea = true;
                }

                EditorPrefs.SetBool("ReselectArea", false);
            }

            // Handle deselect
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape)
            {
                DeselectEditor();
                currentEvent.Use();
            }

            // Draw window
            Handles.BeginGUI();
            m_WindowRect = GUILayout.Window(123457, m_WindowRect, DrawWindow, m_WindowTitle, EditorTools.GetWindowStyle());
            UpdateWindowRect();
            Handles.EndGUI();
        }

        private void DoRoadPlacement(Event currentEvent)
        {
            // Placing new roads
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            Plane groundPlane = new(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float enter))
            {
                m_PreviewPos = ray.GetPoint(enter);
                m_PreviewPos.y = 0.0f;
                EditorTools.DrawPreviewKnot(m_PreviewPos);

                if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
                {
                    Undo.RecordObject(m_TargetProceduralManager, "Add road");

                    m_CurrentPlacingMode = PlacingMode.None;

                    if (m_TargetProceduralManager.AddRoad(out GameObject roadObject) && roadObject.TryGetComponent(out Road road))
                    {
                        EditorPrefs.SetBool("CreateRoad", true);
                        EditorPrefs.SetFloat("RoadPositionX", m_PreviewPos.x);
                        EditorPrefs.SetFloat("RoadPositionZ", m_PreviewPos.z);
                        Selection.activeGameObject = roadObject;
                    }

                    currentEvent.Use();
                }

                // Deselect on right mouse button down (Escape is already handled above)
                if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1){
                    m_CurrentPlacingMode = PlacingMode.None;
                    currentEvent.Use();
                }
            }

            SceneView.RepaintAll();
        }

        private void DoIntersectionPlacement(Event currentEvent)
        {
            // Draw all knots from all roads
            foreach (KeyValuePair<GUID, Road> road in m_TargetProceduralManager.Roads)
            {
                foreach (BezierKnot knot in road.Value.Spline)
                {
                    RoadAttachment roadAttachment = new(road.Key, knot.Guid);

                    bool selected = IsIntersectionPointSelected(roadAttachment);
                    EditorTools.DrawSelectableKnot(knot.Position, selected);

                    // Detect clicks on knot
                    if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0 || currentEvent.alt ||
                        !(HandleUtility.DistanceToCircle(knot.Position, EditorTools.KnotSize) < 10.0f)) continue;
                        
                    if (selected) m_SelectedIntersectionPoints.Remove(roadAttachment);
                    else m_SelectedIntersectionPoints.Add(roadAttachment);

                    currentEvent.Use();
                    break;
                }
            }

            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0 || currentEvent.alt) return;
            
            m_CurrentPlacingMode = PlacingMode.None;
            currentEvent.Use();
        }

        private void DoAreaCreation(Event currentEvent)
        {
            foreach (KeyValuePair<GUID, Sidewalk> sidewalkEntry in m_TargetProceduralManager.Sidewalks)
            {
                for (int i = 0; i < sidewalkEntry.Value.CachedData.Count; i++)
                {
                    SidewalkCache sidewalkData = sidewalkEntry.Value.CachedData[i];

                    if (sidewalkData.Positions == null || sidewalkData.Positions.Count == 0) continue;

                    SidewalkHandle handle = new(sidewalkEntry.Value.Guid, sidewalkEntry.Value.Type == SidewalkType.Connection ? ProceduralMeshType.IntersectionSidewalk : ProceduralMeshType.RoadSidewalk, false, i);
                    Vector3 knotPosition = sidewalkData.Positions[sidewalkData.Positions.Count / 2];

                    bool selected = IsSidewalkPointSelected(handle);
                    EditorTools.DrawSelectableKnot(knotPosition, selected);

                    if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0 || currentEvent.alt ||
                        !(HandleUtility.DistanceToCircle(knotPosition, EditorTools.KnotSize) < 10.0f)) continue;
                        
                    if (selected) m_SelectedSidewalks.Remove(handle);
                    else m_SelectedSidewalks.Add(handle);

                    currentEvent.Use();
                    break;
                }
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                currentEvent.Use();
            }
        }

        private void DoDecorationPlacement(Event currentEvent)
        {
            if (!m_TargetProceduralManager.DecorationCollection) return;
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            Plane groundPlane = new(Vector3.up, Vector3.zero);

            if (!groundPlane.Raycast(ray, out float enter)) return;
            m_PreviewPos = ray.GetPoint(enter);
                
            float handleSize = HandleUtility.GetHandleSize(m_PreviewPos) * .1f;
            Handles.color = Color.green;
            Handles.DrawSolidDisc(m_PreviewPos, Vector3.up, handleSize);
            
            Handles.color = Color.yellow;
            if (m_DecorationPreviewMesh) Handles.DrawWireCube
            (
                m_DecorationPreviewMesh.bounds.center + m_PreviewPos + m_DecorationPlacementOffset,
                    m_DecorationPreviewMesh.bounds.size
            );
            
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                Undo.RecordObject(m_TargetProceduralManager, "Add decoration");
                
                DecorationCollection decorations = m_TargetProceduralManager.DecorationCollection;
                ProceduralAsset decorationAsset;
                Vector3 assetPosition = m_PreviewPos + m_DecorationPlacementOffset;
                switch (m_CurrentDecorationMode)
                {
                    case DecorationMode.WirePole:
                        int prefabIndex = m_RandomPolePrefab
                            ? Random.Range(0, decorations.WirePoles.Count)
                            : m_PolePrefabIndex;
                        decorationAsset = decorations.WirePoles[prefabIndex];
                        m_TargetProceduralManager.AddWirePole(assetPosition, m_DecorationRotationOffset, decorationAsset);
                        break;
                    case DecorationMode.Prefab:
                        decorationAsset = decorations.DecorationPrefabs[m_DecorationPrefabIndex];
                        m_TargetProceduralManager.AddDecoration(assetPosition, m_DecorationRotationOffset, decorationAsset);
                        break;
                }

                currentEvent.Use();
            }
            
            SceneView.RepaintAll();
        }

        private void DoWireConnection(Event currentEvent)
        {
            if (currentEvent.type == EventType.MouseMove) m_MousePosition = currentEvent.mousePosition;
            
            // Draw a handle for every connection point
            foreach (ConnectionPoint connectionPoint in m_TargetProceduralManager.ConnectionPoints.Values)
            {
                if (!connectionPoint.Parent) continue;
                Vector3 pointPosition = connectionPoint.Parent.transform.TransformPoint(connectionPoint.Position);
                Vector3 screenPosition = HandleUtility.WorldToGUIPointWithDepth(pointPosition);

                int hitboxSize = Mathf.RoundToInt(1 / screenPosition.z * ConnectionPointHitboxModifier);

                bool isSelected = m_SelectedConnectionPoints.Contains(connectionPoint);
                
                bool hover = DoSquareHitboxCheck(screenPosition, hitboxSize, m_MousePosition);
                Color handleColour;

                if (isSelected) handleColour = hover ? new Color(1, .45f, 0) : Color.red;
                else handleColour = hover ? Color.yellow : Color.white;

                Handles.color = handleColour;
                
                Handles.DrawWireCube(pointPosition, new Vector3(.2f, .2f, .2f));

                if (!hover || currentEvent.type != EventType.MouseDown || currentEvent.button != 0 || currentEvent.alt) continue;

                if (currentEvent.control || currentEvent.shift)
                {
                    if (!isSelected) m_SelectedConnectionPoints.Add(connectionPoint);
                    else m_SelectedConnectionPoints.Remove(connectionPoint);
                    currentEvent.Use();
                    continue;
                }
                
                if (m_SelectedConnectionPoints.Count > 0)
                {
                    m_TargetProceduralManager.ConnectWires(connectionPoint, m_SelectedConnectionPoints.ToArray());
                    m_SelectedConnectionPoints.Clear();
                }
                else m_SelectedConnectionPoints.Add(connectionPoint);
                currentEvent.Use();
            }
            SceneView.RepaintAll();
        }

        private bool DoSquareHitboxCheck(Vector2 hitboxCenter, int hitboxSize, Vector2 point)
        {
            Vector2 min = hitboxCenter - new Vector2(hitboxSize, hitboxSize);
            Vector2 max = hitboxCenter + new Vector2(hitboxSize, hitboxSize);
            return RectangleCollision(min, max, point);
        }
        
        private bool RectangleCollision(Vector2 min, Vector2 max, Vector2 point) => point.x >= min.x && point.y >= min.y && point.x <= max.x && point.y <= max.y;

        private bool IsIntersectionPointSelected(RoadAttachment roadAttachment) => m_SelectedIntersectionPoints.Contains(roadAttachment);

        private bool IsSidewalkPointSelected(SidewalkHandle sidewalkHandle) => m_SelectedSidewalks.Contains(sidewalkHandle);

        private void ClearSelection()
        {
            m_SelectedIntersectionPoints.Clear();
            m_SelectedSidewalks.Clear();
            m_SelectedConnectionPoints.Clear();
        }

        private void ConnectIntersection()
        {
            Undo.RecordObject(m_TargetProceduralManager, "Add intersection");

            m_CurrentPlacingMode = PlacingMode.None;

            if (m_TargetProceduralManager.AddIntersection(m_SelectedIntersectionPoints, out GameObject roadObject) && roadObject.TryGetComponent(out Intersection intersection))
            {
                Selection.activeGameObject = roadObject;
            }
        }

        private void ConnectArea()
        {
            List<SidewalkHandle> elements = m_SelectedSidewalks.ToList();

            if (m_ReselectArea)
            {
                // Overwrite existing area
                if (m_TargetProceduralManager.UpdateBlockArea(new GUID(EditorPrefs.GetString("ReselectAreaTarget")), elements, out GameObject blockAreaObject))
                {
                    Selection.activeGameObject = blockAreaObject;
                }
            }
            else
            {
                // Add new area
                if (m_TargetProceduralManager.AddBlockArea(elements, out GameObject blockAreaObject) && blockAreaObject.TryGetComponent(out BlockArea blockArea))
                {
                    Selection.activeGameObject = blockAreaObject;
                }
            }

            m_CurrentPlacingMode = PlacingMode.None;
        }

        private void DeselectEditor()
        {
            m_CurrentPlacingMode = PlacingMode.None;
            Selection.activeGameObject = null;
        }
    }

    [InitializeOnLoad]
    public static class ProceduralUndoManager
    {
        static bool s_UndoRedoPerformed;

        static ProceduralUndoManager()
        {
            ObjectChangeEvents.changesPublished += OnChangesPublished;
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            if (!s_UndoRedoPerformed) return;

            HashSet<ProceduralManager> proceduralManagers = null;
            List<ProceduralMesh> proceduralMeshes = null;

            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) != ObjectChangeKind.CreateGameObjectHierarchy) continue;

                stream.GetCreateGameObjectHierarchyEvent(i, out var hierarchyEvent);
                GameObject target = EditorUtility.InstanceIDToObject(hierarchyEvent.instanceId) as GameObject;
                if (!target) continue;

                ProceduralManager proceduralManager = target.GetComponentInParent<ProceduralManager>();

                if (!proceduralManager) continue;

                if (proceduralManagers == null)
                {
                    proceduralManagers = new HashSet<ProceduralManager>();
                }

                proceduralManagers.Add(proceduralManager);


                if (proceduralMeshes == null)
                {
                    proceduralMeshes = new List<ProceduralMesh>();
                }

                if (target.TryGetComponent(out ProceduralMesh proceduralMesh))
                {
                    proceduralMeshes.Add(proceduralMesh);
                }
            }

            if (proceduralManagers == null) return;

            foreach (ProceduralManager proceduralManager in proceduralManagers)
            {
                proceduralManager.Initialize();
            }

            foreach (ProceduralMesh proceduralMesh in proceduralMeshes)
            {
                proceduralMesh.Generate();
                EditorUtility.SetDirty(proceduralMesh);
                proceduralMesh.OnUndoRedo();
            }
           
            SceneView.RepaintAll();
        }

        private static void UndoRedoPerformed()
        {
            s_UndoRedoPerformed = true;

            EditorApplication.delayCall += () =>
            {
                s_UndoRedoPerformed = false;
            };
        }
    }
}