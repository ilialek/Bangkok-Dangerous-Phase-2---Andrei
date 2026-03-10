using System;
using System.Collections.Generic;

namespace FlowGraph
{
    public abstract class NodeData
    {
        public abstract string Name { get; }
        
        // Fields
        public abstract Field this[int index] { get; set; }
        public abstract IReadOnlyList<FieldType> FieldTypes { get; }
        public abstract int FieldCount { get; }
    }

    [Serializable]
    public class PortField : Field
    {
        public PortLink Port;

        public override int PortCount => 1;
        public override PortLink GetPort(int index) => Port;
        public override void SetPort(int index, PortLink port) => Port = port;

        public PortField() : this(PortLink.None) { }
        public PortField(PortLink port)
        {
            Port = port;
        }
    }

    [Serializable]
    public class ValueField<T> : Field
    {
        public T Value;
        
        public override int ValueCount => 1;
        public override object this[int index]
        {
            get => Value;
            set => Value = (T)value;
        }

        public ValueField()
        {
            Value = default(T);
        }

        public ValueField(T value)
        {
            Value = value;
        }
    }

    [Serializable]
    public class ValuePortField<T> : Field
    {
        public T Value;
        public PortLink Port;

        public override int PortCount => 1;
        public override int ValueCount => 1;

        public override PortLink GetPort(int index) => Port;
        public override void SetPort(int index, PortLink port) => Port = port;

        public override object this[int index]
        {
            get => Value;
            set => Value = (T)value;
        }

        public ValuePortField()
        {
            Value = default(T);
            Port = PortLink.None;
        }

        public ValuePortField(T value, PortLink port)
        {
            Value = value;
            Port = port;
        }
    }

    [Serializable]
    public class MutliValuePortField<T> : Field
    {
        public PortLink[] Ports = new PortLink[0];
        public T[] Values = new T[0];

        public override int PortCount => Ports.Length;
        public override int ValueCount => Values.Length;

        public override PortLink GetPort(int index) => Ports[index];
        public override void SetPort(int index, PortLink port) => Ports[index] = port;

        public override object this[int index]
        {
            get
            {
                if (index < 0)
                {
                    return ValueCount;
                }

                return Values[index];
            }
            set
            {
                if (index < 0)
                {
                    HandleSize((int)value);
                    return;
                }

                Values[index] = (T)value;
            }
        }

        private void HandleSize(int count)
        {
            if (count < 0 || count == Values.Length) return;

            int oldLength = Values.Length;
            Array.Resize(ref Ports, count);
            Array.Resize(ref Values, count);

            for (int i = oldLength; i < Ports.Length; i++)
            {
                Ports[i] = new PortLink();
                Values[i] = default;
            }
        }
    }

    public class Field
    {
        public virtual int PortCount => 0;
        public virtual int ValueCount => 0;

        public virtual PortLink GetPort(int index) => PortLink.None;
        public virtual void SetPort(int index, PortLink port) { }

        public virtual object this[int index]
        {
            get => throw new ArgumentOutOfRangeException();
            set => throw new ArgumentOutOfRangeException();
        }


        public static readonly Field None = new Field();
    }

    public readonly struct FieldType
    {
        public readonly PortType PortType;
        public readonly string Name;
        public readonly PortDirection Direction;
        public readonly Type Type;

        public FieldType(PortType portType, string name, PortDirection direction = PortDirection.None, Type type = null)
        {
            PortType = portType;
            Name = name;
            Direction = direction;
            Type = type ?? typeof(bool);
        }

        public FieldType(PortType portType, string name, Type type = null, PortDirection direction = PortDirection.None)
        {
            PortType = portType;
            Name = name;
            Type = type ?? typeof(bool);
            Direction = direction;
        }
    }

    public enum PortDirection
    {
        None,
        Input,
        Output
    }

    [Flags]
    public enum PortType
    {
        None = 0,
        Port = 1 << 0,
        Value = 1 << 1,
        Multi = 1 << 2,

        PortValue = Port | Value,
        MultiPort = Port | Multi,
        MultiValue = Port | Multi,
        MultiPortValue = Port | Value | Multi,
    }
}