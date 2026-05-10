using System.Collections.Generic;
using UnityEngine;

namespace GenericBehaviorTree
{
    public class CombatSlotManager : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform playerTarget;

        [Header("Slot Layout")]
        [SerializeField] private int slotCount = 8;
        [SerializeField] private float slotRadius = 2.5f;
        [SerializeField] private float slotHeightOffset = 0f;

        private readonly Dictionary<EnemyCombatContext, int> enemyToSlot = new();
        private readonly Dictionary<int, EnemyCombatContext> slotToEnemy = new();

        private void Awake()
        {
            if (playerTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerTarget = player.transform;
            }
        }

        public bool TryAssignSlot(EnemyCombatContext enemy)
        {
            if (enemy == null || playerTarget == null)
                return false;

            if (enemyToSlot.TryGetValue(enemy, out int existingSlot))
            {
                enemy.SetAssignedSlot(existingSlot, GetSlotWorldPosition(existingSlot));
                return true;
            }

            int bestSlot = FindBestAvailableSlot(enemy.transform.position);
            if (bestSlot == -1)
                return false;

            enemyToSlot[enemy] = bestSlot;
            slotToEnemy[bestSlot] = enemy;
            enemy.SetAssignedSlot(bestSlot, GetSlotWorldPosition(bestSlot));
            return true;
        }

        public bool TryReassignToBetterSlot(EnemyCombatContext enemy, float improvementThreshold)
        {
            if (enemy == null || playerTarget == null)
                return false;

            if (!enemyToSlot.TryGetValue(enemy, out int currentSlot))
                return TryAssignSlot(enemy);

            Vector3 currentSlotPos = GetSlotWorldPosition(currentSlot);
            float currentSqrDistance = (enemy.transform.position - currentSlotPos).sqrMagnitude;

            int bestFreeSlot = -1;
            float bestFreeSqrDistance = float.MaxValue;

            for (int i = 0; i < slotCount; i++)
            {
                if (slotToEnemy.ContainsKey(i))
                    continue;

                Vector3 freeSlotPos = GetSlotWorldPosition(i);
                float freeSqrDistance = (enemy.transform.position - freeSlotPos).sqrMagnitude;

                if (freeSqrDistance < bestFreeSqrDistance)
                {
                    bestFreeSqrDistance = freeSqrDistance;
                    bestFreeSlot = i;
                }
            }

            if (bestFreeSlot == -1)
            {
                enemy.SetAssignedSlot(currentSlot, currentSlotPos);
                return false;
            }

            float improvement = currentSqrDistance - bestFreeSqrDistance;

            if (improvement < improvementThreshold * improvementThreshold)
            {
                enemy.SetAssignedSlot(currentSlot, currentSlotPos);
                return false;
            }

            slotToEnemy.Remove(currentSlot);
            enemyToSlot[enemy] = bestFreeSlot;
            slotToEnemy[bestFreeSlot] = enemy;

            enemy.SetAssignedSlot(bestFreeSlot, GetSlotWorldPosition(bestFreeSlot));
            return true;
        }

        public bool UpdateAssignedSlotPosition(EnemyCombatContext enemy)
        {
            if (enemy == null || playerTarget == null)
                return false;

            if (!enemyToSlot.TryGetValue(enemy, out int slotIndex))
                return false;

            enemy.SetAssignedSlot(slotIndex, GetSlotWorldPosition(slotIndex));
            return true;
        }

        public void ReleaseSlot(EnemyCombatContext enemy)
        {
            if (enemy == null)
                return;

            if (enemyToSlot.TryGetValue(enemy, out int slotIndex))
            {
                enemyToSlot.Remove(enemy);
                slotToEnemy.Remove(slotIndex);
            }

            enemy.ClearAssignedSlot();
        }

        public Vector3 GetSlotWorldPosition(int slotIndex)
        {
            if (playerTarget == null || slotCount <= 0)
                return Vector3.zero;

            float angleStep = 360f / slotCount;
            float angle = angleStep * slotIndex * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * slotRadius,
                slotHeightOffset,
                Mathf.Sin(angle) * slotRadius
            );

            return playerTarget.position + offset;
        }

        private int FindBestAvailableSlot(Vector3 enemyPosition)
        {
            int bestSlot = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < slotCount; i++)
            {
                if (slotToEnemy.ContainsKey(i))
                    continue;

                Vector3 slotPos = GetSlotWorldPosition(i);
                float sqrDistance = (enemyPosition - slotPos).sqrMagnitude;

                if (sqrDistance < bestDistance)
                {
                    bestDistance = sqrDistance;
                    bestSlot = i;
                }
            }

            return bestSlot;
        }
    }
}
