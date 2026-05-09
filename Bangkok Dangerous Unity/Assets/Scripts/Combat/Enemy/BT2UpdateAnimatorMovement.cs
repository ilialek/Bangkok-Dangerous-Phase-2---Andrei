using MBT;
using UnityEngine;

[AddComponentMenu("")]
[MBTNode("BT2/Tasks/Update Animator Movement")]
public class BT2UpdateAnimatorMovement : Leaf
{
    public override NodeResult Execute()
    {
        BT2EnemyContext context = BT2NodeUtility.GetContext(this);
        return context != null ? BT2NodeUtility.ToNodeResult(context.UpdateAnimatorMovementTask()) : NodeResult.failure;
    }
}
