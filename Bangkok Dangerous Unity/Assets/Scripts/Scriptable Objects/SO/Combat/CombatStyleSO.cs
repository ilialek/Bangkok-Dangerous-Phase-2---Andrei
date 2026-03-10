using UnityEngine;

[CreateAssetMenu(fileName = "New Combat Style", menuName = "Combat/Combat Style")]
public class CombatStyleSO : ScriptableObject
{
    [Header("Starting Attacks")]
    public AttackSO startingLightAttack;
    public AttackSO startingHeavyAttack;
    
    [Header("Special Moves")]
    public AttackSO[] specialMoves;
    public AttackSO[] grabMove;
    
    [Header("Style Properties")]
    public string styleName;
    public float overallSpeedMultiplier = 1f;
    public float overallDamageMultiplier = 1f;
}
