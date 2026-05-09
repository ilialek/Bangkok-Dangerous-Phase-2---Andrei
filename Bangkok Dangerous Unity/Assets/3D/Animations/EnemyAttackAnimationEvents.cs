using UnityEngine;

namespace GenericBehaviorTree
{
    public class EnemyAttackAnimationEvents : MonoBehaviour
    {
        [SerializeField] private EnemyCombatContext combatContext;

        private void Awake()
        {
            if (combatContext == null)
                combatContext = GetComponentInParent<EnemyCombatContext>();
        }

        public void OnAttackAnimationFinished()
        {
            Debug.LogError("Worked");

            if (combatContext == null)
                return;

            combatContext.FinishAttackAnimation();
        }
    }
}