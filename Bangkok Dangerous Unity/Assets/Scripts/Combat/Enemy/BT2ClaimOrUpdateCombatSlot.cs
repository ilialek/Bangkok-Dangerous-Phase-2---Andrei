using MBT;
using UnityEngine;

[AddComponentMenu("")]
[MBTNode("BT2/Tasks/Claim Or Update Combat Slot")]
public class BT2ClaimOrUpdateCombatSlot : Leaf
{
    public override NodeResult Execute()
    {
        BT2EnemyContext context = BT2NodeUtility.GetContext(this);
        return context != null ? BT2NodeUtility.ToNodeResult(context.ClaimOrUpdateCombatSlot()) : NodeResult.failure;
    }
}
