using FlowGraph;
using System;
using System.Collections.Generic;

namespace Dialogue
{
    [Serializable]
    public class TextNode : NodeData
    {
        public override string Name => "Text";

        // Fields
        public ValueField<string> Text = new ValueField<string>();
        public ValueField<string> CharacterGuid = new ValueField<string>(); // Serialize ScriptableObject with guid

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
                2 => Text,
                3 => CharacterGuid,
                _ => Field.None
            };
            set
            {
                switch (index)
                {
                    case 0: InPort = (PortField)value; break;
                    case 1: OutPort = (PortField)value; break;
                    case 2: Text = (ValueField<string>)value; break;
                    case 3: CharacterGuid = (ValueField<string>)value; break;
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
            new FieldType(PortType.Value, "", typeof(string)),
            new FieldType(PortType.Value, "Character", typeof(Character)),
        };
    }
}