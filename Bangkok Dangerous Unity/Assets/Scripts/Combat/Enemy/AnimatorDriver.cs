using UnityEngine;
using UnityEngine.AI;

public class AnimatorDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;

    [Header("Animator Parameter Names")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";

    [Header("Tuning")]
    [Tooltip("Smooth time for speed changes (seconds).")]
    [SerializeField] private float speedDampTime = 0.1f;
    [Tooltip("Treat small speeds as zero to prevent foot jitter.")]
    [SerializeField] private float deadZone = 0.05f;

    private int speedHash;
    private int moveXHash;
    private int moveYHash;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();

        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        speedHash = Animator.StringToHash(speedParam);
        moveXHash = Animator.StringToHash(moveXParam);
        moveYHash = Animator.StringToHash(moveYParam);
    }

    private void Update()
    {
        if (animator == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        float speed = agent.velocity.magnitude;
        if (speed < deadZone) speed = 0f;

        float normalized = (agent.speed > 0.001f) ? (speed / agent.speed) : 0f;

        animator.SetFloat(speedHash, normalized, speedDampTime, Time.deltaTime);

        Vector3 localVel = transform.InverseTransformDirection(agent.velocity);
        float moveX = (Mathf.Abs(localVel.x) < deadZone) ? 0f : localVel.x;
        float moveY = (Mathf.Abs(localVel.z) < deadZone) ? 0f : localVel.z;

        animator.SetFloat(moveXHash, moveX, speedDampTime, Time.deltaTime);
        animator.SetFloat(moveYHash, moveY, speedDampTime, Time.deltaTime);
    }
}
