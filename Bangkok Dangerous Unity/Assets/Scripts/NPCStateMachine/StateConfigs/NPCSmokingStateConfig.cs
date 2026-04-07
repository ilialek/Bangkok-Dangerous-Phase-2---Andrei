using UnityEngine;

[CreateAssetMenu(fileName = "NPCSmokingStateConfig", menuName = "NPC State Machine/States/Smoking State Config")]
public class NPCSmokingStateConfig : ScriptableObject
{
    [Header("Smoking Settings")]
    public float minSmokingTime = 4f;  
    public float maxSmokingTime = 10f;
}
