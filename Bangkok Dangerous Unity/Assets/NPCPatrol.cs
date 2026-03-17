using UnityEngine;
using UnityEngine.AI;

public class NPCPatrol : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform[] destinations;

    [Header("Behavior")]
    [SerializeField] private bool randomOrder = true;
    [SerializeField] private float waitTimeAtPoint = 2f;
    [SerializeField] private float arriveThreshold = 0.25f;

    private int currentIndex = 0;
    private float waitTimer = 0f;
    private bool waiting = false;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (destinations == null || destinations.Length == 0)
        {
            Debug.LogWarning($"{name}: No patrol destinations assigned.");
            enabled = false;
            return;
        }

        GoToNextDestination();
    }

    private void Update()
    {
        if (agent == null || waiting)
        {
            if (waiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    waiting = false;
                    GoToNextDestination();
                }
            }
            return;
        }

        // Path may take a moment to compute
        if (agent.pathPending)
            return;

        // Only continue if the agent has arrived
        if (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arriveThreshold))
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f)
            {
                StartWaiting();
            }
        }
    }

    private void StartWaiting()
    {
        waiting = true;
        waitTimer = waitTimeAtPoint;
        agent.ResetPath();
    }

    private void GoToNextDestination()
    {
        if (randomOrder)
        {
            int nextIndex = Random.Range(0, destinations.Length);

            // Avoid picking the same point twice in a row if possible
            if (destinations.Length > 1)
            {
                while (nextIndex == currentIndex)
                    nextIndex = Random.Range(0, destinations.Length);
            }

            currentIndex = nextIndex;
        }
        else
        {
            currentIndex = (currentIndex + 1) % destinations.Length;
        }

        Transform target = destinations[currentIndex];
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
    }
}