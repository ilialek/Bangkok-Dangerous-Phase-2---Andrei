using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Procedural
{
    public class DecorativeGeneration
    {
        private System.Random m_Random;
        private BuildingSemantic m_Semantics;
        private BuildingSettings m_BuildingSettings;

        private List<FacadeCutout> m_Cutouts;

        // Attachments per surface type
        private List<ProceduralAsset> m_WallAttachments;
        private List<ProceduralAsset> m_FrontAttachments;
        private List<ProceduralAsset> m_RoofAttachments;

        private List<OrientedBounds>[] m_ProceduralBounds; // Stores bounds per level

        public DecorativeGeneration(int seed, BuildingSemantic semantics, BuildingSettings buildingSettings)
        {
            m_Random = new System.Random(seed);
            m_Semantics = semantics;
            m_BuildingSettings = buildingSettings;
            
            // Setup lists
            m_WallAttachments = new List<ProceduralAsset>();
            m_FrontAttachments = new List<ProceduralAsset>();
            m_RoofAttachments = new List<ProceduralAsset>();
            foreach (ProceduralAsset decorative in semantics.Attachments)
            {
                if (decorative.Attachment == null) continue;

                if (decorative.Attachment.Target.HasFlag(SurfaceType.Wall))
                {
                    m_WallAttachments.Add(decorative);
                }

                if (decorative.Attachment.Target.HasFlag(SurfaceType.Front))
                {
                    m_FrontAttachments.Add(decorative);
                }

                if (decorative.Attachment.Target.HasFlag(SurfaceType.Roof))
                {
                    m_RoofAttachments.Add(decorative);
                }
            }
        }

        /// <summary>
        /// Decorative Generation pipeline:
        /// 1) Generate Windows, 2) Generate Front, 3) Generate Wall and Front Attachments, Generate Roof Attachments
        /// </summary>
        public void Generate(Building targetBuilding, List<FacadeList> buildingFaces, out List<FacadeCutout> facadeCutouts)
        {
            if (buildingFaces.Count == 0)
            {
                facadeCutouts = new List<FacadeCutout>();
                return;
            }

            m_ProceduralBounds = new List<OrientedBounds>[buildingFaces.Count + 1];

            for (int i = 0; i < m_ProceduralBounds.Length; i++)
            {
                m_ProceduralBounds[i] = new List<OrientedBounds>();
            }

            foreach (FacadeList facadeList in buildingFaces)
            {
                foreach (Facade facade in facadeList.Facades)
                {
                    Vector3 facadePosition = (facade.Face.BottomRight + facade.Face.BottomLeft + facade.Face.TopLeft + facade.Face.TopRight) / 4.0f;
                    float width = Vector3.Distance(facade.Face.BottomRight, facade.Face.BottomLeft);
                    float height = Vector3.Distance(facade.Face.BottomRight, facade.Face.TopRight);

                    Vector3 facadeRotation = (facade.Face.BottomRight - facade.Face.BottomLeft).normalized;
                    Vector3 facadeSize = new Vector3(width, height, 0.0f);

                    // Create facade bounds
                    OrientedBounds facadeBounds = new OrientedBounds(new Bounds(facadePosition, facadeSize), Quaternion.FromToRotation(Vector3.right, facadeRotation));
                    m_ProceduralBounds[facade.Handle.Level].Add(facadeBounds);
                }
            }

            List<ProceduralSurface> facades = GenerateFacade(buildingFaces);
            List<ProceduralSurface2> roofs = GenerateRoofSurfaces(buildingFaces);

            m_Cutouts = new List<FacadeCutout>();

            foreach (ProceduralSurface facade in facades)
            {
                switch (facade.SurfaceType)
                {
                    case SurfaceType.Front:
                        GenerateFront(facade, targetBuilding);
                        break;

                    case SurfaceType.Wall:
                        GenerateWindowFacade(facade, targetBuilding);
                        break;
                }
            }

            foreach (ProceduralSurface2 roof in roofs)
            {
                GenerateAttachments(roof, targetBuilding);
            }

            facadeCutouts = m_Cutouts;
        }

        public List<ProceduralSurface> GenerateFacade(List<FacadeList> buildingFaces)
        {
            List<ProceduralSurface> facades = new List<ProceduralSurface>();

            for (int i = 0; i < buildingFaces.Count; i++)
            {
                FacadeList storyQuads = buildingFaces[i];
                bool isFront = i == 0;

                FacadeSemantic facadeSemantics = buildingFaces[i].Semantics;

                for (int j = 0; j < storyQuads.Facades.Count; j++)
                {
                    bool addFacade = m_Random.NextDouble() < facadeSemantics.SpawnChance;

                    if (!addFacade) continue;

                    Facade facade = storyQuads.Facades[j];
                    Quad3D face = facade.Face;

                    float facadeDistanceX = Vector3.Distance(face.BottomLeft, face.BottomRight);
                    float facadeDistanceY = Vector3.Distance(face.BottomLeft, face.TopLeft);

                    float gapWidth = facadeDistanceX - Mathf.Max(facadeSemantics.EdgeGap.x, facadeSemantics.WallThickness);
                    float gapWidthPercentage = gapWidth / facadeDistanceX;

                    float gapHeight = facadeDistanceY - facadeSemantics.EdgeGap.y;
                    float gapHeightPercentage = gapHeight / facadeDistanceY;

                    Vector3 bottom1 = Vector3.Lerp(face.BottomLeft, face.BottomRight, gapWidthPercentage);
                    Vector3 bottom2 = Vector3.Lerp(face.BottomRight, face.BottomLeft, gapWidthPercentage);
                    Vector3 top1 = Vector3.Lerp(face.TopLeft, face.TopRight, gapWidthPercentage);
                    Vector3 top2 = Vector3.Lerp(face.TopRight, face.TopLeft, gapWidthPercentage);

                    Vector3 bottomLeft = Vector3.Lerp(bottom1, top1, gapHeightPercentage);
                    Vector3 bottomRight = Vector3.Lerp(bottom2, top2, gapHeightPercentage);
                    Vector3 topLeft = Vector3.Lerp(top1, bottom1, isFront ? 1.0f : gapHeightPercentage); //todo: better gap method
                    Vector3 topRight = Vector3.Lerp(top2, bottom2, isFront ? 1.0f : gapHeightPercentage);

                    Quad3D quad = new Quad3D(bottomLeft, bottomRight, topRight, topLeft);
                    facades.Add(new ProceduralSurface(quad, isFront ? SurfaceType.Front : SurfaceType.Wall, facade.Handle, facadeSemantics));
                }
            }

            return facades;
        }

        public List<ProceduralSurface2> GenerateRoofSurfaces(List<FacadeList> buildingFaces)
        {
            List<ProceduralSurface2> roofs = new List<ProceduralSurface2>();

            for (int i = 0; i < buildingFaces.Count; i++)
            {
                if (buildingFaces[i].HasRoof)
                {
                    roofs.Add(new ProceduralSurface2(buildingFaces[i].Roof, SurfaceType.Roof, new FacadeHandle(buildingFaces[i].Facades[0].Handle.Level, -1)));
                }
            }

            return roofs;
        }

        private void GenerateFront(ProceduralSurface facade, Building targetBuilding)
        {
            if (targetBuilding == null || facade.Semantics.Covers.Count == 0) return;

            int randomFacade = m_Random.Next(facade.Semantics.Covers.Count);
            ProceduralAsset frontAsset = facade.Semantics.Covers[randomFacade];

            Vector3 center = (facade.Quad.BottomLeft + facade.Quad.BottomRight + facade.Quad.TopRight + facade.Quad.TopLeft) / 4.0f;
            Quaternion rotation = Quaternion.FromToRotation(frontAsset.Direction, facade.Quad.Normal);
            Vector3 offset = rotation * frontAsset.Offset;

            Vector3 facadeRight = facade.Quad.BottomRight - facade.Quad.BottomLeft;
            Vector3 facadeUp = facade.Quad.TopLeft - facade.Quad.BottomLeft;

            float facadeWidth = facadeRight.magnitude;
            float facadeHeight = facadeUp.magnitude;

            Vector3 targetSize = new Vector3(facadeRight.magnitude, facadeUp.magnitude, 1.0f);
            Vector3 frontSize = frontAsset.BoundingBox.size;
            float scaleX = targetSize.x / frontSize.x;
            float scaleY = targetSize.y / frontSize.y;
            float uniformScale = Mathf.Min(scaleX, scaleY);
            Vector3 boundScale = frontAsset.BoundingBox.size * uniformScale;

            Bounds scaledBounds = new Bounds(frontAsset.BoundingBox.center, frontAsset.BoundingBox.size * uniformScale);
            Vector3 rightDirection = Quaternion.AngleAxis(90.0f, Vector3.up) * frontAsset.Direction;

            scaledBounds.IntersectRay(new Ray(scaledBounds.center, rightDirection), out float baseWidth1);
            scaledBounds.IntersectRay(new Ray(scaledBounds.center, -rightDirection), out float baseWidth2);

            float defaultFrontWidth = -(baseWidth1 + baseWidth2); // Unity returns negative distances
            float minFrontWidth = Mathf.Lerp(defaultFrontWidth, 0, frontAsset.Stretch);
            float maxFrontWidth = Mathf.Lerp(defaultFrontWidth, facadeWidth, frontAsset.Stretch);
            float windowWidth = Mathf.Clamp(facadeWidth, minFrontWidth, maxFrontWidth);

            Vector3 frontPosition = center + offset;

            GameObject instance = (GameObject)ProceduralManager.InstantiateProceduralPrefab(frontAsset.Prefab, targetBuilding.transform);
            instance.transform.position = frontPosition;
            instance.transform.rotation = rotation;

            float widthScale = windowWidth / defaultFrontWidth;
            Vector3 absRight = new Vector3(Mathf.Abs(rightDirection.x), Mathf.Abs(rightDirection.y), Mathf.Abs(rightDirection.z));  //todo: not sure if abs is correct here but works if object is aligned to either x or z
            instance.transform.localScale += (absRight * (widthScale - 1));
            instance.transform.localScale *= uniformScale;

            if (frontAsset.RequiresCut)
            {
                Vector3 directionScale = Vector3.one + absRight * (widthScale - 1);
                boundScale.x *= directionScale.x;
                boundScale.y *= directionScale.y;
                boundScale.z *= directionScale.z;

                OrientedBounds cutoutBounds = new OrientedBounds(new Bounds(frontPosition, new Vector3(boundScale.x * frontAsset.CutScale.x, boundScale.y, boundScale.z * frontAsset.CutScale.y)), rotation);

                if (Cut(targetBuilding, cutoutBounds, facade.Quad, out Quad3D cutoutQuad))
                {
                    FacadeCutout cutout = new FacadeCutout(facade.Handle);
                    cutout.Quads.Add(cutoutQuad);
                    m_Cutouts.Add(cutout);
                }
            }
        }

        private void GenerateWindowFacade(ProceduralSurface facade, Building targetBuilding)
        {
            if (targetBuilding == null || facade.Semantics.Covers.Count == 0) return;

            int randomWindow = m_Random.Next(facade.Semantics.Covers.Count);
            ProceduralAsset windowAsset = facade.Semantics.Covers[randomWindow];

            Vector3 center = (facade.Quad.BottomLeft + facade.Quad.BottomRight + facade.Quad.TopRight + facade.Quad.TopLeft) / 4.0f;
            Quaternion rotation = Quaternion.FromToRotation(windowAsset.Direction, facade.Quad.Normal);
            Vector3 offset = rotation * windowAsset.Offset;

            Vector3 facadeRight = facade.Quad.BottomRight - facade.Quad.BottomLeft;
            Vector3 facadeUp = facade.Quad.TopLeft - facade.Quad.BottomLeft;

            float facadeWidth = facadeRight.magnitude;
            float facadeHeight = facadeUp.magnitude;

            Vector3 targetSize = new Vector3(facadeRight.magnitude, facadeUp.magnitude, 1.0f);

            Vector3 windowSize = windowAsset.BoundingBox.size;
            float scaleX = targetSize.x / windowSize.x;
            float scaleY = targetSize.y / windowSize.y;
            float uniformScale = Mathf.Min(scaleX, scaleY);

            Bounds scaledBounds = new Bounds(windowAsset.BoundingBox.center, windowAsset.BoundingBox.size * uniformScale);
            Vector3 rightDirection = Quaternion.AngleAxis(90.0f, Vector3.up) * windowAsset.Direction;

            scaledBounds.IntersectRay(new Ray(scaledBounds.center, rightDirection), out float baseWidth1);
            scaledBounds.IntersectRay(new Ray(scaledBounds.center, -rightDirection), out float baseWidth2);

            float defaultWindowWidth = -(baseWidth1 + baseWidth2); // Unity returns negative distances
            float minWindowWidth = Mathf.Lerp(defaultWindowWidth, 0, windowAsset.Stretch);
            float maxWindowWidth = Mathf.Lerp(defaultWindowWidth, facadeWidth, windowAsset.Stretch);

            float windowGap = facade.Semantics.CoverGap;
            int columns = Mathf.FloorToInt((facadeWidth + windowGap) / (defaultWindowWidth + windowGap));

            // Recompute the actual width each window should have
            float totalGapWidth = (columns - 1) * windowGap;
            float availableWidthForWindows = facadeWidth - totalGapWidth;

            float windowWidth = Mathf.Clamp(availableWidthForWindows / columns, minWindowWidth, maxWindowWidth);

            //Debug.Log($"Default: {defaultWindowWidth}, Min: {minWindowWidth}, Max: {maxWindowWidth}");
            //Debug.Log($"Columns: {columns}, width: {windowWidth}");

            Ray backRay = new Ray(windowAsset.BoundingBox.center, -windowAsset.Direction);
            windowAsset.BoundingBox.IntersectRay(backRay, out float distance);
            Vector3 backPosition = windowAsset.BoundingBox.center - windowAsset.Direction * distance;

            float totalRowWidth = (columns * windowWidth) + ((columns - 1) * windowGap);
            float startOffset = -totalRowWidth / 2.0f + windowWidth / 2.0f;

            FacadeCutout cutout = null;
            if (windowAsset.RequiresCut)
            {
                cutout = new FacadeCutout(facade.Handle);
            }

            for (int c = 0; c < columns; c++)
            {
                float xOffset = startOffset + c * (windowWidth + windowGap);
                Vector3 windowPos = center + (facadeRight.normalized * xOffset) - (rotation * (backPosition * uniformScale)) + offset;

                GameObject instance = (GameObject)ProceduralManager.InstantiateProceduralPrefab(windowAsset.Prefab, targetBuilding.transform);
                instance.transform.position = windowPos;
                instance.transform.rotation = rotation;

                float widthScale = windowWidth / defaultWindowWidth;
                Vector3 absRight = new Vector3(Mathf.Abs(rightDirection.x), Mathf.Abs(rightDirection.y), Mathf.Abs(rightDirection.z));  //todo: not sure if abs is correct here but works if object is aligned to either x or z
                instance.transform.localScale += (absRight * (widthScale - 1));
                instance.transform.localScale *= uniformScale;

                Vector3 boundPosition = center + facadeRight.normalized * xOffset;
                boundPosition += facade.Quad.Normal * (distance * uniformScale);

                Vector3 boundScale = windowAsset.BoundingBox.size * uniformScale;
                Vector3 directionScale = Vector3.one + absRight * (widthScale - 1);
                boundScale.x *= directionScale.x;
                boundScale.y *= directionScale.y;
                boundScale.z *= directionScale.z;

                OrientedBounds windowBounds = new OrientedBounds(new Bounds(boundPosition, boundScale), rotation);

                if (!windowAsset.Attachment.AllowOverlap)
                {
                    m_ProceduralBounds[facade.Handle.Level].Add(windowBounds);
                }
                
                if (windowAsset.RequiresCut)
                {
                    OrientedBounds cutoutBounds = new OrientedBounds(new Bounds(boundPosition, new Vector3(boundScale.x * windowAsset.CutScale.x, boundScale.y, boundScale.z * windowAsset.CutScale.y)), rotation);

                    if (Cut(targetBuilding, cutoutBounds, facade.Quad, out Quad3D cutoutQuad))
                    {
                        cutout.Quads.Add(cutoutQuad);
                    }
                }
            }

            if (windowAsset.RequiresCut && cutout.Quads.Count > 0)
            {
                m_Cutouts.Add(cutout);
            }
        }

        private void GenerateAttachments(ProceduralSurface2 facade, Building targetBuilding)
        {
            // Determine decorative count for surface
            List<ProceduralAsset> decorativeList = null;
            Vector3 generationNormal = Vector3.zero;

            switch (facade.SurfaceType)
            {
                case SurfaceType.Wall:
                    decorativeList = m_WallAttachments;
                    generationNormal = new Vector3(0, 0, 1);
                    break;
                case SurfaceType.Front:
                    decorativeList = m_FrontAttachments;
                    generationNormal = new Vector3(0, 0, 1);
                    break;
                case SurfaceType.Roof:
                    decorativeList = m_RoofAttachments;
                    generationNormal = new Vector3(0, 1, 0);
                    break;
            }

            if (decorativeList == null || decorativeList.Count == 0) return;

            float area = facade.GetArea();

            foreach (ProceduralAsset decorative in decorativeList)
            {
                int count = SampleNormalApprox(decorative.Attachment.Density * area);
                for (int i = 0; i < count; i++)
                {
                    GenerateAttachment(facade, targetBuilding, decorative, generationNormal);
                }
            }
        }

        private void GenerateAttachment(ProceduralSurface2 facade, Building targetBuilding, ProceduralAsset attachmentAsset, Vector3 generationNormal)
        {
            Vector3 assetNormal = new Vector3(Mathf.Abs(attachmentAsset.Direction.x), Mathf.Abs(attachmentAsset.Direction.y), Mathf.Abs(attachmentAsset.Direction.z));

            // Get forward size
            float width = Mathf.Max(
              attachmentAsset.BoundingBox.extents.x * assetNormal.x,
              attachmentAsset.BoundingBox.extents.y * assetNormal.y,
              attachmentAsset.BoundingBox.extents.z * assetNormal.z);

            // Generate inner polygon based on floor
            List<Vector3> innerEdges = GenerateInnerEdges(facade.Vertices, width, out Vector3 center);

            // Generate percentage of lines base on its length
            List<float> edgePercentages = GeneratePolygonEdgePercentges(innerEdges);

            OrientedBounds attachmentBounds = new OrientedBounds();

            // Try to generate decorative. Repeat when failed at overlap test
            const int maxIterations = 5;
            for (int i = 0; i < maxIterations; i++)
            {
                // Decide random position in inner shape (Treat as concave shape)
                Vector3 position = GenerateRandomPointInShape(innerEdges, edgePercentages, center, attachmentAsset.Attachment.Weight, out int index);

                // Move position along surface normal to avoid overlap
                position += Vector3.Scale(generationNormal, attachmentAsset.BoundingBox.extents);

                Vector3 vertex1 = innerEdges[index];
                Vector3 vertex2 = innerEdges[(index + 1) % innerEdges.Count];
                Vector3 edgeDirction = (vertex2 - vertex1).normalized;

                Vector3 normal = -Vector3.Cross(generationNormal, edgeDirction).normalized;
                Quaternion rotation = Quaternion.FromToRotation(attachmentAsset.Direction, normal);
                Vector3 offset = rotation * attachmentAsset.Offset;
                attachmentBounds = new OrientedBounds(new Bounds(position + offset, attachmentAsset.BoundingBox.size), rotation);

                if (!attachmentAsset.Attachment.AllowOverlap)
                {
                    // Check content aware bounds
                    bool foundOverlap = false;

                    foreach (OrientedBounds otherBounds in m_ProceduralBounds[facade.Handle.Level + 1])
                    {
                        if (attachmentBounds.Overlaps(otherBounds))
                        {
                            foundOverlap = true;
                            break;
                        }
                    }

                    if (foundOverlap)
                    {
                        if (i == maxIterations - 1)
                        {
                            // Was not able to find an empty spot
                            return;
                        }

                        continue;
                    }
                }

                break;
            }

            GameObject instance = (GameObject)ProceduralManager.InstantiateProceduralPrefab(attachmentAsset.Prefab, targetBuilding.transform);
            instance.transform.position = GetDecorativePosition(attachmentAsset, attachmentBounds);
            instance.transform.rotation = attachmentBounds.Rotation;

            if (!attachmentAsset.Attachment.AllowOverlap)
            {
                m_ProceduralBounds[facade.Handle.Level + 1].Add(attachmentBounds);
            }
        }

        public static Vector3 GetDecorativePosition(ProceduralAsset decorativePrefab, OrientedBounds targetBounds)
        {
            Vector3 diff = decorativePrefab.Prefab.transform.position - decorativePrefab.BoundingBox.center;
            Vector3 rotatedDiff = targetBounds.Rotation * diff;
            return targetBounds.Bounds.center + rotatedDiff;
        }

        public static List<Vector3> GenerateInnerEdges(List<Vector3> baseVertices, float innerOffset, out Vector3 center)
        {
            List<Vector3> innerEdges = new List<Vector3>(baseVertices.Count);
            center = MeshUtils.GetCenter(baseVertices);

            for (int i = 0; i < baseVertices.Count; i++)
            {
                Vector3 last = baseVertices[(i - 1 + baseVertices.Count) % baseVertices.Count];
                Vector3 current = baseVertices[i];
                Vector3 next = baseVertices[(i + 1) % baseVertices.Count];

                Vector3 lastFace = (current - next).normalized;
                Vector3 currentFace = (current - last).normalized;
                Vector3 avg1 = (currentFace + lastFace).normalized;

                // Compute correction so wall thickness stays constant regardless of angle
                float angle1 = Mathf.Acos(Vector3.Dot(lastFace, currentFace));

                if (angle1 < float.Epsilon)
                {
                    Vector3 offset = (center - baseVertices[i]).normalized * innerOffset;
                    innerEdges.Add(baseVertices[i] + offset);
                    continue;
                }

                float offset1 = innerOffset / Mathf.Sin(angle1 * 0.5f);
                innerEdges.Add(current - avg1 * offset1);
            }

            return innerEdges;
        }
        
        private static List<float> GeneratePolygonEdgePercentges(List<Vector3> vertices)
        {
            if (vertices == null || vertices.Count == 0)
            {
                return new List<float>();
            }

            float totalLength = 0.0f;
            List<float> lengths = new List<float>(vertices.Count);

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 vertex1 = vertices[i];
                Vector3 vertex2 = vertices[(i + 1) % vertices.Count];

                float distance = Vector3.Distance(vertex1, vertex2);
                totalLength += distance;
                lengths.Add(totalLength); // normally would add distance, but when adding total distance this can be instantly used in a randomizer
            }

            List<float> edgePercentages = new List<float>(vertices.Count);

            for (int i = 0; i < vertices.Count; i++)
            {
                edgePercentages.Add(lengths[i] / totalLength);

            }

            return edgePercentages;
        }

        private Vector3 GenerateRandomPointInShape(List<Vector3> vertices, List<float> edgePercentages, Vector3 center, float weight, out int index)
        {
            Vector3 edgePoint = GenerateRandomPoinOnLine(vertices, edgePercentages, out index);
            float randomWeight = (float)m_Random.NextDouble();

            if (weight == -1.0f) return edgePoint;

            //x ^ (((log(10, ((a + 1) / (2)))) / (log(10, 0.5))))
            float distance = Mathf.Pow(randomWeight, (Mathf.Log10((weight + 1.0f) / 2.0f)) / -0.3f);
            return Vector3.Lerp(edgePoint, center, distance);
        }

        private Vector3 GenerateRandomPoinOnLine(List<Vector3> vertices, List<float> edgePercentages, out int index)
        {
            // First make random point on edge
            for (index = 0; index < vertices.Count - 1; index++)
            {
                if ((float)m_Random.NextDouble() < edgePercentages[index])
                {
                    Vector3 vertex1 = vertices[index];
                    Vector3 vertex2 = vertices[index + 1];
                    return Vector3.Lerp(vertex1, vertex2, (float)m_Random.NextDouble());
                }
            }
            
            Vector3 vertex3 = vertices[index];
            Vector3 vertex4 = vertices[0];
            return Vector3.Lerp(vertex3, vertex4, (float)m_Random.NextDouble());
        }

        private bool Cut(Building targetBuilding, OrientedBounds scaledBounds, Quad3D facadeQuad,out Quad3D result)
        {
            if (!targetBuilding.Mesh)
            {
                result = new Quad3D();
                return false;
            }

            result = QuadFromBound(scaledBounds, facadeQuad.BottomLeft, facadeQuad.Normal);
            return true;
        }

        public Quad3D QuadFromBound(OrientedBounds bounds, Vector3 planePoint, Vector3 normal)
        {
            Vector3 center = bounds.Bounds.center;
            Vector3 size = bounds.Bounds.size;
            Vector3 extents = size * 0.5f;

            Vector3[] corners = new Vector3[8];
            corners[0] = new Vector3(-extents.x, -extents.y, -extents.z);
            corners[1] = new Vector3(extents.x, -extents.y, -extents.z);
            corners[2] = new Vector3(extents.x, -extents.y, extents.z);
            corners[3] = new Vector3(-extents.x, -extents.y, extents.z);
            corners[4] = new Vector3(-extents.x, extents.y, -extents.z);
            corners[5] = new Vector3(extents.x, extents.y, -extents.z);
            corners[6] = new Vector3(extents.x, extents.y, extents.z);
            corners[7] = new Vector3(-extents.x, extents.y, extents.z);

            // Orient corners
            for (int i = 0; i < 8; i++)
            {
                corners[i] = bounds.Rotation * corners[i] + center;
            }

            // Project all positions onto the plane
            Vector3 axisA = Vector3.Cross(normal, Vector3.up);
            if (axisA.sqrMagnitude < 0.001f)
            {
                axisA = Vector3.Cross(normal, Vector3.right);
            }

            axisA.Normalize();
            Vector3 axisB = Vector3.Cross(normal, axisA).normalized;

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (var corner in corners)
            {
                Vector3 offset = corner - planePoint;
                float x = Vector3.Dot(offset, axisA);
                float y = Vector3.Dot(offset, axisB);

                min.x = Mathf.Min(min.x, x);
                min.y = Mathf.Min(min.y, y);
                max.x = Mathf.Max(max.x, x);
                max.y = Mathf.Max(max.y, y);
            }

            // Create bounds that encapsulate all projected positions
            Quad3D projectedQuad = new Quad3D
            (
                planePoint + axisA * min.x + axisB * max.y, // Bottom Left
                planePoint + axisA * max.x + axisB * max.y, // Bottom Right
                planePoint + axisA * max.x + axisB * min.y, // Top Right
                planePoint + axisA * min.x + axisB * min.y  // Top Left
            );

            return projectedQuad;
        }


        /// <summary>
        /// Normal approximation for distribution
        /// </summary>
        private int SampleNormalApprox(float lambda)
        {
            // Box-Muller
            float u1 = (float)(1.0 - m_Random.NextDouble());
            float u2 = (float)(1.0 - m_Random.NextDouble());
            float z0 = Mathf.Sqrt(-2.0f * Mathf.Log10(u1)) * Mathf.Cos(2.0f * Mathf.PI * u2);
            float val = lambda + Mathf.Sqrt(lambda) * z0;
            int n = (int)Mathf.Round(val);
            return Mathf.Max(0, n);
        }

    }
}