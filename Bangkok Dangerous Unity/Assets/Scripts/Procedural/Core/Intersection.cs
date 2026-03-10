using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Utilities.Splines;

namespace Procedural
{
    public class Intersection : ProceduralMesh
    {
        public List<RoadAttachment> Attachments;
        public List<IntersectionEdge> IntersectionEdges = new List<IntersectionEdge>();

        [SerializeField] private IntersectionSettings m_Settings;

        public IntersectionSettings Settings => m_Settings;

        public void Construct(List<RoadAttachment> attachments)
        {
            m_Guid = GUID.Create();

            // Copy list, but keep references
            Attachments.Clear();
            foreach (RoadAttachment attachment in attachments) 
            {
                Attachments.Add(attachment);
            }

            // Create intersection edges
            Vector3 center = GetAttachmentCenter();
            Attachments.Sort((a, b) =>
            {
                Vector3 positionA = ProceduralManager.Knots[a.KnotGuid].Position;
                Vector3 positionB = ProceduralManager.Knots[b.KnotGuid].Position;
                float angleA = Mathf.Atan2(positionA.z - center.z, positionA.x - center.x);
                float angleB = Mathf.Atan2(positionB.z - center.z, positionB.x - center.x);
                return angleA.CompareTo(angleB);
            });

            IntersectionEdges.Clear();
            for (int i = 0; i < Attachments.Count; i++)
            {
                RoadAttachment attachment1 = Attachments[i];
                RoadAttachment attachment2 = Attachments[(i + 1) % Attachments.Count];

                IntersectionEdge edge = new IntersectionEdge();
                edge.Knot1 = attachment1.KnotGuid;
                edge.Knot2 = attachment2.KnotGuid;
                edge.ControlPoint =  GetIntersectionPoint(attachment1, attachment2) - GetMiddle(attachment1, attachment2);

                IntersectionEdges.Add(edge);
            }

            Generate(false);
        }
        
        private void OnDestroy()
        {
            ProceduralManager?.IntersectionRemoved(m_Guid);
        }

        /// <summary>
        /// Generate complete intersection mesh and all connecting sidewalks
        /// </summary>
        public override void Generate(bool generateAttachedMeshes = true)
        {
            Mesh = RoadGeneration.GenerateIntersectionMesh(ProceduralManager, this, m_Settings);

            if (generateAttachedMeshes)
            {
                ProceduralManager?.RegenerateIntersectionAttachments(m_Guid);
            }
        }

        private Vector3 GetAttachmentCenter()
        {
            Vector3 center = Vector3.zero;

            foreach (RoadAttachment attachment in Attachments)
            {
                center += ProceduralManager.Knots[attachment.KnotGuid].Position;
            }

            return center / Attachments.Count;
        }

        private Vector3 GetMiddle(RoadAttachment attachment1, RoadAttachment attachment2)
        {
            Road road1 = ProceduralManager.Roads[attachment1.RoadGuid];
            BezierKnot knot1 = ProceduralManager.Knots[attachment1.KnotGuid];
            float width1 = road1.Settings.Width;
            RoadGeneration.GetRoadPositions(road1.Spline, knot1.Progress, width1, out Vector3 road1SideA, out Vector3 road1SideB, out Vector3 tangent1);

            // Road 2
            Road road2 = ProceduralManager.Roads[attachment2.RoadGuid];
            BezierKnot knot2 = ProceduralManager.Knots[attachment2.KnotGuid];
            float width2 = road2.Settings.Width;
            RoadGeneration.GetRoadPositions(road2.Spline, knot2.Progress, width2, out Vector3 road2SideA, out Vector3 road2SideB, out Vector3 tangent2);

            Vector3 road1Side = (knot1.Progress <= 0.5f) ? road1SideA : road1SideB;
            Vector3 road2Side = (knot2.Progress <= 0.5f) ? road2SideB : road2SideA;

            // Place the control point halfway between the two closest sides
            return (road1Side + road2Side) * 0.5f;
        }

        public Vector3 GetControlPoint(int index)
        {
            IntersectionEdge edge = IntersectionEdges[index];
            RoadAttachment attachment1 = Attachments.Find(a => a.KnotGuid == edge.Knot1);
            RoadAttachment attachment2 = Attachments.Find(a => a.KnotGuid == edge.Knot2);
            return edge.ControlPoint + GetMiddle(attachment1, attachment2);
        }

        public Vector3 GetRelativeControlPoint(int index)
        {
            IntersectionEdge edge = IntersectionEdges[index];
            RoadAttachment attachment1 = Attachments.Find(a => a.KnotGuid == edge.Knot1);
            RoadAttachment attachment2 = Attachments.Find(a => a.KnotGuid == edge.Knot2);
            return GetMiddle(attachment1, attachment2);
        }

        Vector3 GetRoadIntersection(Vector3 p1, Vector3 d1, Vector3 p2, Vector3 d2)
        {
            Vector2 a = new Vector2(p1.x, p1.z);
            Vector2 b = new Vector2(d1.x, d1.z).normalized;
            Vector2 c = new Vector2(p2.x, p2.z);
            Vector2 d = new Vector2(d2.x, d2.z).normalized;

            float cross = b.x * d.y - b.y * d.x;
            if (Mathf.Abs(cross) < 1e-5f)
            {
                // Parallel � fallback to midpoint
                return (p1 + p2) * 0.5f;
            }

            Vector2 diff = c - a;
            float t = (diff.x * d.y - diff.y * d.x) / cross;
            Vector2 intersection = a + b * t;

            return new Vector3(intersection.x, (p1.y + p2.y) * 0.5f, intersection.y);
        }

        private Vector3 GetIntersectionPoint(RoadAttachment attachment1, RoadAttachment attachment2)
        {
            // Road 1
            Road road1 = ProceduralManager.Roads[attachment1.RoadGuid];
            BezierKnot knot1 = ProceduralManager.Knots[attachment1.KnotGuid];
            float width1 = road1.Settings.Width;
            RoadGeneration.GetRoadPositions(road1.Spline, knot1.Progress, width1, out Vector3 road1SideA, out Vector3 road1SideB, out Vector3 tangent1);

            // Road 2
            Road road2 = ProceduralManager.Roads[attachment2.RoadGuid];
            BezierKnot knot2 = ProceduralManager.Knots[attachment2.KnotGuid];
            float width2 = road2.Settings.Width;
            RoadGeneration.GetRoadPositions(road2.Spline, knot2.Progress, width2,  out Vector3 road2SideA, out Vector3 road2SideB, out Vector3 tangent2);

            Vector3 road1Side = (knot1.Progress <= 0.5f) ? road1SideA : road1SideB;
            Vector3 road2Side = (knot2.Progress <= 0.5f) ? road2SideB : road2SideA;

            return GetRoadIntersection(road1Side, tangent1, road2Side, tangent2);
        }
    }
}