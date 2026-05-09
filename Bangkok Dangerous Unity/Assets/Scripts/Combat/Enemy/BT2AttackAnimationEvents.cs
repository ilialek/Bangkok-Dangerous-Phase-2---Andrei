using UnityEngine;

[DisallowMultipleComponent]
public class BT2AttackAnimationEvents : MonoBehaviour
{
    [SerializeField] private BT2EnemyContext context;

    private void Awake()
    {
        if (context == null)
            context = GetComponentInParent<BT2EnemyContext>();
    }

    public void OnAttackAnimationFinished()
    {
        if (context != null)
            context.NotifyAttackAnimationFinished();
    }

    public void AttackAnimationFinished()
    {
        OnAttackAnimationFinished();
    }
}
