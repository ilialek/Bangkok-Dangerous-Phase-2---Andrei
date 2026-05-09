using UnityEngine;
using UnityEngine.AI;

namespace GenericBehaviorTree
{
    public class MoveToAttackRangeNode : Node
    {
        private Transform self;
        private NavMeshAgent agent;
        private EnemyCombatContext combatContext;
        private float attackRange;
        private float repathThreshold;

        public MoveToAttackRangeNode(
            Transform self,
            NavMeshAgent agent,
            EnemyCombatContext combatContext,
            float attackRange,
            float repathThreshold = 0.2f)
        {
            this.self = self;
            this.agent = agent;
            this.combatContext = combatContext;
            this.attackRange = attackRange;
            this.repathThreshold = repathThreshold;
        }

        public override NodeState Evaluate()
        {
            object targetObject = GetData("target");

            if (targetObject == null || combatContext == null || !NavMeshAgentNavigation.IsReady(agent))
            {
                state = NodeState.FAILURE;
                return state;
            }

            Transform target = (Transform)targetObject;
            if (target == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            if (!combatContext.hasAttackPermission)
            {
                state = NodeState.FAILURE;
                return state;
            }

            float distance = Vector3.Distance(self.position, target.position);

            if (distance <= attackRange)
            {
                NavMeshAgentNavigation.Stop(agent);
                state = NodeState.SUCCESS;
                return state;
            }

            NavMeshAgentNavigation.MoveTo(agent, target.position, attackRange * 0.9f, repathThreshold);

            state = NodeState.RUNNING;
            return state;
        }
    }
}
