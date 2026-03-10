using UnityEngine;

/// <summary>
/// Simple coin pickup. Configure `moneyAmount` in the inspector.
/// When the player collides, the coin adds money via SimpleProgression.Instance.AddMoney(...) and destroys itself.
/// Optional sound and particle effect can be set in the inspector.
/// </summary>s

public class CoinPickup : MonoBehaviour
{
    [Tooltip("Amount of money awarded when the player picks this up.")]
    [SerializeField] private int moneyAmount = 10;

    [Tooltip("Optional sound to play on pickup.")]
    [SerializeField] private AudioClip pickupSound;

    [Tooltip("Volume for the pickup sound (0-1).")]
    [Range(0f, 1f)]
    [SerializeField] private float pickupSoundVolume = 0.7f;

    // Cached collider to validate trigger setup
    private Collider cachedCollider;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
        if (cachedCollider == null)
        {
            Debug.LogError("CoinPickup requires a Collider component.");
        }
        else if (!cachedCollider.isTrigger)
        {
            Debug.LogWarning($"CoinPickup on '{gameObject.name}' expects the collider to be set as a Trigger. Setting isTrigger = true for you.");
            cachedCollider.isTrigger = true;
        }
    }

    // Called when another collider enters this trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // Prefer identifying player by Player component when available
        bool isPlayer = other.TryGetComponent<Player>(out _);

        // Fallback to tag check if Player component isn't present on colliding object
        if (!isPlayer)
            isPlayer = other.CompareTag("Player");

        if (!isPlayer) return;

        // Add money using the game's progression manager
        var progression = SimpleProgression.Instance;
        if (progression != null)
        {
            progression.AddMoney(moneyAmount);
        }
        else
        {
            Debug.LogWarning("CoinPickup: SimpleProgression.Instance not found. Money not added.");
        }

        // Play sound if assigned
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupSoundVolume);
        }

        // Immediately disable visuals and collider to avoid duplicate pickups
        DisableAndDestroy();
    }

    private void DisableAndDestroy()
    {
        // Disable collider
        if (cachedCollider != null)
            cachedCollider.enabled = false;

        // Disable renderers on this GameObject and children (if any)
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.enabled = false;

        // Destroy the GameObject after a short delay to allow audio/VFX to play
        Destroy(gameObject, 0.25f);
    }
}
