using Procedural;
using UnityEditor;
using UnityEngine;

namespace ProceduralEditor
{
    [CustomEditor(typeof(Sidewalk))]
    public class SidewalkEditor : ProceduralMeshEditor<Sidewalk>
    {
        protected override void DrawWindow()
        {
            if (GUILayout.Button("Remove Sidewalk"))
            {
                DeselectEditor();
                Undo.DestroyObjectImmediate(m_TargetProcedural.gameObject);
            }
        }
    }
}