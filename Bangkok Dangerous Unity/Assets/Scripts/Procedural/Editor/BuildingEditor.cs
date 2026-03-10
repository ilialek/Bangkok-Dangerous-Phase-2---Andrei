using Procedural;
using UnityEditor;
using UnityEngine;

namespace ProceduralEditor
{
    [CustomEditor(typeof(Building)), CanEditMultipleObjects]
    public class BuildingEditor : ProceduralMeshEditor<Building>
    {
        private EditorSelection<int> m_Selection = new EditorSelection<int>();

        private bool m_PlacingPosition;
        private Vector3 m_PreviewPos;

        private bool m_BoundsView;

        protected override void Setup()
        {
            // Start road if needed
            if (m_TargetProcedural.Lot.Count == 0 && EditorPrefs.GetBool("CreateBuilding"))
            {
                EditorPrefs.SetBool("CreateBuilding", false);
                Vector3 startPosition = new Vector3(EditorPrefs.GetFloat("BuildingPositionX"), 0.0f, EditorPrefs.GetFloat("BuildingPositionZ"));

                m_PlacingPosition = true;

                Undo.RecordObject(m_TargetProcedural, "Add knot");

                m_TargetProcedural.Lot.Add(startPosition);
                m_Selection.SelectSingle(0);

                m_Selection.Setup();

                EditorUtility.SetDirty(m_TargetProcedural);
            }
        }

        protected override void DrawWindow()
        {
            if (GUILayout.Button(m_PlacingPosition ? "Cancel" : "Add Knot"))
            {
                m_PlacingPosition = !m_PlacingPosition;

                if (m_PlacingPosition)
                {
                    m_BoundsView = false;
                }
            }

            if (GUILayout.Button(m_BoundsView ? "Cancel" : "Bounds"))
            {
                m_BoundsView = !m_BoundsView;
                
                if (m_BoundsView)
                {
                    m_PlacingPosition = false;
                }
            }

            if (GUILayout.Button("Randomize"))
            {
                m_TargetProcedural.Randomize();
                m_TargetProcedural.Generate();
            }

            if (m_Selection.IsAnySelected)
            {
                // Knot Remove field
                if (GUILayout.Button(m_Selection.IsMultiSelect ? "Remove Knots" : "Remove Knot"))
                {
                    Undo.RecordObject(m_TargetProcedural, "Remove Knot");
                    RemoveSelectedKnots();
                    m_Selection.Deselect();
                    m_TargetProcedural.Generate();
                    EditorUtility.SetDirty(m_TargetProcedural);
                    return;
                }

                // Knot Position field
                Vector3 valueToShow;
                bool mixedPosition = false;

                if (m_Selection.IsMultiSelect)
                {
                    // Check if the selected knots have the same position
                    Vector3 firstPos = m_TargetProcedural.Lot[m_Selection.Selectedtems[0]];

                    foreach (int index in m_Selection.Selectedtems)
                    {
                        if (m_TargetProcedural.Lot[index] != firstPos)
                        {
                            mixedPosition = true;
                            break;
                        }
                    }

                    valueToShow = firstPos;
                }
                else
                {
                    valueToShow = m_TargetProcedural.Lot[m_Selection.PrimaryItem];
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
                            Vector3 knot = m_TargetProcedural.Lot[index];
                            knot += delta;
                            m_TargetProcedural.Lot[index] = knot;
                        }
                    }
                    else
                    {
                        Vector3 knot = m_TargetProcedural.Lot[m_Selection.PrimaryItem];
                        knot = newPos;
                        m_TargetProcedural.Lot[m_Selection.PrimaryItem] = knot;
                    }

                    m_TargetProcedural.Generate();
                    EditorUtility.SetDirty(m_TargetProcedural);
                }
            }
            else
            {
                if (GUILayout.Button("Remove Building"))
                {
                    RemoveBuilding();
                }
            }
        }

        protected override void UpdateEditor(Event currentEvent)
        {
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
                RemoveBuilding();
                return;
            }

            // Draw lot
            if (!m_BoundsView)
            {
                if (m_TargetProcedural.Lot.Count > 0)
                {
                    // Draw area edges
                    for (int i = 0; i < m_TargetProcedural.Lot.Count; i++)
                    {
                        EditorTools.DrawStaticKnot(m_TargetProcedural.Lot[i]);
                    }

                    // Draw corners
                    for (int i = 0; i < m_TargetProcedural.Lot.Count; i++)
                    {
                        EditorTools.DrawSelectableKnot(m_TargetProcedural.Lot[i], m_Selection.IsSelected(i));

                        if (m_Selection.HandleKnotSelection(currentEvent, i, m_TargetProcedural.Lot[i])) break;
                    }

                    // Draw edges
                    for (int j = 0; j < m_TargetProcedural.Lot.Count; j++)
                    {
                        Handles.DrawLine(m_TargetProcedural.Lot[j], m_TargetProcedural.Lot[(j + 1) % m_TargetProcedural.Lot.Count]);
                    }
                }
            }

            m_Selection.HandleDrag(currentEvent);

            bool requiresUpdate = false;

            if (m_Selection.IsAnySelected)
            {
                if (m_Selection.IsDragging && currentEvent.type == EventType.MouseDrag && m_Selection.IsSingleSelected)
                {
                    Vector3 newKnot = m_TargetProcedural.Lot[m_Selection.PrimaryItem];

                    Undo.RecordObject(m_TargetProcedural, "Drag Knot");
                    Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                    Plane ground = new Plane(Vector3.up, Vector3.up * newKnot.y);

                    if (ground.Raycast(ray, out float enter))
                    {
                        Vector3 newPos = ray.GetPoint(enter);

                        newKnot = EditorTools.GetKnotPosition(new Vector3(newPos.x, newPos.y, newPos.z));
                        m_TargetProcedural.Lot[m_Selection.PrimaryItem] = newKnot;
                        requiresUpdate = true;
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

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(m_TargetProcedural, "Move Knot");

                        Vector3 delta = endPosition - startPosition;

                        foreach (int knotIndex in m_Selection.Selectedtems)
                        {
                            Vector3 knot = m_TargetProcedural.Lot[knotIndex];
                            knot = EditorTools.GetKnotPosition(knot + delta);
                            m_TargetProcedural.Lot[knotIndex] = knot;
                            requiresUpdate = true;
                        }

                        EditorUtility.SetDirty(m_TargetProcedural);
                    }
                }
            }

            if (m_PlacingPosition)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

                if (groundPlane.Raycast(ray, out float enter))
                {
                    m_PreviewPos = ray.GetPoint(enter);

                    // Get index of new knot
                    int addKnotIndex = m_TargetProcedural.Lot.Count - 1;
                    bool multiSelect = m_Selection.IsMultiSelect;

                    if (multiSelect)
                    {
                        // Insert knot in between selected knots
                        foreach (int knotIndex in m_Selection.Selectedtems)
                        {
                            addKnotIndex = Mathf.Min(addKnotIndex, knotIndex);
                        }
                    }

                    int knotCount = m_TargetProcedural.Lot.Count;
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
                    EditorTools.DrawPreviewKnot(m_PreviewPos);

                    if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
                    {
                        Undo.RecordObject(m_TargetProcedural, "Add knot");

                        m_TargetProcedural.Lot.Insert(addKnotIndex + 1, m_PreviewPos);

                        if (multiSelect)
                        {
                            m_PlacingPosition = false;
                            m_Selection.SelectSingle(addKnotIndex + 1);
                        }
                        else
                        {
                            m_Selection.SelectSingle(m_TargetProcedural.Lot.Count - 1);
                        }
                        requiresUpdate = true;

                        m_Selection.DragToMove();

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

            if (m_BoundsView)
            {
                Debug.Log("Todo");
            }

            if (requiresUpdate)
            {
                m_TargetProcedural.Generate();
                EditorUtility.SetDirty(m_TargetProcedural);
            }

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

        private Vector3 GetAverageSelectedKnotPosition()
        {
            Vector3 position = Vector3.zero;

            foreach (int knotIndex in m_Selection.Selectedtems)
            {
                Vector3 knot = m_TargetProcedural.Lot[knotIndex];
                position += knot;
            }

            return position / m_Selection.SelectedCount;
        }

        private void RemoveSelectedKnots()
        {
            m_Selection.SortSelection();

            for (int i = m_Selection.SelectedCount - 1; i >= 0; i--)
            {
                int knotIndex = m_Selection.Selectedtems[i];

                if (knotIndex < 0 || knotIndex >= m_TargetProcedural.Lot.Count) continue;

                m_TargetProcedural.Lot.RemoveAt(knotIndex);
            }
        }

        private void RemoveBuilding()
        {
            if (m_ProceduralManager)
            {
                MeshUtils.DeleteMeshAsset(m_TargetProcedural.Guid, m_ProceduralManager.TargetMeshCollection);
            }
           
            DeselectEditor();
            Undo.DestroyObjectImmediate(m_TargetProcedural.gameObject);
        }
    }
}