using UnityEngine;

[CreateAssetMenu(fileName = "NPCIdleStateConfig", menuName = "NPC State Machine/States/Idle State Config")]
public class NPCIdleStateConfig : ScriptableObject
{
    [Header("Idle Settings")]
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;

    [Header("Idle Animation Variants")]
    [Tooltip("O=PhoneA, 1=PhoneB, 2=PhonePacing, 3=Texting, 4=Drinking, 5=Sad")]
    public int idleVariantCount = 6;

    public int GetRandomIdleVariant()
    {
        return Random.Range(0, idleVariantCount);
    }   
}