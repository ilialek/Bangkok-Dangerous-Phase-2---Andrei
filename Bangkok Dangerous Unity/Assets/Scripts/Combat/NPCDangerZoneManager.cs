using UnityEngine;
using UnityEngine.AI;

public class NPCDangerZoneManager : MonoBehaviour
{
    public static NPCDangerZoneManager Instance { get; private set; }

    [Header("Danger Zone Settings")]
    [SerializeField] private float dangerRadius = 12f;
    [SerializeField] private float activeDuration = 15f;
    [SerializeField] private float refreshDistance = 8f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugCircle = true;

    private bool hasActiveDangerZone;
    private Vector3 dangerCenter;
    private float dangerEndTime;

    public bool HasActiveDangerZone => hasActiveDangerZone;
    public Vector3 DangerCenter => dangerCenter;
    public float DangerRadius => dangerRadius;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (GameEventsManager.instance != null && GameEventsManager.instance.combatEvents != null)
        {
            GameEventsManager.instance.combatEvents.onFightStarted += HandleFightStarted;
        }
    }

    private void OnDisable()
    {
        if (GameEventsManager.instance != null && GameEventsManager.instance.combatEvents != null)
        {
            GameEventsManager.instance.combatEvents.onFightStarted -= HandleFightStarted;
        }
    }

    private void Update()
    {
        if (!hasActiveDangerZone) return;

        if (Time.time >= dangerEndTime)
        {
            hasActiveDangerZone = false;
        }
    }

    private void HandleFightStarted(Vector3 fightOrigin)
    {
        RefreshDangerZone(fightOrigin);
    }

    public void RefreshDangerZone(Vector3 fightOrigin)
    {
        dangerCenter = fightOrigin;
        dangerEndTime = Time.time + activeDuration;
        hasActiveDangerZone = true;
    }

    public bool IsInsideDangerZone(Vector3 position)
    {
        if (!hasActiveDangerZone) return false;

        Vector3 flatPosition = position;
        Vector3 flatCenter = dangerCenter;

        flatPosition.y = 0f;
        flatCenter.y = 0f;

        return Vector3.Distance(flatPosition, flatCenter) <= dangerRadius;
    }

    public bool TryGetSafePointAwayFromDanger(Vector3 currentPosition, float fleeDistance, out Vector3 safePoint)
    {
        safePoint = Vector3.zero;

        if (!hasActiveDangerZone)
            return false;

        Vector3 awayDirection = currentPosition - dangerCenter;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude < 0.01f)
        {
            awayDirection = Random.insideUnitSphere;
            awayDirection.y = 0f;
        }

        awayDirection.Normalize();

        for (int i = 0; i < 12; i++)
        {
            float angle = Random.Range(-65f, 65f);
            Vector3 rotatedDirection = Quaternion.AngleAxis(angle, Vector3.up) * awayDirection;

            Vector3 candidate = currentPosition + rotatedDirection * fleeDistance;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                continue;

            if (IsInsideDangerZone(hit.position))
                continue;

            safePoint = hit.position;
            return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugCircle || !hasActiveDangerZone) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(dangerCenter, dangerRadius);
    }
}