using UnityEngine;
using UnityEngine.AI;

public class NPCAnimatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Header("Animation")]
    [SerializeField] private string speedParameter = "Speed"; //Must be named "Speed" otherwise change it to the name of the parameter inside the NPCAnimator 
    [SerializeField] private float maxMoveSpeed = 1.2f;
    [SerializeField] private float dampTime = 0.15f;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // adjust animation speed to actual NPC speed
        float currentSpeed = agent.velocity.magnitude;

        // Normalize 
        float normalizedSpeed = Mathf.Clamp01(currentSpeed / maxMoveSpeed);
        Debug.Log($"Normalized Speed: {normalizedSpeed}");

        // Smoothly blend to target value
        animator.SetFloat(speedParameter, normalizedSpeed, dampTime, Time.deltaTime);
    }
}