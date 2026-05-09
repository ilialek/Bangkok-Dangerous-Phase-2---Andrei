using UnityEngine;

namespace GenericBehaviorTree
{
    public class EnemyCombatContext : MonoBehaviour
    {
        [HideInInspector] public CombatDirector combatDirector;
        [HideInInspector] public CombatSlotManager combatSlotManager;

        [HideInInspector] public float lastAttackTime = -Mathf.Infinity;
        [HideInInspector] public float recoveryEndTime = -Mathf.Infinity;
        [HideInInspector] public float attackCommitEndTime = -Mathf.Infinity;

        [Header("Slot State")]
        public int assignedSlotIndex = -1;
        public Vector3 assignedSlotPosition;
        public bool hasAssignedSlot = false;
        public float lastSlotRefreshTime = -999f;

        [Header("Attack Permission")]
        public bool hasAttackPermission = false;

        [Header("Attack Animation State")]
        public bool isAttackInProgress = false;
        public bool attackAnimationFinished = false;

        public bool IsRecovering => Time.time < recoveryEndTime;

        public void StartRecovery(float duration)
        {
            recoveryEndTime = Time.time + duration;
        }

        public void StartAttackCommit(float duration)
        {
            attackCommitEndTime = Time.time + duration;
        }

        public bool IsInAttackCommit()
        {
            return Time.time < attackCommitEndTime;
        }

        public bool IsAttackReady(float cooldown)
        {
            return Time.time >= lastAttackTime + cooldown;
        }

        public void MarkAttackPerformed()
        {
            lastAttackTime = Time.time;
        }

        public void BeginAttackAnimation()
        {
            isAttackInProgress = true;
            attackAnimationFinished = false;
        }

        public void FinishAttackAnimation()
        {
            attackAnimationFinished = true;
        }

        public void ResetAttackAnimationState()
        {
            isAttackInProgress = false;
            attackAnimationFinished = false;
        }

        public void SetAssignedSlot(int slotIndex, Vector3 slotPosition)
        {
            assignedSlotIndex = slotIndex;
            assignedSlotPosition = slotPosition;
            hasAssignedSlot = true;
        }

        public void ClearAssignedSlot()
        {
            assignedSlotIndex = -1;
            assignedSlotPosition = Vector3.zero;
            hasAssignedSlot = false;
        }

        public void MarkSlotRefresh()
        {
            lastSlotRefreshTime = Time.time;
        }

        public bool CanRefreshSlot(float refreshCooldown)
        {
            return Time.time >= lastSlotRefreshTime + refreshCooldown;
        }
    }
}
