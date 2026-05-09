using UnityEngine;

[DisallowMultipleComponent]
public class BT1AttackAnimationEvents : MonoBehaviour
{
    [SerializeField] private BT1CombatAgent combatAgent;

    private void Awake()
    {
        if (combatAgent == null)
            combatAgent = GetComponentInParent<BT1CombatAgent>();
    }

    public void OnAttackAnimationFinished()
    {
        if (combatAgent != null)
            combatAgent.NotifyAttackAnimationFinished();
    }

    public void AttackAnimationFinished()
    {
        OnAttackAnimationFinished();
    }
}
