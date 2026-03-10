using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    /// <summary>
    /// Code from https://codereview.stackexchange.com/questions/277000/union-polygon-algorithm
    /// </summary>
    public class PolygonUtility
    {
        public static List<MergedPolygon> UnionPolygon(List<Polygon> basePolygons)
        {
            List<Polygon> polygons = new List<Polygon>();

            if (basePolygons == null || basePolygons.Count == 0) return new List<MergedPolygon>();

            Queue<int> unvisited = new Queue<int>();

            for (int i = 1; i < basePolygons.Count; i++)
            {
                unvisited.Enqueue(i);
            }

            Polygon current = basePolygons[0];
            polygons.Add(current);

            while (unvisited.Count > 0)
            {
                bool foundOverlap = false;

                int count = unvisited.Count;
                for (int i = 0; i < count; i++)
                {
                    int next = unvisited.Dequeue();
                    Polygon nextPolygon = basePolygons[next];

                    if (current.Overlaps(nextPolygon))
                    {
                        polygons.Add(nextPolygon);
                        foundOverlap = true;
                        current = nextPolygon;
                        break;
                    }
                    else
                    {
                        unvisited.Enqueue(next);
                    }
                }

                if (!foundOverlap)
                {
                    int next = unvisited.Dequeue();
                    current = basePolygons[next];
                    polygons.Add(current);
                }
            }

            List<MergedPolygon> newPolygons = new List<MergedPolygon>();

            // Fix Winding Order
            for (int i = 0; i < polygons.Count; i++)
            {
                polygons[i].FixWindingOrder();
            }

            // Find Outside Point
            Vector2 pointOutside = Vector2.zero;
            for (int i = 0; i < polygons.Count; i++)
            {
                for (int j = 0; j < polygons[i].Vertices.Count; j++)
                {
                    if (polygons[i].Vertices[j].x > pointOutside.x)
                    {
                        pointOutside = polygons[i].Vertices[j];
                    }
                }
            }
            pointOutside += new Vector2(1.0f, 0);

            while (polygons.Count > 0)
            {
                List<Vector2> newPolygon = new List<Vector2>();
                List<Polygon> containingPolygons = new List<Polygon>();

                int P = 0; // currentPolygon
                int I = 0; // currentIndex

                // Setup First Point
                for (int p = 0; p < polygons.Count; p++)
                {
                    if (p != P)
                    {
                        if (!CheckIndex(I, 0, polygons[0].Vertices.Count))
                            break;
                        if (IsPointInPolygon(polygons[0].Vertices[I], polygons[p].Vertices, pointOutside))
                        {
                            p = 0;
                            I++;
                            continue;
                        }
                    }
                }

                HashSet<int> hashedPolygons = new HashSet<int>();

                while (true)
                {
                    hashedPolygons.Add(P);

                    if (newPolygon.Contains(polygons[P].Vertices[I % polygons[P].Vertices.Count]))break;

                    Vector2 a = polygons[P].Vertices[I % polygons[P].Vertices.Count];
                    Vector2 b = polygons[P].Vertices[(I + 1) % polygons[P].Vertices.Count];

                    newPolygon.Add(a);

                    bool intersected = false;
                    Vector3 intersection = new Vector3();
                    Vector3 closestIntersection = new Vector3();
                    float closestDistance = 0.0f;

                    int tp = 0;
                    int ti = 0;
                    for (int p = 0; p < polygons.Count; p++)
                    {
                        if (p != P)
                        {
                            for (int i = 0; i < polygons[p].Vertices.Count; i++)
                            {
                                Vector2 x = polygons[p].Vertices[i];
                                Vector2 y = polygons[p].Vertices[(i + 1) % polygons[p].Vertices.Count];

                                if (AreLinesIntersecting(a, b, x, y, false))
                                {
                                    if (LineLineIntersection(a, (b - a).normalized, x, (y - x).normalized, out intersection))
                                    {
                                        if (newPolygon.Contains(intersection)) continue;

                                        if (!intersected)
                                        {
                                            closestIntersection = intersection;
                                            closestDistance = Vector3.Distance(a, intersection);

                                            tp = p;
                                            ti = i;
                                            intersected = true;
                                        }
                                        else if (Vector3.Distance(a, intersection) < closestDistance)
                                        {
                                            closestIntersection = intersection;
                                            closestDistance = Vector3.Distance(a, intersection);
                                            tp = p;
                                            ti = i;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (intersected)
                    {
                        newPolygon.Add(closestIntersection);
                        P = tp;
                        I = ti;
                    }
                    I++;
                }

                if (hashedPolygons.Count == 1)
                {
                    containingPolygons.Add(polygons[0]);
                    newPolygons.Add(new MergedPolygon(new Polygon(newPolygon), containingPolygons));
                    polygons.RemoveAt(0);
                    continue;
                }

                for (int i = polygons.Count - 1; i >= 0; i--)
                {
                    if (hashedPolygons.Contains(i))
                    {
                        polygons.RemoveAt(i);
                    }
                }

                if (polygons.Count > 0)
                {
                    polygons.Add(new Polygon(newPolygon));
                }
            }

            // Add containing polygons

            for (int i = 0; i < newPolygons.Count; i++)
            {
                MergedPolygon outer = newPolygons[i];
                List<Polygon> contained = new List<Polygon>();

                for (int j = 0; j < basePolygons.Count; j++)
                {
                    Polygon inner = basePolygons[j];

                    if (outer.Polygon == inner) continue;

                    if (outer.Polygon.Overlaps(inner))
                    {
                        contained.Add(inner);
                    }
                }

                outer.ContainingPolygons = contained;
            }

            return newPolygons;
        }

        public static bool CheckIndex(int i, int min, int max)
        {
            return i >= min && i < max;
        }

        public static bool AreLinesIntersecting(Vector2 p1x, Vector2 p1y, Vector2 p2x, Vector2 p2y, bool shouldIncludeEndPoints)
        {
            bool isIntersecting = false;

            float denominator = (p2y.y - p2x.y) * (p1y.x - p1x.x) - (p2y.x - p2x.x) * (p1y.y - p1x.y);

            // Make sure the denominator is > 0, if not the lines are parallel
            if (denominator != 0f)
            {
                float u_a = ((p2y.x - p2x.x) * (p1x.y - p2x.y) - (p2y.y - p2x.y) * (p1x.x - p2x.x)) / denominator;
                float u_b = ((p1y.x - p1x.x) * (p1x.y - p2x.y) - (p1y.y - p1x.y) * (p1x.x - p2x.x)) / denominator;

                // Are the line segments intersecting if the end points are the same
                if (shouldIncludeEndPoints)
                {
                    //Is intersecting if u_a and u_b are between 0 and 1 or exactly 0 or 1
                    if (u_a >= 0f && u_a <= 1f && u_b >= 0f && u_b <= 1f)
                    {
                        isIntersecting = true;
                    }
                }
                else
                {
                    // Is intersecting if u_a and u_b are between 0 and 1
                    if (u_a > 0f && u_a < 1f && u_b > 0f && u_b < 1f)
                    {
                        isIntersecting = true;
                    }
                }

            }

            return isIntersecting;
        }

        public static bool LineLineIntersection(Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2, out Vector3 intersection)
        {
            Vector3 lineVec3 = linePoint2 - linePoint1;
            Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
            Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

            float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

            // Is coplanar, and not parallel
            if (Mathf.Abs(planarFactor) < 0.0001f
                    && crossVec1and2.sqrMagnitude > 0.0001f)
            {
                float s = Vector3.Dot(crossVec3and2, crossVec1and2)
                        / crossVec1and2.sqrMagnitude;
                intersection = linePoint1 + (lineVec1 * s);
                return true;
            }
            else
            {
                intersection = Vector3.zero;
                return false;
            }
        }

        public static bool IsPointInPolygon(Vector2 point, List<Vector2> polygonPoints, Vector2 pointOutside)
        {
            int numIntersections = 0;

            for (int j = 0; j < polygonPoints.Count; j++)
            {

                Vector2 uv1 = polygonPoints[j];
                Vector2 uv2 = polygonPoints[(j + 1) % polygonPoints.Count];

                if (AreLinesIntersecting(point, pointOutside, uv1, uv2, true))
                    numIntersections++;
            }

            return numIntersections != 0 && numIntersections % 2 != 0;

        }
    }

    [System.Serializable]
    public struct Polygon
    {
        public List<Vector2> Vertices;

        public Polygon(List<Vector2> vertices)
        {
            Vertices = new List<Vector2>(vertices);
        }

        public Polygon(Polygon other)
        {
            Vertices = new List<Vector2>(other.Vertices);
        }

        public void FixWindingOrder()
        {
            float sum = 0.0f;
            for (int j = 0; j < Vertices.Count; j++)
            {
                Vector3 a = Vertices[j];
                Vector3 b = Vertices[(j + 1) % Vertices.Count];
                sum += (b.x - a.x) * (b.y + a.y);
            }

            if (Mathf.Sign(sum) < 0)
            {
                Vertices.Reverse();
            }
        }

        public bool GetWinding()
        {
            float sum = 0.0f;
            for (int j = 0; j < Vertices.Count; j++)
            {
                Vector3 a = Vertices[j];
                Vector3 b = Vertices[(j + 1) % Vertices.Count];
                sum += (b.x - a.x) * (b.y + a.y);
            }

            return Mathf.Sign(sum) < 0.0f;
        }

        private Rect GetBounds()
        {
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (Vector2 vertex in Vertices)
            {
                if (vertex.x < minX) minX = vertex.x;
                if (vertex.y < minY) minY = vertex.y;
                if (vertex.x > maxX) maxX = vertex.x;
                if (vertex.y > maxY) maxY = vertex.y;
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public bool Overlaps(Polygon other)
        {
            // Check if bounds overlap
            if (!GetBounds().Overlaps(other.GetBounds())) return false;
        
            // Check if one polygon is fully inside the other
            if (other.Overlaps(Vertices[0]) || Overlaps(other.Vertices[0])) return true;

            // Check if any edge intersects
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vector2 a1 = Vertices[i];
                Vector2 a2 = Vertices[(i + 1) % Vertices.Count];

                for (int j = 0; j < other.Vertices.Count; j++)
                {
                    Vector2 b1 = other.Vertices[j];
                    Vector2 b2 = other.Vertices[(j + 1) % other.Vertices.Count];

                    if (LineIntersect(a1, a2, b1, b2)) return true;
                }
            }

            return false;
        }

        private bool LineIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            // Line-line intersection using cross product
            Vector2 r = p2 - p1;
            Vector2 s = q2 - q1;
            float rxs = Cross(r, s);
            float qpxr = Cross(q1 - p1, r);

            if (Mathf.Approximately(rxs, 0f) && Mathf.Approximately(qpxr, 0f))
            {
                // Collinear case: check overlap
                return (Mathf.Min(p1.x, p2.x) <= Mathf.Max(q1.x, q2.x) &&
                        Mathf.Max(p1.x, p2.x) >= Mathf.Min(q1.x, q2.x) &&
                        Mathf.Min(p1.y, p2.y) <= Mathf.Max(q1.y, q2.y) &&
                        Mathf.Max(p1.y, p2.y) >= Mathf.Min(q1.y, q2.y));
            }

            if (Mathf.Approximately(rxs, 0f) && !Mathf.Approximately(qpxr, 0f))
                return false; // Parallel

            float t = Cross(q1 - p1, s) / rxs;
            float u = Cross(q1 - p1, r) / rxs;

            return t >= 0 && t <= 1 && u >= 0 && u <= 1;
        }

        public bool Overlaps(Vector2 point)
        {
            bool inside = false;
            int j = Vertices.Count - 1;
            for (int i = 0; i < Vertices.Count; j = i++)
            {
                Vector2 vi = Vertices[i];
                Vector2 vj = Vertices[j];

                if (((vi.y > point.y) != (vj.y > point.y)) &&
                    (point.x < (vj.x - vi.x) * (point.y - vi.y) / (vj.y - vi.y) + vi.x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static float Cross(Vector2 v, Vector2 w)
        {
            return v.x * w.y - v.y * w.x;
        }

        public List<Vector3> Get3DPolygon(float height)
        {
            List<Vector3> result = new List<Vector3>(Vertices.Count);

            for (int i = 0; i < Vertices.Count; i++)
            {
                result.Add(new Vector3(Vertices[i].x, height, Vertices[i].y));
            }

            return result;
        }

        public Vector3[] GetDrawList()
        {
            Vector3[] list = new Vector3[Vertices.Count];

            for (int j = 0; j < Vertices.Count; j++)
            {
                list[j] = new Vector3(Vertices[j].x, 0.0f, Vertices[j].y);
            }

            return list;
        }

        public static bool operator ==(Polygon a, Polygon b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Polygon a, Polygon b)
        {
            return !a.Equals(b);
        }

        public bool Equals(Polygon other)
        {
            if (Vertices == null && other.Vertices == null) return true;
            if (Vertices == null || other.Vertices == null) return false;
            if (Vertices.Count != other.Vertices.Count) return false;

            for (int i = 0; i < Vertices.Count; i++)
            {
                if (Vertices[i] != other.Vertices[i]) return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is Polygon other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (var v in Vertices)
            {
                hash = hash * 31 + v.GetHashCode();
            }
               
            return hash;
        }
    }

    [System.Serializable]
    public class MergedPolygon
    {
        public Polygon Polygon;
        public List<Polygon> ContainingPolygons;

        public MergedPolygon()
        {
            Polygon = new Polygon(new List<Vector2>());
            ContainingPolygons = new List<Polygon>();
        }

        public MergedPolygon(Polygon polygon, List<Polygon> containingPolygons)
        {
            Polygon = polygon;
            ContainingPolygons = new List<Polygon>(containingPolygons);
        }

        public MergedPolygon(MergedPolygon other)
        {
            Polygon = other.Polygon;
            ContainingPolygons = new List<Polygon>(other.ContainingPolygons);
        }

    }
}