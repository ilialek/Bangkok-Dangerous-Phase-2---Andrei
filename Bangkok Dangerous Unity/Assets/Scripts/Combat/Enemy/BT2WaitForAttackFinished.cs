using MBT;
using UnityEngine;

[AddComponentMenu("")]
[MBTNode("BT2/Tasks/Wait For Attack Finished")]
public class BT2WaitForAttackFinished : Leaf
{
    public override NodeResult Execute()
    {
        BT2EnemyContext context = BT2NodeUtility.GetContext(this);
        return context != null ? BT2NodeUtility.ToNodeResult(context.WaitForAttackFinished()) : NodeResult.failure;
    }
}
