using MBT;
using UnityEngine;

public static class BT2NodeUtility
{
    public static BT2EnemyContext GetContext(Component node)
    {
        return node != null ? node.GetComponentInParent<BT2EnemyContext>() : null;
    }

    public static NodeResult ToNodeResult(BT2TaskStatus status)
    {
        switch (status)
        {
            case BT2TaskStatus.Success:
                return NodeResult.success;
            case BT2TaskStatus.Running:
                return NodeResult.running;
            default:
                return NodeResult.failure;
        }
    }
}
