using UnityEngine;
using UnityEngine.AI;

namespace GenericBehaviorTree
{
    public class OrbitPlayerNode : Node
    {
        private Transform self;
        private NavMeshAgent agent;
        private EnemyCombatContext combatContext;

        private float minPressureRange;
        private float maxPressureRange;
        private float minRepositionStepDistance;
        private float maxRepositionStepDistance;
        private float repathInterval;
        private float holdMinTime;
        private float holdMaxTime;
        private float pointReachDistance;

        private float attackerLaneDotThreshold;
        private float attackerLanePushAngle;

        private float maxPressureAngleOffset;
        private int pressurePointSampleCount;

        private float supportSeparationRadius;
        private float supportHardSeparationRadius;
        private float supportSeparationWeight;

        private float lastRepathTime = -999f;

        private bool hasPressurePoint = false;
        private Vector3 currentPressurePoint;
        private float holdUntilTime = 0f;
        private bool isHoldingPosition = false;

        public OrbitPlayerNode(
            Transform self,
            NavMeshAgent agent,
            EnemyCombatContext combatContext,
            float pressureCenterRange,
            float minStepDistance = 0.9f,
            float maxStepDistance = 1.8f,
            float repathInterval = 0.15f,
            float holdMinTime = 0.35f,
            float holdMaxTime = 0.85f,
            float pointReachDistance = 0.22f,
            float attackerLaneDotThreshold = 0.82f,
            float attackerLanePushAngle = 45f,
            float maxPressureAngleOffset = 65f,
            int pressurePointSampleCount = 8,
            float supportSeparationRadius = 1.5f,
            float supportHardSeparationRadius = 0.9f,
            float supportSeparationWeight = 1.25f)
        {
            this.self = self;
            this.agent = agent;
            this.combatContext = combatContext;

            minPressureRange = Mathf.Max(0.5f, pressureCenterRange - 0.9f);
            maxPressureRange = pressureCenterRange + 0.9f;

            minRepositionStepDistance = Mathf.Max(0.2f, minStepDistance);
            maxRepositionStepDistance = Mathf.Max(minRepositionStepDistance, maxStepDistance);

            this.repathInterval = repathInterval;
            this.holdMinTime = holdMinTime;
            this.holdMaxTime = holdMaxTime;
            this.pointReachDistance = pointReachDistance;

            this.attackerLaneDotThreshold = attackerLaneDotThreshold;
            this.attackerLanePushAngle = attackerLanePushAngle;

            this.maxPressureAngleOffset = Mathf.Clamp(maxPressureAngleOffset, 0f, 180f);
            this.pressurePointSampleCount = Mathf.Max(1, pressurePointSampleCount);

            this.supportSeparationRadius = Mathf.Max(0.1f, supportSeparationRadius);
            this.supportHardSeparationRadius = Mathf.Clamp(supportHardSeparationRadius, 0.05f, this.supportSeparationRadius);
            this.supportSeparationWeight = Mathf.Max(0f, supportSeparationWeight);
        }

        public override NodeState Evaluate()
        {
            object targetObject = GetData("target");

            if (targetObject == null)
            {
                ClearPressureState();
                state = NodeState.FAILURE;
                return state;
            }

            Transform target = (Transform)targetObject;

            if (target == null || combatContext == null || !NavMeshAgentNavigation.IsReady(agent))
            {
                ClearPressureState();
                state = NodeState.FAILURE;
                return state;
            }

            if (combatContext.IsRecovering)
            {
                ClearPressureState();
                state = NodeState.FAILURE;
                return state;
            }

            if (combatContext.hasAttackPermission)
            {
                ClearPressureState();
                state = NodeState.FAILURE;
                return state;
            }

            float distanceToPlayer = Vector3.Distance(self.position, target.position);

            if (distanceToPlayer > maxPressureRange + 0.75f)
            {
                ClearPressureState();
                state = NodeState.FAILURE;
                return state;
            }

            if (!hasPressurePoint)
            {
                currentPressurePoint = BuildPressurePoint(target);
                hasPressurePoint = true;
                isHoldingPosition = false;
            }

            if (isHoldingPosition)
            {
                NavMeshAgentNavigation.Stop(agent);

                FaceTarget(target);

                if (Time.time >= holdUntilTime)
                {
                    currentPressurePoint = BuildPressurePoint(target);
                    isHoldingPosition = false;
                }

                state = NodeState.RUNNING;
                return state;
            }

            if (Time.time >= lastRepathTime + repathInterval)
            {
                NavMeshAgentNavigation.MoveTo(agent, currentPressurePoint, pointReachDistance, 0f);
                lastRepathTime = Time.time;
            }

            FaceTarget(target);

            float distanceToPoint = Vector3.Distance(self.position, currentPressurePoint);

            bool reachedPoint =
                distanceToPoint <= pointReachDistance + 0.05f ||
                (!agent.pathPending && agent.remainingDistance <= pointReachDistance + 0.05f);

            if (reachedPoint)
            {
                NavMeshAgentNavigation.Stop(agent);
                isHoldingPosition = true;
                holdUntilTime = Time.time + Random.Range(holdMinTime, holdMaxTime);
            }

            state = NodeState.RUNNING;
            return state;
        }

        private Vector3 BuildPressurePoint(Transform target)
        {
            Vector3 toSelf = self.position - target.position;
            toSelf.y = 0f;

            if (toSelf.sqrMagnitude < 0.01f)
                toSelf = self.forward;

            Vector3 baseRadial = toSelf.normalized;

            Vector3 bestPoint = self.position;
            float bestScore = float.NegativeInfinity;
            bool foundValidPoint = false;

            for (int i = 0; i < pressurePointSampleCount; i++)
            {
                float chosenAngle = Random.Range(-maxPressureAngleOffset, maxPressureAngleOffset);
                float chosenRadius = Random.Range(minPressureRange, maxPressureRange);
                float chosenMaxStep = Random.Range(minRepositionStepDistance, maxRepositionStepDistance);

                Vector3 candidateRadial = (Quaternion.AngleAxis(chosenAngle, Vector3.up) * baseRadial).normalized;

                if (combatContext.combatDirector != null)
                {
                    EnemyCombatContext activeAttacker = combatContext.combatDirector.GetPrimaryActiveAttacker();

                    if (activeAttacker != null && activeAttacker != combatContext)
                    {
                        Vector3 attackerOffset = activeAttacker.transform.position - target.position;
                        attackerOffset.y = 0f;

                        if (attackerOffset.sqrMagnitude > 0.01f)
                        {
                            Vector3 attackerRadial = attackerOffset.normalized;
                            float laneAlignment = Vector3.Dot(candidateRadial, attackerRadial);

                            if (laneAlignment > attackerLaneDotThreshold)
                            {
                                Vector3 attackerTangent = Vector3.Cross(Vector3.up, attackerRadial);
                                float side = Vector3.Dot(candidateRadial, attackerTangent);
                                float rotationSign = side >= 0f ? 1f : -1f;

                                Quaternion pushRotation = Quaternion.AngleAxis(attackerLanePushAngle * rotationSign, Vector3.up);
                                candidateRadial = (pushRotation * attackerRadial).normalized;
                            }
                        }
                    }
                }

                Vector3 desiredPosition = target.position + candidateRadial * chosenRadius;

                Vector3 moveOffset = desiredPosition - self.position;
                moveOffset.y = 0f;

                float moveDistance = moveOffset.magnitude;

                if (moveDistance > chosenMaxStep && moveDistance > 0.001f)
                {
                    desiredPosition = self.position + moveOffset.normalized * chosenMaxStep;
                }

                NavMeshHit hit;
                if (!NavMesh.SamplePosition(desiredPosition, out hit, 1.5f, NavMesh.AllAreas))
                    continue;

                Vector3 candidatePoint = hit.position;
                float score = ScoreCandidatePoint(candidatePoint, target);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = candidatePoint;
                    foundValidPoint = true;
                }
            }

            if (foundValidPoint)
                return bestPoint;

            return self.position;
        }

        private float ScoreCandidatePoint(Vector3 candidatePoint, Transform target)
        {
            float score = 0f;

            float candidateRadius = Vector3.Distance(candidatePoint, target.position);
            float pressureMidRadius = (minPressureRange + maxPressureRange) * 0.5f;
            float radiusOffset = Mathf.Abs(candidateRadius - pressureMidRadius);
            score -= radiusOffset * 0.35f;

            float moveDistance = Vector3.Distance(self.position, candidatePoint);
            score -= moveDistance * 0.15f;

            EnemyCombatContext[] allEnemies = Object.FindObjectsByType<EnemyCombatContext>(FindObjectsSortMode.None);

            for (int i = 0; i < allEnemies.Length; i++)
            {
                EnemyCombatContext other = allEnemies[i];

                if (other == null || other == combatContext)
                    continue;

                if (!other.gameObject.activeInHierarchy)
                    continue;

                Vector3 otherPosition = other.transform.position;
                otherPosition.y = candidatePoint.y;

                float distanceToOther = Vector3.Distance(candidatePoint, otherPosition);

                if (!other.hasAttackPermission && !other.IsRecovering && distanceToOther < supportHardSeparationRadius)
                    return float.NegativeInfinity;

                if (!other.hasAttackPermission && !other.IsRecovering && distanceToOther < supportSeparationRadius)
                {
                    float closeness01 = 1f - Mathf.Clamp01(distanceToOther / supportSeparationRadius);
                    score -= closeness01 * supportSeparationWeight;
                }
            }

            return score;
        }

        private void FaceTarget(Transform target)
        {
            Vector3 lookPos = target.position - self.position;
            lookPos.y = 0f;

            if (lookPos.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookPos);
                self.rotation = Quaternion.Slerp(self.rotation, targetRotation, Time.deltaTime * 8f);
            }
        }

        private void ClearPressureState()
        {
            hasPressurePoint = false;
            isHoldingPosition = false;
        }
    }
}