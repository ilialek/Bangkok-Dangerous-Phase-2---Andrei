using UnityEngine;
using System.Collections;

public class NPCLookAtTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform head;
    [SerializeField] private float lookDuration = 3f;
    [SerializeField] private float returnSpeed = 5f;

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
            head.LookAt(target);
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