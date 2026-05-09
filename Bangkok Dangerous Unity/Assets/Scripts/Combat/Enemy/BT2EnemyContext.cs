using GenericBehaviorTree;
using UnityEngine;
using UnityEngine.AI;

public enum BT2TaskStatus
{
    Success,
    Failure,
    Running
}

[DisallowMultipleComponent]
public class BT2EnemyContext : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform player;
    [SerializeField] private CombatDirector combatDirector;
    [SerializeField] private CombatSlotManager combatSlotManager;
    [SerializeField] private EnemyCombatContext combatContext;
    [SerializeField] private CombatDirectorMember combatDirectorMember;
    [SerializeField] private CombatSlotMember combatSlotMember;

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float detectionRange = 15f;

    [Header("Slots And Ranges")]
    [SerializeField] private float slotRadius = 2.5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackRequestRange = 3f;
    [SerializeField] private float arriveDistance = 0.4f;

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

    public bool HasAttackPermission => hasAttackPermission;
    public bool AttackStarted => attackStarted;
    public bool AttackFinished => attackFinished || HasAttackTimedOut();
    public bool IsRecovering => isRecovering;
    public Transform Player => player;

    private void Awake()
    {
        CacheReferences();
        CacheAnimatorHashes();
        AttachBT2AnimationEvents();
    }

    private void Reset()
    {
        CacheReferences();
    }

    private void Update()
    {
        UpdateAnimatorMovement();
    }

    private void OnDisable()
    {
        ReleasePermissionInternal();
    }

    private void OnDestroy()
    {
        ReleasePermissionInternal();
    }

    public bool HasPlayer()
    {
        if (player != null)
            return true;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject == null)
            return false;

        player = playerObject.transform;
        return true;
    }

    public bool IsPlayerInDetectionRange()
    {
        if (!HasPlayer())
            return false;

        bool inRange = Vector3.Distance(transform.position, player.position) <= detectionRange;
        if (!inRange)
            ClearCombatState();

        return inRange;
    }

    public BT2TaskStatus RegisterWithCombatDirector()
    {
        CacheReferences();

        if (combatContext == null)
            return BT2TaskStatus.Failure;

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

        return isRegisteredWithDirector ? BT2TaskStatus.Success : BT2TaskStatus.Failure;
    }

    public BT2TaskStatus ClaimOrUpdateCombatSlot()
    {
        if (!EnsurePlayerAndAgent())
            return BT2TaskStatus.Failure;

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

            return BT2TaskStatus.Success;
        }

        AssignFallbackSlot();
        return BT2TaskStatus.Success;
    }

    public BT2TaskStatus MoveToCombatSlot()
    {
        if (!EnsurePlayerAndAgent())
            return BT2TaskStatus.Failure;

        Vector3 targetPosition = GetCurrentSlotPosition();
        bool isMoving = MoveToPoint(targetPosition, arriveDistance);
        RotateTowardsPlayer();

        if (isMoving)
            return BT2TaskStatus.Running;

        StopAgent();
        RotateTowardsPlayer();
        return BT2TaskStatus.Success;
    }

    public BT2TaskStatus PressureMove()
    {
        if (!EnsurePlayerAndAgent())
            return BT2TaskStatus.Failure;

        if (hasAttackPermission || attackStarted || isRecovering)
            return BT2TaskStatus.Failure;

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
                return BT2TaskStatus.Success;
            }

            return BT2TaskStatus.Running;
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

        return BT2TaskStatus.Running;
    }

    public BT2TaskStatus FacePlayer()
    {
        return RotateTowardsPlayer() ? BT2TaskStatus.Success : BT2TaskStatus.Failure;
    }

    public bool CanRequestAttack()
    {
        if (!EnsurePlayerAndAgent())
            return false;

        if (isRecovering || attackStarted)
            return false;

        if (hasAttackPermission)
            return true;

        if (Vector3.Distance(transform.position, player.position) > attackRequestRange)
            return false;

        if (Time.time < lastAttackTime + attackCooldown)
            return false;

        if (combatDirector == null || combatContext == null)
            return false;

        return combatDirector.CanAttack(combatContext);
    }

    public BT2TaskStatus RequestAttackPermission()
    {
        if (hasAttackPermission)
            return BT2TaskStatus.Success;

        if (combatDirector == null || combatContext == null)
            return BT2TaskStatus.Failure;

        if (!combatDirector.TryClaimAttackPermission(combatContext))
            return BT2TaskStatus.Failure;

        hasAttackPermission = true;
        combatContext.hasAttackPermission = true;
        ClearPressureState();
        return BT2TaskStatus.Success;
    }

    public BT2TaskStatus MoveIntoAttackRange()
    {
        if (!EnsurePlayerAndAgent() || !hasAttackPermission)
            return BT2TaskStatus.Failure;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= attackRange)
        {
            StopAgent();
            RotateTowardsPlayer();
            return BT2TaskStatus.Success;
        }

        MoveToPoint(player.position, attackRange * 0.9f);
        RotateTowardsPlayer();
        return BT2TaskStatus.Running;
    }

    public BT2TaskStatus StartAttack()
    {
        if (!EnsurePlayerAndAgent() || !hasAttackPermission)
            return BT2TaskStatus.Failure;

        if (attackStarted)
            return BT2TaskStatus.Success;

        StopAgent();
        RotateTowardsPlayer();

        attackStarted = true;
        attackFinished = false;
        attackStartTime = Time.time;
        lastAttackTime = Time.time;

        if (combatContext != null)
        {
            combatContext.MarkAttackPerformed();
            combatContext.BeginAttackAnimation();
        }

        if (animator != null)
            animator.SetTrigger(attackTriggerHash);

        return BT2TaskStatus.Success;
    }

    public bool IsAttackFinished()
    {
        if (!attackStarted)
            return false;

        if (combatContext != null && combatContext.attackAnimationFinished)
            attackFinished = true;

        return attackFinished || HasAttackTimedOut();
    }

    public BT2TaskStatus WaitForAttackFinished()
    {
        if (!attackStarted)
            return BT2TaskStatus.Failure;

        if (IsAttackFinished())
            return BT2TaskStatus.Success;

        StopAgent();
        RotateTowardsPlayer();
        return BT2TaskStatus.Running;
    }

    public BT2TaskStatus ReleaseAttackPermission()
    {
        ReleasePermissionInternal();
        attackStarted = false;
        attackFinished = false;

        if (combatContext != null)
            combatContext.ResetAttackAnimationState();

        return BT2TaskStatus.Success;
    }

    public BT2TaskStatus Recover()
    {
        if (!EnsurePlayerAndAgent())
            return BT2TaskStatus.Failure;

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
            return BT2TaskStatus.Running;

        StopAgent();
        isRecovering = false;
        hasRecoveryPoint = false;
        ClearPressureState();

        if (combatContext != null)
            combatContext.StartRecovery(0f);

        return BT2TaskStatus.Success;
    }

    public BT2TaskStatus Idle()
    {
        StopAgent();
        ClearPressureState();
        return BT2TaskStatus.Success;
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

    public void AttackAnimationFinished()
    {
        NotifyAttackAnimationFinished();
    }

    public BT2TaskStatus UpdateAnimatorMovementTask()
    {
        UpdateAnimatorMovement();
        return BT2TaskStatus.Success;
    }

    private void CacheReferences()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent != null)
            agent.updateRotation = false;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (combatContext == null)
            combatContext = GetComponent<EnemyCombatContext>();

        if (combatDirectorMember == null)
            combatDirectorMember = GetComponent<CombatDirectorMember>();

        if (combatSlotMember == null)
            combatSlotMember = GetComponent<CombatSlotMember>();

        if (combatDirector == null)
            combatDirector = FindFirstObjectByType<CombatDirector>();

        if (combatSlotManager == null)
            combatSlotManager = FindFirstObjectByType<CombatSlotManager>();

        if (player == null)
            HasPlayer();

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

    private void AttachBT2AnimationEvents()
    {
        if (animator == null)
            return;

        if (animator.GetComponent<BT2AttackAnimationEvents>() == null)
            animator.gameObject.AddComponent<BT2AttackAnimationEvents>();
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
        if (!HasPlayer())
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

        BT2EnemyContext[] enemies = FindObjectsByType<BT2EnemyContext>(FindObjectsSortMode.None);
        int index = 0;
        int count = Mathf.Max(1, enemies.Length);

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == this)
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

        BT2EnemyContext[] enemies = FindObjectsByType<BT2EnemyContext>(FindObjectsSortMode.None);
        foreach (BT2EnemyContext other in enemies)
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

    private bool HasAttackTimedOut()
    {
        return attackStarted && Time.time >= attackStartTime + attackTimeout;
    }

    private void UpdateAnimatorMovement()
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
