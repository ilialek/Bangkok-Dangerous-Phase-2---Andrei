using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Procedural;
using Utilities.Splines;
using GUID = Utilities.GUID;

namespace ProceduralEditor
{
    [CustomEditor(typeof(Road))]
    public class RoadEditor : ProceduralMeshEditor<Road>
    {
        private bool m_PlacingPosition;
        private Vector3 m_PreviewPos;

        private EditorSelection<int> m_Selection = new EditorSelection<int>();

        private int m_KnotCount;

        public override bool HasPreviewGUI() => false;

        protected override void Setup()
        {
            m_KnotCount = m_TargetProcedural.Spline.Count;

            // Start road if needed
            if (m_KnotCount != 0 || !EditorPrefs.GetBool("CreateRoad")) return;
            
            EditorPrefs.SetBool("CreateRoad", false);
            Vector3 startPosition = new Vector3(EditorPrefs.GetFloat("RoadPositionX"), 0.0f, EditorPrefs.GetFloat("RoadPositionZ"));

            m_PlacingPosition = true;

            Undo.RecordObject(m_TargetProcedural, "Add knot");

            m_TargetProcedural.Spline.Add(startPosition);
            m_Selection.SelectSingle(0);
            AutoUpdateHandles(0);

            m_Selection.Setup();
            m_KnotCount++;

            EditorUtility.SetDirty(m_TargetProcedural);
        }

        protected override void DrawWindow()
        {
            if (GUILayout.Button(m_PlacingPosition ? "Cancel Placement" : "Add Knot"))
            {
                m_PlacingPosition = !m_PlacingPosition;
            }

            if (m_Selection.IsAnySelected)
            {
                // Knot Remove field
                if (GUILayout.Button(m_Selection.IsMultiSelect ? "Remove Knots" : "Remove Knot"))
                {
                    Undo.RecordObject(m_TargetProcedural, "Remove Knot");
                    RemoveSelectedKnots();
                    m_Selection.Deselect();
                    EditorUtility.SetDirty(m_TargetProcedural);
                    return;
                }

                // Knot Mode field
                bool mixedMode = false;
                KnotMode referenceMode = m_TargetProcedural.Spline[m_Selection.PrimaryItem].Mode;

                for (int i = 1; i < m_Selection.SelectedCount; i++)
                {
                    if (m_TargetProcedural.Spline[m_Selection.Selectedtems[i]].Mode != referenceMode)
                    {
                        mixedMode = true;
                        break;
                    }
                }

                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 100.0f;
                EditorGUI.showMixedValue = mixedMode;

                EditorGUI.BeginChangeCheck();
                KnotMode newMode = (KnotMode)EditorGUILayout.EnumPopup("Knot Mode", referenceMode);
                EditorGUI.showMixedValue = false;
                EditorGUIUtility.labelWidth = oldLabelWidth;

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_TargetProcedural, "Change Knot Mode");

                    foreach (int index in m_Selection.Selectedtems)
                    {
                        BezierKnot knot = m_TargetProcedural.Spline[index];
                        knot.Mode = newMode;
                        m_TargetProcedural.Spline[index] = knot;
                        AutoUpdateHandles(index);
                    }

                    EditorUtility.SetDirty(m_TargetProcedural);
                }

                // Knot Position field
                Vector3 valueToShow;
                bool mixedPosition = false;

                if (m_Selection.IsMultiSelect)
                {
                    // Check if the selected knots have the same position
                    Vector3 firstPos = m_TargetProcedural.Spline[m_Selection.Selectedtems[0]].Position;

                    foreach (int index in m_Selection.Selectedtems)
                    {
                        if (m_TargetProcedural.Spline[index].Position != firstPos)
                        {
                            mixedPosition = true;
                            break;
                        }
                    }

                    valueToShow = firstPos;
                }
                else
                {
                    valueToShow = m_TargetProcedural.Spline[m_Selection.PrimaryItem].Position;
                }

                EditorGUI.showMixedValue = mixedPosition;
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = EditorGUILayout.Vector3Field("Knot Position", valueToShow);
                EditorGUI.showMixedValue = false;

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_TargetProcedural, "Move Knot");

                    if (m_Selection.IsMultiSelect)
                    {
                        Vector3 delta = newPos - valueToShow;
                        foreach (int index in m_Selection.Selectedtems)
                        {
                            BezierKnot knot = m_TargetProcedural.Spline[index];
                            knot.Position += delta;
                            m_TargetProcedural.Spline[index] = knot;
                            AutoUpdateHandles(index);
                        }
                    }
                    else
                    {
                        BezierKnot knot = m_TargetProcedural.Spline[m_Selection.PrimaryItem];
                        knot.Position = newPos;
                        m_TargetProcedural.Spline[m_Selection.PrimaryItem] = knot;
                        AutoUpdateHandles(m_Selection.PrimaryItem);
                    }

                    EditorUtility.SetDirty(m_TargetProcedural);
                }
            }
            else
            {
                if (GUILayout.Button("Remove Road"))
                {
                    RemoveRoad();
                }
            }
        }

        protected override void UpdateEditor(Event currentEvent)
        {
            // Handle external knot change
            if (m_KnotCount < m_TargetProcedural.Spline.Count)
            {
                // Knot added
                if (m_TargetProcedural.Spline.Count > 0)
                {
                    m_Selection.SelectSingle(m_TargetProcedural.Spline.Count - 1);
                    m_Selection.DragToMove();
                    AutoUpdateHandles(m_Selection.PrimaryItem);
                }
                else
                {
                    m_Selection.Deselect();
                }

                m_KnotCount = m_TargetProcedural.Spline.Count;
            }
            else if (m_KnotCount > m_TargetProcedural.Spline.Count)
            {
                //Removed knot
                m_KnotCount = m_TargetProcedural.Spline.Count;
                m_Selection.Deselect();
                AutoUpdateHandlesAll();
            }

            // Handle out of bounds index
            if (!ValidateKnotIndices())
            {
                m_Selection.Deselect();
            }

            m_Selection.Validate();

            // Handle deselect
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape)
            {
                if (m_PlacingPosition)
                {
                    m_PlacingPosition = false;
                    m_Selection.Deselect();
                }
                else if (m_Selection.IsAnySelected)
                {
                    m_Selection.Deselect();
                }
                else
                {
                    DeselectEditor();
                }
                currentEvent.Use();
            }


            // Handle delete key
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Delete)
            {
                currentEvent.Use();
                RemoveRoad();
                return;
            }

            // Draw existing positions & handle selection
            Handles.color = Color.cyan;
            for (int i = 0; i < m_TargetProcedural.Spline.Count; i++)
            {
                BezierKnot knot = m_TargetProcedural.Spline[i];

                if (knot.Mode != KnotMode.Auto)
                {
                    // Draw lines connecting handles
                    Handles.color = Color.yellow;
                    Handles.DrawLine(knot.Position, knot.HandleIn);
                    Handles.DrawLine(knot.Position, knot.HandleOut);
                }

                // Draw the curve segment
                if (i < m_TargetProcedural.Spline.Count - 1)
                {
                    BezierKnot nextKnot = m_TargetProcedural.Spline[i + 1];
                    EditorTools.DrawBezierDefault(knot.Position, nextKnot.Position, knot.HandleOut, nextKnot.HandleIn);
                }

                EditorTools.DrawSelectableKnot(knot.Position, m_Selection.IsSelected(i));

                if (m_Selection.HandleKnotSelection(currentEvent, i, knot.Position)) break;
            }

            m_Selection.HandleDrag(currentEvent);

            if (m_Selection.IsAnySelected)
            {
                if (m_Selection.IsDragging && currentEvent.type == EventType.MouseDrag && m_Selection.IsSingleSelected)
                {
                    BezierKnot newKnot = m_TargetProcedural.Spline[m_Selection.PrimaryItem];

                    Undo.RecordObject(m_TargetProcedural, "Drag Knot");
                    Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                    Plane ground = new Plane(Vector3.up, Vector3.up * newKnot.Position.y);

                    if (ground.Raycast(ray, out float enter))
                    {
                        Vector3 newPos = ray.GetPoint(enter);

                        newKnot.Position = EditorTools.GetKnotPosition(new Vector3(newPos.x, newPos.y, newPos.z));
                        m_TargetProcedural.Spline[m_Selection.PrimaryItem] = newKnot;
                        AutoUpdateHandles(m_Selection.PrimaryItem);
                        EditorUtility.SetDirty(m_TargetProcedural);
                    }

                    currentEvent.Use();
                }

                // Move selected point (only show gizmo if not dragging and mouse is not held)
                if (m_Selection.IsMoveSelection)
                {
                    Vector3 startPosition = GetAverageSelectedKnotPosition();
                    EditorGUI.BeginChangeCheck();
                    Vector3 endPosition = Handles.PositionHandle(startPosition, Quaternion.identity);

                    Dictionary<int, (Vector3 handleIn, Vector3 handleOut)> originalHandles = new();
                    foreach (int knotIndex in m_Selection.Selectedtems)
                    {
                        BezierKnot knot = m_TargetProcedural.Spline[knotIndex];
                        if (knot.Mode == KnotMode.Bezier)
                        {
                            Vector3 handleIn = Handles.PositionHandle(knot.HandleIn, Quaternion.identity);
                            Vector3 handleOut = Handles.PositionHandle(knot.HandleOut, Quaternion.identity);
                            originalHandles[knotIndex] = (handleIn, handleOut);
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(m_TargetProcedural, "Move Knot");

                        Vector3 delta = endPosition - startPosition;

                        foreach (int knotIndex in m_Selection.Selectedtems)
                        {
                            BezierKnot knot = m_TargetProcedural.Spline[knotIndex];
                            knot.Position = EditorTools.GetKnotPosition(knot.Position + delta);

                            if (knot.Mode == KnotMode.Bezier && originalHandles.ContainsKey(knotIndex))
                            {
                                knot.HandleIn = originalHandles[knotIndex].handleIn;
                                knot.HandleOut = originalHandles[knotIndex].handleOut;
                            }

                            m_TargetProcedural.Spline[knotIndex] = knot;
                            AutoUpdateHandles(knotIndex);
                        }

                        EditorUtility.SetDirty(m_TargetProcedural);
                    }
                }
            }

            // Placement preview
            if (m_PlacingPosition)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

                if (groundPlane.Raycast(ray, out float enter))
                {
                    m_PreviewPos = ray.GetPoint(enter);
                    m_PreviewPos.y = 0.0f;

                    // Get index of new knot
                    int addKnotIndex = m_TargetProcedural.Spline.Count - 1;
                    bool multiSelect = m_Selection.IsMultiSelect;

                    if (multiSelect)
                    {
                        // Insert knot in between selected knots
                        foreach (int knotIndex in m_Selection.Selectedtems)
                        {
                            addKnotIndex = Mathf.Min(addKnotIndex, knotIndex);
                        }
                    }

                    int knotCount = m_TargetProcedural.Spline.Count;
                    int beforeIndex, afterIndex;

                    if (addKnotIndex >= knotCount)
                    {
                        // Append
                        beforeIndex = knotCount - 1;
                        afterIndex = knotCount;
                    }
                    else
                    {
                        // Insert
                        beforeIndex = addKnotIndex;
                        afterIndex = addKnotIndex + 1;
                    }

                    KnotMode currentMode = beforeIndex >= 0 ? m_TargetProcedural.Spline[beforeIndex].Mode : (afterIndex < knotCount ? m_TargetProcedural.Spline[afterIndex].Mode : KnotMode.Auto);
                    if (currentMode == KnotMode.Bezier) currentMode = KnotMode.Auto;

                    // Preview bezier curves
                    if (knotCount > 0)
                    {
                        Vector3 handleIn0 = Vector3.zero;       // Before
                        Vector3 handleOut0 = Vector3.zero;      // Before
                        Vector3 handleIn1 = Vector3.zero;       // Preview
                        Vector3 handleOut1 = Vector3.zero;      // Preview
                        Vector3 handleIn2 = Vector3.zero;       // After
                        Vector3 handleOut2 = Vector3.zero;      // After

                        if (beforeIndex >= 0)
                        {
                            BezierKnot previousKnot = beforeIndex - 1 >= 0 ? m_TargetProcedural.Spline[beforeIndex - 1] : m_TargetProcedural.Spline[beforeIndex];
                            BezierKnot currentKnot = m_TargetProcedural.Spline[beforeIndex];
                            EditorTools.ComputeAutoHandles(currentKnot.Mode, previousKnot.Position, currentKnot.Position, m_PreviewPos, currentKnot.HandleIn, currentKnot.HandleOut, out handleIn0, out handleOut0);
                        }

                        Vector3 previousForPreview = beforeIndex >= 0 ? m_TargetProcedural.Spline[beforeIndex].Position : m_PreviewPos;
                        Vector3 nextForPreview = afterIndex < knotCount ? m_TargetProcedural.Spline[afterIndex].Position : m_PreviewPos;
                        EditorTools.ComputeAutoHandles(currentMode, previousForPreview, m_PreviewPos, nextForPreview, Vector3.zero, Vector3.zero, out handleIn1, out handleOut1);

                        if (afterIndex < knotCount)
                        {
                            BezierKnot currentKnot = m_TargetProcedural.Spline[afterIndex];
                            BezierKnot nextKnot = afterIndex + 1 < knotCount ? m_TargetProcedural.Spline[afterIndex + 1] : m_TargetProcedural.Spline[afterIndex];
                            EditorTools.ComputeAutoHandles(currentKnot.Mode, m_PreviewPos, currentKnot.Position, nextKnot.Position, currentKnot.HandleIn, currentKnot.HandleOut, out handleIn2, out handleOut2);
                        }

                        // Draw the four bezier-curves

                        // Knot 0 -> Knot 1
                        if (beforeIndex - 1 >= 0)
                        {
                            BezierKnot knot0 = m_TargetProcedural.Spline[beforeIndex - 1];
                            BezierKnot knot1 = m_TargetProcedural.Spline[beforeIndex];
                            EditorTools.DrawBezierPreview(knot0.Position, knot1.Position, knot0.HandleOut, handleIn0);
                        }

                        // Knot 1 -> Knot 2
                        if (beforeIndex >= 0)
                        {
                            BezierKnot knot1 = m_TargetProcedural.Spline[beforeIndex];
                            EditorTools.DrawBezierPreview(knot1.Position, m_PreviewPos, handleOut0, handleIn1);
                        }

                        // Knot 2 -> Knot 3
                        if (afterIndex < knotCount)
                        {
                            BezierKnot knot3 = m_TargetProcedural.Spline[afterIndex];
                            EditorTools.DrawBezierPreview(m_PreviewPos, knot3.Position, handleOut1, handleIn2);
                        }

                        // Knot 3 -> Knot 4
                        if (afterIndex + 1 < knotCount)
                        {
                            BezierKnot knot3 = m_TargetProcedural.Spline[afterIndex];
                            BezierKnot knot4 = m_TargetProcedural.Spline[afterIndex + 1];
                            EditorTools.DrawBezierPreview(knot3.Position, knot4.Position, handleOut2, knot4.HandleIn);
                        }
                    }

                    EditorTools.DrawPreviewKnot(m_PreviewPos);

                    if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
                    {
                        Undo.RecordObject(m_TargetProcedural, "Add knot");

                        m_TargetProcedural.Spline.Insert(addKnotIndex + 1, m_PreviewPos, currentMode);

                        if (multiSelect)
                        {
                            m_PlacingPosition = false;
                            m_Selection.SelectSingle(addKnotIndex + 1);
                        }
                        else
                        {
                            m_Selection.SelectSingle(m_TargetProcedural.Spline.Count - 1);
                        }
                        AutoUpdateHandles(addKnotIndex + 1);

                        m_Selection.DragToMove();
                        m_KnotCount++;

                        EditorUtility.SetDirty(m_TargetProcedural);
                        currentEvent.Use();
                    }

                    // Deselect on right mouse button down (Escape is already handled above)
                    if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
                    {
                        m_PlacingPosition = false;
                        currentEvent.Use();
                    }
                }

                SceneView.RepaintAll();
            }

            // Try to update spline if needed
            m_TargetProcedural.Spline.CheckForUpdate();

            // Handle correct deselection
            if (Selection.activeGameObject == m_TargetProcedural.gameObject && currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0 && !currentEvent.alt)
            {
                if (m_WindowRect.Contains(currentEvent.mousePosition))
                {
                    currentEvent.Use();
                }
                else if (m_Selection.IsAnySelected)
                {
                    m_Selection.Deselect();
                    currentEvent.Use();
                }
                else
                {
                    DeselectEditor();
                    currentEvent.Use();
                }
            }
        }

        private bool ValidateKnotIndices()
        {
            foreach (int knotIndex in m_Selection.Selectedtems)
            {
                if (knotIndex < 0 || knotIndex >= m_TargetProcedural.Spline.Count) return false;
            }

            return true;
        }

        private void RemoveSelectedKnots()
        {
            m_Selection.SortSelection();

            for (int i = m_Selection.SelectedCount - 1; i >= 0; i--)
            {
                int knotIndex = m_Selection.Selectedtems[i];

                if (knotIndex < 0 || knotIndex >= m_TargetProcedural.Spline.Count) continue;

                m_TargetProcedural.Spline.RemoveAt(knotIndex);
                m_KnotCount--;

                int startKnot = Mathf.Max(0, knotIndex - 1);
                int endKnot = Mathf.Min(m_TargetProcedural.Spline.Count - 1, knotIndex);

                AutoUpdateHandles(startKnot);
                AutoUpdateHandles(endKnot);
            }
        }

        private Vector3 GetAverageSelectedKnotPosition()
        {
            Vector3 position = Vector3.zero;

            foreach (int knotIndex in m_Selection.Selectedtems)
            {
                BezierKnot knot = m_TargetProcedural.Spline[knotIndex];
                position += knot.Position;
            }

            return position / m_Selection.SelectedCount;
        }

        private void RemoveRoad()
        {
            DeselectEditor();

            // Remove attached sidewalks
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            if (m_ProceduralManager.RoadSidewalks.TryGetValue(m_TargetProcedural.Guid, out List<GUID> sidewalkGuids))
            {
                foreach (GUID sidewalkGuid in sidewalkGuids)
                {
                    if (!m_ProceduralManager.Sidewalks.TryGetValue(sidewalkGuid, out Sidewalk sidewalk)) continue;

                    Undo.DestroyObjectImmediate(sidewalk.gameObject);
                }
            }

            Undo.DestroyObjectImmediate(m_TargetProcedural.gameObject);
            Undo.CollapseUndoOperations(undoGroup);
        }

        private void AutoUpdateHandlesAll()
        {
            if (m_TargetProcedural.Spline.Count < 2) return;

            for (int i = 0; i < m_TargetProcedural.Spline.Count; i++)
            {
                BezierKnot knot = m_TargetProcedural.Spline[i];
                Vector3 prev = i > 0 ? m_TargetProcedural.Spline[i - 1].Position : knot.Position;
                Vector3 next = i < m_TargetProcedural.Spline.Count - 1 ? m_TargetProcedural.Spline[i + 1].Position : knot.Position;

                EditorTools.ComputeAutoHandles(knot.Mode, prev, knot.Position, next, knot.HandleIn, knot.HandleOut, out knot.HandleIn, out knot.HandleOut);
                m_TargetProcedural.Spline[i] = knot;
            }

            m_TargetProcedural.Spline.Recalculate();
        }

        private void AutoUpdateHandles(int knotIndex)
        {
            if (knotIndex < 0 || knotIndex >= m_TargetProcedural.Spline.Count) return;
            if (m_TargetProcedural.Spline.Count < 2) return;

            // Loop over adjacent knots
            int startKnot = Mathf.Max(0, knotIndex - 1);
            int endKnot = Mathf.Min(m_TargetProcedural.Spline.Count - 1, knotIndex + 1);

            for (int i = startKnot; i <= endKnot; i++)
            {
                BezierKnot knot = m_TargetProcedural.Spline[i];
                Vector3 previous = i > 0 ? m_TargetProcedural.Spline[i - 1].Position : knot.Position;
                Vector3 next = i < m_TargetProcedural.Spline.Count - 1 ? m_TargetProcedural.Spline[i + 1].Position : knot.Position;

                EditorTools.ComputeAutoHandles(knot.Mode, previous, knot.Position, next, knot.HandleIn, knot.HandleOut, out knot.HandleIn, out knot.HandleOut);
                m_TargetProcedural.Spline[i] = knot;
            }

            for (int i = startKnot; i <= endKnot; i++)
            {
                m_TargetProcedural.Spline.CalculateLengthsAt(i, i == endKnot);
            }
        }
    }
}