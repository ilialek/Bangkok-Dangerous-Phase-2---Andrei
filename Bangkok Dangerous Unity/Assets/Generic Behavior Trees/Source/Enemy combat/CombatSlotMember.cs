using UnityEngine;

namespace GenericBehaviorTree
{
    public class CombatSlotMember : MonoBehaviour
    {
        [SerializeField] private EnemyCombatContext combatContext;
        [SerializeField] private CombatSlotManager combatSlotManager;

        private void Awake()
        {
            if (combatContext == null)
                combatContext = GetComponent<EnemyCombatContext>();

            if (combatSlotManager == null)
                combatSlotManager = FindFirstObjectByType<CombatSlotManager>();
        }

        private void Start()
        {
            if (combatContext != null && combatSlotManager != null)
            {
                combatContext.combatSlotManager = combatSlotManager;
                combatSlotManager.TryAssignSlot(combatContext);
            }
        }

        private void OnDestroy()
        {
            if (combatContext != null && combatSlotManager != null)
            {
                combatSlotManager.ReleaseSlot(combatContext);
            }
        }
    }
}
