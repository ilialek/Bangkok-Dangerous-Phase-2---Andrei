using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [Tooltip("How often to update the destination (seconds). Lower = more responsive, higher = cheaper.")]
    [SerializeField] private float repathInterval = 0.1f;
    [Tooltip("Extra buffer added to agent.stoppingDistance when following.")]
    [SerializeField] private float stopBuffer = 0.0f;

    private float repathTimer;
    private bool isStoppedByLogic;

    public NavMeshAgent Agent => agent;
    public Transform Target => target;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!IsAgentReady()) return;

        if (isStoppedByLogic || target == null)
        {
            // Keep agent stopped if we are not actively following.
            if (!agent.isStopped) agent.isStopped = true;
            return;
        }

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            agent.isStopped = false;

            // Only set destination if target is on/near navmesh and agent can move.
            agent.SetDestination(target.position);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        repathTimer = 0f; // update immediately
    }

    public void Stop()
    {
        isStoppedByLogic = true;
        if (IsAgentReady())
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    public void Resume()
    {
        isStoppedByLogic = false;
        repathTimer = 0f;
    }

    private bool IsAgentReady()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    // public void SetDesiredDistance(float meters)
    // {
    //     if (agent == null) return;
    //     agent.stoppingDistance = Mathf.Max(0f, meters) + Mathf.Max(0f, stopBuffer);
    // }

    public bool IsInStoppingRange()
    {
        if (agent == null || target == null) return false;
        if (agent.pathPending) return false;
        return agent.remainingDistance <= agent.stoppingDistance;
    }
}
