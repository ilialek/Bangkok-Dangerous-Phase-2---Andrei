using System.Collections.Generic;
using UnityEngine;

namespace GenericBehaviorTree
{
    public class CombatDirector : MonoBehaviour
    {
        [Header("Attack Rules")]
        [SerializeField] private int maxSimultaneousAttackers = 1;
        [SerializeField] private float sameEnemyAttackDelay = 2.0f;
        [SerializeField] private float globalAttackInterval = 1.0f;

        private readonly List<EnemyCombatContext> registeredEnemies = new();
        private readonly HashSet<EnemyCombatContext> activeAttackers = new();
        private readonly Dictionary<EnemyCombatContext, float> lastAttackTimes = new();

        private float nextGlobalAttackTime = 0f;

        public void RegisterEnemy(EnemyCombatContext enemy)
        {
            if (enemy == null)
                return;

            if (!registeredEnemies.Contains(enemy))
                registeredEnemies.Add(enemy);

            if (!lastAttackTimes.ContainsKey(enemy))
                lastAttackTimes.Add(enemy, -999f);
        }

        public void UnregisterEnemy(EnemyCombatContext enemy)
        {
            if (enemy == null)
                return;

            registeredEnemies.Remove(enemy);
            activeAttackers.Remove(enemy);
            lastAttackTimes.Remove(enemy);
        }

        public bool CanAttack(EnemyCombatContext enemy)
        {
            if (enemy == null)
                return false;

            if (!registeredEnemies.Contains(enemy))
                RegisterEnemy(enemy);

            if (Time.time < nextGlobalAttackTime)
                return false;

            if (activeAttackers.Contains(enemy))
                return true;

            if (activeAttackers.Count >= maxSimultaneousAttackers)
                return false;

            if (lastAttackTimes.TryGetValue(enemy, out float lastAttackTime))
            {
                if (Time.time < lastAttackTime + sameEnemyAttackDelay)
                    return false;
            }

            return true;
        }

        public bool TryClaimAttackPermission(EnemyCombatContext enemy)
        {
            if (enemy == null)
                return false;

            if (!registeredEnemies.Contains(enemy))
                RegisterEnemy(enemy);

            if (Time.time < nextGlobalAttackTime)
                return false;

            if (activeAttackers.Contains(enemy))
                return true;

            if (activeAttackers.Count >= maxSimultaneousAttackers)
                return false;

            if (!lastAttackTimes.TryGetValue(enemy, out float enemyLastAttackTime))
                enemyLastAttackTime = -999f;

            if (Time.time < enemyLastAttackTime + sameEnemyAttackDelay)
                return false;

            foreach (EnemyCombatContext other in registeredEnemies)
            {
                if (other == null || other == enemy)
                    continue;

                if (activeAttackers.Contains(other))
                    continue;

                if (!lastAttackTimes.TryGetValue(other, out float otherLastAttackTime))
                    otherLastAttackTime = -999f;

                if (Time.time < otherLastAttackTime + sameEnemyAttackDelay)
                    continue;

                if (otherLastAttackTime < enemyLastAttackTime)
                    return false;
            }

            activeAttackers.Add(enemy);
            return true;
        }

        public void ReleaseAttackPermission(EnemyCombatContext enemy)
        {
            if (enemy == null)
                return;

            activeAttackers.Remove(enemy);
        }

        public EnemyCombatContext GetPrimaryActiveAttacker()
        {
            foreach (EnemyCombatContext attacker in activeAttackers)
            {
                if (attacker != null)
                    return attacker;
            }

            return null;
        }

        public void NotifyAttackPerformed(EnemyCombatContext enemy)
        {
            if (enemy == null)
                return;

            if (!lastAttackTimes.ContainsKey(enemy))
                lastAttackTimes.Add(enemy, Time.time);
            else
                lastAttackTimes[enemy] = Time.time;

            nextGlobalAttackTime = Time.time + globalAttackInterval;
        }
    }
}