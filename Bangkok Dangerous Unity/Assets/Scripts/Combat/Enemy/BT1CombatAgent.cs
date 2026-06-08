using System.Reflection;
using AleM.BehaviourTrees;
using GenericBehaviorTree;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class BT1CombatAgent : BTAgent
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform player;
    [SerializeField] private CombatDirector combatDirector;
    [SerializeField] private CombatSlotManager combatSlotManager;
    [SerializeField] private EnemyCombatContext combatContext;

    [Header("Ranges")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float waitNearPlayerRange = 3f;
    [SerializeField] private float slotRadius = 2.5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float slotArriveDistance = 0.4f;

    [Header("Pressure Movement")]
    [SerializeField] private float pressureMoveRadius = 3f;
    [SerializeField] private float pressureMoveDistance = 1.25f;
    [SerializeField] private float pressureRepathInterval = 0.25f;
    [SerializeField] private float pressurePointReachDistance = 0.25f;
    [SerializeField] private float pressureHoldMinTime = 0.35f;
    [SerializeField] private float pressureHoldMaxTime = 0.85f;
    [SerializeField] private float crowdingRadius = 1.1f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackTimeout = 2.5f;
    [SerializeField] private string attackTriggerName = "Attack";

    [Header("Recovery")]
    [SerializeField] private float recoveryDistance = 1.75f;
    [SerializeField] private float recoveryDuration = 0.8f;
    [SerializeField] private float recoveryReachDistance = 0.3f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private float animatorDampTime = 0.1f;
    [SerializeField] private float animatorDeadZone = 0.05f;

    private static readonly FieldInfo TreeContainerField =
        typeof(BTAgent).GetField("treeContainer", BindingFlags.Instance | BindingFlags.NonPublic);

    private int speedHash;
    private int moveXHash;
    private int moveYHash;
    private int attackTriggerHash;

    private bool isRegisteredWithDirector;
    private bool hasAttackPermission;
    private bool attackStarted;
    private bool attackFinished;
    private bool isRecovering;

    private float lastAttackTime = -999f;
    private float attackStartTime = -999f;
    private float recoveryStartTime = -999f;
    private float lastPressureRepathTime = -999f;
    private float holdPressureUntilTime;

    private bool hasPressurePoint;
    private bool isHoldingPressurePoint;
    private Vector3 pressurePoint;

    private bool hasRecoveryPoint;
    private Vector3 recoveryPoint;

    protected override void Start()
    {
        CacheReferences();
        CacheAnimatorHashes();

        if (!HasTreeContainerAssigned())
        {
            Debug.LogWarning($"{nameof(BT1CombatAgent)} on {name} has no BT1 tree container assigned yet. Create the BT1 graph and Set Container for Agent before playtesting.", this);
            return;
        }

        base.Start();
    }

    private void Update()
    {
        UpdateAnimatorParameters();
    }

    private void OnDisable()
    {
        ReleasePermissionInternal();
    }

    private void OnDestroy()
    {
        ReleasePermissionInternal();
    }

    public NodeBT.Status HasPlayer()
    {
        if (player != null)
            return NodeBT.Status.SUCCESS;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject == null)
            return NodeBT.Status.FAILURE;

        player = playerObject.transform;
        return NodeBT.Status.SUCCESS;
    }

    public NodeBT.Status IsPlayerInDetectionRange()
    {
        if (player == null && HasPlayer() == NodeBT.Status.FAILURE)
            return NodeBT.Status.FAILURE;

        if (Vector3.Distance(transform.position, player.position) <= detectionRange)
            return NodeBT.Status.SUCCESS;

        ClearCombatState();
        return NodeBT.Status.FAILURE;
    }

    public NodeBT.Status RegisterWithCombatDirector()
    {
        CacheReferences();

        if (combatContext == null)
            return NodeBT.Status.FAILURE;

        if (combatDirector == null)
            combatDirector = FindFirstObjectByType<CombatDirector>();

        if (combatSlotManager == null)
            combatSlotManager = FindFirstObjectByType<CombatSlotManager>();

        if (combatDirector != null)
        {
            combatDirector.RegisterEnemy(combatContext);
            combatContext.combatDirector = combatDirector;
            isRegisteredWithDirector = true;
        }

        if (combatSlotManager != null)
            combatContext.combatSlotManager = combatSlotManager;

        return isRegisteredWithDirector ? NodeBT.Status.SUCCESS : NodeBT.Status.FAILURE;
    }

    public NodeBT.Status ClaimOrUpdateCombatSlot()
    {
        if (!EnsurePlayerAndAgent())
            return NodeBT.Status.FAILURE;

        if (combatContext != null && combatSlotManager != null)
        {
            combatContext.combatSlotManager = combatSlotManager;

            if (!combatContext.hasAssignedSlot)
            {
                if (!combatSlotManager.TryAssignSlot(combatContext))
                    AssignFallbackSlot();
            }
            else
            {
                combatSlotManager.UpdateAssignedSlotPosition(combatContext);
            }

            return NodeBT.Status.SUCCESS;
        }

        AssignFallbackSlot();
        return NodeBT.Status.SUCCESS;
    }

    public NodeBT.Status MoveToCombatSlot()
    {
        if (!EnsurePlayerAndAgent())
            return NodeBT.Status.FAILURE;

        Vector3 targetPosition = GetCurrentSlotPosition();
        bool isMoving = MoveToPoint(targetPosition, slotArriveDistance);
        RotateTowardsPlayer();

        if (isMoving)
            return NodeBT.Status.RUNNING;

        StopAgent();
        RotateTowardsPlayer();
        return NodeBT.Status.SUCCESS;
    }

    public NodeBT.Status PressureMove()
    {
        if (!EnsurePlayerAndAgent())
            return NodeBT.Status.FAILURE;

        if (hasAttackPermission || attackStarted || isRecovering)
            return NodeBT.Status.FAILURE;

        if (!hasPressurePoint)
        {
            pressurePoint = BuildPressurePoint();
            hasPressurePoint = true;
            isHoldingPressurePoint = false;
        }

        if (isHoldingPressurePoint)
        {
            StopAgent();
            RotateTowardsPlayer();

            if (Time.time >= holdPressureUntilTime)
            {
                ClearPressureState();
                return NodeBT.Status.SUCCESS;
            }

            return NodeBT.Status.RUNNING;
        }

        if (Time.time >= lastPressureRepathTime + pressureRepathInterval)
        {
            MoveToPoint(pressurePoint, pressurePointReachDistance);
            lastPressureRepathTime = Time.time;
        }

        RotateTowardsPlayer();

        bool reachedPoint = Vector3.Distance(transform.position, pressurePoint) <= pressurePointReachDistance + 0.05f ||
            (!agent.pathPending && agent.remainingDistance <= pressurePointReachDistance + 0.05f);

        if (reachedPoint)
        {
            StopAgent();
            isHoldingPressurePoint = true;
            holdPressureUntilTime = Time.time + Random.Range(pressureHoldMinTime, pressureHoldMaxTime);
        }

        return NodeBT.Status.RUNNING;
    }

    public NodeBT.Status FacePlayer()
    {
        return RotateTowardsPlayer() ? NodeBT.Status.SUCCESS : NodeBT.Status.FAILURE;
    }

    public NodeBT.Status CanRequestAttack()
    {
        if (!EnsurePlayerAndAgent())
            return NodeBT.Status.FAILURE;

        if (isRecovering || attackStarted)
            return NodeBT.Status.FAILURE;

        if (hasAttackPermission)
            return NodeBT.Status.SUCCESS;

        if (Vector3.Distance(transform.position, player.position) > waitNearPlayerRange)
            return NodeBT.Status.FAILURE;

        if (Time.time < lastAttackTime + attackCooldown)
            return NodeBT.Status.FAILURE;

        if (combatDirector == null || combatContext == null)
            return NodeBT.Status.FAILURE;

        return combatDirector.CanAttack(combatContext) ? NodeBT.Status.SUCCESS : NodeBT.Status.FAILURE;
    }

    public NodeBT.Status RequestAttackPermission()
    {
        if (hasAttackPermission)
            return NodeBT.Status.SUCCESS;

        if (combatDirector == null || combatContext == null)
            return NodeBT.Status.FAILURE;

        if (!combatDirector.TryClaimAttackPermission(combatContext))
            return NodeBT.Status.FAILURE;

        hasAttackPermission = true;
        combatContext.hasAttackPermission = true;
        ClearPressureState();
        return NodeBT.Status.SUCCESS;
    }

    public NodeBT.Status MoveIntoAttackRange()
    {
        if (!EnsurePlayerAndAgent() || !hasAttackPermission)
            return NodeBT.Status.FAILURE;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= attackRange)
        {
            StopAgent();
            return NodeBT.Status.SUCCESS;
        }

        MoveToPoint(player.position, attackRange * 0.9f);
        RotateTowardsPlayer();
        return NodeBT.Status.RUNNING;
    }

    public NodeBT.Status StartAttack()
    {
        if (!EnsurePlayerAndAgent() || !hasAttackPermission)
            return NodeBT.Status.FAILURE;

        if (attackStarted)
            return NodeBT.Status.SUCCESS;

        StopAgent();
        RotateTowardsPlayer();

        attackStarted = true;
        attackFinished = false;
        attackStartTime = Time.time;
        lastAttackTime = Time.time;

        if (GameEventsManager.instance != null && GameEventsManager.instance.combatEvents != null)
        {
            Vector3 fightOrigin = player != null ? player.position : transform.position;
            GameEventsManager.instance.combatEvents.FightStarted(fightOrigin);
        }

        if (combatContext != null)
        {
            combatContext.MarkAttackPerformed();
            combatContext.BeginAttackAnimation();
        }

        if (animator != null)
            animator.SetTrigger(attackTriggerHash);

        return NodeBT.Status.SUCCESS;
    }

    public NodeBT.Status IsAttackFinished()
    {
        if (!attackStarted)
            return NodeBT.Status.FAILURE;

        if (combatContext != null && combatContext.attackAnimationFinished)
            attackFinished = true;

        if (attackFinished || Time.time >= attackStartTime + attackTimeout)
            return NodeBT.Status.SUCCESS;

        StopAgent();
        RotateTowardsPlayer();
        return NodeBT.Status.RUNNING;
    }

    public NodeBT.Status Recover()
    {
        if (!EnsurePlayerAndAgent())
            return NodeBT.Status.FAILURE;

        if (!isRecovering)
        {
            isRecovering = true;
            recoveryStartTime = Time.time;
            hasRecoveryPoint = false;
        }

        if (!hasRecoveryPoint)
        {
            recoveryPoint = BuildRecoveryPoint();
            hasRecoveryPoint = true;
        }

        bool stillMoving = MoveToPoint(recoveryPoint, recoveryReachDistance);
        RotateTowardsPlayer();
        bool durationElapsed = Time.time >= recoveryStartTime + recoveryDuration;
        bool reachedPoint = !stillMoving || Vector3.Distance(transform.position, recoveryPoint) <= recoveryReachDistance + 0.05f;

        if (!durationElapsed || !reachedPoint)
            return NodeBT.Status.RUNNING;

        StopAgent();
        isRecovering = false;
        hasRecoveryPoint = false;
        ClearPressureState();

        if (combatContext != null)
            combatContext.StartRecovery(0f);

        return NodeBT.Status.SUCCESS;
    }

    public NodeBT.Status ReleaseAttackPermission()
    {
        ReleasePermissionInternal();
        attackStarted = false;
        attackFinished = false;

        if (combatContext != null)
            combatContext.ResetAttackAnimationState();

        return NodeBT.Status.SUCCESS;
    }

    public NodeBT.Status Idle()
    {
        StopAgent();
        ClearPressureState();
        return NodeBT.Status.SUCCESS;
    }

    public void NotifyAttackAnimationFinished()
    {
        attackFinished = true;

        if (combatContext != null)
            combatContext.FinishAttackAnimation();
    }

    public void OnAttackAnimationFinished()
    {
        NotifyAttackAnimationFinished();
    }

    private void CacheReferences()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            // BT1 combat should face the player while pathing, not the agent's steering direction.
            agent.updateRotation = false;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (combatContext == null)
            combatContext = GetComponent<EnemyCombatContext>();

        if (combatDirector == null)
            combatDirector = FindFirstObjectByType<CombatDirector>();

        if (combatSlotManager == null)
            combatSlotManager = FindFirstObjectByType<CombatSlotManager>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (combatContext != null)
        {
            if (combatDirector != null)
                combatContext.combatDirector = combatDirector;

            if (combatSlotManager != null)
                combatContext.combatSlotManager = combatSlotManager;
        }
    }

    private void CacheAnimatorHashes()
    {
        speedHash = Animator.StringToHash(speedParam);
        moveXHash = Animator.StringToHash(moveXParam);
        moveYHash = Animator.StringToHash(moveYParam);
        attackTriggerHash = Animator.StringToHash(attackTriggerName);
    }

    private bool HasTreeContainerAssigned()
    {
        return TreeContainerField != null && TreeContainerField.GetValue(this) != null;
    }

    private bool EnsurePlayerAndAgent()
    {
        CacheReferences();

        if (player == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        return Vector3.Distance(transform.position, player.position) <= detectionRange;
    }

    private bool MoveToPoint(Vector3 position, float stoppingDistance)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            hit.position = position;

        agent.isStopped = false;
        agent.stoppingDistance = Mathf.Max(0f, stoppingDistance);

        if (!agent.hasPath || Vector3.Distance(agent.destination, hit.position) > 0.2f)
            agent.SetDestination(hit.position);

        if (agent.pathPending)
            return true;

        return agent.remainingDistance > agent.stoppingDistance + 0.05f;
    }

    private bool RotateTowardsPlayer()
    {
        if (player == null && HasPlayer() == NodeBT.Status.FAILURE)
            return false;

        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude <= 0.001f)
            return true;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        return true;
    }

    private void StopAgent()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (agent.hasPath)
            agent.ResetPath();

        agent.isStopped = true;
    }

    private Vector3 GetCurrentSlotPosition()
    {
        if (combatContext != null && combatContext.hasAssignedSlot)
            return combatContext.assignedSlotPosition;

        AssignFallbackSlot();

        if (combatContext != null && combatContext.hasAssignedSlot)
            return combatContext.assignedSlotPosition;

        return player != null ? player.position + Vector3.back * slotRadius : transform.position;
    }

    private void AssignFallbackSlot()
    {
        if (player == null)
            return;

        BT1CombatAgent[] agents = FindObjectsByType<BT1CombatAgent>(FindObjectsSortMode.None);
        int index = 0;
        int count = Mathf.Max(1, agents.Length);

        for (int i = 0; i < agents.Length; i++)
        {
            if (agents[i] == this)
            {
                index = i;
                break;
            }
        }

        float angle = (360f / count) * index * Mathf.Deg2Rad;
        Vector3 slotPosition = player.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * slotRadius;

        if (combatContext != null)
            combatContext.SetAssignedSlot(index, slotPosition);
    }

    private Vector3 BuildPressurePoint()
    {
        Vector3 fromPlayer = transform.position - player.position;
        fromPlayer.y = 0f;

        if (fromPlayer.sqrMagnitude < 0.01f)
            fromPlayer = transform.forward;

        Vector3 bestPoint = transform.position;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < 8; i++)
        {
            float angle = Random.Range(-70f, 70f);
            Vector3 radial = Quaternion.AngleAxis(angle, Vector3.up) * fromPlayer.normalized;
            Vector3 desired = player.position + radial * Random.Range(pressureMoveRadius - 0.75f, pressureMoveRadius + 0.75f);

            Vector3 offset = desired - transform.position;
            offset.y = 0f;
            if (offset.magnitude > pressureMoveDistance)
                desired = transform.position + offset.normalized * pressureMoveDistance;

            if (!NavMesh.SamplePosition(desired, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                continue;

            float score = ScorePressurePoint(hit.position);
            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = hit.position;
            }
        }

        return bestPoint;
    }

    private float ScorePressurePoint(Vector3 point)
    {
        float score = -Vector3.Distance(transform.position, point) * 0.15f;

        BT1CombatAgent[] agents = FindObjectsByType<BT1CombatAgent>(FindObjectsSortMode.None);
        foreach (BT1CombatAgent other in agents)
        {
            if (other == null || other == this || !other.isActiveAndEnabled)
                continue;

            float distance = Vector3.Distance(point, other.transform.position);
            if (distance < crowdingRadius * 0.65f)
                return float.NegativeInfinity;

            if (distance < crowdingRadius)
                score -= 1f - distance / crowdingRadius;
        }

        return score;
    }

    private Vector3 BuildRecoveryPoint()
    {
        Vector3 fromPlayer = transform.position - player.position;
        fromPlayer.y = 0f;

        if (fromPlayer.sqrMagnitude < 0.01f)
            fromPlayer = -transform.forward;

        float angle = Random.Range(20f, 45f) * (Random.value < 0.5f ? -1f : 1f);
        Vector3 recoveryDirection = Quaternion.AngleAxis(angle, Vector3.up) * fromPlayer.normalized;
        Vector3 desired = transform.position + recoveryDirection * recoveryDistance;

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            return hit.position;

        desired = transform.position + fromPlayer.normalized * recoveryDistance;
        if (NavMesh.SamplePosition(desired, out hit, 1.5f, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    private void ReleasePermissionInternal()
    {
        if (combatContext != null)
            combatContext.hasAttackPermission = false;

        if (combatDirector != null && combatContext != null && hasAttackPermission)
        {
            if (attackStarted)
                combatDirector.NotifyAttackPerformed(combatContext);

            combatDirector.ReleaseAttackPermission(combatContext);
        }

        hasAttackPermission = false;
    }

    private void ClearCombatState()
    {
        ReleasePermissionInternal();
        attackStarted = false;
        attackFinished = false;
        isRecovering = false;
        hasRecoveryPoint = false;
        ClearPressureState();
    }

    private void ClearPressureState()
    {
        hasPressurePoint = false;
        isHoldingPressurePoint = false;
    }

    private void UpdateAnimatorParameters()
    {
        if (animator == null || agent == null || !agent.enabled)
            return;

        float speed = agent.velocity.magnitude;
        if (speed < animatorDeadZone)
            speed = 0f;

        float normalizedSpeed = agent.speed > 0.001f ? speed / agent.speed : 0f;
        animator.SetFloat(speedHash, normalizedSpeed, animatorDampTime, Time.deltaTime);

        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);
        float moveX = Mathf.Abs(localVelocity.x) < animatorDeadZone ? 0f : localVelocity.x;
        float moveY = Mathf.Abs(localVelocity.z) < animatorDeadZone ? 0f : localVelocity.z;

        animator.SetFloat(moveXHash, moveX, animatorDampTime, Time.deltaTime);
        animator.SetFloat(moveYHash, moveY, animatorDampTime, Time.deltaTime);
    }
}
 