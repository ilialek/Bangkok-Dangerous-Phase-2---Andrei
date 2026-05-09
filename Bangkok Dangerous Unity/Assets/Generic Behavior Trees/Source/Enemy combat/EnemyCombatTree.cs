using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace GenericBehaviorTree
{
    public class EnemyCombatTree : Tree
    {
        [Header("References")]
        public NavMeshAgent agent;
        public Animator animator;
        public EnemyCombatContext combatContext;
        public CombatDirector combatDirector;
        public CombatSlotManager combatSlotManager;

        [Header("Targeting")]
        public string playerTag = "Player";
        public float detectionRange = 15f;

        [Header("Movement")]
        public float slotStopDistance = 0.4f;
        public float slotRefreshCooldown = 1.0f;
        public float slotImprovementThreshold = 0.75f;

        [Header("Pressure Movement")]
        public float pressureRange = 3.0f;
        public float pressureStepDistanceMin = 0.9f;
        public float pressureStepDistanceMax = 1.6f;
        public float pressureRepathInterval = 0.15f;
        public float pressureHoldMinTime = 0.4f;
        public float pressureHoldMaxTime = 0.9f;
        public float pressurePointReachDistance = 0.22f;

        [Header("Pressure Lane Avoidance")]
        public float attackerLaneDotThreshold = 0.82f;
        public float attackerLanePushAngle = 45f;

        [Header("Pressure Point Sampling")]
        public float pressureAngleOffset = 65f;
        public int pressurePointSampleCount = 8;

        [Header("Support Separation")]
        public float supportSeparationRadius = 1.5f;
        public float supportHardSeparationRadius = 0.9f;
        public float supportSeparationWeight = 1.25f;

        [Header("Combat")]
        public float attackRange = 2f;
        public float waitNearPlayerRange = 3.0f;
        public float attackCooldown = 1.5f;
        public float recoveryDuration = 0.8f;
        public int damage = 10;
        public string attackTriggerName = "Attack";

        [Header("Recovery Movement")]
        public float recoveryRepathDistanceThreshold = 0.2f;
        public float recoveryPeelDistance = 1.75f;
        public float recoveryPeelAngleMin = 20f;
        public float recoveryPeelAngleMax = 45f;
        public float recoveryPeelReachDistance = 0.3f;
        public float recoveryFacePlayerTurnSpeed = 8f;

        protected override Node SetupTree()
        {
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (combatContext == null)
                combatContext = GetComponent<EnemyCombatContext>();

            if (combatDirector == null)
                combatDirector = FindFirstObjectByType<CombatDirector>();

            if (combatSlotManager == null)
                combatSlotManager = FindFirstObjectByType<CombatSlotManager>();

            if (combatContext != null)
            {
                if (combatContext.combatDirector == null)
                    combatContext.combatDirector = combatDirector;

                if (combatContext.combatSlotManager == null)
                    combatContext.combatSlotManager = combatSlotManager;
            }

            Node root = new Sequence(new List<Node>
            {
                new FindPlayerNode(transform, playerTag, detectionRange),

                new Selector(new List<Node>
                {
                    new RecoverNode(
                        agent,
                        combatContext,
                        slotStopDistance,
                        recoveryRepathDistanceThreshold,
                        recoveryPeelDistance,
                        recoveryPeelAngleMin,
                        recoveryPeelAngleMax,
                        recoveryPeelReachDistance,
                        recoveryFacePlayerTurnSpeed
                    ),

                    new Sequence(new List<Node>
                    {
                        new IsPlayerInRangeNode(transform, waitNearPlayerRange),
                        new ClaimAttackPermissionNode(combatContext, combatDirector),

                        new MoveToAttackRangeNode(
                            transform,
                            agent,
                            combatContext,
                            attackRange
                        ),

                        new AttackPlayerNode(
                            transform,
                            agent,
                            attackRange,
                            attackCooldown,
                            recoveryDuration,
                            damage,
                            animator,
                            combatContext,
                            combatDirector,
                            attackTriggerName)
                    }),

                    new OrbitPlayerNode(
                            transform,
                            agent,
                            combatContext,
                            pressureRange,
                            pressureStepDistanceMin,
                            pressureStepDistanceMax,
                            pressureRepathInterval,
                            pressureHoldMinTime,
                            pressureHoldMaxTime,
                            pressurePointReachDistance,
                            attackerLaneDotThreshold,
                            attackerLanePushAngle,
                            pressureAngleOffset,
                            pressurePointSampleCount,
                            supportSeparationRadius,
                            supportHardSeparationRadius,
                            supportSeparationWeight
                    ),

                    new MoveToCombatSlotNode(
                        transform,
                        agent,
                        combatContext,
                        slotStopDistance,
                        slotRefreshCooldown,
                        slotImprovementThreshold
                    )
                })
            });

            return root;
        }
    }
}
