using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameArchitecture
{
    // A scope component can store references to objects, but only one object per type.
    // Also only one scope can be added on an object
    [DefaultExecutionOrder(-5)]
    public class ScopeComponent : MonoBehaviour
    {
        public ScopeNode Node;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddReference(object reference)
        {
            Node.AddReference(reference);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveReference(object reference)
        {
            Node.RemoveReference(reference);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetReference<T>(out T result) where T : class
        {
            return Node.TryGetReference<T>(out result);
        }
    }
}