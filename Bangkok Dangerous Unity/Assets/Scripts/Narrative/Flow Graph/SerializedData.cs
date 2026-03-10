using UnityEngine;
using System;

namespace FlowGraph 
{
    [Serializable]
    public class SerializedData
    {
        public string Data;
        [SerializeField] private string m_TypeName;

        public Type Type
        {
            get => !string.IsNullOrEmpty(m_TypeName) ? Type.GetType(m_TypeName) : null;
            set => m_TypeName = value != null ? value.AssemblyQualifiedName : null;
        }

        public SerializedData(Type type, string data)
        {
            Type = type;
            Data = data;
        }

        public static SerializedData Serialize(object target) => new SerializedData(target.GetType(), JsonUtility.ToJson(target));

        public static object Deserialize(SerializedData serializedData) => JsonUtility.FromJson(serializedData.Data, serializedData.Type);
    }
}