using FlowGraph;
using System;
using System.Collections.Generic;

namespace Quest
{
    [Serializable]
    public class MissionNode : NodeData
    {
        public override string Name => "Mission";

        // Fields
        public ValueField<string> Title = new ValueField<string>();
        public ValueField<string> Description = new ValueField<string>();

        // Ports
        public PortField InPort = new PortField();
        public PortField OutPort = new PortField();

        // Access
        public override Field this[int index]
        {
            get => index switch
            {
                0 => InPort,
                1 => OutPort,
                2 => Title,
                3 => Description,
                _ => Field.None
            };
            set
            {
                switch (index)
                {
                    case 0: InPort = (PortField)value; break;
                    case 1: OutPort = (PortField)value; break;
                    case 2: Title = (ValueField<string>)value; break;
                    case 3: Description = (ValueField<string>)value; break;
                }
            }
        }

        public override int FieldCount => 4;

        // Types
        public override IReadOnlyList<FieldType> FieldTypes => m_FieldTypes;

        private static readonly FieldType[] m_FieldTypes =
        {
            new FieldType(PortType.Port, "In", PortDirection.Input, typeof(string)),
            new FieldType(PortType.Port, "Out", PortDirection.Output,  typeof(string)),
            new FieldType(PortType.Value, "Title", typeof(string)),
            new FieldType(PortType.Value, "Description", typeof(string)),
        };
    }
}