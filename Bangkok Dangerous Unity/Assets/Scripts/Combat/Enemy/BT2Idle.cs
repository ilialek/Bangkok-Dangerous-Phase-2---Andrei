using MBT;
using UnityEngine;

[AddComponentMenu("")]
[MBTNode("BT2/Tasks/Idle")]
public class BT2Idle : Leaf
{
    public override NodeResult Execute()
    {
        BT2EnemyContext context = BT2NodeUtility.GetContext(this);
        return context != null ? BT2NodeUtility.ToNodeResult(context.Idle()) : NodeResult.failure;
    }
}
