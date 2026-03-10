using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using FlowGraph;
using System;
using UnityEditor.Experimental.GraphView;

namespace FlowGraphEditor
{
    public class FlowGraphWindow : EditorWindow
    {
        private FlowGraphView m_MeshGraphView;
        private FlowGraphObject m_FlowGraphObject;

        public void LoadFromGraphObject(FlowGraphObject graphObject)
        {
            m_FlowGraphObject = graphObject;

            ConstructGraphView();
            CreateToolbar();
        }

        private void ConstructGraphView()
        {
            if (m_FlowGraphObject == null) return;

            m_MeshGraphView = new FlowGraphView(m_FlowGraphObject);
            m_MeshGraphView.StretchToParentSize();
            m_MeshGraphView.RegisterCallback<KeyDownEvent>(OnKeyDown);
            rootVisualElement.Add(m_MeshGraphView);
        }

        private void CreateToolbar()
        {
            Toolbar toolbar = new();

            Button saveButton = new(() => SaveGraphView(m_MeshGraphView))
            {
                text = "Save"
            };
            toolbar.Add(saveButton);

            rootVisualElement.Add(toolbar);
        }

        private void OnKeyDown(KeyDownEvent keyEvent)
        {
            if (keyEvent.keyCode == KeyCode.Space)
            {
                NodeSearchWindow searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
                searchWindow.Initialize(this, m_FlowGraphObject);
                SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), searchWindow);
                keyEvent.StopPropagation();
            }
        }

        public void SaveGraphView(FlowGraphView flowGraphView)
        {
            flowGraphView.Serialize();
            m_FlowGraphObject.OnBeforeSerialize();
            EditorUtility.SetDirty(m_FlowGraphObject);
            AssetDatabase.SaveAssetIfDirty(m_FlowGraphObject);
        }

        public void CreateNode(Type nodeType, string title)
        {
            NodeData nodeData = (NodeData)Activator.CreateInstance(nodeType);

            float width = 200;
            float height = 100;

            Vector2 graphViewCenter = m_MeshGraphView.worldBound.size / 2.0f;
            Vector2 graphLocation = new Vector2(m_MeshGraphView.viewTransform.position.x, m_MeshGraphView.viewTransform.position.y);
            Vector2 localPosition = (graphViewCenter - graphLocation) / m_MeshGraphView.viewTransform.scale;
            Rect nodeRect = new Rect(localPosition.x - width / 2.0f, localPosition.y - height / 2.0f, width, height);

            FlowGraphNodeData graphNodeData = m_FlowGraphObject.AddNode(title, nodeRect, nodeType, nodeData);
            m_MeshGraphView.AddNode(graphNodeData, nodeData);
        }

        private void OnDisable()
        {
            if (m_MeshGraphView != null)
            {
                SaveGraphView(m_MeshGraphView);
                rootVisualElement.Remove(m_MeshGraphView);
            }
        }
    }
}