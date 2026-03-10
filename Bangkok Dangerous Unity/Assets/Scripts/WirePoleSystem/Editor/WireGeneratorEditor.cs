using UnityEditor;
using UnityEngine;
using Utilities.Splines;

namespace WirePoleSystem.Editor{
    [CustomEditor(typeof(WireGenerator))]
    public class WireGeneratorEditor : UnityEditor.Editor{
        private bool knotEditActive;
        public override void OnInspectorGUI(){
            WireGenerator wire = target as WireGenerator;
            EditorGUI.BeginChangeCheck();
            knotEditActive ^= GUILayout.Button("Edit wire knots");
            if(GUILayout.Button("Update mesh") && wire) wire.UpdateWire();
            if (GUILayout.Button("Add point") && wire){
                Undo.RecordObject(wire, "Added wire point");
                wire.AddPoint();
            }
            if(EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
            DrawDefaultInspector();
        }

        private void OnSceneGUI(){
            if (!knotEditActive) return;
            WireGenerator wire = target as WireGenerator;
            
            if (!wire) return;
            
            Handles.color = Color.blue;
            
            for (int i = 0; i < wire.Spline.Count; i++){
                // if (i <= 0 || i >= wire.SplineKnots.Count - 1) continue;
                BezierKnot wireKnot = wire.Spline[i];
                
                EditorGUI.BeginChangeCheck();
                Vector3 newKnotPosition = Handles.PositionHandle(wireKnot.Position, wire.transform.rotation);
                if (!EditorGUI.EndChangeCheck()) continue;
                
                Undo.RecordObject(wire, "Modified wire spline knot");
                wireKnot.Position = newKnotPosition;
                wire.UpdateSpline();
                // We need to manually call the update method here
                wire.UpdateWire();
            }
        }
    }
}