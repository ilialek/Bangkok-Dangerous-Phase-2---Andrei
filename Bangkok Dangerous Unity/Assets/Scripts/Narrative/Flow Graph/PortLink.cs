using System;
using Utilities;

namespace FlowGraph
{
    [Serializable]
    public struct PortLink
    {
        public GUID TargetNode;
        public PortHandle Handle;

        public PortLink(GUID targetNode, int portIndex = 0, int subPortIndex = 0)
        {
            TargetNode = targetNode;
            Handle = new PortHandle(portIndex, subPortIndex);
        }

        public bool IsValid() => !string.IsNullOrEmpty(TargetNode.Key);

        
        public static readonly PortLink None = new PortLink(GUID.None);
    }

    public struct PortHandle
    {
        public int Index;
        public int SubIndex;

        public PortHandle(int index, int subIndex)
        {
            Index = index;
            SubIndex = subIndex;
        }
    }
}