using MBT;
using UnityEngine;

[AddComponentMenu("")]
[MBTNode("BT2/Tasks/Move Into Attack Range")]
public class BT2MoveIntoAttackRange : Leaf
{
    public override NodeResult Execute()
    {
        BT2EnemyContext context = BT2NodeUtility.GetContext(this);
        return context != null ? BT2NodeUtility.ToNodeResult(context.MoveIntoAttackRange()) : NodeResult.failure;
    }
}
