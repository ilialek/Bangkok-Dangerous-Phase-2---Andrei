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

    private readonly List<GameObject> activeNPCs = new List<GameObject>();
    private float nextSpawnCheckTime;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        DespawnFarNPCs();

        if (Time.time < nextSpawnCheckTime)
            return;

        nextSpawnCheckTime = Time.time + spawnCheckInterval;

        TrySpawnNPCs();
    }

    private void TrySpawnNPCs()
    {
        while (activeNPCs.Count < maxActiveNPCs)
        {
            bool spawned = false;

            for (int i = 0; i < spawnAttemptsPerCheck; i++)
            {
                if (TryGetValidSpawnPoint(out Vector3 spawnPoint))
                {
                    SpawnNPC(spawnPoint);
                    spawned = true;
                    break;
                }
            }

            if (!spawned)
                break;
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
            return false;
        }

        if (!NavMesh.FindClosestEdge(hit.position, out NavMeshHit edgeHit, NavMesh.AllAreas))
        {
            return false;
        }

        if (edgeHit.distance < minDistanceFromNavMeshEdge)
        {
            return false;
        }

        if (preventSpawningInCameraView && IsSpawnPointVisible(hit.position))
        {
            return false;
        }

        spawnPoint = hit.position;
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

        if (Physics.Raycast(cameraPosition, direction.normalized, out RaycastHit hitInfo, distanceToPoint, occlusionLayerMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return true;
    }

    private void SpawnNPC(Vector3 position)
    {
        GameObject prefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];

        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject npc = Instantiate(prefab, position, rotation);

        activeNPCs.Add(npc);
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
                Destroy(npc);
                activeNPCs.RemoveAt(i);
            }
        }
    }
}