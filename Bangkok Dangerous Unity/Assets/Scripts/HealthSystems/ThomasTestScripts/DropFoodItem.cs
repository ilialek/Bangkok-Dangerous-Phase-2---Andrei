using UnityEngine;
/// <summary>
/// Attach to an enemy or container to give it a chance to drop a health/food item.
/// Configure the prefab and drop chance in the Inspector. Call TryDrop() when you want
/// to attempt a drop (for example on death).
/// </summary>
public class DropFoodItem : MonoBehaviour
{
    [Header("Drop Configuration")]
    [Tooltip("Array of healing item prefabs to randomly choose from when dropping. Each prefab should be setup (tagged) as a HealingItem if you rely on that tag elsewhere.")]
    [SerializeField] private GameObject[] healingItemPrefabs;
    [Tooltip("Chance to drop between 0 and 1 (0 = never, 1 = always)")]
    [Range(0f, 1f)]
    [SerializeField] private float dropChance = 0.25f;
    [Tooltip("Base world-space offset from this object where the prefab will be spawned")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.5f, 0f);
    [Tooltip("Random radius around the spawn offset for variety in drop positions")]
    [SerializeField] private float spawnRandomRadius = 0.5f;
    [Tooltip("Maximum number of times this object can be hit and drop items (0 = unlimited)")]
    [SerializeField] private int maxDrops = 0;
    [Tooltip("If true, the script will attempt a drop in Start (useful for testing)")]
    [SerializeField] private bool dropOnStart = false;

    [Header("Idle Indicator")]
    [Tooltip("Optional particle system (child or separate object) to play while this object hasn't been hit yet.")]
    [SerializeField] private ParticleSystem idleIndicator;

    private int dropsRemaining;
    [Header("Drop-On-Hit")]
    [Tooltip("If true, you can call TriggerDropOnHit() to make this object shake and drop the item when attacked")]
    [SerializeField] private bool enableDropOnHit = true;
    [Tooltip("If true, only allow one drop via TriggerDropOnHit (prevents multiple drops from repeated attacks)")]
    [SerializeField] private bool dropOnceOnHit = true;
    [Tooltip("Duration of the shake animation (seconds)")]
    [SerializeField] private float shakeDuration = 0.25f;
    [Tooltip("Magnitude of the local-position shake")]
    [SerializeField] private float shakeMagnitude = 0.1f;
    [Tooltip("Impulse force applied to spawned item's rigidbody on drop")]
    [SerializeField] private float dropForce = 2f;
    [Tooltip("Randomness multiplier applied to the drop impulse")]
    [SerializeField] private float dropRandomness = 0.5f;
    [Header("Collider-based Hit Detection")]
    [Tooltip("If true, only consider collisions with GameObjects tagged with PlayerTag (reduces false positives)")]
    [SerializeField] private bool usePlayerTagCheck = true;
    [Tooltip("Player tag to check when usePlayerTagCheck is true")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Fallback matching radius (meters). If the event target doesn't directly match this object, we treat the attack as 'hitting' this object if the target is within this radius.")]
    [SerializeField] private float fallbackMatchRadius = 1f;

    private bool hasDroppedOnHit = false;
    // Track a player InputBufferCombatSystem when they are overlapping our trigger but not yet attacking.
    private InputBufferCombatSystem overlappingPlayerIB = null;
    private Coroutine monitorAttackCoroutine = null;

    private void Awake()
    {
        // If the idle indicator is assigned but not playing, ensure it's playing at start
        if (idleIndicator != null && !idleIndicator.isPlaying)
        {
            idleIndicator.Play();
        }
    }

    /// <summary>
    /// Try to drop a random healing item from the array based on the dropChance.
    /// Call this from your death logic or whenever you want to attempt a drop.
    /// </summary>
    public void TryDrop()
    {
        if (healingItemPrefabs == null || healingItemPrefabs.Length == 0)
        {
            return;
        }

        float roll = Random.value; // 0..1
        if (roll <= dropChance)
        {
            // Select random prefab from array
            GameObject selectedPrefab = healingItemPrefabs[Random.Range(0, healingItemPrefabs.Length)];
            if (selectedPrefab == null)
            {
                return;
            }
            
            Vector3 randomOffset = new Vector3(Random.Range(-spawnRandomRadius, spawnRandomRadius), 0f, Random.Range(-spawnRandomRadius, spawnRandomRadius));
            Vector3 spawnPos = transform.position + spawnOffset + randomOffset;
            GameObject spawned = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
            // If we spawned an item, remove the idle indicator
            StopAndDestroyIdleIndicator();
            hasDroppedOnHit = true;
        }
    }

    private void Start()
    {
        if (dropOnStart) TryDrop();
        dropsRemaining = maxDrops;
    }

    /// <summary>
    /// Force a drop immediately (ignores dropChance). Randomly selects from healing item array.
    /// </summary>
    public void ForceDrop()
    {
        if (healingItemPrefabs == null || healingItemPrefabs.Length == 0)
        {
            return;
        }

        // Select random prefab from array
        GameObject selectedPrefab = healingItemPrefabs[Random.Range(0, healingItemPrefabs.Length)];
        if (selectedPrefab == null)
        {
            return;
        }
        
        Vector3 randomOffset = new Vector3(Random.Range(-spawnRandomRadius, spawnRandomRadius), 0f, Random.Range(-spawnRandomRadius, spawnRandomRadius));
        Vector3 spawnPos = transform.position + spawnOffset + randomOffset;
        GameObject spawned = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
        StopAndDestroyIdleIndicator();
        hasDroppedOnHit = true;
    }

    /// <summary>
    /// Allows changing the drop chance at runtime.
    /// </summary>
    public void SetDropChance(float chance)
    {
        dropChance = Mathf.Clamp01(chance);
    }

    /// <summary>
    /// Public method to trigger a shake-and-drop behavior (call from your attack logic).
    /// If enableDropOnHit is false or dropOnceOnHit prevented drops, this will do nothing.
    /// </summary>
    public void TriggerDropOnHit()
    {
        if (!enableDropOnHit)
        {
            return;
        }
        if (!Application.isPlaying)
        {
            return;
        }

        // Stop the idle indicator as soon as the player hits this object so the visual cue ends immediately.
        StopIdleIndicator();

        // Check if we have drops remaining
        bool hasDropsRemaining = (maxDrops <= 0) || (dropsRemaining > 0);

        // If dropOnceOnHit is enabled and we've already dropped, only allow shake if we still have drops
        if (dropOnceOnHit && hasDroppedOnHit && !hasDropsRemaining)
        {
            StartCoroutine(Shake());
            return;
        }
        if (dropOnceOnHit && hasDroppedOnHit && hasDropsRemaining)
        {
            return;
        }

        // Always shake when hit
        if (!hasDropsRemaining)
        {
            StartCoroutine(Shake());
        }
        else
        {
            StartCoroutine(ShakeAndDrop());
        }
    }

    private void OnEnable()
    {
        EventBus<PlayerAttackHitEvent>.Subscribe(OnPlayerAttackHit);
    }

    private void OnDisable()
    {
        EventBus<PlayerAttackHitEvent>.Unsubscribe(OnPlayerAttackHit);
    }

    private void OnPlayerAttackHit(PlayerAttackHitEvent evt)
    {
        // If the attack's target is this GameObject (or the same root), trigger drop on hit
        if (evt == null || evt.target == null) return;

        // robust matching: same object, child of this, this is child of target, or share same root
        bool isMatch = evt.target == gameObject
                       || evt.target.transform.IsChildOf(transform)
                       || transform.IsChildOf(evt.target.transform)
                       || evt.target.transform.root == transform.root;

        if (isMatch)
        {
            TriggerDropOnHit();
            return;
        }

        // Fallback: if the event target is physically near this object (within fallbackMatchRadius), treat it as a hit.
        if (evt.target != null && fallbackMatchRadius > 0f)
        {
            float dist = Vector3.Distance(evt.target.transform.position, transform.position);
            if (dist <= fallbackMatchRadius)
            {
                TriggerDropOnHit();
                return;
            }
        }
    }
    private System.Collections.IEnumerator ShakeAndDrop()
    {
        // local shake (so we don't permanently move the prefab)
        Vector3 originalLocalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            Vector3 offset = Random.insideUnitSphere * shakeMagnitude;
            transform.localPosition = originalLocalPos + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // restore position
        transform.localPosition = originalLocalPos;

        // Check if we have drops remaining before spawning
        if (maxDrops > 0 && dropsRemaining <= 0)
        {
            hasDroppedOnHit = true;
            yield break;
        }

        // Apply drop chance roll
        float roll = Random.value; // 0..1
        if (roll > dropChance)
        {
            hasDroppedOnHit = true;
            yield break;
        }

        // spawn a random healing item prefab at a randomized world position
        if (healingItemPrefabs != null && healingItemPrefabs.Length > 0)
        {
            // Select random prefab from array
            GameObject selectedPrefab = healingItemPrefabs[Random.Range(0, healingItemPrefabs.Length)];
            if (selectedPrefab == null)
            {
                hasDroppedOnHit = true;
                yield break;
            }
            
            Vector3 randomOffset = new Vector3(Random.Range(-spawnRandomRadius, spawnRandomRadius), 0f, Random.Range(-spawnRandomRadius, spawnRandomRadius));
            Vector3 spawnPos = transform.position + spawnOffset + randomOffset;
            GameObject spawned = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

            // apply physics impulse if the spawned item has a Rigidbody
            Rigidbody rb = spawned.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 impulse = Vector3.up * dropForce + Random.insideUnitSphere * dropRandomness;
                rb.AddForce(impulse, ForceMode.Impulse);
            }

            // If we spawned an item, remove the idle indicator (only when an item actually spawned)
            StopAndDestroyIdleIndicator();

            // Decrement drops remaining
            if (maxDrops > 0)
            {
                dropsRemaining--;
                Debug.Log($"DropFoodItem: dropped item. Remaining drops: {dropsRemaining}/{maxDrops}");
            }


        }
        else
        {
            Debug.LogWarning("DropFoodItem: no healing item prefabs assigned, cannot drop on hit.", this);
        }
        // Always rotate at the end of ShakeAndDrop
        MarkAsDepleted();

        hasDroppedOnHit = true;
    }

    private void MarkAsDepleted()
    {
        // Rotate 90 degrees on X-axis
        transform.Rotate(90f, 0f, 0f);

        Debug.Log($"{gameObject.name} is now depleted and rotated.");
    }

    private System.Collections.IEnumerator Shake()
    {
        // Just shake without dropping
        Vector3 originalLocalPos = transform.localPosition;
        float elapsed = 0f;

        Debug.Log($"DropFoodItem.Shake starting on {gameObject.name} (no drops remaining)");

        while (elapsed < shakeDuration)
        {
            Vector3 offset = Random.insideUnitSphere * shakeMagnitude;
            transform.localPosition = originalLocalPos + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // restore position
        transform.localPosition = originalLocalPos;
    }

    /// <summary>
    /// Stops and destroys the optional idle indicator particle system if present.
    /// Called when an item is successfully spawned so the indicator no longer shows.
    /// </summary>
    private void StopAndDestroyIdleIndicator()
    {
        if (idleIndicator == null) return;

        try
        {
            idleIndicator.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        catch { }

        // If the particle is on a child GameObject we remove the whole object
        var go = idleIndicator.gameObject;
        idleIndicator = null;
        if (go != null)
        {
            Destroy(go);
        }
    }

    /// <summary>
    /// Stops the idle indicator particle (no destruction). Useful to immediately hide the effect on hit
    /// while still allowing reuse or waiting until a spawn actually occurs to destroy.
    /// </summary>
    private void StopIdleIndicator()
    {
        if (idleIndicator == null) return;
        try
        {
            idleIndicator.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        catch { }
        try
        {
            idleIndicator.gameObject.SetActive(false);
        }
        catch { }
    }

    // Collider-based detection: when the player physically collides with this object while attacking,
    // trigger the drop. This is a fallback / alternative to the EventBus approach.
    private void OnTriggerEnter(Collider other)
    {
        HandlePossiblePlayerHit(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        // If the exiting collider belonged to the cached overlapping player, stop monitoring
        var ib = other.GetComponentInParent<InputBufferCombatSystem>();
        if (ib != null && ib == overlappingPlayerIB)
        {
            StopMonitoringOverlap();
        }
    }

    private void HandlePossiblePlayerHit(GameObject other)
    {
        if (other == null) return;

        if (usePlayerTagCheck && !other.CompareTag(playerTag))
        {
            // if the immediate collider isn't tagged, try parent as well
            if (!other.transform.IsChildOf(transform) && (other.transform.root == null || !other.CompareTag(playerTag)))
            {
                return;
            }
        }

        // Look up the InputBufferCombatSystem on the colliding object or its parents
        var ib = other.GetComponentInParent<InputBufferCombatSystem>();
        if (ib == null)
        {
            // not the player
            return;
        }

        // If the player is attacking, trigger the drop immediately
        if (ib.IsAttacking())
        {
            Debug.Log($"DropFoodItem: detected collision with attacking player ({other.name}) on {gameObject.name}. Triggering drop.");
            TriggerDropOnHit();
            return;
        }

        // Player is overlapping but not attacking yet: start monitoring for when they begin an attack.
        Debug.Log($"DropFoodItem: player ({other.name}) overlapping but not attacking. Will monitor for attack.");
        StartMonitoringOverlap(ib);
    }

    private void StartMonitoringOverlap(InputBufferCombatSystem ib)
    {
        if (ib == null) return;

        // If we already have a monitored player and it's the same, do nothing
        if (overlappingPlayerIB == ib && monitorAttackCoroutine != null) return;

        // Stop any previous monitor
        StopMonitoringOverlap();

        overlappingPlayerIB = ib;
        monitorAttackCoroutine = StartCoroutine(MonitorOverlapForAttack(ib));
    }

    private void StopMonitoringOverlap()
    {
        if (monitorAttackCoroutine != null)
        {
            StopCoroutine(monitorAttackCoroutine);
            monitorAttackCoroutine = null;
        }
        overlappingPlayerIB = null;
    }

    private System.Collections.IEnumerator MonitorOverlapForAttack(InputBufferCombatSystem ib)
    {
        if (ib == null) yield break;

        // Poll until the player starts attacking or they leave / this object is destroyed
        while (ib != null && !ib.IsAttacking() && gameObject != null && this.enabled)
        {
            yield return null;
        }

        // If they started attacking while overlapping, trigger the drop
        if (ib != null && ib.IsAttacking())
        {
            Debug.Log($"DropFoodItem: monitored player started attacking while overlapping. Triggering drop on {gameObject.name}.");
            TriggerDropOnHit();
        }

        // Clear monitoring state
        StopMonitoringOverlap();
    }

}
