using MBT;
using UnityEngine;

[AddComponentMenu("")]
[MBTNode("BT2/Conditions/Attack Finished")]
public class BT2IsAttackFinishedCondition : Condition
{
    public override bool Check()
    {
        BT2EnemyContext context = BT2NodeUtility.GetContext(this);
        return context != null && context.IsAttackFinished();
    }
}
