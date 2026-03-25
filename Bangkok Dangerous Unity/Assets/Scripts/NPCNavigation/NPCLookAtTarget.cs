using UnityEngine;
using System.Collections;

public class NPCLookAtTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform head;
    [SerializeField] private float lookDuration = 3f;
    [SerializeField] private float returnSpeed = 5f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.5f, 0f);

    private Quaternion defaultLocalRotation;
    private Coroutine lookCoroutine;
    private bool isLooking;

    private void Start()
    {
        defaultLocalRotation = head.localRotation;
    }

    private void LateUpdate()
    {
        if (isLooking && target != null)
        {
            Vector3 targetPosition = target.position + lookOffset;

            // Ignore vertical difference so the head only turns left/right
            targetPosition.y = head.position.y;

            Vector3 directionToTarget = targetPosition - head.position;

            if (directionToTarget.sqrMagnitude > 0.001f)
            {
                Vector3 bodyForward = transform.forward;
                bodyForward.y = 0f;
                bodyForward.Normalize();

                Vector3 flatDirection = directionToTarget;
                flatDirection.y = 0f;
                flatDirection.Normalize();

                float angle = Vector3.Angle(bodyForward, flatDirection);

                Vector3 finalDirection;

                if (angle <= maxLookAngle)
                {
                    finalDirection = flatDirection;
                }
                else
                {
                    finalDirection = Vector3.RotateTowards(
                        bodyForward,
                        flatDirection,
                        Mathf.Deg2Rad * maxLookAngle,
                        0f
                    );
                }

                head.rotation = Quaternion.LookRotation(finalDirection, Vector3.up);
            }
        }
        else
        {
            head.localRotation = Quaternion.Slerp(
                head.localRotation,
                defaultLocalRotation,
                returnSpeed * Time.deltaTime
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("NPC"))
            return;

        if (other.transform.root == transform.root)
            return;

        target = other.transform;

        if (lookCoroutine != null)
        {
            StopCoroutine(lookCoroutine);
        }

        lookCoroutine = StartCoroutine(LookAtForSeconds());
    }

    private IEnumerator LookAtForSeconds()
    {
        isLooking = true;
        yield return new WaitForSeconds(lookDuration);
        isLooking = false;
        target = null;
    }
}