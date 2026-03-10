using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper component for the Enemy Telegraph UI prefab.
/// Attach this to the root GameObject of your telegraph UI prefab.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class EnemyTelegraphUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Radial fill image showing attack countdown")]
    public Image fillImage;

    [Tooltip("Icon image (e.g., crosshair, warning symbol)")]
    public Image iconImage;

    [Tooltip("Optional text showing numerical countdown")]
    public TextMeshProUGUI timerText;

    private void Reset()
    {
        // Auto-find components when script is first added
        fillImage = transform.Find("Fill")?.GetComponent<Image>();
        iconImage = transform.Find("Icon")?.GetComponent<Image>();
        timerText = transform.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
    }
}