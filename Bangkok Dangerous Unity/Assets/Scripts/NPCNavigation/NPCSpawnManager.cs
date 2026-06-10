using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCSpawnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;

    [Header("NPC Prefabs")]
    [SerializeField] private GameObject[] npcPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] private int maxActiveNPCs = 20;
    [SerializeField] private float spawnCheckInterval = 1f;
    [SerializeField] private int spawnAttemptsPerCheck = 10;

    [Header("Spawn Distance")]
    [SerializeField] private float minSpawnDistance = 15f;
    [SerializeField] private float maxSpawnDistance = 35f;

    [Header("Despawn Distance")]
    [SerializeField] private float despawnDistance = 50f;

    [Header("NavMesh Settings")]
    [SerializeField] private float navMeshSampleDistance = 3f;
    [SerializeField] private float minDistanceFromNavMeshEdge = 0.75f;

    [Header("Visibility / Occlusion Settings")]
    [SerializeField] private bool preventSpawningInCameraView = true;
    [SerializeField] private float cameraViewPadding = 0.1f;
    [SerializeField] private LayerMask occlusionLayerMask;
    [SerializeField] private float visibilityCheckHeight = 1.6f;

    [Header("Testing Logs")]
    [SerializeField] private bool enableTestLogs = true;
    [SerializeField] private bool logRejectedSpawnAttempts = true;
    [SerializeField] private bool logSummary = true;
    [SerializeField] private float summaryInterval = 10f;

    private readonly List<GameObject> activeNPCs = new List<GameObject>();
    private float nextSpawnCheckTime;
    private float nextSummaryTime;

    private int totalSpawned;
    private int totalDespawned;
    private int totalSpawnAttempts;
    private int successfulSpawnPoints;

    private int rejectedNotOnNavMesh;
    private int rejectedNoEdgeFound;
    private int rejectedTooCloseToEdge;
    private int rejectedVisibleToCamera;
    private int rejectedDangerZone;

    private int maxActiveObserved;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (enableTestLogs)
        {
            Debug.Log(
                $"[SpawnTest] NPCSpawnManager initialized. " +
                $"Max Active NPCs: {maxActiveNPCs} | " +
                $"Spawn Distance: {minSpawnDistance}-{maxSpawnDistance} | " +
                $"Despawn Distance: {despawnDistance} | " +
                $"Spawn Attempts Per Check: {spawnAttemptsPerCheck}"
            );
        }
    }

    private void Update()
    {
        DespawnFarNPCs();

        if (logSummary && enableTestLogs && Time.time >= nextSummaryTime)
        {
            nextSummaryTime = Time.time + summaryInterval;
            PrintTestSummary();
        }

        if (Time.time < nextSpawnCheckTime)
            return;

        nextSpawnCheckTime = Time.time + spawnCheckInterval;

        TrySpawnNPCs();
    }

    private void TrySpawnNPCs()
    {
        if (activeNPCs.Count >= maxActiveNPCs)
        {
            if (enableTestLogs)
            {
                Debug.Log(
                    $"[SpawnTest] Spawn check skipped. Active NPC limit reached: " +
                    $"{activeNPCs.Count}/{maxActiveNPCs}"
                );
            }

            return;
        }

        int activeBeforeCheck = activeNPCs.Count;
        int spawnedThisCheck = 0;

        while (activeNPCs.Count < maxActiveNPCs)
        {
            bool spawned = false;

            for (int i = 0; i < spawnAttemptsPerCheck; i++)
            {
                totalSpawnAttempts++;

                if (TryGetValidSpawnPoint(out Vector3 spawnPoint))
                {
                    SpawnNPC(spawnPoint);
                    spawned = true;
                    spawnedThisCheck++;
                    break;
                }
            }

            if (!spawned)
                break;
        }

        if (enableTestLogs)
        {
            Debug.Log(
                $"[SpawnTest] Spawn check finished. " +
                $"Active before: {activeBeforeCheck} | " +
                $"Spawned this check: {spawnedThisCheck} | " +
                $"Active after: {activeNPCs.Count}/{maxActiveNPCs}"
            );
        }
    }

    private bool TryGetValidSpawnPoint(out Vector3 spawnPoint)
    {
        spawnPoint = Vector3.zero;

        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);

        Vector3 candidate =
            player.position +
            new Vector3(randomDirection.x, 0f, randomDirection.y) * randomDistance;

        if (!NavMesh.SamplePosition(
                candidate,
                out NavMeshHit hit,
                navMeshSampleDistance,
                NavMesh.AllAreas))
        {
            rejectedNotOnNavMesh++;

            if (enableTestLogs && logRejectedSpawnAttempts)
            {
                Debug.Log(
                    $"[SpawnTest] Rejected spawn point: not close enough to NavMesh. " +
                    $"Candidate: {FormatVector(candidate)} | " +
                    $"Rejected Not On NavMesh: {rejectedNotOnNavMesh}"
                );
            }

            return false;
        }

        if (!NavMesh.FindClosestEdge(hit.position, out NavMeshHit edgeHit, NavMesh.AllAreas))
        {
            rejectedNoEdgeFound++;

            if (enableTestLogs && logRejectedSpawnAttempts)
            {
                Debug.Log(
                    $"[SpawnTest] Rejected spawn point: could not find closest NavMesh edge. " +
                    $"Point: {FormatVector(hit.position)} | " +
                    $"Rejected No Edge Found: {rejectedNoEdgeFound}"
                );
            }

            return false;
        }

        if (edgeHit.distance < minDistanceFromNavMeshEdge)
        {
            rejectedTooCloseToEdge++;

            if (enableTestLogs && logRejectedSpawnAttempts)
            {
                Debug.Log(
                    $"[SpawnTest] Rejected spawn point: too close to NavMesh edge. " +
                    $"Point: {FormatVector(hit.position)} | " +
                    $"Edge Distance: {edgeHit.distance:F2} | " +
                    $"Required Minimum: {minDistanceFromNavMeshEdge:F2} | " +
                    $"Rejected Too Close To Edge: {rejectedTooCloseToEdge}"
                );
            }

            return false;
        }

        if (NPCDangerZoneManager.Instance != null &&
            NPCDangerZoneManager.Instance.IsInsideDangerZone(hit.position))
        {
            rejectedDangerZone++;

            if (enableTestLogs && logRejectedSpawnAttempts)
            {
                Debug.Log(
                    $"[SpawnTest] Rejected spawn point: inside active danger zone. " +
                    $"Point: {FormatVector(hit.position)} | " +
                    $"Rejected Danger Zone: {rejectedDangerZone}"
                );
            }

            return false;
        }

        if (preventSpawningInCameraView && IsSpawnPointVisible(hit.position))
        {
            rejectedVisibleToCamera++;

            if (enableTestLogs && logRejectedSpawnAttempts)
            {
                Debug.Log(
                    $"[SpawnTest] Rejected spawn point: visible to player camera. " +
                    $"Point: {FormatVector(hit.position)} | " +
                    $"Rejected Visible To Camera: {rejectedVisibleToCamera}"
                );
            }

            return false;
        }

        spawnPoint = hit.position;
        successfulSpawnPoints++;

        if (enableTestLogs)
        {
            float distanceToPlayer = Vector3.Distance(player.position, spawnPoint);

            Debug.Log(
                $"[SpawnTest] Valid spawn point found. " +
                $"Point: {FormatVector(spawnPoint)} | " +
                $"Distance To Player: {distanceToPlayer:F2} | " +
                $"Distance From NavMesh Edge: {edgeHit.distance:F2} | " +
                $"Successful Spawn Points: {successfulSpawnPoints}"
            );
        }

        return true;
    }

    private bool IsSpawnPointVisible(Vector3 spawnPoint)
    {
        Vector3 checkPoint = spawnPoint + Vector3.up * visibilityCheckHeight;
        Vector3 viewerPoint = playerCamera.WorldToViewportPoint(checkPoint);

        bool isInFrontOfCamera = viewerPoint.z > 0;

        bool isInsideView =
            viewerPoint.x > -cameraViewPadding &&
            viewerPoint.x < 1f + cameraViewPadding &&
            viewerPoint.y > -cameraViewPadding &&
            viewerPoint.y < 1f + cameraViewPadding;

        if (!isInFrontOfCamera || !isInsideView)
            return false;

        Vector3 cameraPosition = playerCamera.transform.position;
        Vector3 direction = checkPoint - cameraPosition;
        float distanceToPoint = direction.magnitude;

        if (Physics.Raycast(
                cameraPosition,
                direction.normalized,
                out RaycastHit hitInfo,
                distanceToPoint,
                occlusionLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            if (enableTestLogs && logRejectedSpawnAttempts)
            {
                Debug.Log(
                    $"[SpawnTest] Spawn point was inside camera view but occluded by geometry. " +
                    $"Point: {FormatVector(spawnPoint)} | " +
                    $"Occluder: {hitInfo.collider.name}"
                );
            }

            return false;
        }

        return true;
    }

    private void SpawnNPC(Vector3 position)
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0)
        {
            Debug.LogWarning("[SpawnTest] Cannot spawn NPC because no NPC prefabs are assigned.");
            return;
        }

        GameObject prefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];

        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject npc = Instantiate(prefab, position, rotation);

        activeNPCs.Add(npc);

        totalSpawned++;

        if (activeNPCs.Count > maxActiveObserved)
            maxActiveObserved = activeNPCs.Count;

        if (enableTestLogs)
        {
            Debug.Log(
                $"[SpawnTest] Spawned NPC: {npc.name} | " +
                $"Position: {FormatVector(position)} | " +
                $"Active NPCs: {activeNPCs.Count}/{maxActiveNPCs} | " +
                $"Total Spawned: {totalSpawned} | " +
                $"Max Active Observed: {maxActiveObserved}/{maxActiveNPCs}"
            );
        }
    }

    private void DespawnFarNPCs()
    {
        for (int i = activeNPCs.Count - 1; i >= 0; i--)
        {
            GameObject npc = activeNPCs[i];

            if (npc == null)
            {
                activeNPCs.RemoveAt(i);
                continue;
            }

            float distanceToPlayer = Vector3.Distance(player.position, npc.transform.position);

            if (distanceToPlayer >= despawnDistance)
            {
                if (enableTestLogs)
                {
                    Debug.Log(
                        $"[SpawnTest] Despawned NPC: {npc.name} | " +
                        $"Distance To Player: {distanceToPlayer:F2} | " +
                        $"Despawn Distance: {despawnDistance:F2} | " +
                        $"Active Before Despawn: {activeNPCs.Count}/{maxActiveNPCs}"
                    );
                }

                Destroy(npc);
                activeNPCs.RemoveAt(i);

                totalDespawned++;

                if (enableTestLogs)
                {
                    Debug.Log(
                        $"[SpawnTest] Active NPCs after despawn: {activeNPCs.Count}/{maxActiveNPCs} | " +
                        $"Total Despawned: {totalDespawned}"
                    );
                }
            }
        }
    }

    private void PrintTestSummary()
    {
        Debug.Log(
            $"[SpawnTestSummary] " +
            $"Runtime: {Time.time:F1}s | " +
            $"Active NPCs: {activeNPCs.Count}/{maxActiveNPCs} | " +
            $"Max Active Observed: {maxActiveObserved}/{maxActiveNPCs} | " +
            $"Total Spawn Attempts: {totalSpawnAttempts} | " +
            $"Valid Spawn Points: {successfulSpawnPoints} | " +
            $"Total Spawned: {totalSpawned} | " +
            $"Total Despawned: {totalDespawned} | " +
            $"Rejected Not On NavMesh: {rejectedNotOnNavMesh} | " +
            $"Rejected No Edge: {rejectedNoEdgeFound} | " +
            $"Rejected Too Close To Edge: {rejectedTooCloseToEdge} | " +
            $"Rejected Visible To Camera: {rejectedVisibleToCamera} | " +
            $"Rejected Danger Zone: {rejectedDangerZone}"
        );
    }

    private string FormatVector(Vector3 value)
    {
        return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
    }
}