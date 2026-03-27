using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class NPCLookAtTarget : MonoBehaviour
{
    [Header("Look Settings")]    
    [SerializeField] private Transform target;
    [SerializeField] private float lookChance = 0.5f;
    [SerializeField] private float lookDuration = 1.5f;
    [SerializeField] private float lookBlendSpeed = 3f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string npcTag = "NPC";

    [Header("Look Weights")]
    [SerializeField][Range(0f, 1f)] private float overallWeight = 1f;
    [SerializeField][Range(0f, 1f)] private float bodyWeight = 0.15f;
    [SerializeField][Range(0f, 1f)] private float headWeight = 1f;
    [SerializeField][Range(0f, 1f)] private float eyesWeight = 0f;
    [SerializeField][Range(0f, 1f)] private float clampWeight = 0.6f;

    private Animator animator;
    private Coroutine lookCoroutine;
    private bool isLooking;
    private float currentLookWeight;
    private Vector3 lastLookPoint;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        lastLookPoint = transform.position + transform.forward * 10f;
    }

    private void Update()
    {
        float targetWeight = isLooking ? overallWeight : 0f;

        currentLookWeight = Mathf.MoveTowards(
            currentLookWeight,
            targetWeight,
            lookBlendSpeed * Time.deltaTime
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag) && !other.CompareTag(npcTag))
            return;
        if (other.transform.root == transform.root)
            return;
        if (Random.value > lookChance)
            return;

       target = other.transform;

        if (lookCoroutine != null)
            StopCoroutine(lookCoroutine);

        lookCoroutine = StartCoroutine(LookAtForSeconds());
    }

    private IEnumerator LookAtForSeconds()
    {
        isLooking = true;
        yield return new WaitForSeconds(lookDuration);
        isLooking = false;
        target = null;
    }

    private void OnAnimatorIK(int layerIndex)
    {

        if (currentLookWeight <= 0.001f)
        {
            animator.SetLookAtWeight(0f);
            return;
        }

        Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        if (headBone == null)
        {
            animator.SetLookAtWeight(0f);
            return;
        }

        if (target != null)
        {
            Vector3 targetPosition = target.position + lookOffset;

            // Keep the look horizontal
            targetPosition.y = headBone.position.y;

            Vector3 bodyForward = transform.forward;
            bodyForward.y = 0f;
            bodyForward.Normalize();

            Vector3 directionToTarget = targetPosition - transform.position;
            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude > 0.001f)
            {
                directionToTarget.Normalize();

                float angle = Vector3.Angle(bodyForward, directionToTarget);

                Vector3 finalDirection = angle <= maxLookAngle
                    ? directionToTarget
                    : Vector3.RotateTowards(
                        bodyForward,
                        directionToTarget,
                        Mathf.Deg2Rad * maxLookAngle,
                        0f
                    );

                lastLookPoint = headBone.position + finalDirection * 10f;
                lastLookPoint.y = headBone.position.y;
            }
        }

        animator.SetLookAtWeight(
            currentLookWeight,
            bodyWeight,
            headWeight,
            eyesWeight,
            clampWeight
        );

        animator.SetLookAtPosition(lastLookPoint);
    }
}