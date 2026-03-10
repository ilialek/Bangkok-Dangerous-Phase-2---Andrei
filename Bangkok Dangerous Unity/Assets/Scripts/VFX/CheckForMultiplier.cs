using UnityEngine;

public class CheckForMultiplier : MonoBehaviour
{
    [SerializeField] private GameObject multiplierIndicator;
    [SerializeField] private GameObject speedMultiplierIndicator;

    private Player player;

    private void Start()
    {
        player = FindObjectOfType<Player>();
        if (player == null)
        {
            Debug.LogError("CheckForMultiplier: Could not find Player in scene!");
        }

        // Ensure the indicators are off by default
        if (multiplierIndicator != null)
            multiplierIndicator.SetActive(false);
        if (speedMultiplierIndicator != null)
            speedMultiplierIndicator.SetActive(false);
    }

    private void Update()
    {
        if (player == null)
            return;

        // Enable damage multiplier indicator when damage multiplier is active (> 1.0)
        if (multiplierIndicator != null)
        {
            bool isDamageMultiplierActive = player.DamageMultiplier > 1f;
            multiplierIndicator.SetActive(isDamageMultiplierActive);
        }

        // Enable speed multiplier indicator when speed multiplier is active (> 1.0)
        if (speedMultiplierIndicator != null)
        {
            PlayerHealthSystem healthSystem = FindObjectOfType<PlayerHealthSystem>();
            bool isSpeedMultiplierActive = healthSystem != null && healthSystem.SpeedMultiplier > 1f;
            speedMultiplierIndicator.SetActive(isSpeedMultiplierActive);
        }
    }
}
