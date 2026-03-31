using UnityEngine;
using UnityEngine.AI;

public class NPCStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform roamCenter;

    [Header("State Configs")]
    [SerializeField] private NPCIdleStateConfig idleConfig;
    [SerializeField] private NPCRoamStateConfig roamConfig;

    private NPCState currentState;

    public NPCIdleState IdleState { get; private set; }
    public NPCRoamState RoamState { get; private set; }

    public NavMeshAgent Agent => agent;
    public Animator Animator => animator;
    public Transform RoamCenter => roamCenter;

    public NPCIdleStateConfig IdleConfig => idleConfig;
    public NPCRoamStateConfig RoamConfig => roamConfig;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        IdleState = new NPCIdleState(this);
        RoamState = new NPCRoamState(this);
    }

    private void Start()
    {
        ChangeState(IdleState);
    }

    private void Update()
    {
        currentState?.Tick();
        UpdateAnimator();
    }

    public void ChangeState(NPCState newState)
    {
        if (newState == null) return;

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public Vector3 GetRoamOrigin()
    {
        return roamCenter != null ? roamCenter.position : transform.position;
    }

    public bool TryGetRandomRoamPoint(out Vector3 point)
    {
        point = Vector3.zero;

        if (roamConfig == null)
        {
            Debug.LogWarning($"{name}: Missing Roam State Config.");
            return false;
        }

        Vector3 origin = GetRoamOrigin();

        for (int i = 0; i < roamConfig.maxSampleAttempts; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * roamConfig.roamRadius;
            randomDirection.y = 0f;

            Vector3 targetPosition = origin + randomDirection;

            if (NavMesh.SamplePosition(
                targetPosition,
                out NavMeshHit hit,
                roamConfig.navMeshSampleDistance,
                NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }

        return false;
    }

    public bool HasReachedDestination()
    {
        if (agent == null) return false;
        if (agent.pathPending) return false;
        if (agent.remainingDistance > agent.stoppingDistance) return false;
        if (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f) return false;

        return true;
    }

    private void UpdateAnimator()
    {
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
    }
}