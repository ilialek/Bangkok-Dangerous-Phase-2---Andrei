using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Procedural;
using Utilities.Splines;
using GUID = Utilities.GUID;

namespace ProceduralEditor
{
    [CustomEditor(typeof(Intersection))]
    public class IntersectionEditor : ProceduralMeshEditor<Intersection>
    {
        private EditorSelection<RoadAttachment> m_AttachmentSelection = new EditorSelection<RoadAttachment>();
        private EditorSelection<IntersectionEdge> m_EdgeSelection = new EditorSelection<IntersectionEdge>();

        private HashSet<RoadAttachment> m_RoadUpdateRequired = new HashSet<RoadAttachment>();

        protected override void DrawWindow()
        {
            if (GUILayout.Button("Remove Intersection"))
            {
                RemoveIntersection();
            }
        }

        protected override void UpdateEditor(Event currentEvent)
        {
            // Handle out of bounds index
            Validate();
            m_AttachmentSelection.Validate();
            m_EdgeSelection.Validate();

            // Handle delete key
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Delete)
            {
                currentEvent.Use();
                RemoveIntersection();
                return;
            }

            // Draw intersection attachments
            for (int i = 0; i < m_TargetProcedural.Attachments.Count; i++)
            {
                RoadAttachment attachment = m_TargetProcedural.Attachments[i];
                BezierKnot knot = m_ProceduralManager.Knots[attachment.KnotGuid];

                EditorTools.DrawSelectableKnot(knot.Position, m_AttachmentSelection.IsSelected(attachment));

                if (m_AttachmentSelection.HandleKnotSelection(currentEvent, attachment, knot.Position))
                {
                    m_EdgeSelection.Deselect();
                    break;
                }
            }

            // Draw control points
            for (int i = 0; i < m_TargetProcedural.IntersectionEdges.Count; i++)
            {
                IntersectionEdge edge = m_TargetProcedural.IntersectionEdges[i];
                Vector3 controlPoint = m_TargetProcedural.GetControlPoint(i);
                EditorTools.DrawSelectableKnot(controlPoint, m_EdgeSelection.IsSelected(edge));

                if (m_EdgeSelection.HandleKnotSelection(currentEvent, edge, controlPoint))
                {
                    m_AttachmentSelection.Deselect();
                }
            }

            m_AttachmentSelection.HandleDrag(currentEvent);

            if (m_AttachmentSelection.IsAnySelected)
            {
                // Handle single drag
                if (m_AttachmentSelection.IsDragging && currentEvent.type == EventType.MouseDrag && m_AttachmentSelection.IsSingleSelected)
                {
                    BezierKnot knot = m_ProceduralManager.Knots[m_AttachmentSelection.PrimaryItem.KnotGuid];

                    Undo.RecordObject(m_TargetProcedural, "Drag Knot");
                    Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                    Plane ground = new Plane(Vector3.up, Vector3.up * knot.Position.y);

                    if (ground.Raycast(ray, out float enter))
                    {
                        Vector3 newPos = ray.GetPoint(enter);
                        knot.Position = EditorTools.GetKnotPosition(new Vector3(newPos.x, newPos.y, newPos.z));
                        AutoUpdateHandles(m_AttachmentSelection.PrimaryItem);
                        EditorUtility.SetDirty(m_TargetProcedural);
                    }

                    currentEvent.Use();
                }

                // Move selected point (only show gizmo if not dragging and mouse is not held)
                if (m_AttachmentSelection.IsMoveSelection)
                {
                    Vector3 startPosition = GetAverageSelectedKnotPosition();
                    EditorGUI.BeginChangeCheck();
                    Vector3 endPosition = Handles.PositionHandle(startPosition, Quaternion.identity);

                    Dictionary<BezierKnot, (Vector3 handleIn, Vector3 handleOut)> originalHandles = new();
                    foreach (RoadAttachment roadAttachment in m_AttachmentSelection.Selectedtems)
                    {
                        BezierKnot knot = m_ProceduralManager.Knots[roadAttachment.KnotGuid];
                        if (knot.Mode == KnotMode.Bezier)
                        {
                            Vector3 handleIn = Handles.PositionHandle(knot.HandleIn, Quaternion.identity);
                            Vector3 handleOut = Handles.PositionHandle(knot.HandleOut, Quaternion.identity);
                            originalHandles[knot] = (handleIn, handleOut);
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.IncrementCurrentGroup();
                        int undoGroup = Undo.GetCurrentGroup();
                        Vector3 delta = endPosition - startPosition;

                        foreach (RoadAttachment roadAttachment in m_AttachmentSelection.Selectedtems)
                        {
                            Road road = m_ProceduralManager.Roads[roadAttachment.RoadGuid];
                            BezierKnot knot = m_ProceduralManager.Knots[roadAttachment.KnotGuid];
                            int knotIndex = road.Spline.GetKnotIndex(knot);

                            if (knotIndex == -1) continue;

                            knot.Position = EditorTools.GetKnotPosition(knot.Position + delta);

                            if (knot.Mode == KnotMode.Bezier && originalHandles.ContainsKey(knot))
                            {
                                knot.HandleIn = originalHandles[knot].handleIn;
                                knot.HandleOut = originalHandles[knot].handleOut;
                            }

                            road.Spline[knotIndex] = knot;
                            AutoUpdateHandles(roadAttachment);
                            EditorUtility.SetDirty(road);
                        }

                        EditorUtility.SetDirty(m_TargetProcedural);

                        Undo.CollapseUndoOperations(undoGroup);
                    }
                }
            }

            m_EdgeSelection.HandleDrag(currentEvent);
            if (m_EdgeSelection.IsAnySelected)
            {
                // Handle single drag
                if (m_EdgeSelection.IsDragging && currentEvent.type == EventType.MouseDrag && m_EdgeSelection.IsSingleSelected)
                {
                    IntersectionEdge edge = m_EdgeSelection.PrimaryItem;
                    int edgeIndex = m_TargetProcedural.IntersectionEdges.IndexOf(edge);

                    if (edgeIndex >= 0)
                    {
                        RoadAttachment attachment1 = FindAttachment(edge.Knot1);
                        RoadAttachment attachment2 = FindAttachment(edge.Knot2);

                        Undo.RecordObject(m_TargetProcedural, "Drag Control Point");
                        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                        Vector3 relativeControlPoint = m_TargetProcedural.GetRelativeControlPoint(edgeIndex);
                        Vector3 controlPoint = edge.ControlPoint + relativeControlPoint;
                        Plane ground = new Plane(Vector3.up, Vector3.up * controlPoint.y);

                        if (ground.Raycast(ray, out float enter))
                        {
                            Vector3 newPos = ray.GetPoint(enter);
                            edge.ControlPoint = EditorTools.GetKnotPosition(newPos) - relativeControlPoint;
                            m_TargetProcedural.IntersectionEdges[edgeIndex] = edge;
                            EditorUtility.SetDirty(m_TargetProcedural);
                            m_TargetProcedural.Generate();
                        }

                        currentEvent.Use();
                    }
                }
            }

            // Check for updates
            foreach (RoadAttachment roadAttachment in m_RoadUpdateRequired)
            {
                m_ProceduralManager.RoadAttachmentChanged(roadAttachment.RoadGuid);
                m_ProceduralManager.Roads[roadAttachment.RoadGuid].Spline.CheckForUpdate();
                m_ProceduralManager.GenerateIntersections(roadAttachment.KnotGuid);
            }
            
            m_RoadUpdateRequired.Clear();
            
            // Handle correct deselection
            if (Selection.activeGameObject != m_TargetProcedural.gameObject ||
                currentEvent.type != EventType.MouseDown || currentEvent.button != 0 || currentEvent.alt) return;
            
            if (m_AttachmentSelection.IsAnySelected) m_AttachmentSelection.Deselect();
            else if (m_EdgeSelection.IsAnySelected) m_EdgeSelection.Deselect();
            else DeselectEditor();

            currentEvent.Use();
        }

        private Vector3 GetAverageSelectedKnotPosition()
        {
            Vector3 position = Vector3.zero;

            foreach (RoadAttachment roadAttachment in m_AttachmentSelection.Selectedtems)
            {
                BezierKnot knot = m_ProceduralManager.Knots[roadAttachment.KnotGuid];
                position += knot.Position;
            }

            return position / m_AttachmentSelection.SelectedCount;
        }

        private RoadAttachment FindAttachment(GUID knotGuid)
        {
            foreach (RoadAttachment attachment in m_TargetProcedural.Attachments
                         .Where(attachment => attachment.KnotGuid == knotGuid)) return attachment;

            return RoadAttachment.Zero;
        }

        private void Validate()
        {
            if (m_AttachmentSelection.SelectedCount == 0) return;

            for (int i = m_AttachmentSelection.SelectedCount - 1; i >= 0; i--)
            {
                if (m_AttachmentSelection.Selectedtems[i] == null || m_AttachmentSelection.Selectedtems[i].RoadGuid.Key == "" || m_ProceduralManager.Knots[m_AttachmentSelection.Selectedtems[i].KnotGuid] == null)
                {
                    m_AttachmentSelection.RemoveFromSelection(i);
                }
            }

            if (m_AttachmentSelection.Empty)
            {
                m_AttachmentSelection.Deselect();
            }
        }

        private void RemoveIntersection()
        {
            DeselectEditor();

            // Remove attached sidewalks
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GUID sidewalkGuid in m_ProceduralManager.RoadSidewalks[m_TargetProcedural.Guid])
            {
                Undo.DestroyObjectImmediate(m_ProceduralManager.Sidewalks[sidewalkGuid].gameObject);
            }

            Undo.DestroyObjectImmediate(m_TargetProcedural.gameObject);
            Undo.CollapseUndoOperations(undoGroup);
        }

        private void AutoUpdateHandlesAll()
        {
            foreach (RoadAttachment attachment in m_AttachmentSelection.Selectedtems)
            {
                EditorTools.UpdateRoadHandles(m_ProceduralManager.Roads[attachment.RoadGuid]);
            }
        }

        private void AutoUpdateHandles(RoadAttachment attachment)
        {
            Road road = m_ProceduralManager.Roads[attachment.RoadGuid];

            if (road.Spline.Count < 2) return;

            // Loop over adjacent knots
            BezierKnot baseKnot = m_ProceduralManager.Knots[attachment.KnotGuid];
            int knotIndex = road.Spline.GetKnotIndex(baseKnot);
            int startKnot = Mathf.Max(0, knotIndex - 1);
            int endKnot = Mathf.Min(road.Spline.Count - 1, knotIndex + 1);

            for (int i = startKnot; i <= endKnot; i++)
            {
                BezierKnot knot = road.Spline[i];
                Vector3 previous = i > 0 ? road.Spline[i - 1].Position : knot.Position;
                Vector3 next = i < road.Spline.Count - 1 ? road.Spline[i + 1].Position : knot.Position;

                EditorTools.ComputeAutoHandles(knot.Mode, previous, knot.Position, next, knot.HandleIn, knot.HandleOut, out knot.HandleIn, out knot.HandleOut);
                road.Spline[i] = knot;
            }

            for (int i = startKnot; i <= endKnot; i++)
            {
                road.Spline.CalculateLengthsAt(i, i == endKnot);
            }

            m_RoadUpdateRequired.Add(attachment);
        }
    }
}