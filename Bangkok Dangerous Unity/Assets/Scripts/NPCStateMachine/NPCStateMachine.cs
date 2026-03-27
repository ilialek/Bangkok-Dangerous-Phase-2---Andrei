using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;

public class NPCStateMachine : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Roaming")]
    public float roamRadius = 10f;
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;

    [Header("Optional")]
    public Transform roamCenter;

    private NPCState currentState;

    public NPCIdleState IdleState { get; private set; }
    public NPCRoamState RoamState { get; private set; }

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
        Vector3 origin = GetRoamOrigin();

        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
            randomDirection.y = 0f;

            Vector3 targetPosition = origin + randomDirection;

            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }

        point = Vector3.zero;
        return false;
    }

    public float GetRandomIdleDuration()
    {
        return Random.Range(minIdleTime, maxIdleTime);
    }

    public bool HasReachedDestination()
    {
        if (agent.pathPending) return false;
        if (agent.remainingDistance > agent.stoppingDistance) return false;
        if (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f) return false;

        return true;
    }

    private void UpdateAnimator()
    {
        if (animator == null || agent == null) return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
    }
}