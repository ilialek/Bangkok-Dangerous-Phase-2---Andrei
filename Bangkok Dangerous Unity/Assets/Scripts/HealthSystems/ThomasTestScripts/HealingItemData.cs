using UnityEngine;

[CreateAssetMenu(fileName = "NewHealingItem", menuName = "Items/Healing Item Data")]
public class HealingItemData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Name of the healing item")]
    public string itemName = "Healing Item";
    
    [Tooltip("Sprite to display in the UI slot")]
    public Sprite itemSprite;

    [Tooltip("Description of the item shown in tooltips")]
    public string itemDescription = "A healing item";

    [Header("Healing Properties")]
    [Tooltip("Total amount of health this item restores")]
    public int healAmount = 25;
    
    [Tooltip("How long the healing effect lasts in seconds")]
    public float healDuration = 5f;
    
    [Tooltip("How often healing ticks occur in seconds")]
    public float healTickRate = 0.5f;

    [Header("Movement Modifier")]
    [Tooltip("Speed multiplier applied to player while active (1 = normal, >1 = faster, <1 = slower)")]
    [Range(0.1f, 3f)]
    public float speedMultiplier = 1f;

    [Header("Combat Modifier")]
    [Tooltip("Damage multiplier applied to player attacks while active (1 = normal, >1 = more damage)")]
    [Range(0.1f, 5f)]
    public float damageMultiplier = 1f;

    [Header("Visual Effects")]
    [Tooltip("Particle system prefab to spawn on player while this item is active")]
    public ParticleSystem activeParticleEffect;
}
