using UnityEngine;

public class EnemyAttack : IState
{
    private EnemyAI enemy;
    private int attackDamage;
    private float attackCooldown;
    private CombatStyleSO combatStyle;
    private Player player;

    public EnemyAttack(EnemyAI owner, int damage, float cooldown, CombatStyleSO style, Player target)
    {
        enemy = owner;
        attackDamage = damage;
        attackCooldown = cooldown;
        combatStyle = style;
        player = target;
    }

    public void ExecuteState()
    {
        Debug.Log("Attacking the player with " + combatStyle.styleName);
    }

    public void OnEnterState()
    {
        
    }

    public void OnExitState()
    {
        
    }
}
