using MBT;
using UnityEngine;

[AddComponentMenu("")]
[MBTNode("BT2/Tasks/Release Attack Permission")]
public class BT2ReleaseAttackPermission : Leaf
{
    public override NodeResult Execute()
    {
        BT2EnemyContext context = BT2NodeUtility.GetContext(this);
        return context != null ? BT2NodeUtility.ToNodeResult(context.ReleaseAttackPermission()) : NodeResult.failure;
    }
}
