using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using FlowGraph;

namespace FlowGraphEditor
{
    [CustomEditor(typeof(FlowGraphObject<>))]
    public class FlowGraphObjectEditor : Editor
    {
        [OnOpenAsset]
        public static bool OpenGraphAsset(int instanceID, int line)
        {
            FlowGraphObject meshGraphObject = EditorUtility.InstanceIDToObject(instanceID) as FlowGraphObject;
            if (meshGraphObject == null) return false;

            // Check if there's already a window open
            bool hasOpenWindows = EditorWindow.HasOpenInstances<FlowGraphWindow>();
            // Focus the open window
            if (hasOpenWindows) EditorWindow.FocusWindowIfItsOpen<FlowGraphWindow>();
            // Create a new window
            else
            {
                FlowGraphWindow meshGraphWindow = EditorWindow.CreateWindow<FlowGraphWindow>(meshGraphObject.Name);

                Vector2 windowSize = new(
                    Screen.currentResolution.width * .5f,
                    Screen.currentResolution.height * .5f
                );
                Vector2 windowPosition = new(
                    (Screen.currentResolution.width - windowSize.x) * .15f,
                    (Screen.currentResolution.height - windowSize.y) * .15f
                );
                meshGraphWindow.position = new Rect(
                    windowPosition,
                    windowSize
                );

                meshGraphObject.Setup();
                meshGraphWindow.LoadFromGraphObject(meshGraphObject);
            }

            return true;
        }
    }
}