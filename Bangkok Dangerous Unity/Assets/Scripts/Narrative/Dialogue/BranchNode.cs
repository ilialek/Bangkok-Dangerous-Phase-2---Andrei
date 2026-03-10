using UnityEngine;
using FlowGraph;
using System.Collections.Generic;

namespace Dialogue
{
    public class BranchNode : NodeData
    {
        public override string Name => "Branch";

        // Fields
        public MutliValuePortField<string> Options = new MutliValuePortField<string>();

        // Ports
        public PortField InPort = new PortField();

        // Access
        public override Field this[int index]
        {
            get => index switch
            {
                0 => InPort,
                1 => Options,
                _ => Field.None
            };
            set
            {
                switch (index)
                {
                    case 0: InPort = (PortField)value; break;
                    case 2: Options = (MutliValuePortField<string>)value; break;
                }
            }
        }

        public override int FieldCount => 2;

        // Types
        public override IReadOnlyList<FieldType> FieldTypes => m_FieldTypes;

        private static readonly FieldType[] m_FieldTypes =
        {
            new FieldType(PortType.Port, "In", PortDirection.Input, typeof(string)),
            new FieldType(PortType.MultiPortValue, "Option", PortDirection.Output, typeof(string)),
        };
    }
}