using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using FlowGraph;

namespace FlowGraphEditor
{
    public class FlowGraphView : GraphView
    {
        private FlowGraphObject m_TargetGraphObject;
        private List<FlowGraphNode> m_GraphNodes;

        public FlowGraphView(FlowGraphObject flowGraphObject)
        {
            m_TargetGraphObject = flowGraphObject;
            name = flowGraphObject.name;

            m_GraphNodes = new List<FlowGraphNode>();
            styleSheets.Add(Resources.Load<StyleSheet>("FlowGraph"));
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale + 5);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            GridBackground grid = new();
            Insert(0, grid);
            grid.StretchToParentSize();

            Deserialize();
            CenterViewOnFirstNode();

            graphViewChanged = OnGraphChange;
        }

        private GraphViewChange OnGraphChange(GraphViewChange change)
        {
            if (change.elementsToRemove != null)
            {
                foreach (FlowGraphNode element in change.elementsToRemove.OfType<FlowGraphNode>())
                {
                    RemoveNode(element);
                }

                foreach (Edge element in change.elementsToRemove.OfType<Edge>())
                {
                    RemoveEdge(element);
                }
            }

            return change;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();
            ports.ForEach((port) =>
            {
                if (port != startPort && port.node != startPort.node && port.portType == startPort.portType && startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });

            return compatiblePorts;
        }

        public void Reload()
        {
            Serialize();
            m_GraphNodes.Clear();
            Deserialize();
        }

        public void Serialize()
        {
            foreach (FlowGraphNode node in m_GraphNodes)
            {
                node.Serialize();
            }
        }

        public void Deserialize()
        {
            IReadOnlyList<NodeData> nodeDatas = m_TargetGraphObject.GetNodeData();
            IReadOnlyList<FlowGraphNodeData> graphNodeDatas = m_TargetGraphObject.GetGraphNodeData();

            m_GraphNodes = new List<FlowGraphNode>(m_TargetGraphObject.Count);
            for (int i = 0; i < m_TargetGraphObject.Count; i++)
            {
                m_GraphNodes.Add(null);
            }

            // Add nodes
            for (int i = 0; i < m_TargetGraphObject.Count; i++)
            {
                AddNode(graphNodeDatas[i], nodeDatas[i], i);
            }

            // Connect nodes
            for (int i = 0; i < m_TargetGraphObject.Count; i++)
            {
                AddNodeConnections(nodeDatas[i], graphNodeDatas[i], PortDirection.Output);
            }
        }

        public void AddNodeConnections(NodeData nodeData, FlowGraphNodeData graphNodeData, PortDirection portDirection)
        {
            for (int j = 0; j < nodeData.FieldCount; j++)
            {
                if (nodeData.FieldTypes[j].Direction != portDirection) continue;

                for (int k = 0; k < nodeData[j].PortCount; k++)
                {
                    if (!nodeData[j].GetPort(k).IsValid()) continue;

                    PortLink portLink1 = new PortLink(graphNodeData.Guid, j, k);
                    PortLink portLink2 = nodeData[j].GetPort(k);

                    ConnectNodes(portDirection == PortDirection.Output ? portLink1 : portLink2, portDirection == PortDirection.Output ? portLink2 : portLink1);
                }
            }
        }

        public void AddNode(FlowGraphNodeData graphNodeData, NodeData nodeData, int index = -1)
        {
            if (graphNodeData == null)
            {
                Debug.LogWarning("Can not add node - node data is null");
                return;
            }

            if (graphNodeData.NodeType == null)
            {
                Debug.LogWarning("Can not add not of type null");
                return;
            }

            FlowGraphNode flowGraphNode = new FlowGraphNode(m_TargetGraphObject, nodeData, graphNodeData, this);

            if (index < 0 || index >= m_GraphNodes.Count)
            {
                m_GraphNodes.Add(flowGraphNode);
            }
            else
            {
                m_GraphNodes[index] = flowGraphNode;
            }

            AddElement(flowGraphNode);
        }

        public void RemoveNode(FlowGraphNode flowGraphNode)
        {
            m_GraphNodes.Remove(flowGraphNode);
            m_TargetGraphObject.RemoveNode(flowGraphNode.FlowGraphNodeData.Guid);

            if (m_TargetGraphObject.Count < 2 && m_TargetGraphObject.ValidateEntry())
            {
                // Needs to update graph
                Reload();
            }
        }

        public void RemoveEdge(Edge edge)
        {
            
        }

        public void ConnectNodes(PortLink port1, PortLink port2)
        {
            int nodeIndex1, nodeIndex2;

            try
            {
                nodeIndex1 = m_TargetGraphObject.GetNodeIndex(port1.TargetNode);
                nodeIndex2 = m_TargetGraphObject.GetNodeIndex(port2.TargetNode);
            }
            catch
            {
                Debug.LogWarning("Failed to connect nodes");
                return;
            }
            

            if (nodeIndex1 == nodeIndex2)
            {
                Debug.LogWarning("Can not connect node with itself");
                return;
            }

            FlowGraphNode node1 = m_GraphNodes[nodeIndex1];
            FlowGraphNode node2 = m_GraphNodes[nodeIndex2];

            Port inputPort = node1.GetPort(port1.Handle);
            Port outputPort = node2.GetPort(port2.Handle);

            if (inputPort == null || outputPort == null) return;

            Edge connection = new Edge()
            {
                input = outputPort,
                output = inputPort
            };

            outputPort.Connect(connection);
            inputPort.Connect(connection);

            AddElement(connection);
        }

        public void CenterViewOnFirstNode()
        {
            if (m_GraphNodes == null || m_GraphNodes.Count == 0 || m_GraphNodes[0] == null) return;

            schedule.Execute(() =>
            {
                Rect nodeRect = m_GraphNodes[0].GetPosition();
                Vector2 nodeCenter = nodeRect.center;
                Vector2 viewCenter = contentRect.size * 0.5f;
                Vector3 newPosition = viewTransform.position;
                newPosition.x = -nodeCenter.x + viewCenter.x;
                newPosition.y = -nodeCenter.y + viewCenter.y;

                viewTransform.position = newPosition;
            });
        }
    }
}