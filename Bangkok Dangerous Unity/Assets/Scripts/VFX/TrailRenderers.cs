using System.Collections;
using UnityEngine;

public class TrailRenderers : MonoBehaviour
{
    [SerializeField] private GameObject trail1;
    [SerializeField] private GameObject trail2;
    [SerializeField] private float closeDelay = 0.3f;

    private InputBufferCombatSystem combatSystem;
    private bool wasAttacking = false;
    private Coroutine disableCoroutine;

    private void Start()
    {
        // Turn off both trail GameObjects at game start
        if (trail1 != null)
            trail1.SetActive(false);
        
        if (trail2 != null)
            trail2.SetActive(false);
        
        // Get reference to combat system
        combatSystem = GetComponent<InputBufferCombatSystem>();
        if (combatSystem == null)
            combatSystem = FindObjectOfType<InputBufferCombatSystem>();
        
        Debug.Log("TrailRenderers: Both trails disabled at game start");
    }

    private void Update()
    {
        if (combatSystem == null)
            return;

        bool isAttacking = combatSystem.IsAttacking();

        // Combat started - enable trails
        if (isAttacking && !wasAttacking)
        {
            StopDisableCoroutine();
            EnableTrails();
        }
        // Combat stopped - schedule disable
        else if (!isAttacking && wasAttacking)
        {
            disableCoroutine = StartCoroutine(DisableTrailsAfterDelay(closeDelay));
        }

        wasAttacking = isAttacking;
    }

    private void EnableTrails()
    {
        if (trail1 != null)
            trail1.SetActive(true);
        if (trail2 != null)
            trail2.SetActive(true);
    }

    private IEnumerator DisableTrailsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (trail1 != null)
            trail1.SetActive(false);
        if (trail2 != null)
            trail2.SetActive(false);
    }

    private void StopDisableCoroutine()
    {
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
            disableCoroutine = null;
        }
    }
}
