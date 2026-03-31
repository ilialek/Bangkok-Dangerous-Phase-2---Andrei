using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu(fileName = "NPCTalkingStateConfig", menuName = "NPC State Machine/NPCTalkingStateConfig")]
public class NPCTalkingStateConfig : ScriptableObject
{
    [Header("Talking Settings")]
    public float minSmokingTime = 4f;
    public float maxSmokingTime = 10f;
}
