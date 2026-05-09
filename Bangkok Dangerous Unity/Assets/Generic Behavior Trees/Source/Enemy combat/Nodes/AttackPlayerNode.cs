using UnityEngine;
using UnityEngine.AI;

namespace GenericBehaviorTree
{
    public class AttackPlayerNode : Node
    {
        private Transform self;
        private NavMeshAgent agent;
        private float attackRange;
        private float attackCooldown;
        private float recoveryDuration;
        private int damage;
        private Animator animator;
        private EnemyCombatContext combatContext;
        private CombatDirector combatDirector;

        private int attackTriggerHash;
        private bool attackTriggered = false;

        public AttackPlayerNode(
            Transform self,
            NavMeshAgent agent,
            float attackRange,
            float attackCooldown,
            float recoveryDuration,
            int damage,
            Animator animator,
            EnemyCombatContext combatContext,
            CombatDirector combatDirector,
            string attackTriggerName = "Attack"
        )
        {
            this.self = self;
            this.agent = agent;
            this.attackRange = attackRange;
            this.attackCooldown = attackCooldown;
            this.recoveryDuration = recoveryDuration;
            this.damage = damage;
            this.animator = animator;
            this.combatContext = combatContext;
            this.combatDirector = combatDirector;
            attackTriggerHash = Animator.StringToHash(attackTriggerName);
        }

        public override NodeState Evaluate()
        {
            object targetObject = GetData("target");

            if (targetObject == null)
            {
                ResetAttackState();
                ReleaseAttackPermission();
                state = NodeState.FAILURE;
                return state;
            }

            Transform target = (Transform)targetObject;
            if (target == null || combatContext == null || combatDirector == null)
            {
                ResetAttackState();
                ReleaseAttackPermission();
                state = NodeState.FAILURE;
                return state;
            }

            if (!combatContext.hasAttackPermission)
            {
                ResetAttackState();
                state = NodeState.FAILURE;
                return state;
            }

            // If attack already started, wait until the animation event says it is finished.
            if (attackTriggered)
            {
                NavMeshAgentNavigation.Stop(agent);

                if (!combatContext.attackAnimationFinished)
                {
                    state = NodeState.RUNNING;
                    return state;
                }

                combatContext.StartRecovery(recoveryDuration);
                combatDirector.NotifyAttackPerformed(combatContext);
                ReleaseAttackPermission();
                ResetAttackState();

                state = NodeState.SUCCESS;
                return state;
            }

            if (combatContext.IsRecovering)
            {
                state = NodeState.FAILURE;
                return state;
            }

            float distance = Vector3.Distance(self.position, target.position);
            if (distance > attackRange)
            {
                state = NodeState.FAILURE;
                return state;
            }

            if (NavMeshAgentNavigation.IsReady(agent))
            {
                if (agent.pathPending)
                {
                    state = NodeState.RUNNING;
                    return state;
                }

                if (agent.remainingDistance > agent.stoppingDistance + 0.05f)
                {
                    state = NodeState.RUNNING;
                    return state;
                }

                if (agent.velocity.sqrMagnitude > 0.01f)
                {
                    state = NodeState.RUNNING;
                    return state;
                }

                NavMeshAgentNavigation.Stop(agent);
            }

            if (!combatContext.IsAttackReady(attackCooldown))
            {
                state = NodeState.FAILURE;
                return state;
            }

            Vector3 lookPos = target.position - self.position;
            lookPos.y = 0f;

            if (lookPos != Vector3.zero)
                self.rotation = Quaternion.LookRotation(lookPos);

            combatContext.MarkAttackPerformed();
            combatContext.BeginAttackAnimation();

            if (animator != null)
                animator.SetTrigger(attackTriggerHash);

            Debug.Log(self.name + " attacked " + target.name + " for " + damage + " damage.");

            attackTriggered = true;

            state = NodeState.RUNNING;
            return state;
        }

        private void ReleaseAttackPermission()
        {
            if (combatContext == null)
                return;

            combatContext.hasAttackPermission = false;

            if (combatDirector != null)
                combatDirector.ReleaseAttackPermission(combatContext);
        }

        private void ResetAttackState()
        {
            attackTriggered = false;

            if (combatContext != null)
                combatContext.ResetAttackAnimationState();
        }
    }
}