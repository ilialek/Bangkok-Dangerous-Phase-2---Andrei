using UnityEngine;

[CreateAssetMenu(fileName = "NPCIdleStateConfig", menuName = "NPC State Machine/States/Idle State Config")]
public class NPCIdleStateConfig : ScriptableObject
{
    [Header("Idle Settings")]
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;
}