using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameArchitecture
{
    // Keeps a reference to all scopes
    [DefaultExecutionOrder(-10)]
    public class ReferenceManager
    {
        private static ScopeData m_GlobalScope = new ScopeData();
        private static Dictionary<int, ScopeNode> m_SceneScope = new Dictionary<int, ScopeNode>();

        //----------------------------------------------------
        //      Setup functions
        //----------------------------------------------------

        /// <summary>
        /// Setup scene load callback, before first scene is loaded
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        /// <summary>
        /// Setup the references on the current gameobjects in the scene, 
        /// Function called after Awake and OnEnable but before start
        /// </summary>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AddSceneScope(scene);
        }

        /// <summary>
        /// Removes any saved references when a scene is unloaded
        /// </summary>
        private static void OnSceneUnloaded(Scene scene)
        {
            m_SceneScope.Remove(scene.handle);
        }

        /// <summary>
        /// After creating a new instance of an object call this function to setup its references correctly.
        /// Do not use this function when creating multiple instances that need to reference each othe.
        /// Call first SetupInstance_Register on the individual objects and SetupInstance_Setup later
        /// </summary>
        public static void SetupInstance(GameObject target)
        {
            IReference[] references = target.GetComponentsInChildren<IReference>(true);

            foreach (IReference reference in references)
            {
                reference.Register();
            }

            foreach (IReference reference in references)
            {
                reference.Setup();
            }
        }

        public static void SetupInstance_Register(GameObject target)
        {
            IReference[] references = target.GetComponentsInChildren<IReference>(true);

            foreach (IReference reference in references)
            {
                reference.Register();
            }
        }

        public static void SetupInstance_Setup(GameObject target)
        {
            IReference[] references = target.GetComponentsInChildren<IReference>(true);

            foreach (IReference reference in references)
            {
                reference.Setup();
            }
        }


        //----------------------------------------------------
        //      Scope functions
        //----------------------------------------------------

        /// <summary>
        /// Registers a new scene and create the scope tree with the existing objects.
        /// Call this function when loading a new scene (before?)
        /// </summary>
        public static void AddSceneScope(Scene scene)
        {
            ScopeNode sceneScopeNode = new ScopeNode(null, null);

            // Traverse through the entire hierachy and create the scope tree
            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (GameObject target in rootObjects)
            {
                if (target.TryGetComponent(out ScopeComponent scopeComponent))
                {
                    ScopeNode node = new ScopeNode(target, sceneScopeNode);
                    scopeComponent.Node = node;
                    sceneScopeNode.AddChild(node);
                    AddScope_Internal(target, node);
                }
                else
                {
                    AddScope_Internal(target, sceneScopeNode);
                }
            }

            m_SceneScope.Add(scene.handle, sceneScopeNode);

            // Call functions on reference scripts
            foreach (GameObject target in rootObjects)
            {
                SetupInstance_Register(target);
            }

            foreach (GameObject target in rootObjects)
            {
                SetupInstance_Setup(target);
            }
        }

        /// <summary>
        /// Recursively loops through the hierachy of a parent gameobject and adds the scope to the correct nodes
        /// </summary>
        private static void AddScope_Internal(GameObject target, ScopeNode parentNode)
        {
            foreach (Transform child in target.transform)
            {
                if (child.TryGetComponent(out ScopeComponent scopeComponent))
                {
                    ScopeNode node = new ScopeNode(child.gameObject, parentNode);
                    scopeComponent.Node = node;
                    parentNode.AddChild(node);
                    AddScope_Internal(child.gameObject, node);
                }
                else
                {
                    AddScope_Internal(child.gameObject, parentNode);
                }
            }
        }

        /// <summary>
        /// Function searches for parent scope and reoders the scope tree for a new tree
        /// </summary>
        public static void InsertScope(GameObject target)
        {
            // Check if scope already exists on the target
            {
                ScopeNode self = target.GetComponent<ScopeNode>();
                if (self != null) return;
            }

            ScopeNode parentScope = GetParentScope(target);

            //Parent scope should exist
            if (parentScope == null) return;

            List<ScopeNode> childrenScopes = GetChildrenScopes(target, parentScope);

            ScopeNode scopeNode = new ScopeNode(target, parentScope);

            foreach (ScopeNode childScope in childrenScopes)
            {
                parentScope.RemoveChild(childScope);
                scopeNode.AddChild(childScope);
                childScope.UpdateParentNode(scopeNode);
            }

            parentScope.AddChild(scopeNode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ScopeNode GetParentScope(GameObject target)
        {
            ScopeComponent scopeComponent = target.GetComponentInParent<ScopeComponent>(true);

            if (scopeComponent == null) return null;

            return scopeComponent.Node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<ScopeNode> GetChildrenScopes(GameObject target, ScopeNode parentNode)
        {
            List<ScopeNode> childScopes = new List<ScopeNode>();
            ScopeComponent[] scopes = target.GetComponentsInChildren<ScopeComponent>(true);

            foreach (var scope in scopes)
            {
                // Check if the child of the parent node is also the child of the target object
                if (scope.gameObject.transform.IsChildOf(target.transform))
                {
                    childScopes.Add(scope.Node);
                }
            }

            return childScopes;
        }

        //todo also allow scopes to be added at a different location (like in a parent)

        //----------------------------------------------------
        //      Reference functions
        //----------------------------------------------------

        /// <summary>
        /// Adds the reference based on the target GameObject into the correct scope
        /// </summary>
        public static void AddReference(ScopeType scopeType, GameObject target, object reference)
        {
            switch (scopeType)
            {
                case ScopeType.Global:
                    AddReference_Global(reference);
                    break;
                case ScopeType.Scene:
                    AddReference_Scene(target, reference);
                    break;
                case ScopeType.Self:
                    AddReference_Self(target, reference);
                    break;
                case ScopeType.Parent:
                    throw new NotImplementedException();
                case ScopeType.Children:
                    throw new NotImplementedException();
            }
        }

        public static void AddReference_Global(object reference)
        {
            m_GlobalScope.AddReference(reference);
        }

        public static void AddReference_Scene(GameObject target, object reference)
        {
            if (!m_SceneScope.ContainsKey(target.scene.handle))
            {
                Debug.LogWarning($"Scene {target.scene.name} is not setup as a scene scope.");
                return;
            }

            ScopeNode sceneScope = m_SceneScope[target.scene.handle];
            sceneScope.AddReference(reference);
        }

        public static void AddReference_Self(GameObject target, object reference)
        {
            ScopeComponent scopeComponent = target.GetComponent<ScopeComponent>();

            if (scopeComponent == null)
            {
                Debug.LogWarning($"No scope on {target.name} added.");
                return;
            }

            scopeComponent.AddReference(reference);
        }

        public static void AddReference_Other(GameObject target, object reference, ScopeComponent scope)
        {
            scope.AddReference(reference);
        }


        /// <summary>
        /// Removes the reference based on the target GameObject into the correct scope
        /// </summary>
        public static void RemoveReference(ScopeType scopeType, GameObject target, object reference)
        {
            switch (scopeType)
            {
                case ScopeType.Global:
                    RemoveReference_Global(reference);
                    break;
                case ScopeType.Scene:
                    RemoveReference_Scene(target, reference);
                    break;
                case ScopeType.Self:
                    RemoveReference_Self(target, reference);
                    break;
                case ScopeType.Parent:
                    throw new NotImplementedException();
                case ScopeType.Children:
                    throw new NotImplementedException();
            }
        }

        public static void RemoveReference_Global(object reference)
        {
            m_GlobalScope.RemoveReference(reference);
        }

        public static void RemoveReference_Scene(GameObject target, object reference)
        {
            if (!m_SceneScope.ContainsKey(target.scene.handle))
            {
                Debug.LogWarning($"Scene {target.scene.name} is not setup as a scene scope.");
                return;
            }

            ScopeNode sceneScope = m_SceneScope[target.scene.handle];
            sceneScope.RemoveReference(reference);
        }

        public static void RemoveReference_Self(GameObject target, object reference)
        {
            ScopeComponent scopeComponent = target.GetComponent<ScopeComponent>();

            if (scopeComponent == null)
            {
                Debug.LogWarning($"No scope on {target.name} added.");
                return;
            }

            scopeComponent.RemoveReference(reference);
        }

        public static void RemoveReference_Other(GameObject target, object reference, ScopeComponent scope)
        {
            scope.RemoveReference(reference);
        }

        public static bool TryGetReference<T>(ScopeType scopeType, GameObject target, out T result) where T : class
        {
            switch (scopeType)
            {
                case ScopeType.Global:
                    return TryGetReference_Global<T>(out result);
                case ScopeType.Scene:
                    return TryGetReference_Scene<T>(target, out result);
                case ScopeType.Self:
                    return TryGetReference_Self<T>(target, out result);
                case ScopeType.Parent:
                    return TryGetReference_Parent<T>(target, out result);
                case ScopeType.Children:
                    return TryGetReference_Children<T>(target, out result);
            }

            // Will never be reached but compiler is stupid
            result = null;
            return false;
        }

        public static bool TryGetReference_Global<T>(out T result) where T : class
        {
            return m_GlobalScope.TryGetReference<T>(out result);
        }

        public static bool TryGetReference_Scene<T>(GameObject target, out T result) where T : class
        {
            if (!m_SceneScope.ContainsKey(target.scene.handle))
            {
                Debug.LogWarning($"Scene {target.scene.name} is not setup as a scene scope.");
                result = null;
                return false;
            }

            ScopeNode sceneScope = m_SceneScope[target.scene.handle];
            return sceneScope.TryGetReference<T>(out result);
        }

        public static bool TryGetReference_Self<T>(GameObject target, out T result) where T : class
        {
            ScopeComponent scopeComponent = target.GetComponent<ScopeComponent>();

            if (scopeComponent == null)
            {
                Debug.LogWarning($"No scope on {target.name} added.");
                result = null;
                return false;
            }

            return scopeComponent.TryGetReference<T>(out result);
        }

        /// <summary>
        /// Searches upwards for the reference. It searches through every parent object, excluding itself.
        /// </summary>
        public static bool TryGetReference_Parent<T>(GameObject target, out T result) where T : class
        {
            if (target.TryGetComponent(out ScopeComponent targetScope))
            {
                return TryGetReference_ParentFromScope(targetScope.Node, out result);
            }

            Transform parentTransform = target.transform.parent;

            if (parentTransform == null)
            {
                Debug.LogWarning("Root object cannot get reference from parent.");
                result = null;
                return false;
            }

            ScopeComponent parentScopeComponent = parentTransform.GetComponentInParent<ScopeComponent>(true);

            if (parentScopeComponent == null)
            {
                Debug.LogWarning("Parent scope not found.");
                result = null;
                return false;
            }

            if (parentScopeComponent.TryGetReference<T>(out result)) return true;

            // Reference not present in scope component. Search for more upward components
            return TryGetReference_Parent<T>(parentScopeComponent.gameObject, out result);
        }

        /// <summary>
        /// Searches upwards for the reference. It expects a scope on the target from which it will search through parent scopes, excluding itself.
        /// </summary>
        public static bool TryGetReference_ParentFromScope<T>(ScopeNode scopeNode, out T result) where T : class
        {
            if (scopeNode.TryGetReference(out result)) return true;

            return TryGetReference_ParentFromScope(scopeNode.GetParentScope(), out result);
        }

        /// <summary>
        /// Searches upwards for the reference. It searches through every child object, excluding itself.
        /// Expensive function. Use with care.
        /// </summary>
        public static bool TryGetReference_Children<T>(GameObject target, out T result) where T : class
        {
            foreach (Transform childTransform in target.transform)
            {
                if (childTransform.TryGetComponent(out ScopeComponent childScopeComponent))
                {
                    if (childScopeComponent.TryGetReference<T>(out result)) return true;
                }

                if (TryGetReference_Children<T>(childTransform.gameObject, out result)) return true;
            }

            result = null;
            return false;
        }
    }
}