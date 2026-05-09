using MBT;
using UnityEngine;

[AddComponentMenu("")]
[MBTNode("BT2/Tasks/Register With Combat Director")]
public class BT2RegisterWithCombatDirector : Leaf
{
    public override NodeResult Execute()
    {
        BT2EnemyContext context = BT2NodeUtility.GetContext(this);
        return context != null ? BT2NodeUtility.ToNodeResult(context.RegisterWithCombatDirector()) : NodeResult.failure;
    }
}
