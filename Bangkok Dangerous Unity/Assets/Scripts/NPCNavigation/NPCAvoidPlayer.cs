using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCAvoidPlayer : MonoBehaviour
{
    [Header("Will be assigned automatically")]
    [SerializeField] private Transform player;
    [SerializeField] private NavMeshAgent agent;


    [Header("Avoidance Settings")]
    [SerializeField] private float avoidRadius = 3f;
    [SerializeField] private float avoidStrengthMultiplier = 2f;
    [SerializeField] private float velocitySmooth = 5f;

    [Header("Behavior Tweaks")]
    [SerializeField] private bool useForwardBias = true;
    [SerializeField] private bool onlyAvoidMovingPlayer = true;
    [SerializeField] private float movingPlayerThreshold = 0.1f;

    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        if (player != null)
            lastPlayerPosition = player.position;
    }

    void Update()
    {
        UpdatePlayerVelocity();
        ApplyAvoidance();
    }

    void UpdatePlayerVelocity()
    {
        playerVelocity = (player.position - lastPlayerPosition) / Time.deltaTime;
        lastPlayerPosition = player.position;
    }

    void ApplyAvoidance()
    {
        Vector3 toPlayer = transform.position - player.position;
        float distance = toPlayer.magnitude;

        if (distance > avoidRadius)
            return;

        float strength = 1f - (distance / avoidRadius);

        // Reduce effect if player is not moving
        if (onlyAvoidMovingPlayer && playerVelocity.magnitude < movingPlayerThreshold)
        {
            strength *= 0.3f;
        }

        // Forward bias (NPC reacts more if player is moving toward it)
        if (useForwardBias)
        {
            Vector3 playerForward = playerVelocity.normalized;
            float forwardDot = Vector3.Dot(playerForward, -toPlayer.normalized);
            float forwardFactor = Mathf.Clamp01(forwardDot + 0.5f);
            strength *= forwardFactor;
        }

        // Side-step direction (perpendicular)
        Vector3 sideStep = Vector3.Cross(Vector3.up, toPlayer).normalized;

        // Make direction stable (avoid jitter)
        float dir = Mathf.Sign(Vector3.Dot(player.forward, sideStep));
        sideStep *= dir;

        Vector3 desiredVelocity = agent.desiredVelocity;
        Vector3 avoidance = sideStep * strength * avoidStrengthMultiplier;

        Vector3 finalVelocity = desiredVelocity + avoidance;

        agent.velocity = Vector3.Lerp(agent.velocity, finalVelocity, Time.deltaTime * velocitySmooth);
    }
}