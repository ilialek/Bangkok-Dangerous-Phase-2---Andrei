using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace GameArchitecture
{
    public class ScopeNode : IEquatable<ScopeNode>
    {
        private ScopeData m_ScopeData;
        private readonly GameObject m_Target;
        private ScopeNode m_ParentScope;
        private List<ScopeNode> m_ChildNodes;

        public ScopeNode(GameObject target, ScopeNode parentScope)
        {
            m_ScopeData = new ScopeData();
            m_Target = target;
            m_ChildNodes = new List<ScopeNode>();
            m_ParentScope = parentScope;
        }

        //----------------------------------------------------
        //      Reference functions
        //----------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddReference(object reference)
        {
            m_ScopeData.AddReference(reference);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveReference(object reference)
        {
            m_ScopeData.RemoveReference(reference);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetReference<T>(out T result) where T : class
        {
            return m_ScopeData.TryGetReference<T>(out result);
        }

        //----------------------------------------------------
        //      Node functions
        //----------------------------------------------------

        public void AddChild(ScopeNode childNode)
        {
            m_ChildNodes.Add(childNode);
        }

        public bool RemoveChild(ScopeNode childNode)
        {
            return m_ChildNodes.Remove(childNode);
        }

        public void UpdateParentNode(ScopeNode parentNode)
        {
            m_ParentScope = parentNode;
        }

        public ScopeNode GetParentScope()
        {
            return m_ParentScope;
        }

        public bool IsSceneNode()
        {
            return m_Target == null || m_ParentScope == null;
        }

        public override int GetHashCode() => m_Target.GetInstanceID(); //todo not monobehavior

        public override bool Equals(object obj) => obj is ScopeNode && Equals((ScopeNode)obj);

        public bool Equals(ScopeNode other) => m_Target.GetInstanceID() == other.m_Target.GetInstanceID();

        public static bool operator ==(ScopeNode left, ScopeNode right) => left.Equals(right);

        public static bool operator !=(ScopeNode left, ScopeNode right) => !left.Equals(right);
    }
}