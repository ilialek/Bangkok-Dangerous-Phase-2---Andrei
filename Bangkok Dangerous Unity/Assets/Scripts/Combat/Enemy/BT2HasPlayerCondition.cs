using MBT;
using UnityEngine;

[AddComponentMenu("")]
[MBTNode("BT2/Conditions/Has Player")]
public class BT2HasPlayerCondition : Condition
{
    public override bool Check()
    {
        BT2EnemyContext context = BT2NodeUtility.GetContext(this);
        return context != null && context.HasPlayer();
    }
}
