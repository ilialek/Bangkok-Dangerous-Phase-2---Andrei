using UnityEditor;
using UnityEngine;

namespace WirePoleSystem.Editor{
    [CustomEditor(typeof(WirePole))]
    public class WirePoleEditor : UnityEditor.Editor{
        private bool pointEditActive;
        
        public override void OnInspectorGUI(){
            WirePole wirePole = (WirePole) target;
            GUILayout.Label("Connection points");
            EditorGUI.BeginChangeCheck();
            if (GUILayout.Button("Add connection point")){
                Undo.RegisterCompleteObjectUndo(wirePole, "Added connection point");
                wirePole.AddConnectionPoint();
            }
            pointEditActive ^= GUILayout.Button("Edit connections points");
            GUILayout.Label("Wires");
            if(GUILayout.Button("Generate wires")) wirePole.UpdateWires();
            if(GUILayout.Button("Purge wires")) wirePole.PurgeWires();
            DrawDefaultInspector();
            if(EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
        }
        
        private void OnSceneGUI(){
            if (!pointEditActive) return;
            WirePole pole = (WirePole) target;
            Handles.color = Color.blue;

            Transform poleTransform = pole.transform;
            foreach (ConnectionPoint point in pole.ConnectionPoints){
                EditorGUI.BeginChangeCheck();
                Vector3 newPointPosition = Handles.PositionHandle(
                    poleTransform.TransformPoint(point.Position),
                    poleTransform.rotation
                );
                
                if (!EditorGUI.EndChangeCheck()) continue;
                
                Undo.RecordObject(pole, "Modified connection point position");
                point.Position = poleTransform.InverseTransformPoint(newPointPosition);
                pole.UpdateWires();
                pole.OnTransformChange?.Invoke();
            }
        }
    }
}