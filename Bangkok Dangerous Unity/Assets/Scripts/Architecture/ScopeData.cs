using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GameArchitecture
{
    public class ScopeData
    {
        private Dictionary<Type, object> m_References;

        public ScopeData()
        {
            m_References = new Dictionary<Type, object>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddReference(object reference)
        {
            Type type = reference.GetType();
            if (!m_References.ContainsKey(type))
            {
                m_References.Add(type, reference);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveReference(object reference)
        {
            Type type = reference.GetType();
            if (m_References.ContainsKey(type))
            {
                m_References.Remove(type);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetReference<T>(out T result) where T : class
        {
            Type type = typeof(T);
            if (m_References.TryGetValue(type, out var reference))
            {
                result = reference as T;
                return true;
            }

            result = null;
            return false;
        }
    }
}