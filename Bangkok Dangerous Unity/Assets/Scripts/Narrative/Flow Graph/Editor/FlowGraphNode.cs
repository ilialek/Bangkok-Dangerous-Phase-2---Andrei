using System;
using UnityEditor.Experimental.GraphView;
using FlowGraph;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;

namespace FlowGraphEditor
{
    public class FlowGraphNode : Node
    {
        public FlowGraphObject TargetFlowGraph;
        public NodeData NodeData;
        public FlowGraphNodeData FlowGraphNodeData;
        private FlowGraphView m_FlowGraphView;
        private Dictionary<PortHandle, Port> m_Ports;

        public FlowGraphNode(FlowGraphObject targetFlowGraph, NodeData nodeData, FlowGraphNodeData flowGraphNodeData, FlowGraphView flowGraphView)
        {
            TargetFlowGraph = targetFlowGraph;
            NodeData = nodeData;
            FlowGraphNodeData = flowGraphNodeData;
            m_FlowGraphView = flowGraphView;

            Deserialize();
        }

        public void Reload()
        {
            Serialize();
            ClearNode();
            Deserialize();
            m_FlowGraphView.AddNodeConnections(NodeData, FlowGraphNodeData, PortDirection.Input);
            m_FlowGraphView.AddNodeConnections(NodeData, FlowGraphNodeData, PortDirection.Output);
        }

        public void Serialize()
        {
            FlowGraphNodeData.Title = title;
            FlowGraphNodeData.Position = GetPosition();

            // Serialize port links
            for (int i = 0; i < NodeData.FieldCount; i++)
            {
                for (int j = 0; j < NodeData[i].PortCount; j++)
                {
                    NodeData[i].SetPort(j, PortLink.None);
                }
            }

            foreach (Port port in outputContainer.Children())
            {
                PortLink outputPortLink = (PortLink)port.userData;

                if (outputPortLink.Handle.SubIndex >= NodeData[outputPortLink.Handle.Index].PortCount) continue;

                foreach (Edge edge in port.connections) //todo only supports one connection
                {
                    
                    Port otherPort = edge.input == port ? edge.output : edge.input;
                    PortLink inputPortLink = (PortLink)otherPort.userData;
                    NodeData[outputPortLink.Handle.Index].SetPort(outputPortLink.Handle.SubIndex, inputPortLink);
                }
            }

            foreach (Port port in inputContainer.Children())
            {
                PortLink inputPortLink = (PortLink)port.userData;

                if (inputPortLink.Handle.SubIndex >= NodeData[inputPortLink.Handle.Index].PortCount) continue;

                foreach (Edge edge in port.connections) //todo only supports one connection
                {
                    Port otherPort = edge.input == port ? edge.output : edge.input;
                    PortLink outputPortLink = (PortLink)otherPort.userData;
                    NodeData[inputPortLink.Handle.Index].SetPort(outputPortLink.Handle.SubIndex, outputPortLink);
                }
            }
        }

        public void Deserialize()
        {
            title = FlowGraphNodeData.Title;
            SetPosition(FlowGraphNodeData.Position);

            m_Ports = new Dictionary<PortHandle, Port>();

            // Setup fields
            for (int i = 0, p = 0; i < NodeData.FieldCount; i++)
            {
                Field field = NodeData[i];
                FieldType fieldType = NodeData.FieldTypes[i];

                bool multiField = fieldType.PortType.HasFlag(PortType.Multi);

                if (multiField)
                {
                    AddField(new FieldType(PortType.Value, "Count", typeof(int)), new PortHandle(i, -1));

                    for (int j = 0; j < field.PortCount; j++)
                    {
                        string name = $"{fieldType.Name} {j + 1}";
                        FieldType multiFieldType = new FieldType(fieldType.PortType, name, fieldType.Direction, fieldType.Type);

                        PortLink portLink = new PortLink(FlowGraphNodeData.Guid, i, j);
                        Port port = AddPortField(multiFieldType, portLink);

                        multiFieldType = new FieldType(fieldType.PortType, "", fieldType.Direction, fieldType.Type);
                        AddField(multiFieldType, new PortHandle(i, j), port);
                    }
                }
                else
                {
                    for (int j = 0; j < field.PortCount; j++)
                    {
                        PortLink portLink = new PortLink(FlowGraphNodeData.Guid, i, j);
                        AddPortField(fieldType, portLink);
                    }

                    for (int j = 0; j < field.ValueCount; j++)
                    {
                        AddField(fieldType, new PortHandle(i, j));
                    }
                }
            }
        }

        private Port AddPortField(FieldType fieldType, PortLink portLink)
        {
            if (fieldType.Direction == PortDirection.Input)
            {
                return AddInputPort(fieldType.Name, fieldType.Type, portLink);
            }

            return AddOutputPort(fieldType.Name, fieldType.Type, portLink);
        }

        private Port AddInputPort(string name, Type type, PortLink portLink)
        {
            Port port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, type);
            port.name = "";
            port.portName = name;
            port.userData = portLink;

            m_Ports.Add(portLink.Handle, port);
            inputContainer.Add(port);
           
            RefreshExpandedState();
            RefreshPorts();

            return port;
        }

        private Port AddOutputPort(string name, Type type, PortLink portLink)
        {
            Port port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, type);
            port.name = "";
            port.portName = name;
            port.userData = portLink;

            m_Ports.Add(portLink.Handle, port);
            outputContainer.Add(port);

            RefreshExpandedState();
            RefreshPorts();

            return port;
        }

        public void AddField(FieldType fieldType, PortHandle portHandle, Port port = null)
        {
            VisualElement visualElement = null;

            if (fieldType.Type == typeof(string))
            {
                TextField field = new TextField(fieldType.Name);
                field.isDelayed = true;
                field.userData = portHandle;
                field.AddToClassList("node-field");
                field.value = (string)NodeData[portHandle.Index][portHandle.SubIndex];

                field.RegisterValueChangedCallback(changeEvent =>
                {
                    NodeData[portHandle.Index][portHandle.SubIndex] = changeEvent.newValue;
                });

                visualElement = field;
            }
            else if (fieldType.Type == typeof(int))
            {
                IntegerField field = new IntegerField(fieldType.Name);
                field.isDelayed = true;
                field.userData = portHandle;
                field.AddToClassList("node-field");
                field.value = (int)NodeData[portHandle.Index][portHandle.SubIndex];

                field.RegisterValueChangedCallback(changeEvent =>
                {
                    NodeData[portHandle.Index][portHandle.SubIndex] = changeEvent.newValue;

                    if (portHandle.SubIndex < 0)
                    {
                        // Multi field: reload node
                        Reload();
                    }
                });

                visualElement = field;
            }
            else if (fieldType.Type == typeof(float))
            {
                FloatField field = new FloatField(fieldType.Name);
                field.isDelayed = true;
                field.userData = portHandle;
                field.AddToClassList("node-field");
                field.value = (int)NodeData[portHandle.Index][portHandle.SubIndex];

                field.RegisterValueChangedCallback(changeEvent =>
                {
                    NodeData[portHandle.Index][portHandle.SubIndex] = changeEvent.newValue;
                });

                visualElement = field;
            }
            else if (fieldType.Type == typeof(bool))
            {
                Toggle field = new Toggle(fieldType.Name);
                field.userData = portHandle;
                field.AddToClassList("node-field");
                field.value = (bool)NodeData[portHandle.Index][portHandle.SubIndex];

                field.RegisterValueChangedCallback(changeEvent =>
                {
                    NodeData[portHandle.Index][portHandle.SubIndex] = changeEvent.newValue;
                });

                visualElement = field;
            }
            else if (typeof(ScriptableObject).IsAssignableFrom(fieldType.Type))
            {
                ObjectField field = new ObjectField(fieldType.Name);
                field.objectType = fieldType.Type;
                field.userData = portHandle;
                field.AddToClassList("node-field");
                field.value = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath((string)NodeData[portHandle.Index][portHandle.SubIndex]));

                field.RegisterValueChangedCallback(changeEvent =>
                {
                    NodeData[portHandle.Index][portHandle.SubIndex] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(changeEvent.newValue));
                });

                visualElement = field;
            }

            if (visualElement != null)
            {
                mainContainer.Add(visualElement);
                
                if (port != null)
                {
                    port.contentContainer.Add(visualElement);
                }
            }
        }

        private void ClearNode()
        {
            DisconnectAllPorts(inputContainer);
            DisconnectAllPorts(outputContainer);
            inputContainer.Clear();
            outputContainer.Clear();

            // The first and second element are node relevant elements. From index 2 it stores added fields
            for (int i = 2; i < mainContainer.childCount; i++)
            {
                mainContainer.RemoveAt(i);
            }

            m_Ports?.Clear();
            RefreshExpandedState();
            RefreshPorts();
        }

        private void DisconnectAllPorts(VisualElement container)
        {
            foreach (Port port in container.Children())
            {
                List<Edge> edges = new List<Edge>(port.connections);

                foreach (Edge edge in edges)
                {
                    edge.input.Disconnect(edge);
                    edge.output.Disconnect(edge);
                    edge.parent?.Remove(edge);
                }
            }
        }

        public Port GetPort(PortHandle handle) => m_Ports[handle];
    }
}