using UnityEngine;
using UnityEngine.AI;

namespace GenericBehaviorTree
{
    public class RecoverNode : Node
    {
        private NavMeshAgent _agent;
        private EnemyCombatContext _combatContext;

        private float _slotStopDistance;
        private float _repathDistanceThreshold;

        private float _peelDistance;
        private float _peelAngleMin;
        private float _peelAngleMax;
        private float _peelReachDistance;
        private float _facePlayerTurnSpeed;

        private bool _hasPeelPoint = false;
        private bool _reachedPeelPoint = false;
        private Vector3 _peelPoint;

        public RecoverNode(
            NavMeshAgent agent,
            EnemyCombatContext combatContext,
            float slotStopDistance = 0.4f,
            float repathDistanceThreshold = 0.2f,
            float peelDistance = 1.75f,
            float peelAngleMin = 20f,
            float peelAngleMax = 45f,
            float peelReachDistance = 0.3f,
            float facePlayerTurnSpeed = 8f)
        {
            _agent = agent;
            _combatContext = combatContext;
            _slotStopDistance = slotStopDistance;
            _repathDistanceThreshold = repathDistanceThreshold;

            _peelDistance = peelDistance;
            _peelAngleMin = peelAngleMin;
            _peelAngleMax = peelAngleMax;
            _peelReachDistance = peelReachDistance;
            _facePlayerTurnSpeed = facePlayerTurnSpeed;
        }

        public override NodeState Evaluate()
        {
            if (_combatContext == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            if (!_combatContext.IsRecovering)
            {
                ClearRecoveryState();
                state = NodeState.FAILURE;
                return state;
            }

            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
            {
                state = NodeState.RUNNING;
                return state;
            }

            Transform target = GetData("target") as Transform;
            if (target == null)
            {
                HandleFallbackSlotRecovery();
                state = NodeState.RUNNING;
                return state;
            }

            FaceTarget(target);

            if (!_hasPeelPoint)
            {
                _peelPoint = BuildPeelPoint(target);
                _hasPeelPoint = true;
                _reachedPeelPoint = false;
            }

            if (!_reachedPeelPoint)
            {
                float peelDistanceToPoint = Vector3.Distance(_agent.transform.position, _peelPoint);

                if (peelDistanceToPoint > _peelReachDistance)
                {
                    _agent.isStopped = false;
                    _agent.stoppingDistance = _peelReachDistance;

                    if (!_agent.hasPath || Vector3.Distance(_agent.destination, _peelPoint) > _repathDistanceThreshold)
                    {
                        _agent.SetDestination(_peelPoint);
                    }
                }
                else
                {
                    if (_agent.hasPath)
                        _agent.ResetPath();

                    _agent.isStopped = true;
                    _reachedPeelPoint = true;
                }

                state = NodeState.RUNNING;
                return state;
            }

            HandleFallbackSlotRecovery();

            state = NodeState.RUNNING;
            return state;
        }

        private void HandleFallbackSlotRecovery()
        {
            if (_combatContext.hasAssignedSlot)
            {
                if (_combatContext.combatSlotManager != null)
                {
                    _combatContext.combatSlotManager.UpdateAssignedSlotPosition(_combatContext);
                }

                Vector3 slotPos = _combatContext.assignedSlotPosition;
                float distance = Vector3.Distance(_agent.transform.position, slotPos);

                if (distance > _slotStopDistance)
                {
                    _agent.isStopped = false;
                    _agent.stoppingDistance = _slotStopDistance;

                    if (!_agent.hasPath || Vector3.Distance(_agent.destination, slotPos) > _repathDistanceThreshold)
                    {
                        _agent.SetDestination(slotPos);
                    }
                }
                else
                {
                    if (_agent.hasPath)
                        _agent.ResetPath();

                    _agent.isStopped = true;
                }
            }
            else
            {
                if (_agent.hasPath)
                    _agent.ResetPath();

                _agent.isStopped = true;
            }
        }

        private Vector3 BuildPeelPoint(Transform target)
        {
            Vector3 selfPos = _agent.transform.position;
            Vector3 fromPlayer = selfPos - target.position;
            fromPlayer.y = 0f;

            if (fromPlayer.sqrMagnitude < 0.01f)
            {
                fromPlayer = _agent.transform.forward;
                fromPlayer.y = 0f;
            }

            Vector3 awayDir = fromPlayer.normalized;

            float angle = Random.Range(_peelAngleMin, _peelAngleMax);
            float sign = Random.value < 0.5f ? -1f : 1f;
            Quaternion rotation = Quaternion.AngleAxis(angle * sign, Vector3.up);

            Vector3 peelDir = (rotation * awayDir).normalized;
            Vector3 desiredPoint = selfPos + peelDir * _peelDistance;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(desiredPoint, out hit, 1.5f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            desiredPoint = selfPos + awayDir * _peelDistance;

            if (NavMesh.SamplePosition(desiredPoint, out hit, 1.5f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return selfPos;
        }

        private void FaceTarget(Transform target)
        {
            Vector3 toTarget = target.position - _agent.transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(toTarget.normalized);
                _agent.transform.rotation = Quaternion.Slerp(
                    _agent.transform.rotation,
                    lookRotation,
                    Time.deltaTime * _facePlayerTurnSpeed
                );
            }
        }

        private void ClearRecoveryState()
        {
            _hasPeelPoint = false;
            _reachedPeelPoint = false;
        }
    }
}