using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NPCZoneRoaming : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Behaviour Settings")]
    [SerializeField] private float waitTimeAtPoint = 2f;
    [SerializeField] private float arriveThreshold = 0.25f;
    [SerializeField] private float navMeshSampleRadius = 2f;
    [SerializeField] private int maxPointsAttempts = 10;

    private float waitTimer = 0f;
    private bool waiting = false;
    private RoamZone currentZone;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        PickNewDestination();
    }

    private void Update()
    {
        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                PickNewDestination();
            }
            return;
        }

        if (agent.pathPending) return;

        if (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arriveThreshold))
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f)
            {
                StartWaiting();
            }
        }
    }

    private void StartWaiting()
    {
        waiting = true;
        waitTimer = waitTimeAtPoint;
        agent.ResetPath();
    }

    private void PickNewDestination()
    {
        List<RoamZone> zones = RoamZoneManager.Instance.GetAllZones();
        if (zones == null || zones.Count == 0)
        {
            Debug.LogWarning("No roam zones found in scene.");
            return;
        }

        currentZone = ChooseRandomZone(zones);

        if (TryGetPointInZone(currentZone, out Vector3 destination))
        {
            agent.SetDestination(destination);
        }
        else
        {
            Debug.LogWarning($"{name}: Failed to find valid NavMesh point in zone {currentZone.name}");
        }
    }

    private RoamZone ChooseRandomZone(List<RoamZone> zones)
    {
        float totalWeight = 0f;

        foreach (RoamZone zone in zones)
            totalWeight += zone.weight;

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (RoamZone zone in zones)
        {
            currentWeight += zone.weight;
            if (randomValue <= currentWeight)
                return zone;
        }

        return zones[zones.Count - 1];
    }

    private bool TryGetPointInZone(RoamZone zone, out Vector3 result)
    {
        for (int i = 0; i < maxPointsAttempts; i++)
        {
            Vector3 randomPoint = zone.GetRandomPointInside();
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }
}
