using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Utilities.Splines;

namespace Procedural
{
    public class RoadGeneration
    {
        public static Mesh GenerateRoadMesh(Spline spline, RoadSettings settings, out List<SplinePosition> cachedVerticesLeft, out List<SplinePosition> cachedVerticesRight)
        {
            settings.Validate();

            MeshCreator roadMeshCreator = new MeshCreator();

            // Calculate positions from splines
            cachedVerticesLeft = new List<SplinePosition>();
            cachedVerticesRight = new List<SplinePosition>();
            uint quadCount = 0;
            float t = 0.0f;
            float splineLength = spline.Length;
            float maxStep = settings.MaxRoadTilling / splineLength;
            Vector3 lastForward = Vector3.zero;
            List<float> times = new List<float>();

            while (t < 1.0f)
            {
                GetRoadPositions(spline, t, settings.Width, out _, out _, out Vector3 forward);
                GetRoadPositions(spline, Mathf.Min(t + maxStep, 1.0f), settings.Width, out _, out _, out Vector3 forwardNext);

                float dot = Vector3.Dot(forward.normalized, forwardNext.normalized);
                float curvature = Mathf.Clamp01((1.0f - dot) * settings.CurveScale);
                int divisions = Mathf.RoundToInt(Mathf.Lerp(1, settings.MaxRoadDivisions, curvature));
                float step = maxStep / divisions;

                for (int j = 0; j < divisions; j++)
                {
                    times.Add(t + j * step);
                }

                lastForward = forward;
                t += maxStep;
            }
            times.Add(1.0f);

            Vector3 curvePoint1 = Vector3.positiveInfinity;
            Vector3 curvePoint2 = Vector3.positiveInfinity;
            int controlPointCount1 = 0;
            int controlPointCount2 = 0;

            for (int i = 0; i < times.Count; i++)
            {
                float time = times[i];
                GetRoadPositions(spline, time, settings.Width, out Vector3 position1, out Vector3 position2, out Vector3 forward);

                position1 = ProcessPosition(spline, position1, settings.Width, false, ref curvePoint1, ref controlPointCount1, ref cachedVerticesLeft, i, ref times);
                position2 = ProcessPosition(spline, position2, settings.Width, true, ref curvePoint2, ref controlPointCount2, ref cachedVerticesRight, i, ref times);

                cachedVerticesLeft.Add(new SplinePosition(position1, time));
                cachedVerticesRight.Add(new SplinePosition(position2, time));
                quadCount++;
            }

            GetRoadPositions(spline, 1.0f, settings.Width, out Vector3 endPosition1, out Vector3 endPosition2, out _);
            cachedVerticesLeft.Add(new SplinePosition(endPosition1, 1.0f));
            cachedVerticesRight.Add(new SplinePosition(endPosition2, 1.0f));

            if (cachedVerticesLeft.Count == 0 || cachedVerticesRight.Count == 0)
            {
                cachedVerticesLeft.Clear();
                cachedVerticesRight.Clear();
                return roadMeshCreator.GetMesh();
            }

            for (int j = 1; j < quadCount + 1; j++)
            {
                Vector3 prevLeft = cachedVerticesLeft[j - 1].Position;
                Vector3 prevRight = cachedVerticesRight[j - 1].Position;
                Vector3 currLeft = cachedVerticesLeft[j].Position;
                Vector3 currRight = cachedVerticesRight[j].Position;

                roadMeshCreator.AddQuad(prevLeft, prevRight, currLeft, currRight, 0, 1, 0, 1);
            }

            return roadMeshCreator.GetMesh();
        }

        public static Mesh GenerateIntersectionMesh(ProceduralManager proceduralManager, Intersection intersection, IntersectionSettings settings)
        {
            MeshCreator intersectionMeshCreator = new MeshCreator();

            Vector3 attachmentCenter = GetAttachmentCenter(proceduralManager, intersection.Attachments);

            List<VertexInfo> vertexInfos = new List<VertexInfo>();
            Vector3 center = Vector3.zero;

            // Get positions and save the knots
            foreach (RoadAttachment attachment in intersection.Attachments)
            {
                Road targetRoad = proceduralManager.Roads[attachment.RoadGuid];
                BezierKnot targetKnot = proceduralManager.Knots[attachment.KnotGuid];
                float roadWidth = targetRoad.Settings.Width;

                GetIntersectionPositions(targetRoad, targetKnot, attachmentCenter, roadWidth, roadWidth, settings, out Vector3 position1, out Vector3 position2, out _, out float progress1, out float progress2, out bool side);

                vertexInfos.Add(new VertexInfo { RoadGuid = attachment.RoadGuid, KnotGuid = attachment.KnotGuid, Position = position1, Progress = progress1, Side = side });
                vertexInfos.Add(new VertexInfo { RoadGuid = attachment.RoadGuid, KnotGuid = attachment.KnotGuid, Position = position2, Progress = progress2, Side = side });

                center += position1;
                center += position2;
            }

            if (vertexInfos.Count == 0)
            {
                return intersectionMeshCreator.GetMesh();
            }

            center /= vertexInfos.Count;

            vertexInfos.Sort((a, b) =>
            {
                float angleA = Mathf.Atan2(a.Position.z - center.z, a.Position.x - center.x);
                float angleB = Mathf.Atan2(b.Position.z - center.z, b.Position.x - center.x);
                return angleA.CompareTo(angleB);
            });

            // Triangle Fan methode, faster but worse geometry
            // Iterate edges between sorted vertices
            // for (int i = 0; i < vertexInfos.Count; i++)
            // {
            //     VertexInfo vertexInfo1 = vertexInfos[i];
            //     VertexInfo vertexInfo2 = vertexInfos[(i + 1) % vertexInfos.Count];
            //
            //     int edgeIndex = GetIntersectionEdgeIndex(intersection.IntersectionEdges, vertexInfo1.KnotGuid, vertexInfo2.KnotGuid);
            //
            //     if (edgeIndex >= 0)
            //     {
            //         Vector3 prevPoint = EvaluateQuadraticBezier(vertexInfo1.Position, intersection.GetControlPoint(edgeIndex), vertexInfo2.Position, 0.0f);
            //         curve.Vertices.Add(prevPoint);
            //         for (int s = 1; s <= settings.Resolution; s++)
            //         {
            //             float t = (float)s / settings.Resolution;
            //             Vector3 nextPoint = EvaluateQuadraticBezier(vertexInfo1.Position, intersection.GetControlPoint(edgeIndex), vertexInfo2.Position, t);
            //             intersectionMeshCreator.AddTriangleWorldSpace(center, nextPoint, prevPoint);
            //
            //             prevPoint = nextPoint;
            //         }
            //     }
            //     else
            //     {
            //         intersectionMeshCreator.AddTriangleWorldSpace(center, vertexInfo2.Position, vertexInfo1.Position);
            //     }
            // }
            //
            // return intersectionMeshCreator.GetMesh();

            // Convert to 2d vector
            List<Vector3> edges = new List<Vector3>();

            for (int i = 0; i < vertexInfos.Count; i++)
            {
                VertexInfo vertexInfo1 = vertexInfos[i];
                VertexInfo vertexInfo2 = vertexInfos[(i + 1) % vertexInfos.Count];

                int edgeIndex = GetIntersectionEdgeIndex(intersection.IntersectionEdges, vertexInfo1.KnotGuid, vertexInfo2.KnotGuid);

                if (edgeIndex >= 0)
                {
                    Vector3 prevPoint = EvaluateQuadraticBezier(vertexInfo1.Position, intersection.GetControlPoint(edgeIndex), vertexInfo2.Position, 0.0f);
                    edges.Add(prevPoint);
                    for (int s = 1; s <= settings.Resolution; s++)
                    {
                        float t = (float)s / settings.Resolution;
                        Vector3 nextPoint = EvaluateQuadraticBezier(vertexInfo1.Position, intersection.GetControlPoint(edgeIndex), vertexInfo2.Position, t);
                        edges.Add(nextPoint);
                    }
                }
                else if (vertexInfo1.Progress > 0.0f && vertexInfo2.Progress > 0.0f)
                {
                    // Add points to fit road
                    Road targetRoad = proceduralManager.Roads[vertexInfo1.RoadGuid];

                    float minProgress = Mathf.Min(vertexInfo1.Progress, vertexInfo2.Progress);
                    float maxProgress = Mathf.Max(vertexInfo1.Progress, vertexInfo2.Progress);

                    List<SplinePosition> roadVertices = targetRoad.GetCachedVertices(vertexInfo1.Side);

                    foreach (SplinePosition splinePosition in roadVertices)
                    {
                        if (splinePosition.Progress > minProgress && splinePosition.Progress < maxProgress)
                        {
                            edges.Add(splinePosition.Position);
                        }   
                    }
                }
            }

            Vector2[] vertices2 = new Vector2[edges.Count];
            for (int i = 0; i < edges.Count; i++)
            {
                vertices2[i] = new Vector2(edges[i].x, edges[i].z);
            }

            Triangulator triangulator = new Triangulator(vertices2);
            int[] indices = triangulator.Triangulate();

            Mesh mesh = new Mesh();
            mesh.SetVertices(edges);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();

            if (Vector3.Dot(mesh.normals[1], Vector3.up) < 0)
            {
                for (int i = 0; i < indices.Length; i += 3)
                {
                    (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
                }

                mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals();
            }

            //mesh.SetUVs(0, m_Uvs);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh GenerateSidewalkRoadMesh(ProceduralManager proceduralManager, SidewalkSettings settings, GUID sidewalkGuid, GUID roadGuid, int side, List<SidewalkBreak> breaks, out List<SidewalkCache> cache)
        {
            MeshCreator sidewalkMeshCreator = new MeshCreator();

            IReadOnlyList<SidewalkBreak> mergedBreaks = ProcessBreaks(breaks);
            cache = new List<SidewalkCache>();

            if (mergedBreaks.Count > 0)
            {
                for (int i = 0; i < mergedBreaks.Count + 1; i++)
                {
                    float start = i == 0 ? 0.0f : mergedBreaks[i - 1].End;
                    float end = i == mergedBreaks.Count ? 1.0f : mergedBreaks[i].Start;

                    GenerateSidewalkRoadMesh(proceduralManager, sidewalkMeshCreator, settings, sidewalkGuid, roadGuid, side, start, end, out SidewalkCache subCache);
                    subCache.Handle.Index = i;
                    cache.Add(subCache);
                }
            }
            else
            {
                GenerateSidewalkRoadMesh(proceduralManager, sidewalkMeshCreator, settings, sidewalkGuid, roadGuid, side, 0.0f, 1.0f, out SidewalkCache subCache);
                cache.Add(subCache);
            }

            return sidewalkMeshCreator.GetMesh();
        }

        public static void GenerateSidewalkRoadMesh(ProceduralManager proceduralManager, MeshCreator sidewalkMeshCreator, SidewalkSettings settings, GUID sidewalkGuid, GUID roadGuid, int side, float start, float end, out SidewalkCache cache)
        {
            Road road = proceduralManager.Roads[roadGuid];
            Spline spline = road.Spline;
            RoadSettings roadSettings = road.Settings;
            roadSettings.Validate();

            // Calculate positions from splines
            float t = start;
            float splineLength = spline.Length;
            float maxStep = roadSettings.MaxRoadTilling / splineLength;
            List<float> times = new List<float>();

            while (t < end)
            {
                GetRoadPositions(spline, t, roadSettings.Width, out _, out _, out Vector3 forward);
                GetRoadPositions(spline, Mathf.Min(t + maxStep, end), roadSettings.Width, out _, out _, out Vector3 forwardNext);

                float dot = Vector3.Dot(forward.normalized, forwardNext.normalized);
                float curvature = Mathf.Clamp01((1.0f - dot) * roadSettings.CurveScale);
                int divisions = Mathf.RoundToInt(Mathf.Lerp(1, roadSettings.MaxRoadDivisions, curvature));
                float step = maxStep / divisions;

                for (int j = 0; j < divisions; j++)
                {
                    times.Add(t + j * step);
                }

                t += maxStep;
            }
            times.Add(end);

            // Calculate inner vertices
            List<Vector3> innerVertices = new List<Vector3>();
            List<Vector3> outerVertices = new List<Vector3>();

            Vector3 curvePoint1 = Vector3.positiveInfinity;
            Vector3 curvePoint2 = Vector3.positiveInfinity;
            int controlPointCount1 = 0;
            int controlPointCount2 = 0;

            for (int i = 0; i < times.Count; i++)
            {
                float time = times[i];
                GetRoadPositions(spline, time, roadSettings.Width, out Vector3 position1, out Vector3 position2, out Vector3 forward);

                if (side < 0)
                {
                    position1 = ProcessPosition(spline, position1, roadSettings.Width, false, ref curvePoint1, ref controlPointCount1, ref innerVertices, i, ref times);
                    innerVertices.Add(position1);
                }
                else
                {
                    position2 = ProcessPosition(spline, position2, roadSettings.Width, true, ref curvePoint2, ref controlPointCount2, ref innerVertices, i, ref times);
                    innerVertices.Add(position2);
                }
            }

            // Calculate outer vertices
            curvePoint1 = Vector3.positiveInfinity;
            curvePoint2 = Vector3.positiveInfinity;
            controlPointCount1 = 0;
            controlPointCount2 = 0;

            float totalWidth = roadSettings.Width + settings.Width;

            for (int i = 0; i < times.Count; i++)
            {
                float time = times[i];
                GetRoadPositions(spline, time, totalWidth, out Vector3 position1, out Vector3 position2, out Vector3 forward);

                if (side < 0)
                {
                    position1 = ProcessPosition(spline, position1, totalWidth, false, ref curvePoint1, ref controlPointCount1, ref outerVertices, i, ref times);
                    outerVertices.Add(position1);
                }
                else
                {
                    position2 = ProcessPosition(spline, position2, totalWidth, true, ref curvePoint2, ref controlPointCount2, ref outerVertices, i, ref times);
                    outerVertices.Add(position2);
                }
            }

            if (innerVertices.Count == 0 || outerVertices.Count == 0 && innerVertices.Count == outerVertices.Count)
            {
                cache = new SidewalkCache(new SidewalkHandle(sidewalkGuid, ProceduralMeshType.RoadSidewalk, false, 0), new List<Vector3>());
                return;
            }

            // Generate mesh top face
            if (side > 0)
                sidewalkMeshCreator.StartTriangleStrip(outerVertices[0] + Vector3.up * settings.Height, innerVertices[0] + Vector3.up * settings.Height, Vector2.zero, Vector2.up);
            else
                sidewalkMeshCreator.StartTriangleStrip(innerVertices[0] + Vector3.up * settings.Height, outerVertices[0] + Vector3.up * settings.Height, Vector2.zero, Vector2.up);

            for (int i = 1; i < innerVertices.Count; i++)
            {
                float u = (float)i / (innerVertices.Count - 1);

                if (side > 0)
                {
                    sidewalkMeshCreator.AddStripPoint(outerVertices[i] + Vector3.up * settings.Height, new Vector2(u, 0.0f));
                    sidewalkMeshCreator.AddStripPoint(innerVertices[i] + Vector3.up * settings.Height, new Vector2(u, 1.0f));
                }
                else
                {
                    sidewalkMeshCreator.AddStripPoint(innerVertices[i] + Vector3.up * settings.Height, new Vector2(u, 1.0f));
                    sidewalkMeshCreator.AddStripPoint(outerVertices[i] + Vector3.up * settings.Height, new Vector2(u, 0.0f));
                }
            }

            sidewalkMeshCreator.FinishTriangleStrip();

            // Generate mesh side
            if (side > 0)
                sidewalkMeshCreator.StartTriangleStrip(innerVertices[0] + Vector3.up * settings.Height, innerVertices[0], Vector2.zero, Vector2.up);
            else
                sidewalkMeshCreator.StartTriangleStrip(innerVertices[0], innerVertices[0] + Vector3.up * settings.Height, Vector2.zero, Vector2.up);

            for (int i = 1; i < innerVertices.Count; i++)
            {
                float u = (float)i / (innerVertices.Count - 1);

                if (side > 0)
                {
                    sidewalkMeshCreator.AddStripPoint(innerVertices[i] + Vector3.up * settings.Height, new Vector2(u, 0.0f));
                    sidewalkMeshCreator.AddStripPoint(innerVertices[i], new Vector2(u, 1.0f));
                }
                else
                {
                    sidewalkMeshCreator.AddStripPoint(innerVertices[i], new Vector2(u, 1.0f));
                    sidewalkMeshCreator.AddStripPoint(innerVertices[i] + Vector3.up * settings.Height, new Vector2(u, 0.0f));
                }
            }

            sidewalkMeshCreator.FinishTriangleStrip();

            // Adjust outer vertices with their height
            for (int i = 0; i < outerVertices.Count; i++)
            {
                outerVertices[i] += Vector3.up * settings.Height;
            }

            cache = new SidewalkCache(new SidewalkHandle(sidewalkGuid, ProceduralMeshType.RoadSidewalk, false, 0), outerVertices);
        }

        /// <summary>
        /// Removes invalid breaks and merges breaks when possible. Expects sorted breask by their start value
        /// </summary>
        public static IReadOnlyList<SidewalkBreak> ProcessBreaks(List<SidewalkBreak> breaks)
        {
            List<SidewalkBreak> newBreaks = new List<SidewalkBreak>();

            if (breaks.Count < 2)
            {
                return breaks;
            }

            // Merging intervals algorithm

            float currentStart = breaks[0].Start;
            float currentEnd = breaks[0].End;

            for (int i = 1; i < breaks.Count; i++)
            {
                // Check if current interval overlaps with the next interval
                if (currentEnd < breaks[i].Start)
                {
                    // No overlap
                    newBreaks.Add(new SidewalkBreak(GUID.None, currentStart, currentEnd));
                    currentStart = breaks[i].Start;
                    currentEnd = breaks[i].End;
                }
                else
                {
                    currentEnd = Mathf.Max(currentEnd, breaks[i].End);
                }
            }

            // Add last merged interval
            newBreaks.Add(new SidewalkBreak(GUID.None, currentStart, currentEnd));

            return newBreaks;
        }

        public static Mesh GenerateSidewalkIntersectionMesh(ProceduralManager proceduralManager, SidewalkSettings settings, GUID sidewalkGuid, GUID intersectionGuid, RoadAttachment roadAttachment1, RoadAttachment roadAttachment2, out List<SidewalkCache> cache)
        {
            if (!proceduralManager.Intersections.TryGetValue(intersectionGuid, out Intersection intersection))
            {
                cache = new List<SidewalkCache>();
                return null;
            }

            MeshCreator sidewalkMeshCreator = new MeshCreator();

            IntersectionSettings intersectionSettings = intersection.Settings;

            Vector3 attachmentCenter = GetAttachmentCenter(proceduralManager, intersection.Attachments);

            List<VertexInfo2> vertexInfos = new List<VertexInfo2>(4);

            List<Vector3> knotPositions = new List<Vector3>(2);
            List<Vector3> forwards = new List<Vector3>(2);

            Vector3 center = Vector3.zero;

            // Get positions and save the knots
            for (int i = 0; i < 2; i++)
            {
                RoadAttachment attachment = i == 0 ? roadAttachment1 : roadAttachment2;

                Road targetRoad = proceduralManager.Roads[attachment.RoadGuid];
                BezierKnot targetKnot = proceduralManager.Knots[attachment.KnotGuid];

                RoadSettings roadSettings = targetRoad.Settings;
                roadSettings.Validate();

                GetIntersectionPositions(targetRoad, targetKnot, attachmentCenter, roadSettings.Width, roadSettings.Width, intersectionSettings, out Vector3 positionInner1, out Vector3 positionInner2, out Vector3 forward, out float progress1, out float progress2, out bool side);

                if (progress1 != progress2)
                {
                    progress1 = Mathf.Clamp01(progress1);
                    progress2 = Mathf.Clamp01(progress2);

                    if (progress2 < progress1)
                    {
                        (progress1, progress2) = (progress2, progress1);
                    }
                }

                knotPositions.Add(targetKnot.Position);
                forwards.Add(forward);

                Vector3 positionOuter1, positionOuter2;

                float rightWidth = settings.Width;
                float leftWidth = settings.Width;

                float rightHeight = settings.Height;
                float leftHeight = settings.Height;
                
                // Check if needs to use road sidewalk width
                if (proceduralManager.RoadSidewalks.TryGetValue(targetRoad.Guid, out List<GUID> sidewalkGuids))
                {
                    foreach (GUID otherSidewalkGuid in sidewalkGuids)
                    {
                        if (proceduralManager.Sidewalks.TryGetValue(otherSidewalkGuid, out Sidewalk sidewalk))
                        {
                            sidewalk.Settings.Validate();
                            
                            if (targetKnot.Progress <= 0.0 || targetKnot.Progress >= 1.0f)
                            {
                                if (sidewalk.Type == SidewalkType.RoadLeft)
                                {
                                    rightWidth = sidewalk.Settings.Width;
                                    rightHeight = sidewalk.Settings.Height;
                                }
                                else if (sidewalk.Type == SidewalkType.RoadRight)
                                {
                                    leftWidth = sidewalk.Settings.Width;
                                    leftHeight = sidewalk.Settings.Height;
                                }
                            }
                            else
                            {
                                GetRoadPositions(targetRoad.Spline, targetKnot.Progress, 1.0f, 1.0f, out Vector3 left, out Vector3 right, out _, out _);

                                bool usable = false;

                                if (Vector3.Distance(left, attachmentCenter) < Vector3.Distance(right, attachmentCenter))
                                {
                                    if (sidewalk.Type == SidewalkType.RoadLeft)
                                    {
                                        usable = true;

                                        rightWidth = sidewalk.Settings.Width;
                                        rightHeight = sidewalk.Settings.Height;

                                        leftWidth = sidewalk.Settings.Width;
                                        leftHeight = sidewalk.Settings.Height;
                                    }
                                }
                                else
                                {
                                    if (sidewalk.Type == SidewalkType.RoadRight)
                                    {
                                        usable = true;

                                        rightWidth = sidewalk.Settings.Width;
                                        rightHeight = sidewalk.Settings.Height;

                                        leftWidth = sidewalk.Settings.Width;
                                        leftHeight = sidewalk.Settings.Height;
                                    }
                                }

                                if (usable && progress1 != progress2)
                                {
                                    sidewalk.AddBreak(intersectionGuid, progress1, progress2);
                                    sidewalk.Generate(false);
                                }
                            }
                        }
                    }

                    float roadWidthLeft = roadSettings.Width + leftWidth;
                    float roadWidthRight = roadSettings.Width + rightWidth;

                    if (targetKnot.Progress <= 0.0 || targetKnot.Progress >= 1.0f)
                    {
                        GetRoadPositions(targetRoad.Spline, targetKnot.Progress, roadWidthLeft, roadWidthRight, out positionOuter1, out positionOuter2, out _);
                    }
                    else
                    {
                        // Almost the same as GetIntersectionPositions() but with the previous progress
                        
                        float sideMultiplier = side ? 1.0f : -1.0f;

                        targetRoad.Spline.Evaluate(progress2, out Vector3 middle1, out Vector3 forward1, out Vector3 up1);
                        targetRoad.Spline.Evaluate(progress1, out Vector3 middle2, out Vector3 forward2, out Vector3 up2);

                        Vector3 right1 = Vector3.Cross(forward1, up1).normalized;
                        Vector3 right2 = Vector3.Cross(forward2, up2).normalized;

                        positionOuter1 = middle1 + right1 * (sideMultiplier * roadWidthLeft);
                        positionOuter2 = middle2 + right2 * (sideMultiplier * roadWidthRight);
                    }
                }
                else
                {
                    float sidewalkWidth = roadSettings.Width + settings.Width;
                    GetIntersectionPositions(targetRoad, targetKnot, attachmentCenter, sidewalkWidth, sidewalkWidth, intersectionSettings, out positionOuter1, out positionOuter2, out _, out _, out _, out _);
                }

                vertexInfos.Add(new VertexInfo2 { KnotGuid = attachment.KnotGuid, InnerPosition = positionInner1, OuterPosition = positionOuter1, Height = rightHeight, Width = rightWidth });
                vertexInfos.Add(new VertexInfo2 { KnotGuid = attachment.KnotGuid, InnerPosition = positionInner2, OuterPosition = positionOuter2, Height = leftHeight, Width = leftWidth });

                center += positionInner1;
                center += positionInner2;
            }

            center /= 4;

            List<bool> flipped = new List<bool>(2);

            for (int i = 0; i < 2; i++)
            {
                Vector3 direction1 = center - knotPositions[i];
                flipped.Add(Vector3.Dot(direction1, forwards[i]) > 0.0f);
            }

            // Use center two vertices
            VertexInfo2 vertexInfo1 = vertexInfos[flipped[0] ? 1 : 0];
            VertexInfo2 vertexInfo2 = vertexInfos[flipped[1] ? 2 : 3];
            
            List<Vector3> innerVertices = new List<Vector3>();
            List<float> innerHeights = new List<float>(2);

            List<Vector3> outerVertices = new List<Vector3>();
            List<float> outerHeights = new List<float>();

            // Find intersection edge by matching knot GUIDs (not expecting many entries, so should be fast)
            int edgeIndex = GetIntersectionEdgeIndex(intersection.IntersectionEdges, vertexInfo1.KnotGuid, vertexInfo2.KnotGuid);

            if (edgeIndex >= 0)
            {
                // Calculate outer and inner control point
                Vector3 innerControlPoint = intersection.GetControlPoint(edgeIndex);
                Vector3 innerDirection = vertexInfo1.InnerPosition - vertexInfo2.InnerPosition;
                innerDirection = new Vector3(-innerDirection.z, 0, innerDirection.x).normalized;
                float averageWidth = 0.5f * (vertexInfo1.Width + vertexInfo2.Width);
                Vector3 outerControlPoint = innerControlPoint + innerDirection * averageWidth;

                Vector3 outerPoint = EvaluateQuadraticBezier(vertexInfo1.OuterPosition, outerControlPoint, vertexInfo2.OuterPosition, 0.0f);
                Vector3 innerPoint = EvaluateQuadraticBezier(vertexInfo1.InnerPosition, innerControlPoint, vertexInfo2.InnerPosition, 0.0f);

                innerVertices.Add(innerPoint);
                outerVertices.Add(outerPoint);

                innerHeights.Add(vertexInfo1.Height);
                outerHeights.Add(vertexInfo1.Height);

                for (int s = 1; s <= intersectionSettings.Resolution; s++)
                {
                    float t = (float)s / intersectionSettings.Resolution;

                    outerPoint = EvaluateQuadraticBezier(vertexInfo1.OuterPosition, outerControlPoint, vertexInfo2.OuterPosition, t);
                    innerPoint = EvaluateQuadraticBezier(vertexInfo1.InnerPosition, innerControlPoint, vertexInfo2.InnerPosition, t);

                    innerVertices.Add(innerPoint);
                    outerVertices.Add(outerPoint);

                    float height = Mathf.Lerp(vertexInfo1.Height, vertexInfo2.Height, t);
                    innerHeights.Add(height);
                    outerHeights.Add(height);
                }
            }

            // Generate mesh top faces
            sidewalkMeshCreator.StartTriangleStrip(outerVertices[0] + Vector3.up * outerHeights[0], innerVertices[0] + Vector3.up * innerHeights[0], Vector2.zero, Vector2.zero);

            for (int j = 1; j < innerVertices.Count; j++)
            {
                sidewalkMeshCreator.AddStripPoint(outerVertices[j] + Vector3.up * outerHeights[j], Vector2.zero);
                sidewalkMeshCreator.AddStripPoint(innerVertices[j] + Vector3.up * innerHeights[j], Vector2.zero);
            }

            sidewalkMeshCreator.FinishTriangleStrip();

            // Generate mesh side faces
            sidewalkMeshCreator.StartTriangleStrip(innerVertices[0] + Vector3.up * innerHeights[0], innerVertices[0], Vector2.zero, Vector2.zero);

            for (int j = 1; j < innerVertices.Count; j++)
            {
                sidewalkMeshCreator.AddStripPoint(innerVertices[j] + Vector3.up * innerHeights[j], Vector2.zero);
                sidewalkMeshCreator.AddStripPoint(innerVertices[j], Vector2.zero);
            }

            sidewalkMeshCreator.FinishTriangleStrip();

            // Adjust outer vertices with their height
            for (int i = 0; i < outerVertices.Count; i++)
            {
                outerVertices[i] += Vector3.up * outerHeights[i];
            }

            cache = new List<SidewalkCache>
            {
                new SidewalkCache(new SidewalkHandle(sidewalkGuid, ProceduralMeshType.IntersectionSidewalk, false, 0), outerVertices)
            };

            return sidewalkMeshCreator.GetMesh();
        }

        private static int GetIntersectionEdgeIndex(List<IntersectionEdge> intersectionEdges, GUID knotGuid1, GUID knotGuid2)
        {
            int edgeIndex;

            if (knotGuid1 != knotGuid2)
            {
                if (intersectionEdges.Count > 2)
                {
                    // Find intersection edge by matching knot GUIDs (not expecting many entries, so should be fast)
                    edgeIndex = intersectionEdges.FindIndex(e => (e.Knot1 == knotGuid1 && e.Knot2 == knotGuid2) || (e.Knot1 == knotGuid2 && e.Knot2 == knotGuid1));
                }
                else
                {
                    edgeIndex = intersectionEdges.FindIndex(e => (e.Knot1 == knotGuid1 && e.Knot2 == knotGuid2));
                }
            }
            else
            {
                edgeIndex = -1;
            }

            return edgeIndex;
        }

        private static Vector3 GetAttachmentCenter(ProceduralManager proceduralManager, List<RoadAttachment> attachments)
        {
            Vector3 attachmentCenter = Vector3.zero;

            foreach (RoadAttachment attachment in attachments)
            {

                Road targetRoad = proceduralManager.Roads[attachment.RoadGuid];
                BezierKnot targetKnot = proceduralManager.Knots[attachment.KnotGuid];
                attachmentCenter += targetKnot.Position;
            }

            return attachmentCenter / attachments.Count;
        }

        private static void GetIntersectionPositions(Road targetRoad, BezierKnot targetKnot, Vector3 attachmentCenter, float roadWidthLeft, float roadWidthRight, IntersectionSettings settings, out Vector3 position1, out Vector3 position2, out Vector3 forward, out float progress1, out float progress2, out bool side)
        {
            if (targetKnot.Progress <= 0.0 || targetKnot.Progress >= 1.0f)
            {
                // Road attachment is at either end of the road
                GetRoadPositions(targetRoad.Spline, targetKnot.Progress, roadWidthLeft, roadWidthRight, out position1, out position2, out forward);

                progress1 = 0.0f;
                progress2 = 0.0f;
                side = false;
            }
            else
            {
                // Road attachment is in the middle of the road

                // Calculate side
                GetRoadPositions(targetRoad.Spline, targetKnot.Progress, roadWidthLeft, roadWidthRight, out Vector3 left, out Vector3 right, out Vector3 tangent, out Vector3 up);

                Vector3 targetMiddlePoint = Vector3.Distance(left, attachmentCenter) < Vector3.Distance(right, attachmentCenter) ? left : right;
                side = Vector3.Distance(left, attachmentCenter) < Vector3.Distance(right, attachmentCenter);
                float sideMultiplier = side ? 1.0f : -1.0f;
                
                Vector3 approximation1 = targetMiddlePoint + tangent * settings.DefaultRoadWidth;
                Vector3 approximation2 = targetMiddlePoint - tangent * settings.DefaultRoadWidth;

                targetRoad.Spline.EvaluateFromPosition(approximation1, out Vector3 middle1, out Vector3 forward1, out Vector3 up1, out progress1);
                targetRoad.Spline.EvaluateFromPosition(approximation2, out Vector3 middle2, out Vector3 forward2, out Vector3 up2, out progress2);

                Vector3 right1 = Vector3.Cross(forward1, up1).normalized;
                Vector3 right2 = Vector3.Cross(forward2, up2).normalized;
                
                position1 = middle1 + right1 * (sideMultiplier * roadWidthLeft);
                position2 = middle2 + right2 * (sideMultiplier * roadWidthRight);

                forward = Vector3.Cross(position2 - position1, Vector3.up).normalized;
            }
        }

        class VertexInfo
        {
            public GUID RoadGuid;
            public GUID KnotGuid;
            public Vector3 Position;
            public float Progress;
            public bool Side;
        }

        class VertexInfo2
        {
            public GUID KnotGuid;
            public Vector3 InnerPosition;
            public Vector3 OuterPosition;
            public float Height;
            public float Width;
        }

        /// <summary>
        /// Sortes the procedural elements in the list based on the position and calculates the their correct winding order. Removes duplicate elements
        /// </summary>
        public static void SortAreaElements(ProceduralManager proceduralManager, ref List<SidewalkHandle> elements)
        {
            if (elements == null || elements.Count == 0) return;

            // Make sure every Guid is in the list only once
            HashSet<GUID> elementGuids = new HashSet<GUID>();

            for (int i = elements.Count - 1; i >= 0; i--)
            {
                GUID guid = elements[i].Guid;
                if (!elementGuids.Add(guid))
                {
                    elements.RemoveAt(i);
                }
            }

            (SidewalkHandle element, Vector3 start, Vector3 end, int index) sidewalkData0 = new();
            List<(SidewalkHandle element, Vector3 start, Vector3 end, int index)> sidewalkData = new();

            for (int i = 0; i < elements.Count; i++)
            {
                if (proceduralManager.Sidewalks.TryGetValue(elements[i].Guid, out var sidewalk))
                {
                    if (sidewalk.CachedData != null && sidewalk.CachedData.Count > 0 && sidewalk.CachedData[elements[i].Index].Positions.Count > 1)
                    {
                        if (i == 0)
                        {
                            sidewalkData0 = (elements[i], sidewalk.CachedData[elements[i].Index].Positions[0], sidewalk.CachedData[elements[i].Index].Positions[^1], i);
                        }
                        else
                        {
                            sidewalkData.Add((elements[i], sidewalk.CachedData[elements[i].Index].Positions[0], sidewalk.CachedData[elements[i].Index].Positions[^1], i));
                        }
                    }
                }
            }

            if (sidewalkData.Count + 1 != elements.Count) return;

            int[] order = new int[elements.Count];
            order[0] = 0;

            // Element at index 0 is used as based. Reverse always false
            Vector3 lastPosition = sidewalkData0.end;

            for (int counter = 1;  counter < elements.Count; counter++)
            {
                float minDinstance = float.PositiveInfinity;
                int newIndex = 0;
                int tempIndex = 0;
                bool reverse = false;

                for (int i = 0; i < sidewalkData.Count; i++)
                {
                    float distance1 = Vector3.Distance(lastPosition, sidewalkData[i].start);
                    float distance2 = Vector3.Distance(lastPosition, sidewalkData[i].end);
                    float currentDistance = Mathf.Min(distance1, distance2);

                    if (currentDistance < minDinstance)
                    {
                        minDinstance = currentDistance;
                        newIndex = sidewalkData[i].index;
                        tempIndex = i;
                        reverse = distance2 < distance1;
                    }
                }

                order[counter] = newIndex;
                SidewalkHandle element = elements[newIndex];
                element.Reverse = reverse;
                elements[newIndex] = element;
                lastPosition = reverse ? sidewalkData[tempIndex].start : sidewalkData[tempIndex].end;
                sidewalkData.RemoveAt(tempIndex);
            }

            List<SidewalkHandle> sortedElements = new List<SidewalkHandle>(new SidewalkHandle[elements.Count]);

            for (int i = 0; i < elements.Count; i++)
            {
                sortedElements[i] = elements[order[i]];
            }

            elements = sortedElements;
        }

        public static Mesh GenerateBlockArea(ProceduralManager proceduralManager, BlockAreaSettings settings, List<SidewalkHandle> elements, out List<Vector3> vertices)
        {
            settings.Validate();

            vertices = new List<Vector3>();

            if (elements == null || elements.Count == 0) return null;

            if (!proceduralManager.Sidewalks.TryGetValue(elements[0].Guid, out Sidewalk sidewalk)) return null;

            Vector3 previousPosition = sidewalk.CachedData[elements[0].Index].Positions[^1];

            foreach (SidewalkHandle element in elements)
            {
                switch (element.MeshType)
                {
                    case ProceduralMeshType.RoadSidewalk:
                    case ProceduralMeshType.IntersectionSidewalk:
                        if (!proceduralManager.Sidewalks.TryGetValue(element.Guid, out sidewalk)) continue;

                        List<Vector3> sideVertices = sidewalk.CachedData[element.Index].Positions;

                        if (sideVertices == null || sideVertices.Count == 0) continue;

                        Vector3 firstPosition = sideVertices[0];
                        Vector3 lastPosition = sideVertices[^1];

                        if (element.Reverse)
                        {
                            sideVertices.Reverse();
                        }

                        vertices.AddRange(sideVertices);
                        previousPosition = sideVertices[^1];

                        break;

                    case ProceduralMeshType.Road:
                    case ProceduralMeshType.Intersection:
                    default:
                        throw new System.Exception();

                }            
            }

            // Remove points that are close together
            for (int i = vertices.Count - 1; i > 0; i--)
            {
                if (Vector3.Distance(vertices[i], vertices[i - 1]) < 0.005f)
                {
                    vertices.RemoveAt(i);
                }
            }

            vertices.RemoveAt(vertices.Count - 1);

            return GenerateLotMesh(vertices);
        }

        public static Mesh GenerateLotMesh(List<Vector3> edges)
        {
            // Convert to 2d vector
            Vector2[] vertices2 = new Vector2[edges.Count];
            for (int i = 0; i < edges.Count; i++)
            {
                vertices2[i] = new Vector2(edges[i].x, edges[i].z);
            }

            Triangulator triangulator = new Triangulator(vertices2);
            int[] indices = triangulator.Triangulate();

            Mesh mesh = new Mesh();
            mesh.SetVertices(edges);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();

            if (Vector3.Dot(mesh.normals[1], Vector3.up) < 0)
            {
                for (int i = 0; i < indices.Length; i += 3)
                {
                    (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
                }

                mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals();
            }

            //mesh.SetUVs(0, m_Uvs);
            mesh.RecalculateBounds();

            return mesh;
        }

        public static void GetRoadPositions(Spline spline, float step, float width, out Vector3 position1, out Vector3 position2, out Vector3 forward)
        {
            spline.Evaluate(step, out Vector3 position, out forward, out Vector3 up);
            Vector3 right = Vector3.Cross(forward, up).normalized;
            position1 = right * width + position;
            position2 = -right * width + position;
        }

        public static void GetRoadPositions(Spline spline, float step, float leftWidth, float rightWidth, out Vector3 position1, out Vector3 position2, out Vector3 forward)
        {
            spline.Evaluate(step, out Vector3 position, out forward, out Vector3 up);
            Vector3 right = Vector3.Cross(forward, up).normalized;
            position1 = right * rightWidth + position;
            position2 = -right * leftWidth + position;
        }

        public static void GetRoadPositions(Spline spline, float step, float width, out Vector3 position1, out Vector3 position2, out Vector3 forward, out Vector3 up)
        {
            spline.Evaluate(step, out Vector3 position, out forward, out up);
            Vector3 right = Vector3.Cross(forward, up).normalized;
            position1 = right * width + position;
            position2 = -right * width + position;
        }
        
        public static void GetRoadPositions(Spline spline, float step, float leftWidth, float rightWidth, out Vector3 position1, out Vector3 position2, out Vector3 forward, out Vector3 up)
        {
            spline.Evaluate(step, out Vector3 position, out forward, out up);
            Vector3 right = Vector3.Cross(forward, up).normalized;
            position1 = right * rightWidth + position;
            position2 = -right * leftWidth + position;
        }

        private static Vector3 EvaluateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1.0f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        private static Vector3 ProcessPosition(Spline spline, Vector3 position, float width, bool side, ref Vector3 curvePoint, ref int controlPointCount, ref List<SplinePosition> positions, int currentIndex, ref List<float> times)
        {
            spline.GetClosestPointOnSpline(position, out float distance, out _, out _);

            if (float.IsPositiveInfinity(distance))
            {
                curvePoint = position;
                return position;
            }

            if (distance + 0.01f < width && !float.IsInfinity(curvePoint.x))
            {
                controlPointCount++;
                return curvePoint;
            }

            if (controlPointCount > 0) 
            {
                float t0 = times[currentIndex - controlPointCount];
                float t1 = times[currentIndex];

                Vector3 newCurvePoint = GetMiddlePoint(spline, width, side, t0, t1);

                for (int i = currentIndex - controlPointCount; i < currentIndex; i++)
                {
                    positions[i] = new SplinePosition(newCurvePoint, (t0 + t1) * 0.5f);
                }

                controlPointCount = 0;
            }
                
            curvePoint = position;
            return position;
        }

        private static Vector3 ProcessPosition(Spline spline, Vector3 position, float width, bool side, ref Vector3 curvePoint, ref int controlPointCount, ref List<Vector3> positions, int currentIndex, ref List<float> times)
        {
            spline.GetClosestPointOnSpline(position, out float distance, out _, out _);

            if (float.IsPositiveInfinity(distance))
            {
                curvePoint = position;
                return position;
            }

            if (distance + 0.01f < width && !float.IsInfinity(curvePoint.x))
            {
                controlPointCount++;
                return curvePoint;
            }

            if (controlPointCount > 0)
            {
                float t0 = times[currentIndex - controlPointCount];
                float t1 = times[currentIndex];

                Vector3 newCurvePoint = GetMiddlePoint(spline, width, side, t0, t1);

                for (int i = currentIndex - controlPointCount; i < currentIndex; i++)
                {
                    positions[i] = newCurvePoint;
                }

                controlPointCount = 0;
            }

            curvePoint = position;
            return position;
        }

        private static Vector3 GetMiddlePoint(Spline spline, float width, bool side, float start, float end)
        {
            spline.Evaluate(start, out Vector3 startPosition, out _, out _);
            spline.Evaluate(end, out Vector3 endPosition, out _, out _);

            GetRoadPositions(spline, start, width, out Vector3 startLeft, out Vector3 startRight, out _);
            GetRoadPositions(spline, end, width, out Vector3 endLeft, out Vector3 endRight, out _);

            Vector3 a = side ? startRight : startLeft;
            Vector3 b = side ? endRight : endLeft;

            //if (TryGetIntersection(startPosition, a, endPosition, b, out Vector3 intersection))
            //{
            //    return intersection;
            //}

            return Vector3.Lerp(a, b, 0.5f);
        }

        private static bool TryGetIntersection(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2, out Vector3 intersection)
        {
            Vector3 dir1 = end1 - start1;
            Vector3 dir2 = end2 - start2;

            float a1 = dir1.x;
            float b1 = -dir2.x;
            float c1 = start2.x - start1.x;

            float a2 = dir1.z;
            float b2 = -dir2.z;
            float c2 = start2.z - start1.z;

            float det = a1 * b2 - a2 * b1;

            if (Mathf.Abs(det) < 1e-6f)
            {
                // Lines are parallel
                intersection = Vector3.zero;
                return false;
            }

            float t1 = (c1 * b2 - c2 * b1) / det;
            intersection = start1 + dir1 * t1;
            return true;
        }

        private static void SmoothVertices(ref List<Vector3> vertices, int smoothingIterations = 5, float smoothing = 0.1f)
        {
            if (vertices.Count < 3) return;

            for (int iteration = 0; iteration < smoothingIterations; iteration++)
            {
                for (int i = 1; i < vertices.Count - 1; i++)
                {
                    Vector3 lastPosition = vertices[i - 1];
                    Vector3 nextPosition = vertices[i + 1];
                    Vector3 targetPosition = (lastPosition + nextPosition) * 0.5f;

                    // Move towards average
                    vertices[i] = Vector3.Lerp(vertices[i], targetPosition, smoothing);
                }
            }
        }
    }
}