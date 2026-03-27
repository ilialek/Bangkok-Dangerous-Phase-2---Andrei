using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RoamZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public ZoneType zoneType;
    public float weight = 1f;

    private BoxCollider boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true; 
    }

    public Vector3 GetRandomPointInside()
    {
        Vector3 center = boxCollider.bounds.center;
        Vector3 size = boxCollider.bounds.size;

        float randomX = Random.Range(-size.x / 2f, size.x /2f);
        float randomZ = Random.Range(-size.z / 2f, size.z /2f);

        Vector3 randomPoint = new Vector3(center.x + randomX, center.y, center.z + randomZ);
        return randomPoint;
    }

    private void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        Gizmos.color = Color.blue;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(col.center, col.size);
    }
}
