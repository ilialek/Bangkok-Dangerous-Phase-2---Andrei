using UnityEngine;

[CreateAssetMenu(fileName = "NPCTalkingStateConfig", menuName = "NPC State Machine/States/Talking State Config")]
public class NPCTalkingStateConfig : ScriptableObject
{
    [Header("Talking Settings")]
    public float minTalkingTime = 4f;
    public float maxTalkingTime = 10f;
}
