using UnityEngine;

/// <summary>
/// Pickup component for healing items. References a HealingItemData ScriptableObject
/// that defines the item's properties (heal amount, duration, buffs, etc.)
/// </summary>
public class HealingItem : MonoBehaviour
{
    [Header("Item Configuration")]
    [Tooltip("The data asset that defines this healing item's properties")]
    public HealingItemData itemData;

    [Header("Optional Visuals")]
    [Tooltip("Optional: renderer to disable when picked up")]
    public Renderer itemRenderer;
    
    [Tooltip("Optional: collider to disable when picked up")]
    public Collider itemCollider;

    private void Start()
    {
        if (itemData == null)
        {
            Debug.LogError($"HealingItem on {gameObject.name} has no itemData assigned!", this);
        }
    }

    /// <summary>
    /// Called by PlayerHealthSystem when this item is picked up
    /// </summary>
    public void OnPickedUp()
    {
        // Disable visuals and collision
        if (itemRenderer != null)
            itemRenderer.enabled = false;
        
        if (itemCollider != null)
            itemCollider.enabled = false;

        // Destroy after a short delay to avoid reference issues
        Destroy(gameObject, 0.1f);
    }
}
