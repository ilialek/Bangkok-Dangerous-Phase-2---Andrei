using UnityEngine;

[CreateAssetMenu(menuName = "NPC/States/Roam Config")]
public class NPCRoamStateConfig : ScriptableObject
{
    [Header("Roaming")]
    public float roamRadius = 10f;
    public int maxSampleAttempts = 30;

    [Header("NavMesh Sampling")]
    public float navMeshSampleDistance = 1.5f;
    public float maxSnapDistance = 0.75f;
    public float minDistanceFromNavMeshEdge = 1.5f;

    [Header("Stationary Behaviour")]
    public int minDestinationsBeforeStationary = 2;
    [Range(0f, 1f)] public float baseStationaryChance = 0.25f;
    [Range(0f, 1f)] public float stationaryChanceMultiplier = 0.1f;

    [Header("Stationary State Weights")]
    public float idleWeight = 1f;
    public float smokingWeight = 1f;
    public float talkingWeight = 1f;
}