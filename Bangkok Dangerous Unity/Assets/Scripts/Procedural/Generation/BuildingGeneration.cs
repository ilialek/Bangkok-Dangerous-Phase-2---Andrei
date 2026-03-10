using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Procedural
{
    public class BuildingGeneration
    {
        private System.Random m_Random;
        private BuildingSettings m_Settings;
        private BuildingSemantic m_Semantics;

        private float m_StartHeight = 0.0f;

        private List<FacadeList> m_Facades;
        private List<Vector3> m_EndFloor;

        private Dictionary<FacadeHandle, FacadeCutout> m_FacadeCutouts;

        public BuildingGeneration(int seed, BuildingSettings settings, BuildingSemantic semantics)
        {
            m_Random = new System.Random(seed);
            m_Settings = settings;
            m_Semantics = semantics;
            m_Settings.Validate();
        }

        [ContextMenu("Generate Building")]
        public Mesh Generate(List<Vector3> lotVertices, List<FacadeCutout> cutouts, out List<FacadeList> facades)
        {
            MeshCreator buildingMeshCreator = new MeshCreator();

            m_FacadeCutouts = new Dictionary<FacadeHandle, FacadeCutout>();
            if (cutouts != null)
            {
                foreach (FacadeCutout cutout in cutouts)
                {
                    m_FacadeCutouts.Add(cutout.FacadeHandle, cutout);
                }
            }

            m_Facades = new List<FacadeList>();
            m_EndFloor = new List<Vector3>();

            if (lotVertices == null || lotVertices.Count == 0 || m_Settings.StoryCount < 1)
            {
                facades = m_Facades;
                return buildingMeshCreator.GetMesh();
            }

            Polygon lotPolygon = new Polygon();
            lotPolygon.Vertices = new List<Vector2>(lotVertices.Count);
            for (int i = 0; i < lotVertices.Count; i++)
            {
                lotPolygon.Vertices.Add(new Vector2(lotVertices[i].x, lotVertices[i].z));
            }

            lotPolygon.FixWindingOrder();
            Polygon currentPolygon = new Polygon(lotPolygon);

            List<Vector3> floorPoints = currentPolygon.Get3DPolygon(m_StartHeight + m_Settings.HeightOffset);
            Vector3 floorCenter = MeshUtils.GetCenter(floorPoints);
            
            // Generate base facades per level

            // Generate floors
            List<FacadeSemantic> floorSemantics = GetFloorSemantics(m_Settings.StoryCount, m_StartHeight + m_Settings.HeightOffset, out float finalHeight);
            float currentFloorHeight = floorSemantics[0].StoryHeight + m_StartHeight + m_Settings.HeightOffset;
            List<Vector3> level = AddSection(buildingMeshCreator, floorPoints, 0, floorSemantics[0], currentFloorHeight, finalHeight, true);

            for (int i = 1; i < m_Settings.StoryCount; i++)
            {
                bool addFloor = false;
                if (m_Random.NextDouble() < m_Settings.ShrinkChance && currentPolygon.Vertices.Count > 1)
                {
                    Polygon lastRoof = currentPolygon;
                    ShrinkPolygon(ref currentPolygon, out Polygon shrunkPolygon, new Vector2(floorCenter.x, floorCenter.z), out addFloor);

                    // Update roof of last segment
                    if (m_Facades != null && m_Facades.Count > 0)
                    {
                        m_Facades[m_Facades.Count - 1].Roof = shrunkPolygon.Get3DPolygon(currentFloorHeight);
                        m_Facades[m_Facades.Count - 1].HasRoof = true;
                    }
                }

                floorPoints = currentPolygon.Get3DPolygon(currentFloorHeight);
                currentFloorHeight += floorSemantics[i].StoryHeight;
                level = AddSection(buildingMeshCreator, floorPoints, i, floorSemantics[i], currentFloorHeight, finalHeight, addFloor);
            }

            // Mark last story roof
            m_Facades[m_Facades.Count - 1].HasRoof = true;

            facades = m_Facades;

            //Move transform to mesh center
            //transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            //transform.position += transform.TransformVector(floorCenter);
            //buildingMeshCreator.Move(-floorCenter);
            //meshCreator2.Move(-floorCenter);

            return buildingMeshCreator.GetMesh();
        }
        
        private List<Vector3> AddSection(MeshCreator meshCreator, List<Vector3> floorPoints, int level, FacadeSemantic floorSemantic, float yOffset, float maxHeight, bool addFloor)
        {
            List<Vector3> levelPoints = new List<Vector3>();

            // Add floor
            Triangulate(meshCreator, floorPoints);
            Triangulate(meshCreator, floorPoints, true);

            m_EndFloor.Clear();

            FacadeSemantic facadeSemantic = RandomizeFacadeSemantic(floorSemantic.StoryHeight, level == 0);

            if (facadeSemantic == null)
            {
                Debug.LogWarning("Can not add section without facade semantic assigned");
                return new List<Vector3>();
            }

            FacadeList result = new FacadeList(floorSemantic.StoryHeight, facadeSemantic);
            
            // Extrude floor mesh
            for (int i = 0; i < floorPoints.Count; i++)
            {
                Vector3 pointB = floorPoints[i];
                Vector3 pointA = floorPoints[(i + 1) % floorPoints.Count];

                Vector3 last = floorPoints[(i + (floorPoints.Count - 1)) % floorPoints.Count];
                Vector3 next = floorPoints[(i + 2) % floorPoints.Count];

                Vector3 topA = new Vector3(pointA.x, yOffset, pointA.z);
                Vector3 topB = new Vector3(pointB.x, yOffset, pointB.z);

                Quad3D facadeQuad = new Quad3D(pointA, pointB, topB, topA);

                FacadeHandle handle = new FacadeHandle(level, i);
                Facade facade = new Facade(facadeQuad, handle);

                float wallThickness = facadeSemantic.WallThickness;
                if (floorPoints.Count < 3)
                {
                    wallThickness = 0.0f;
                }

                if (m_FacadeCutouts.TryGetValue(handle, out FacadeCutout cutout))
                {
                    AddCutoutFacade(meshCreator, facadeQuad, last, next, cutout, wallThickness);
                }
                else
                {
                    AddFacade(meshCreator, facadeQuad, last, next, wallThickness);
                }

                result.Facades.Add(facade);
                levelPoints.Add(topA);
            }

            // Create roof mesh
            Triangulate(meshCreator, levelPoints);
            Triangulate(meshCreator, levelPoints, true);
            result.Roof = levelPoints;

            if (result.Facades.Count > 0)
            {
                m_Facades.Add(result);
            }

            return levelPoints;
        }

        public static void AddFacade(MeshCreator target, Quad3D facadeQuad, Vector3 last, Vector3 next, float wallThickness = 0.0f)
        {
            // Adding uvs:
            //float width = Vector3.Distance(pointA, pointB) / m_Settings.MaxWidth;
            //meshCreator.AddQuad(facadeQuad, (1.0f - width) / 2.0f, 1.0f - ((1.0f - width) / 2.0f), pointA.y / maxHeight, yOffset / maxHeight);

            //if (wallThickness > 0.0f)
            //{
            //    Quad3D innerWall = GetInnerWall(facadeQuad, next, last, yOffset, wallThickness);
            //    meshCreator.AddQuad(innerWall, (1.0f - width) / 2.0f, 1.0f - ((1.0f - width) / 2.0f), pointA.y / maxHeight, yOffset / maxHeight);
            //}

            float upperY = facadeQuad.TopRight.y;
            target.AddQuad(facadeQuad);

            if (wallThickness > 0.0f)
            {
                Quad3D innerWall = GetInnerWall(facadeQuad, next, last, upperY, wallThickness);
                target.AddQuad(innerWall);
            }
        }

        public static void AddCutoutFacade(MeshCreator target, Quad3D facadeQuad, Vector3 last, Vector3 next,  FacadeCutout cutout, float wallThickness = 0.0f)
        {
            Vector3 topRight, bottomRight;

            float lowerY = facadeQuad.BottomRight.y;
            float upperY = facadeQuad.TopRight.y;

            for (int j = 0; j < cutout.Quads.Count; j++)
            {
                // Handle top side, bot side, and left side
                Quad3D cutoutQuad = cutout.Quads[j];

                Vector3 cutLeftTop = new Vector3(cutoutQuad.TopLeft.x, upperY, cutoutQuad.TopLeft.z);
                Vector3 cutRightTop = new Vector3(cutoutQuad.TopRight.x, upperY, cutoutQuad.TopRight.z);
                Vector3 cutLeftBottom = new Vector3(cutoutQuad.BottomLeft.x, lowerY, cutoutQuad.BottomLeft.z);
                Vector3 cutRightBottom = new Vector3(cutoutQuad.BottomRight.x, lowerY, cutoutQuad.BottomRight.z);

                if (j + 1 < cutout.Quads.Count)
                {
                    Quad3D nextQuad = cutout.Quads[j + 1];
                    topRight = new Vector3(nextQuad.TopLeft.x, upperY, nextQuad.TopLeft.z);
                    bottomRight = new Vector3(nextQuad.BottomLeft.x, lowerY, nextQuad.BottomLeft.z);
                }
                else
                {
                    topRight = facadeQuad.TopLeft;
                    bottomRight = facadeQuad.BottomLeft;
                }

                WindowPosition windowPosition = WindowPosition.None;

                if (j == 0)
                    windowPosition |= WindowPosition.Start;
                if (j == cutout.Quads.Count - 1)
                    windowPosition |= WindowPosition.End;

                Quad3D windowFacadeQuad = facadeQuad;

                if (!windowPosition.HasFlag(WindowPosition.Start))
                {
                    Quad3D previousQuad = cutout.Quads[j - 1];
                    windowFacadeQuad.BottomRight = previousQuad.BottomRight;
                    windowFacadeQuad.TopRight = previousQuad.TopRight;
                }

                Quad3D secondaryQuad = new Quad3D(cutLeftBottom, bottomRight, topRight, cutLeftTop);
                TriangulateCutoutWallSegment(target, windowPosition, windowFacadeQuad, cutoutQuad, secondaryQuad);

                // Extrude inwards
                if (wallThickness > 0.0f)
                {
                    // Generate side walls of window
                    Vector3 innerOffset = cutoutQuad.Normal * wallThickness;
                    Quad3D innerCut = cutoutQuad - innerOffset;

                    target.StartTriangleStrip(cutoutQuad.TopLeft, innerCut.TopLeft, Vector2.zero, Vector2.zero);
                    target.AddStripPoint(cutoutQuad.TopRight, Vector2.zero);
                    target.AddStripPoint(innerCut.TopRight, Vector2.zero);
                    target.AddStripPoint(cutoutQuad.BottomRight, Vector2.zero);
                    target.AddStripPoint(innerCut.BottomRight, Vector2.zero);
                    target.AddStripPoint(cutoutQuad.BottomLeft, Vector2.zero);
                    target.AddStripPoint(innerCut.BottomLeft, Vector2.zero);
                    target.AddStripPoint(cutoutQuad.TopLeft, Vector2.zero);
                    target.AddStripPoint(innerCut.TopLeft, Vector2.zero);
                    target.FinishTriangleStrip();

                    // Generate inner wall
                    Quad3D innerWall = GetInnerWall(facadeQuad, next, last, upperY, wallThickness);
                    Quad3D innerWindowFacadeQuad = windowFacadeQuad - innerOffset;
                    Quad3D innerCutoutQuad = cutoutQuad - innerOffset;
                    Quad3D innerSecondaryQuad = secondaryQuad - innerOffset;

                    if (windowPosition.HasFlag(WindowPosition.Start))
                    {
                        innerWindowFacadeQuad.TopRight = innerWall.TopLeft;
                        innerWindowFacadeQuad.BottomRight = innerWall.BottomLeft;
                    }

                    if (windowPosition.HasFlag(WindowPosition.End))
                    {
                        innerWindowFacadeQuad.TopLeft = innerWall.TopRight;
                        innerWindowFacadeQuad.BottomLeft = innerWall.BottomRight;

                        innerSecondaryQuad.TopRight = innerWall.TopRight;
                        innerSecondaryQuad.BottomRight = innerWall.BottomRight;
                    }

                    TriangulateCutoutWallSegment(target, windowPosition, innerWindowFacadeQuad, innerCutoutQuad, innerSecondaryQuad, true);
                }
            }
        }

        private FacadeSemantic RandomizeFacadeSemantic(float targetHeight, bool isFront)
        {
            if (isFront)
            {
                if (m_Semantics.FrontSemantics == null || m_Semantics.FrontSemantics.Count == 0)
                {
                    Debug.LogWarning("Missing front semantic in the building semantic - Can not create front floor");
                    return null;
                }

                IEnumerable<FacadeSemantic> possibleSemantics = m_Semantics.FrontSemantics.Where(semantics => Mathf.Approximately(semantics.StoryHeight, targetHeight));
                int randomFacade = m_Random.Next(possibleSemantics.Count());
                return possibleSemantics.ElementAt(randomFacade);
            }
            else
            {
                if (m_Semantics.UpperFacades == null || m_Semantics.UpperFacades.Count == 0)
                {
                    Debug.LogWarning("Missing upper semantic in the building semantic - Can not create upper floor");
                    return null;
                }

                IEnumerable<FacadeSemantic> possibleSemantics = m_Semantics.UpperFacades.Where(semantics => Mathf.Approximately(semantics.StoryHeight, targetHeight));
                int randomFacade = m_Random.Next(possibleSemantics.Count());
                return possibleSemantics.ElementAt(randomFacade);
            }
        }

        private enum WindowPosition 
        {
            None = 0,
            Start = 1 << 0,
            End = 1 << 1
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TriangulateCutoutWallSegment(MeshCreator meshCreator, WindowPosition windowPosition, Quad3D facadeQuad, Quad3D cutoutQuad, Quad3D secondaryQuad, bool invert = false)
        {
            if (windowPosition.HasFlag(WindowPosition.Start)) // Start
            {
                meshCreator.StartTriangleStrip(secondaryQuad.TopRight, cutoutQuad.TopRight, Vector2.zero, Vector2.zero, !invert);
                meshCreator.AddStripPoint(facadeQuad.TopRight, Vector2.zero);
                meshCreator.AddStripPoint(cutoutQuad.TopLeft, Vector2.zero);
                meshCreator.AddStripPoint(facadeQuad.BottomRight, Vector2.zero);
                meshCreator.AddStripPoint(cutoutQuad.BottomLeft, Vector2.zero);
                meshCreator.AddStripPoint(secondaryQuad.BottomRight, Vector2.zero);
                meshCreator.AddStripPoint(cutoutQuad.BottomRight, Vector2.zero);
                meshCreator.FinishTriangleStrip();
            }
            else // Middle or end
            {
                meshCreator.StartTriangleStrip(secondaryQuad.TopRight, cutoutQuad.TopRight, Vector2.zero, Vector2.zero, !invert);
                meshCreator.AddStripPoint(secondaryQuad.TopLeft, Vector2.zero);
                meshCreator.AddStripPoint(cutoutQuad.TopLeft, Vector2.zero);
                meshCreator.AddStripPoint(facadeQuad.TopRight, Vector2.zero);
                meshCreator.AddStripPoint(cutoutQuad.BottomLeft, Vector2.zero);
                meshCreator.AddStripPoint(facadeQuad.BottomRight, Vector2.zero);
                meshCreator.AddStripPoint(secondaryQuad.BottomLeft, Vector2.zero);
                meshCreator.AddStripPointWithLast(cutoutQuad.BottomRight, Vector2.zero);
                meshCreator.AddStripPoint(secondaryQuad.BottomRight, Vector2.zero);
                meshCreator.FinishTriangleStrip();
            }

            if (windowPosition.HasFlag(WindowPosition.End))
            {
                // Finalize right side
                if (invert)
                {
                    Quad3D finalQuad = new Quad3D(facadeQuad.TopLeft, cutoutQuad.TopRight, cutoutQuad.BottomRight, facadeQuad.BottomLeft);
                    meshCreator.AddQuad(finalQuad);
                }
                else
                {
                    Quad3D finalQuad = new Quad3D(facadeQuad.BottomLeft, cutoutQuad.BottomRight, cutoutQuad.TopRight, facadeQuad.TopLeft);
                    meshCreator.AddQuad(finalQuad);
                }
            }
        }

        private void Triangulate(MeshCreator meshCreator, List<Vector3> vertices, bool invert = false)
        {
            // Convert to 2d vector
            Vector2[] vertices2 = new Vector2[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices2[i] = new Vector2(vertices[i].x, vertices[i].z);
            }

            Triangulator triangulator = new Triangulator(vertices2);
            int[] indices = triangulator.Triangulate();

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();

            bool checkInvert = Vector3.Dot(mesh.normals[1], Vector3.up) < 0;

            if (invert) checkInvert = !checkInvert;

            if (checkInvert)
            {
                for (int i = 0; i < indices.Length; i += 3)
                {
                    int tmp = indices[i + 1];
                    indices[i + 1] = indices[i + 2];
                    indices[i + 2] = tmp;
                }
            }

            meshCreator.AddStrip(vertices, indices);
        }

        private static Quad3D GetInnerWall(Quad3D facadeQuad, Vector3 next, Vector3 last, float targetHeight, float wallThickness)
        {
            Vector3 lastFace = (facadeQuad.BottomRight - facadeQuad.BottomLeft).normalized;
            Vector3 currentFace = (facadeQuad.BottomRight - last).normalized;
            Vector3 nextFace = (next - facadeQuad.BottomLeft).normalized;

            Vector3 avg1 = (currentFace + lastFace).normalized;
            Vector3 avg2 = (lastFace + nextFace).normalized;

            // Compute correction so wall thickness stays constant regardless of angle
            float angle1 = Mathf.Acos(Vector3.Dot(lastFace, currentFace));
            float angle2 = Mathf.Acos(Vector3.Dot(nextFace, lastFace));

            float offset1 = wallThickness / Mathf.Sin(angle1 * 0.5f);
            float offset2 = wallThickness / Mathf.Sin(angle2 * 0.5f);

            Vector3 inner1 = facadeQuad.BottomLeft + avg2 * offset2;
            Vector3 inner2 = facadeQuad.BottomRight - avg1 * offset1;

            Vector3 topInner1 = new Vector3(inner1.x, targetHeight, inner1.z);
            Vector3 topInner2 = new Vector3(inner2.x, targetHeight, inner2.z);

            return new Quad3D(inner2, inner1, topInner1, topInner2);
        }

        // Find polygon with most containing vertices
        private MergedPolygon GetBiggestMergedPolygon(List<MergedPolygon> polygons)
        {
            int currentCount = 0;
            int polygonIndex = 0;
            for (int i = 0; i < polygons.Count; i++)
            {
                if (polygons[i].Polygon.Vertices.Count > currentCount)
                {
                    currentCount = polygons[i].ContainingPolygons.Count;
                    polygonIndex = i;
                }
            }

            return polygons[polygonIndex];
        }

        private List<FacadeSemantic> GetFloorSemantics(int floorCount, float heightOffset, out float finalHeight)
        {
            List<FacadeSemantic> floorSemantics = new List<FacadeSemantic>(floorCount);

            finalHeight = heightOffset;
            for (int i = 0; i < floorCount; i++)
            {
                FacadeSemantic floorSemantic;

                if (i == 0)
                {
                    if (m_Semantics.FrontSemantics == null || m_Semantics.FrontSemantics.Count == 0)
                    {
                        Debug.LogWarning("Missing front semantic in the building semantic - Can not create front floor");
                        continue;
                    }

                    int randomFacade = m_Random.Next(0, m_Semantics.FrontSemantics.Count);
                    floorSemantic = m_Semantics.FrontSemantics[randomFacade];
                }
                else
                {
                    if (m_Semantics.UpperFacades == null || m_Semantics.UpperFacades.Count == 0)
                    {
                        Debug.LogWarning("Missing upper semantic in the building semantic - Can not create upper floor");
                        continue;
                    }

                    int randomFacade = m_Random.Next(0, m_Semantics.UpperFacades.Count);
                    floorSemantic = m_Semantics.UpperFacades[randomFacade];
                }

                finalHeight += floorSemantic.StoryHeight;
                floorSemantics.Add(floorSemantic);
            }

            return floorSemantics;
        }

        private void ShrinkPolygon(ref Polygon polygon, out Polygon shrunkPolygon, Vector2 center, out bool generateFloor)
        {
            generateFloor = false;
            int count = polygon.Vertices.Count;

            Polygon newPolygon = new Polygon(new List<Vector2>(polygon.Vertices.Count));
            shrunkPolygon = new Polygon(new List<Vector2>(4));
            
            int randomVertex = m_Random.Next(0, count);
            
            for (int i = randomVertex == count - 1 ? 1 : 0; i < randomVertex; i++)
            {
                newPolygon.Vertices.Add(polygon.Vertices[i]);
            }
            shrunkPolygon.Vertices.Add(polygon.Vertices[randomVertex]);

            Vector2 targetDirection = RotateVector(polygon.Vertices[randomVertex] - polygon.Vertices[(randomVertex + 1) % count], 90).normalized;

            float offsetDistance = -(float)(-m_Settings.ShrinkMin - m_Random.NextDouble() * m_Settings.ShrinkMax);

            if (offsetDistance < 0)
            {
                generateFloor = true;
            }

            for (int i = randomVertex; i <= randomVertex + 1; i++)
            {
                int index = i % count;

                Vector2 direction1 = (polygon.Vertices[index] - polygon.Vertices[(index - 1 + count) % count]).normalized;
                Vector2 direction2 = (polygon.Vertices[index] - polygon.Vertices[(index + 1) % count]).normalized;

                Vector2[] candidates = { direction1, -direction1, direction2, -direction2 };
                Vector2 bestDirection = candidates[0];
                float bestDot = Vector2.Dot(bestDirection, targetDirection);

                for (int j = 1; j < candidates.Length; j++)
                {
                    float dot = Vector2.Dot(candidates[j], targetDirection);
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        bestDirection = candidates[j];
                    }
                }

                Vector3 vertex = polygon.Vertices[index] + bestDirection * offsetDistance;
                newPolygon.Vertices.Add(vertex);
                shrunkPolygon.Vertices.Add(vertex);
            }

            for (int i = randomVertex + 2; i < count; i++)
            {
                newPolygon.Vertices.Add(polygon.Vertices[i % count]);
            }

            shrunkPolygon.Vertices.Add(polygon.Vertices[(randomVertex + 1) % count]);

            if (!newPolygon.GetWinding())
            {
                polygon = new Polygon(newPolygon);
            }
        }

        //private List<Polygon> CreateRandomPolygons(int shapeCount, Polygon container)
        //{
        //    List<Polygon> shapeData = new List<Polygon>();

        //    int baseShapesCount = shapeCount;

        //    // Generate random overlapping shapes
        //    for (int i = 0; i < shapeCount; i++)
        //    {
        //        // Shape shape = m_Settings.ShapeSettings.Value.GetRandomShape((float)m_Random.NextDouble());
        //        if (GeneratePolygonInsideLot(container, shapeCount, 4, out Vector2 shapeCenter, out List<Vector2> shapePoints))
        //        {
        //            shapeData.Add(new Polygon(shapePoints));
        //        }
        //    }

        //    return shapeData;
        //}

        //private bool GeneratePolygonInsideLot(Polygon lot, int totalCount, int sideCount, out Vector2 polygonCenter, out List<Vector2> polygon)
        //{
        //    if (sideCount < 3 || lot.Vertices == null || lot.Vertices.Count < 3)
        //    {
        //        polygonCenter = Vector2.zero;
        //        polygon = null;
        //        return false;
        //    }

        //    float longestEdge = 0.0f;
        //    Vector2 lotDirection = Vector2.right;
        //    for (int i = 0; i < lot.Vertices.Count; i++)
        //    {
        //        Vector2 a = lot.Vertices[i];
        //        Vector2 b = lot.Vertices[(i + 1) % lot.Vertices.Count];
        //        float len = (b - a).magnitude;
        //        if (len > longestEdge)
        //        {
        //            longestEdge = len;
        //            lotDirection = (b - a).normalized;
        //        }
        //    }

        //    float lotAngle = Mathf.Atan2(lotDirection.y, lotDirection.x);

        //    Vector2 center = MeshUtils.GetCenter(lot.Vertices);
        //    polygonCenter = GetRandomPointInPolygon(lot.Vertices, center);
        //    polygonCenter = Vector2.Lerp(center, polygonCenter, m_Settings.CenterEdgePercentage);

        //    float maxRadius = float.MaxValue;
        //    for (int i = 0; i < lot.Vertices.Count; i++)
        //    {
        //        float distance = MeshUtils.DistanceFromPointToEdge(polygonCenter, lot.Vertices[i], lot.Vertices[(i + 1) % lot.Vertices.Count]);
        //        maxRadius = Mathf.Min(maxRadius, distance);
        //    }

        //    // Use a margin to avoid touching edges
        //    float radius = maxRadius * m_Settings.LotUsePercentage;

        //    // Align shape to lot
        //    float startAngle = Mathf.Atan2(lotDirection.y, lotDirection.x);

        //    polygon = new List<Vector2>();

        //    if (sideCount == 4)
        //    {
        //        float width = radius * (0.5f + (float)m_Random.NextDouble());
        //        float height = radius * (0.5f + (float)m_Random.NextDouble());

        //        // Rectangle corners
        //        polygon.Add(polygonCenter + RotateVector(new Vector2(-width, -height) * 0.5f, startAngle));
        //        polygon.Add(polygonCenter + RotateVector(new Vector2(width, -height) * 0.5f, startAngle));
        //        polygon.Add(polygonCenter + RotateVector(new Vector2(width, height) * 0.5f, startAngle));
        //        polygon.Add(polygonCenter + RotateVector(new Vector2(-width, height) * 0.5f, startAngle));
        //    }
        //    else
        //    {
        //        // Regular polygon for other side counts
        //        for (int i = 0; i < sideCount; i++)
        //        {
        //            float angle = startAngle + 2.0f * Mathf.PI * i / sideCount;
        //            Vector2 point = polygonCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        //            polygon.Add(point);
        //        }
        //    }

        //    return true;
        //}

        private Vector2 RotateVector(Vector2 v, float angle)
        {
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        //private Vector2 GetRandomPointInPolygon(List<Vector2> points, Vector2 center)
        //{
        //    // Choose a random triangle
        //    int vertexIndex = m_Random.Next(points.Count);
        //    Vector2 vertexA = points[vertexIndex];
        //    Vector2 vertexB = points[(vertexIndex + 1) % points.Count];

        //    // Choose a random point inside the triangle
        //    float weightEdge = (float)m_Random.NextDouble();
        //    float weightCenter = (float)m_Random.NextDouble();

        //    // Keep the point inside the triangle (flip if needed)
        //    if (weightEdge + weightCenter > 1.0f)
        //    {
        //        weightEdge = 1.0f - weightEdge;
        //        weightCenter = 1.0f - weightCenter;
        //    }

        //    return vertexA + weightEdge * (vertexB - vertexA) + weightCenter * (center - vertexA); ;
        //}
    }
}