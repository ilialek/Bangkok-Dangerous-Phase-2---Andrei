using MBT;
using UnityEngine;

[AddComponentMenu("")]
[MBTNode("BT2/Tasks/Request Attack Permission")]
public class BT2RequestAttackPermission : Leaf
{
    public override NodeResult Execute()
    {
        BT2EnemyContext context = BT2NodeUtility.GetContext(this);
        return context != null ? BT2NodeUtility.ToNodeResult(context.RequestAttackPermission()) : NodeResult.failure;
    }
}
