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
    [SerializeField] private NPCSmokingStateConfig smokingConfig;
    [SerializeField] private NPCTalkingStateConfig talkingConfig;

    private NPCState currentState;
    private int destinationsVisitedSinceLastStationary = 0;
    public string CurrentStateName => currentState != null ? currentState.GetType().Name : "None";

    public NPCIdleState IdleState { get; private set; }
    public NPCRoamState RoamState { get; private set; }
    public NPCSmokingState SmokingState { get; private set; }
    public NPCTalkingState TalkingState { get; private set; }

    public NavMeshAgent Agent => agent;
    public Animator Animator => animator;
    public Transform RoamCenter => roamCenter;

    public NPCIdleStateConfig IdleConfig => idleConfig;
    public NPCRoamStateConfig RoamConfig => roamConfig;
    public NPCSmokingStateConfig SmokingConfig => smokingConfig;
    public NPCTalkingStateConfig TalkingConfig => talkingConfig;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        IdleState = new NPCIdleState(this);
        RoamState = new NPCRoamState(this);
        SmokingState = new NPCSmokingState(this);
        TalkingState = new NPCTalkingState(this);
    }

    private void Start()
    {
        ChangeState(RoamState);
    }

    private void Update()
    {
        currentState?.Tick();
        UpdateAnimator();
    }

    public void ChangeState(NPCState newState)
    {
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

        Vector3 origin = GetRoamOrigin();

        for (int i = 0; i < roamConfig.maxSampleAttempts; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * roamConfig.roamRadius;
            Vector3 candidate = origin + new Vector3(random2D.x, 0f, random2D.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, roamConfig.navMeshSampleDistance, NavMesh.AllAreas))
            {
                if (NavMesh.FindClosestEdge(hit.position, out NavMeshHit edgeHit, NavMesh.AllAreas))
                {
                    if (edgeHit.distance >= roamConfig.MinEdgeDistance)
                    {
                        point = hit.position;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public bool HasReachedDestination()
    {
        if (agent == null)
            return false;

        if (agent.pathPending)
            return false;

        if (agent.remainingDistance > agent.stoppingDistance)
            return false;

        if (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f)
            return false;

        return true;
    }

    public void HandleRoamDestinationReached()
    {
        destinationsVisitedSinceLastStationary++;

        if (!CanEnterStationaryState())
        {
            ChangeState(RoamState);
            return;
        }

        float chance = GetCurrentStationaryChance();
        float roll = Random.value;

        if (roll <= chance)
        {
            ChangeState(GetRandomSoloStationaryState());
        }
        else
        {
            ChangeState(RoamState);
        }
    }

    public void NotifyStationaryStateFinished()
    {
        destinationsVisitedSinceLastStationary = 0;
        ChangeState(RoamState);
    }

    private bool CanEnterStationaryState()
    {
        return destinationsVisitedSinceLastStationary >= roamConfig.minDestinationsBeforeStationary;
    }

    private float GetCurrentStationaryChance()
    {
        int extraDestinations =
            destinationsVisitedSinceLastStationary - roamConfig.minDestinationsBeforeStationary;

        float chance =
            roamConfig.baseStationaryChance +
            (extraDestinations * roamConfig.stationaryChanceMultiplier);

        return Mathf.Clamp01(chance);
    }

    private NPCState GetRandomSoloStationaryState()
    {
        float totalWeight = roamConfig.idleWeight + roamConfig.smokingWeight;

        if (totalWeight <= 0f)
            return IdleState;

        float roll = Random.value * totalWeight;

        if (roll < roamConfig.idleWeight)
            return IdleState;

        return SmokingState;
    }

    private void UpdateAnimator()
    {
        if (agent == null || animator == null)
            return;

        animator.SetFloat("Speed", agent.velocity.magnitude);
    }
}