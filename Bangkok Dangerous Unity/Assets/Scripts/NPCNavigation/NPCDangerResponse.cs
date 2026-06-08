using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCDangerResponder : MonoBehaviour
{
    [Header("Flee Settings")]
    [SerializeField] private float fleeDistance = 14f;
    [SerializeField] private float fleeSpeed = 3.5f;
    [SerializeField] private float checkInterval = 0.25f;
    [SerializeField] private float reachedSafeDistance = 1f;

    private NavMeshAgent agent;
    private NPCStateMachine stateMachine;

    private float originalSpeed;
    private float nextCheckTime;
    private bool isFleeing;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stateMachine = GetComponent<NPCStateMachine>();

        if (agent != null)
        {
            originalSpeed = agent.speed;
        }
    }

    private void OnEnable()
    {
        if (GameEventsManager.instance != null && GameEventsManager.instance.combatEvents != null)
        {
            GameEventsManager.instance.combatEvents.onFightStarted += HandleFightStarted;
        }
    }

    private void OnDisable()
    {
        if (GameEventsManager.instance != null && GameEventsManager.instance.combatEvents != null)
        {
            GameEventsManager.instance.combatEvents.onFightStarted -= HandleFightStarted;
        }
    }

    private void Update()
    {
        if (Time.time < nextCheckTime) return;
        nextCheckTime = Time.time + checkInterval;

        NPCDangerZoneManager dangerZone = NPCDangerZoneManager.Instance;

        if (dangerZone == null || !dangerZone.HasActiveDangerZone)
        {
            StopFleeing();
            return;
        }

        if (dangerZone.IsInsideDangerZone(transform.position))
        {
            FleeFromDanger();
            return;
        }

        if (isFleeing && HasReachedDestination())
        {
            StopFleeing();
        }
    }

    private void HandleFightStarted(Vector3 fightOrigin)
    {
        NPCDangerZoneManager dangerZone = NPCDangerZoneManager.Instance;

        if (dangerZone == null) return;

        if (dangerZone.IsInsideDangerZone(transform.position))
        {
            FleeFromDanger();
        }
    }

    private void FleeFromDanger()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        NPCDangerZoneManager dangerZone = NPCDangerZoneManager.Instance;
        if (dangerZone == null) return;

        if (!dangerZone.TryGetSafePointAwayFromDanger(transform.position, fleeDistance, out Vector3 safePoint))
            return;

        isFleeing = true;

        if (stateMachine != null)
        {
            stateMachine.enabled = false;
        }

        agent.isStopped = false;
        agent.speed = fleeSpeed;
        agent.stoppingDistance = 0f;
        agent.SetDestination(safePoint);
    }

    private void StopFleeing()
    {
        if (!isFleeing) return;

        isFleeing = false;

        if (agent != null)
        {
            agent.speed = originalSpeed;
        }

        if (stateMachine != null)
        {
            stateMachine.enabled = true;
        }
    }

    private bool HasReachedDestination()
    {
        if (agent == null) return true;
        if (agent.pathPending) return false;
        if (agent.remainingDistance > reachedSafeDistance) return false;
        return true;
    }
}