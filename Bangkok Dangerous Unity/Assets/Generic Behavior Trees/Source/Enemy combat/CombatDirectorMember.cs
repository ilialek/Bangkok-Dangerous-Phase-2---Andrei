using UnityEngine;

namespace GenericBehaviorTree
{
    public class CombatDirectorMember : MonoBehaviour
    {
        [SerializeField] private EnemyCombatContext combatContext;
        [SerializeField] private CombatDirector combatDirector;

        private void Awake()
        {
            if (combatContext == null)
                combatContext = GetComponent<EnemyCombatContext>();

            if (combatDirector == null)
                combatDirector = FindFirstObjectByType<CombatDirector>();
        }

        private void Start()
        {
            if (combatDirector != null && combatContext != null)
            {
                combatDirector.RegisterEnemy(combatContext);
            }
        }

        private void OnDestroy()
        {
            if (combatDirector != null && combatContext != null)
            {
                combatDirector.UnregisterEnemy(combatContext);
            }
        }
    }
}
