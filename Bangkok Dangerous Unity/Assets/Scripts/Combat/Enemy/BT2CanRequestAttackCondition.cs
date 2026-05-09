using MBT;
using UnityEngine;

[AddComponentMenu("")]
[MBTNode("BT2/Conditions/Can Request Attack")]
public class BT2CanRequestAttackCondition : Condition
{
    public override bool Check()
    {
        BT2EnemyContext context = BT2NodeUtility.GetContext(this);
        return context != null && context.CanRequestAttack();
    }
}
