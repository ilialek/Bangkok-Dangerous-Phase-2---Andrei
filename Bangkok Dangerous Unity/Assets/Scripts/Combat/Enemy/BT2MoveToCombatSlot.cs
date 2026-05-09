using MBT;
using UnityEngine;

[AddComponentMenu("")]
[MBTNode("BT2/Tasks/Move To Combat Slot")]
public class BT2MoveToCombatSlot : Leaf
{
    public override NodeResult Execute()
    {
        BT2EnemyContext context = BT2NodeUtility.GetContext(this);
        return context != null ? BT2NodeUtility.ToNodeResult(context.MoveToCombatSlot()) : NodeResult.failure;
    }
}
