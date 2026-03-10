using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using Procedural;
using Utilities.Splines;

namespace ProceduralEditor
{
    public class EditorTools : MonoBehaviour
    {
        public static void SetTransformHandleVisibility(bool hide)
        {
            Type type = typeof (Tools);
            FieldInfo field = type.GetField ("s_Hidden", BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue (null, hide);
        }

        public const float GridDivisions = 1000.0f;

        public static Vector3 GetKnotPosition(Vector3 originalPosition)
        {
            return new Vector3(
                Mathf.Round(originalPosition.x * GridDivisions) / GridDivisions,
                Mathf.Round(originalPosition.y * GridDivisions) / GridDivisions,
                Mathf.Round(originalPosition.z * GridDivisions) / GridDivisions
            );
        }

        public static void UpdateRoadHandles(Road road) => road.Spline.UpdateHandles();

        public static void ComputeAutoHandles(KnotMode mode, Vector3 previous, Vector3 current, Vector3 next, Vector3 lastHandleIn, Vector3 lastHandleOut, out Vector3 handleIn, out Vector3 handleOut)
        {
            switch (mode)
            {
                case KnotMode.Linear:
                    handleIn = current;
                    handleOut = current;
                    break;

                case KnotMode.Auto:
                    // Tangent direction from Catmull�Rom
                    Vector3 tangent = (next - previous).normalized;

                    // Scale handle length based on local distances
                    float d0 = Vector3.Distance(previous, current);
                    float d1 = Vector3.Distance(current, next);
                    float scale = Mathf.Min(d0, d1) / 3.0f;

                    handleIn = current - tangent * scale;
                    handleOut = current + tangent * scale;
                    break;

                case KnotMode.Bezier:
                default:
                    handleIn = lastHandleIn;
                    handleOut = lastHandleOut;
                    break;
            }
        }

        public static GUIStyle GetWindowStyle()
        {
            GUIStyle windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = GUI.skin.window.normal.background },
                active = { background = GUI.skin.window.normal.background },
                focused = { background = GUI.skin.window.normal.background },
                onNormal = { background = GUI.skin.window.normal.background },
                onActive = { background = GUI.skin.window.normal.background },
                onFocused = { background = GUI.skin.window.normal.background }
            };
            return windowStyle;
        }

        public const float KnotSize = 0.1f;

        public static void DrawKnot(Vector3 position, Color color)
        {
            float handleSize = HandleUtility.GetHandleSize(position) * KnotSize;
            Handles.color = color;
            Handles.DrawSolidDisc(position, Vector3.up, handleSize);
        }

        public static void DrawSelectableKnot(Vector3 position, bool selected)
        {
            float handleSize = HandleUtility.GetHandleSize(position) * KnotSize;
            Handles.color = selected ? Color.yellow : Color.cyan;
            Handles.DrawSolidDisc(position, Vector3.up, handleSize);
        }

        public static void DrawPreviewKnot(Vector3 position)
        {
            float handleSize = HandleUtility.GetHandleSize(position) * KnotSize;
            Handles.color = Color.green;
            Handles.DrawSolidDisc(position, Vector3.up, handleSize);
        }

        public static void DrawStaticKnot(Vector3 position)
        {
            float handleSize = HandleUtility.GetHandleSize(position) * 0.07f;
            Handles.color = Color.black;
            Handles.DrawSolidDisc(position, Vector3.up, handleSize);
        }

        public static void DrawBezierDefault(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent)
        {
            Handles.color = Color.cyan;
            Handles.DrawBezier(startPosition, endPosition, startTangent, endTangent, Color.cyan, null, 3.0f);
        }

        public static void DrawBezierPreview(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent)
        {
            Handles.color = Color.green;
            Handles.DrawBezier(startPosition, endPosition, startTangent, endTangent, Color.green, null, 3.0f);
        }

        public static void DrawKnot3D(Vector3 position, float size = 0.2f)
        {
            float handleSize = HandleUtility.GetHandleSize(position) * size;
            Handles.color = Color.cyan;
            Handles.SphereHandleCap(0,position, Quaternion.identity, handleSize, EventType.Repaint);
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        }

        public static void DrawBounds(Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;
            Vector3 extents = size * 0.5f;

            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
            corners[1] = center + new Vector3(extents.x, -extents.y, -extents.z);
            corners[2] = center + new Vector3(extents.x, -extents.y, extents.z);
            corners[3] = center + new Vector3(-extents.x, -extents.y, extents.z);
            corners[4] = center + new Vector3(-extents.x, extents.y, -extents.z);
            corners[5] = center + new Vector3(extents.x, extents.y, -extents.z);
            corners[6] = center + new Vector3(extents.x, extents.y, extents.z);
            corners[7] = center + new Vector3(-extents.x, extents.y, extents.z);

            Handles.color = Color.yellow;

            // Bottom rectangle
            Handles.DrawLine(corners[0], corners[1]);
            Handles.DrawLine(corners[1], corners[2]);
            Handles.DrawLine(corners[2], corners[3]);
            Handles.DrawLine(corners[3], corners[0]);

            // Top rectangle
            Handles.DrawLine(corners[4], corners[5]);
            Handles.DrawLine(corners[5], corners[6]);
            Handles.DrawLine(corners[6], corners[7]);
            Handles.DrawLine(corners[7], corners[4]);

            // Vertical edges
            for (int i = 0; i < 4; i++)
            {
                Handles.DrawLine(corners[i], corners[i + 4]);
            }
        }

        public static void DrawOrientedBounds(OrientedBounds bounds)
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

            Handles.color = Color.yellow;

            // Bottom rectangle
            Handles.DrawLine(corners[0], corners[1]);
            Handles.DrawLine(corners[1], corners[2]);
            Handles.DrawLine(corners[2], corners[3]);
            Handles.DrawLine(corners[3], corners[0]);

            // Top rectangle
            Handles.DrawLine(corners[4], corners[5]);
            Handles.DrawLine(corners[5], corners[6]);
            Handles.DrawLine(corners[6], corners[7]);
            Handles.DrawLine(corners[7], corners[4]);

            // Vertical edges
            for (int i = 0; i < 4; i++)
            {
                Handles.DrawLine(corners[i], corners[i + 4]);
            }
        }
    }
}