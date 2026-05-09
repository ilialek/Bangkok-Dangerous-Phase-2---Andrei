using UnityEngine;
using UnityEngine.AI;

namespace GenericBehaviorTree
{

    public class MoveToCombatSlotNode : Node
    {
        private Transform self;
        private NavMeshAgent agent;
        private EnemyCombatContext combatContext;
        private float slotStopDistance;
        private float repathDistanceThreshold;
        private float slotRefreshCooldown;
        private float slotImprovementThreshold;

        public MoveToCombatSlotNode(
            Transform self,
            NavMeshAgent agent,
            EnemyCombatContext combatContext,
            float slotStopDistance,
            float slotRefreshCooldown = 1.0f,
            float slotImprovementThreshold = 0.75f,
            float repathDistanceThreshold = 0.2f)
        {
            this.self = self;
            this.agent = agent;
            this.combatContext = combatContext;
            this.slotStopDistance = slotStopDistance;
            this.slotRefreshCooldown = slotRefreshCooldown;
            this.slotImprovementThreshold = slotImprovementThreshold;
            this.repathDistanceThreshold = repathDistanceThreshold;
        }

        public override NodeState Evaluate()
        {
            if (combatContext == null || combatContext.combatSlotManager == null || !NavMeshAgentNavigation.IsReady(agent))
            {
                state = NodeState.FAILURE;
                return state;
            }

            if (!combatContext.hasAssignedSlot)
            {
                if (!combatContext.combatSlotManager.TryAssignSlot(combatContext))
                {
                    state = NodeState.FAILURE;
                    return state;
                }

                combatContext.MarkSlotRefresh();
            }
            else
            {
                if (combatContext.CanRefreshSlot(slotRefreshCooldown))
                {
                    combatContext.combatSlotManager.TryReassignToBetterSlot(
                        combatContext,
                        slotImprovementThreshold
                    );

                    combatContext.MarkSlotRefresh();
                }
                else
                {
                    combatContext.combatSlotManager.UpdateAssignedSlotPosition(combatContext);
                }
            }

            Vector3 slotPos = combatContext.assignedSlotPosition;
            float distance = Vector3.Distance(self.position, slotPos);

            if (distance > slotStopDistance)
            {
                NavMeshAgentNavigation.MoveTo(agent, slotPos, slotStopDistance, repathDistanceThreshold);
                state = NodeState.RUNNING;
                return state;
            }

            NavMeshAgentNavigation.Stop(agent);

            object targetObject = GetData("target");
            if (targetObject is Transform target)
            {
                Vector3 lookPos = target.position - self.position;
                lookPos.y = 0f;

                if (lookPos != Vector3.zero)
                    self.rotation = Quaternion.LookRotation(lookPos);
            }

            state = NodeState.RUNNING;
            return state;
        }
    }
}
