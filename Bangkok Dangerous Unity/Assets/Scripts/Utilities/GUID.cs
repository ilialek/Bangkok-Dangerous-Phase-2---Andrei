using System;
using UnityEngine;

namespace Utilities{
    [Serializable]
    public struct GUID
    {
        [SerializeField] private string m_Key;

        public string Key => m_Key;

        public GUID(string key)
        {
            m_Key = key;
        }

        public static GUID Create()
        {
            return new GUID(Guid.NewGuid().ToString());
        }

        public static readonly GUID None = new GUID("");

        public override string ToString()
        {
            return m_Key;
        }

        public override bool Equals(object obj)
        {
            return obj is GUID other && Equals(other);
        }

        public bool Equals(GUID other)
        {
            return m_Key == other.m_Key;
        }

        public override int GetHashCode()
        {
            return m_Key != null ? m_Key.GetHashCode() : 0;
        }

        public static bool operator ==(GUID left, GUID right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GUID left, GUID right)
        {
            return !(left == right);
        }
    }
}