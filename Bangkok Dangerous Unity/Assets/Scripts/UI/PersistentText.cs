using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A persistent Text component that cannot be disabled.
/// Used for UI elements that must always remain visible.
/// </summary>
public class PersistentText : Text
{
    private bool _forceEnabled = true;

    public new bool enabled
    {
        get { return _forceEnabled; }
        set
        {
            // Always stay enabled - ignore attempts to disable
            _forceEnabled = true;
            base.enabled = true;
        }
    }

    protected override void OnDisable()
    {
        // ABSOLUTELY PREVENT disabling - immediately re-enable with maximum priority
        base.enabled = true;
        _forceEnabled = true;
        
        // Also ensure the GameObject stays active
        if (gameObject != null && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        Debug.LogError($"CRITICAL SECURITY BREACH: PersistentText {gameObject?.name ?? "unknown"} was disabled! IMMEDIATE RE-ACTIVATION.");
    }

    protected override void Start()
    {
        base.Start();
        // Double-check we're enabled
        base.enabled = true;
        _forceEnabled = true;
    }

#if UNITY_EDITOR
#endif
}