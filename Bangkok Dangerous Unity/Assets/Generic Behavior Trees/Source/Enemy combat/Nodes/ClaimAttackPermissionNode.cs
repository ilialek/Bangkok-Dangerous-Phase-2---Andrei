
namespace GenericBehaviorTree
{
    public class ClaimAttackPermissionNode : Node
    {
        private EnemyCombatContext combatContext;
        private CombatDirector combatDirector;

        public ClaimAttackPermissionNode(EnemyCombatContext combatContext, CombatDirector combatDirector)
        {
            this.combatContext = combatContext;
            this.combatDirector = combatDirector;
        }

        public override NodeState Evaluate()
        {
            if (combatContext == null || combatDirector == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            if (combatContext.hasAttackPermission)
            {
                state = NodeState.SUCCESS;
                return state;
            }

            if (combatDirector.TryClaimAttackPermission(combatContext))
            {
                combatContext.hasAttackPermission = true;
                state = NodeState.SUCCESS;
                return state;
            }

            state = NodeState.FAILURE;
            return state;
        }
    }
}