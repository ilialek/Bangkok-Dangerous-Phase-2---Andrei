using UnityEngine;

[CreateAssetMenu(fileName = "NPCRoamStateConfig", menuName = "NPC State Machine/States/Roam State Config")]
public class NPCRoamStateConfig : ScriptableObject
{
    [Header("Roam Settings")]
    public float roamRadius = 10f;
    public int maxSampleAttempts = 10;
    public float navMeshSampleDistance = 5f;
    public float MinEdgeDistance = 0.75f;

    [Header("Roam Cycle Settings")]
    public int minDestinationsBeforeStationary = 3;
    [Range(0f, 1f)] public float baseStationaryChance = 0.15f;
    [Range(0f, 1f)] public float stationaryChanceMultiplier = 0.15f;

    [Header("Stationaty State Selection")]
    [Range(0f, 1f)] public float idleWeight = 0.7f;
    [Range(0f, 1f)] public float smokingWeight = 0.3f;
}