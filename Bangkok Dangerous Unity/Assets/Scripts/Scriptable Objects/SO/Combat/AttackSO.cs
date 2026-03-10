using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public enum BodyPart
{
    LeftFist,
    RightFist,
    LeftKnee,
    RightKnee,
    LeftFoot,
    RightFoot
}

public enum AttackEffect
{
    None,
    Stagger,
    Stun,
    Knockback,
    Bleed
}

[CreateAssetMenu(fileName = "New Attack", menuName = "Combat/Attack")]
public class AttackSO : ScriptableObject
{
    [Header("Animation")]
    public string animationTrigger;
    
    [Header("Combat")]
    public float damage;
    public float staminaCost;
    public bool canBeBlocked = true;
    public float comboWindowStart = 0.5f;
    public float comboWindowEnd = 0.8f;
    
    [Header("Combo Flow")]
    public AttackSO[] lightAttackFollowUps;
    public AttackSO[] heavyAttackFollowUps;
    public bool canCancelToMovement = true;
    public bool isFinisher = false;
    
    [Header("Effects")]
    public List<AttackEffect> effects = new();
    public float hitstop = 0f;
    public float knockback = 2f;

    [Header("Hitbox Configuration")]
    [Tooltip("Which body part colliders should be active for this attack")]
    public List<BodyPart> activeBodyParts = new();
}
