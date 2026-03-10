using System.Collections.Generic;
using UnityEngine;
using System;
using Utilities;

namespace FlowGraph
{
    public abstract class FlowGraphObject<TEntryNode> : FlowGraphObject where TEntryNode : NodeData, new()
    {
        public override bool ValidateEntry()
        {
            if (GraphNodeData.Count == 1)
            {
                NodeEntry = 0;
                return false;
            }
            else if (GraphNodeData.Count == 0)
            {
                // Add entry node
                AddNode("Entry", new Rect(0, 0, 100, 40), typeof(TEntryNode), new TEntryNode());
                return true;
            }

            return false;
        }
    }

    public abstract class FlowGraphObject : ScriptableObject, ISerializationCallbackReceiver
    {
        public abstract string Name { get; }

        protected List<NodeData> Data = new List<NodeData>();
        [SerializeField, HideInInspector] protected List<FlowGraphNodeData> GraphNodeData = new List<FlowGraphNodeData>();

        [SerializeField] protected int NodeEntry = -1;
        protected Dictionary<GUID, int> NodeIndex;

        public abstract List<Type> NodeTypes { get; }

        public void Setup()
        {
            HandleSerialization();
            ValidateEntry();
        }

        public IReadOnlyList<NodeData> GetNodeData() => Data;
        public IReadOnlyList<FlowGraphNodeData> GetGraphNodeData() => GraphNodeData;

        public NodeData GetNodeData(GUID guid)
        {
            HandleSerialization();

            if (NodeIndex.TryGetValue(guid, out int index) && ValidateIndex(index))
            {
                return Data[index];
            }

            Debug.LogWarning($"Could not retrieve NodeData of {guid}");
            return default(NodeData);
        }

        public FlowGraphNodeData GetGraphNodeData(GUID guid)
        {
            HandleSerialization();

            if (NodeIndex.TryGetValue(guid, out int index) && ValidateIndex(index))
            {
                return GraphNodeData[index];
            }

            Debug.LogWarning($"Could not retrieve GraphNodeData of {guid}");
            return null;
        }

        public (FlowGraphNodeData, NodeData) GetNode(GUID guid)
        {
            HandleSerialization();

            if (NodeIndex.TryGetValue(guid, out int index) && ValidateIndex(index))
            {
                return (GraphNodeData[index], Data[index]);
            }

            Debug.LogWarning($"Could not retrieve NodeData of {guid}");
            return (null, null);
        }

        public (FlowGraphNodeData, NodeData) GetNodeEntry()
        {
            if (ValidateIndex(NodeEntry))
            {
                return (GraphNodeData[NodeEntry], Data[NodeEntry]);
            }

            Debug.LogWarning("Node Entry not set");
            return (null, null);
        }

        public int GetNodeIndex(GUID guid)
        {
            HandleSerialization();
            return NodeIndex[guid];
        }

        public int Count => Mathf.Min(Data.Count, GraphNodeData.Count);

        public FlowGraphNodeData AddNode(string title, Rect position, Type nodeType, NodeData data)
        {
            return AddNode(title, GUID.Create(), position, nodeType, data);
        }

        public FlowGraphNodeData AddNode(string title, GUID guid, Rect position, Type nodeType, NodeData data)
        {
            HandleSerialization();

            if (!ValidateNewGuid(guid)) return null;

            FlowGraphNodeData graphNodeData = new FlowGraphNodeData(title, guid, position, nodeType);

            GraphNodeData.Add(graphNodeData);
            Data.Add(data);
            NodeIndex.Add(guid, GraphNodeData.Count - 1);
            ValidateEntry();

            return graphNodeData;
        }

        public void InsertNode(int index, string Title, GUID guid, Rect Position, Type NodeType, NodeData data)
        {
            HandleSerialization();

            if (!ValidateNewGuid(guid)) return;
            if (!ValidateIndex(index)) return;

            GraphNodeData.Insert(index, new FlowGraphNodeData(Title, guid, Position, NodeType));
            Data.Insert(index, data);

            // Organize new node indexes
            for (int i = index; i < GraphNodeData.Count; i++)
            {
                NodeIndex[GraphNodeData[i].Guid] = i;
            }

            ValidateEntry();
        }

        public void RemoveNode(GUID guid)
        {
            HandleSerialization();

            if (!NodeIndex.ContainsKey(guid)) return;

            int index = NodeIndex[guid];
            GraphNodeData.RemoveAt(index);
            Data.RemoveAt(index);
            HandleSerialization(true);
        }

        public void SetNodeEntry(GUID guid)
        {
            HandleSerialization();

            NodeEntry = NodeIndex[guid];
        }

        protected void HandleSerialization(bool forceReload = false)
        {
            if (!forceReload && NodeIndex != null && NodeIndex.Count == GraphNodeData.Count) return;

            NodeIndex = new Dictionary<GUID, int>();

            for (int i = 0; i < GraphNodeData.Count; i++)
            {
                NodeIndex.Add(GraphNodeData[i].Guid, i);
            }
        }

        protected bool ValidateNewGuid(GUID guid)
        {
            if (NodeIndex.ContainsKey(guid))
            {
                Debug.LogWarning($"Duplicate GUID: {guid}");
                return false;
            }

            if (GraphNodeData.Count != Data.Count)
            {
                Debug.LogWarning("GraphNodeData and NodeData list do not match. Can not add new node");
                return false;
            }

            return true;
        }

        protected bool ValidateIndex(int index)
        {
            if (index < 0 || index > GraphNodeData.Count)
            {
                Debug.LogWarning("Invalid index to insert new node");
                return false;
            }

            return true;
        }

        public abstract bool ValidateEntry();


        // Serialize nodedata. Can't serialize abstract classes by default
        [SerializeField, HideInInspector] protected List<SerializedData> m_SerializedData;

        public void OnBeforeSerialize()
        {
            if (Data == null) return;

            m_SerializedData = new List<SerializedData>();
            foreach (NodeData data in Data)
            {
                m_SerializedData.Add(SerializedData.Serialize(data));
            }
        }

        public void OnAfterDeserialize()
        {
            if (m_SerializedData == null) return;
            
            Data = new List<NodeData>();
            foreach (SerializedData serialized in m_SerializedData)
            {
                if (serialized == null || serialized.Type == null) continue;

                Data.Add((NodeData)SerializedData.Deserialize(serialized));
            }
        }
    }
}