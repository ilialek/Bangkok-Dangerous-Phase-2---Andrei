using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Combat/Enemy Data", order = 1)]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public int maxHealth;
    public float moveSpeed;
    public int attackDamage;
    public float attackCooldown;
    public CombatStyleSO combatStyle;
    public int carriedExperience;
}
