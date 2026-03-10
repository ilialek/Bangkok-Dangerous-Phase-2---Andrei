using Procedural;
using UnityEditor;
using UnityEngine;

namespace ProceduralEditor
{
    public abstract class ProceduralMeshEditor<T> : Editor where T : ProceduralMesh
    {
        protected T m_TargetProcedural;
        protected ProceduralManager m_ProceduralManager;

        protected Rect m_WindowRect = new Rect(100, 100, 200, 100);

        private void OnEnable()
        {
            m_TargetProcedural = target as T;
            m_ProceduralManager = m_TargetProcedural.GetComponentInParent<ProceduralManager>();

            if (!m_TargetProcedural || !m_ProceduralManager) return;

            SceneView.duringSceneGui += OnSceneGUI;
            m_TargetProcedural.Selected();
            EditorTools.SetTransformHandleVisibility(true);

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                SetWindowRect(sceneView);
            }

            Setup();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            m_TargetProcedural?.Deselected();
            EditorTools.SetTransformHandleVisibility(false);
        }

        private void SetWindowRect(SceneView sceneView)
        {
            Rect sceneViewRect = sceneView.position;
            m_WindowRect.x = sceneViewRect.width - m_WindowRect.width - 5;
            m_WindowRect.y = sceneViewRect.height - m_WindowRect.height - 30;
        }

        /// <summary>
        /// Regenerate when inspector values have been modified
        /// </summary>
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            base.OnInspectorGUI();

            if (EditorGUI.EndChangeCheck())
            {
                (target as T).Generate();
            }
        }

        private void DrawWindow_Internal(int id)
        {
            // Exit button at top-right
            const float buttonSize = 20.0f;
            if (GUI.Button(new Rect(m_WindowRect.width - buttonSize - 4, 4, buttonSize, buttonSize), "X"))
            {
                DeselectEditor();
            }

            DrawWindow();

            if (Event.current.type == EventType.Repaint)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                m_WindowRect.height = lastRect.yMax + 8.0f;
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!m_TargetProcedural || !m_ProceduralManager) return;

            Event currentEvent = Event.current;

            Validate();
            UpdateEditor(currentEvent);

            // Handle deselect
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape)
            {
                DeselectEditor();
                currentEvent.Use();
            }

            //Handle deselect
            if ((!m_TargetProcedural || Selection.activeGameObject == m_TargetProcedural?.gameObject) && currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                if (m_WindowRect.Contains(currentEvent.mousePosition))
                {
                    currentEvent.Use();
                }
                else
                {
                    DeselectEditor();
                    currentEvent.Use();
                }
            }

            // Draw window
            Handles.BeginGUI();
            m_WindowRect = GUILayout.Window(123456, m_WindowRect, DrawWindow_Internal, $"{typeof(T).Name} Editor", EditorTools.GetWindowStyle());
            SetWindowRect(sceneView);
            Handles.EndGUI();
        }

        private void Validate()
        {
            if (!m_ProceduralManager.Initialized)
            {
                m_ProceduralManager.Initialize();
            }
        }

        /// <summary>
        /// Select parent procedural manager, instead of losing selection completely
        /// </summary>
        protected void DeselectEditor()
        {
            if (Event.current.type == EventType.MouseDown)
            {
                GameObject clickedObject = HandleUtility.PickGameObject(Event.current.mousePosition, false);

                if (clickedObject != null)
                {
                    Selection.activeGameObject = clickedObject;
                    return;
                }
            }

            Selection.activeGameObject = m_TargetProcedural.GetComponentInParent<ProceduralManager>().gameObject;
        }

        protected virtual void Setup() { }

        protected virtual void DrawWindow() { }
        protected virtual void UpdateEditor(Event currentEvent) { }
    }
}