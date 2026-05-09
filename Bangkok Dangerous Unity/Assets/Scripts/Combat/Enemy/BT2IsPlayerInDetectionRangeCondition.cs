using MBT;
using UnityEngine;

[AddComponentMenu("")]
[MBTNode("BT2/Conditions/Player In Detection Range")]
public class BT2IsPlayerInDetectionRangeCondition : Condition
{
    public override bool Check()
    {
        BT2EnemyContext context = BT2NodeUtility.GetContext(this);
        return context != null && context.IsPlayerInDetectionRange();
    }
}
