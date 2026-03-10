using System;
using UnityEngine;
using Utilities;

namespace FlowGraph
{
    [Serializable]
    public class FlowGraphNodeData
    {
        public string Title;
        public GUID Guid;
        public Rect Position;

        [SerializeField] private string m_TypeName;

        public Type NodeType
        {
            get => !string.IsNullOrEmpty(m_TypeName) ? Type.GetType(m_TypeName) : null;
            set => m_TypeName = value != null ? value.AssemblyQualifiedName : null;
        }

        public FlowGraphNodeData(string title, GUID guid, Rect position, Type nodeType)
        {
            Title = title;
            Guid = guid;
            Position = position;
            NodeType = nodeType;
        }
    }
}