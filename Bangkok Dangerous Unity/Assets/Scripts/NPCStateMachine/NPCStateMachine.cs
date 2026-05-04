using System.Collections.Generic;
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

    public bool CanIdle => idleConfig != null;
    public bool CanRoam => roamConfig != null;
    public bool CanSmoke => smokingConfig != null;
    public bool CanTalk => talkingConfig != null;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (CanIdle) IdleState = new NPCIdleState(this);

        if (CanRoam) RoamState = new NPCRoamState(this);

        if (CanSmoke) SmokingState = new NPCSmokingState(this);

        if (CanTalk) TalkingState = new NPCTalkingState(this);
    }

    private void Start()
    {
        NPCState startingState = GetStartingState();

        if (startingState == null)
        {
            Debug.LogWarning($"{name} has no NPC state configs assigned.", this);
            enabled = false;
            return;
        }

        ChangeState(startingState);
    }

    private void Update()
    {
        currentState?.Tick();
        UpdateAnimator();
    }

    public void ChangeState(NPCState newState)
    {
        if (newState == null)
        {
            Debug.LogWarning($"{name} tried to change to a null NPC state.", this);
            return;
        }

        currentState?.Exit();

        currentState = newState;
        currentState.Enter();
    }

    private NPCState GetStartingState()
    {
        if (CanRoam)
            return RoamState;

        List<NPCState> availableStates = GetAvailableStationaryStates();

        if (availableStates.Count == 0)
            return null;

        return availableStates[Random.Range(0, availableStates.Count)];
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

            if (!NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    roamConfig.navMeshSampleDistance,
                    NavMesh.AllAreas))
            {
                continue;
            }

            float snapDistance = Vector3.Distance(candidate, hit.position);

            if (snapDistance > roamConfig.maxSnapDistance)
            {
                continue;
            }

            if (!NavMesh.FindClosestEdge(hit.position, out NavMeshHit edgeHit, NavMesh.AllAreas))
            {
                continue;
            }

            if (edgeHit.distance < roamConfig.minDistanceFromNavMeshEdge)
            {
                continue;
            }

            point = hit.position;
            return true;
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
            NPCState stationaryState = GetRandomAvailableStationaryState();

            if (stationaryState != null)
            {
                ChangeState(stationaryState);
                return;
            }
        }

        ChangeState(RoamState);
    }

    public void NotifyStationaryStateFinished()
    {
        destinationsVisitedSinceLastStationary = 0;

        if (CanRoam)
        {
            ChangeState(RoamState);
            return;
        }

        NPCState nextStationaryState = GetRandomAvailableStationaryState();

        if (nextStationaryState != null)
            ChangeState(nextStationaryState);
    }

    private bool CanEnterStationaryState()
    {
        if (!CanRoam)
            return false;

        if (GetAvailableStationaryStates().Count == 0)
            return false;

        return destinationsVisitedSinceLastStationary >= roamConfig.minDestinationsBeforeStationary;
    }

    private float GetCurrentStationaryChance()
    {
        if (!CanRoam)
            return 0f;

        int extraDestinations =
            destinationsVisitedSinceLastStationary - roamConfig.minDestinationsBeforeStationary;

        float chance =
            roamConfig.baseStationaryChance +
            extraDestinations * roamConfig.stationaryChanceMultiplier;

        return Mathf.Clamp01(chance);
    }

    private NPCState GetRandomAvailableStationaryState()
    {
        List<NPCState> availableStates = GetAvailableStationaryStates();

        if (availableStates.Count == 0)
            return null;

        return availableStates[Random.Range(0, availableStates.Count)];
    }

    private List<NPCState> GetAvailableStationaryStates()
    {
        List<NPCState> states = new List<NPCState>();

        if (CanIdle)
            states.Add(IdleState);

        if (CanSmoke)
            states.Add(SmokingState);

        if (CanTalk)
            states.Add(TalkingState);

        return states;
    }

    private void UpdateAnimator()
    {
        if (agent == null || animator == null)
            return;

        animator.SetFloat("Speed", agent.velocity.magnitude);
    }
}