using System.Collections.Generic;
using UnityEngine;

#region Data Structures

/// <summary>
/// Represents a mapping between a body part and its associated collider.
/// </summary>
[System.Serializable]
public class BodyPartCollider
{
    public BodyPart bodyPart;
    public Collider collider;
}

#endregion

/// <summary>
/// Manages body part colliders for combat hit detection.
/// Activates and deactivates colliders based on attack requirements.
/// </summary>
public class AttackColliderManager : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Body Part Colliders")]
    [SerializeField] private List<BodyPartCollider> bodyPartColliders = new List<BodyPartCollider>();
    
    #endregion

    #region Private Fields
    
    private Dictionary<BodyPart, Collider> colliderMap = new Dictionary<BodyPart, Collider>();
    
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeColliderMap();
    }
    
    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the collider map and disables all colliders by default.
    /// </summary>
    private void InitializeColliderMap()
    {
        foreach (var bodyPartCollider in bodyPartColliders)
        {
            colliderMap[bodyPartCollider.bodyPart] = bodyPartCollider.collider;

            // Disable all colliders by default
            if (bodyPartCollider.collider != null)
            {
                bodyPartCollider.collider.enabled = false;
            }
        }
    }
    
    #endregion

    #region Public API

    /// <summary>
    /// Activates specific body part colliders.
    /// </summary>
    /// <param name="bodyParts">List of body parts to activate</param>
    /// <returns>List of activated colliders</returns>
    public List<Collider> ActivateColliders(List<BodyPart> bodyParts)
    {
        List<Collider> activatedColliders = new List<Collider>();

        foreach (var bodyPart in bodyParts)
        {
            if (colliderMap.TryGetValue(bodyPart, out Collider collider) && collider != null)
            {
                collider.enabled = true;
                activatedColliders.Add(collider);
            }
        }

        return activatedColliders;
    }

    /// <summary>
    /// Deactivates all body part colliders.
    /// </summary>
    public void DeactivateAllColliders()
    {
        foreach (var kvp in colliderMap)
        {
            if (kvp.Value != null)
            {
                kvp.Value.enabled = false;
            }
        }
    }

    /// <summary>
    /// Gets all currently active colliders.
    /// </summary>
    /// <returns>List of active colliders</returns>
    public List<Collider> GetActiveColliders()
    {
        List<Collider> activeColliders = new List<Collider>();

        foreach (var kvp in colliderMap)
        {
            if (kvp.Value != null && kvp.Value.enabled)
            {
                activeColliders.Add(kvp.Value);
            }
        }

        return activeColliders;
    }
    
    #endregion
}