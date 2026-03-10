using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using System.IO;
using GUID = Utilities.GUID;

namespace Procedural
{
    public static class MeshUtils
    {
        public static List<Vector2> ToList2 (List<Vector3> list)
        {
            if (list == null) return new List<Vector2>();

            List<Vector2> result = new List<Vector2>(list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                result.Add(new Vector2(list[i].x, list[i].z));
            }

            return result;
        }

        public static Vector2 GetCenter(List<Vector2> points)
        {
            Vector2 total = points.Aggregate(Vector2.zero, (current, p) => current + p);

            return total / points.Count;
        }

        public static Vector3 GetCenter(List<Vector3> points)
        {
            Vector3 total = points.Aggregate(Vector3.zero, (current, p) => current + p);

            return total / points.Count;
        }

        public static Vector2 GetCenter3To2(List<Vector3> points)
        {
            Vector2 total = points.Aggregate(Vector2.zero, (current, point) => current + new Vector2(point.x, point.z));

            return total / points.Count;
        }

        public static bool GetLineLineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection)
        {
            intersection = Vector2.zero;

            Vector2 r = a2 - a1;
            Vector2 s = b2 - b1;

            float denominator = r.x * s.y - r.y * s.x;

            //Lines are parallel
            if (Mathf.Approximately(denominator, 0f)) return false;

            Vector2 difference = b1 - a1;
            float uNumerator = difference.x * r.y - difference.y * r.x;
            float tNumerator = difference.x * s.y - difference.y * s.x;

            float t = tNumerator / denominator;
            float u = uNumerator / denominator;

            //Check if intersection occurs
            if (!(t >= 0f) || !(t <= 1f) || !(u >= 0f) || !(u <= 1f)) return false;
            intersection = a1 + t * r;
            return true;

        }

        public static float DistanceFromPointToEdge(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ap = point - a;
            float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude);
            Vector2 projection = a + t * ab;
            return Vector2.Distance(point, projection);
        }

        //Check if points lies in convex shape
        public static bool PointInPolygon(List<Vector2> points, Vector2 point)
        {
            int count = points.Count;
            if (count < 3) return false;

            bool? sign = null;

            for (int i = 0; i < count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % count];

                Vector2 edge = b - a;
                Vector2 toPoint = point - a;

                float cross = edge.x * toPoint.y - edge.y * toPoint.x;

                //Check if point is on the edge
                if (cross == 0) continue;

                bool currentSign = cross > 0;

                if (sign == null)
                {
                    sign = currentSign;
                }
                else if (sign != currentSign)
                {
                    //Point is outside
                    return false;
                }
            }

            //Point is inside
            return true;
        }

        public static bool IsConvex(List<Vector3> shape)
        {
            return IsConvex(ToList2(shape));
        }

        public static bool IsConvex(List<Vector2> shape)
        {
            bool negative = false;
            bool positive = false;

            for (int i = 0; i < shape.Count; i++)
            {
                Vector2 pointA = shape[i];
                Vector2 pointB = shape[(i + 1) % shape.Count];
                Vector2 pointC = shape[(i + 2) % shape.Count];

                // Calculates cross between the two lines
                Vector2 ba = pointA - pointB;
                Vector2 bc = pointC - pointB;
                float cross = ba.x * bc.y - ba.y * bc.x;

                // Check if all angles of the shape have the same sign
                if (cross < 0)
                {
                    negative = true;
                }
                else if (cross > 0)
                {
                    positive = true;
                }

                if (negative && positive) return false;
            }

            return true;
        }

        public static Mesh CreateMeshAsset(Mesh mesh, GUID targetGuid, MeshCollection meshCollection, string subFolder = "Meshes")
        {
#if UNITY_EDITOR
            if (mesh == null) return null;

            string collectionName = meshCollection != null ? meshCollection.name : "Default";
            string directory = $"Data/Generated/{collectionName}/{subFolder}";
            string fullPath = Path.Combine(Application.dataPath, directory);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            MeshUtility.Optimize(mesh);

            string path = $"Assets/{directory}/{targetGuid.ToString()}.asset";

            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                Mesh assetCopy = Object.Instantiate(mesh);
                AssetDatabase.CreateAsset(assetCopy, path);
                return AssetDatabase.LoadAssetAtPath<Mesh>(path);
            }
            else
            {
                // Copy data into the existing mesh
                existing.Clear();
                existing.vertices = mesh.vertices;
                existing.triangles = mesh.triangles;
                existing.normals = mesh.normals;
                existing.uv = mesh.uv;
                existing.colors = mesh.colors;
                existing.tangents = mesh.tangents;

                EditorUtility.SetDirty(existing);
                return existing;
            }
#else
            return mesh;
#endif
        }

        public static void DeleteMeshAsset(GUID targetGuid, MeshCollection meshCollection, string subFolder = "Meshes")
        {
#if UNITY_EDITOR
            string collectionName = meshCollection != null ? meshCollection.name : "Default";
            string directory = $"Data/Generated/{collectionName}/{subFolder}";
            string path = $"Assets/{directory}/{targetGuid.ToString()}.asset";

            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
            {
                bool b = AssetDatabase.DeleteAsset(path);
            }
#endif
        }

        public static void SaveMesh()
        {
#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
#endif
        }
    }
}