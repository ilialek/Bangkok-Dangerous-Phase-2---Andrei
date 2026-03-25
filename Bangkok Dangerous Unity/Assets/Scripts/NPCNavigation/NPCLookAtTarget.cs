using UnityEngine;

public class NPCLookAtTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform head;

    private void LateUpdate()
    {
        head.LookAt(target);
    }
}
