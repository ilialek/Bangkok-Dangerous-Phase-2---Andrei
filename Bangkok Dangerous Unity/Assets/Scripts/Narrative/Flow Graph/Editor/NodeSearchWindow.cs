using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;
using System.Collections.Generic;
using FlowGraph;

namespace FlowGraphEditor
{
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private FlowGraphWindow m_Window;
        private FlowGraphObject m_GraphObject;

        public void Initialize(FlowGraphWindow window, FlowGraphObject graphObject)
        {
            m_Window = window;
            m_GraphObject = graphObject;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0)
            };

            int index = 1;
            foreach (Type nodeType in m_GraphObject.NodeTypes)
            {
                string nodeName = ((NodeData)Activator.CreateInstance(nodeType)).Name;
                tree.Add(new SearchTreeEntry(new GUIContent(nodeName))
                {
                    level = 1,
                    userData = (nodeType, nodeName)
                });
                index++;
            }

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            (Type selectedType, string nodeName) = ((Type, string))entry.userData;

            if (selectedType == null) return false;

            m_Window.CreateNode(selectedType, nodeName);
            return true;
        }
    }
}
