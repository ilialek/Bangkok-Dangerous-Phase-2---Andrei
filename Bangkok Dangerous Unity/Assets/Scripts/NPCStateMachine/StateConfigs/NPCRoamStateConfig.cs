using UnityEngine;

[CreateAssetMenu(fileName = "NPCRoamStateConfig", menuName = "NPC State Machine/States/Roam State Config")]
public class NPCRoamStateConfig : ScriptableObject
{
    [Header("Roam Settings")]
    public float roamRadius = 10f;
    public int maxSampleAttempts = 10;
    public float navMeshSampleDistance = 5f;
    public float MinEdgeDistance = 0.75f;
}