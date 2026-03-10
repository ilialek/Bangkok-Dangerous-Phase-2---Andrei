using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Manages visual and timing effects for player attacks hitting targets.
/// Handles camera shake and hitstop (freeze frame) effects based on attack properties.
/// </summary>
public class HitEffects : MonoBehaviour
{
    public static HitEffects Instance;

    [Header("Camera Shake References")]
    [SerializeField] private CinemachineCamera cinemachine;
    private CinemachineBasicMultiChannelPerlin noise;

    [Header("Camera Shake Settings")]
    [SerializeField] private float lightHitAmplitude = 0.3f;
    [SerializeField] private float lightHitFrequency = 1.5f;
    [SerializeField] private float lightHitDuration = 0.1f;
    
    [SerializeField] private float heavyHitAmplitude = 0.8f;
    [SerializeField] private float heavyHitFrequency = 2f;
    [SerializeField] private float heavyHitDuration = 0.15f;
    
    [SerializeField] private float finisherHitAmplitude = 1.5f;
    [SerializeField] private float finisherHitFrequency = 2.5f;
    [SerializeField] private float finisherHitDuration = 0.2f;

    [Header("Hitstop Settings")]
    [SerializeField] private bool enableHitstop = true;
    [SerializeField] private float hitstopTimeScale = 0.05f;
    [SerializeField] private float defaultHitstopDuration = 0.08f;
    [SerializeField] private float finisherHitstopMultiplier = 2f;

    private float shakeTimer = 0f;
    private int currentHitstopId = -1;

    #region Initialization
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Setup camera shake
        if (cinemachine == null)
            cinemachine = GetComponent<CinemachineCamera>();
        
        if (cinemachine != null)
            noise = cinemachine.GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();

        if (noise == null)
        {
            Debug.LogWarning("HitEffects: CinemachineBasicMultiChannelPerlin component not found!");
        }
        else
        {
            // Initialize camera shake to zero - only activate on hits
            noise.AmplitudeGain = 0f;
            noise.FrequencyGain = 0f;
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
    #endregion

    #region Event Handlers
    private void OnPlayerAttackHit(PlayerAttackHitEvent evt)
    {
        // Only trigger effects for successful hits on valid targets
        if (evt.target == null || evt.attackData == null)
            return;

        // Trigger camera shake based on attack type
        TriggerCameraShakeForAttack(evt.attackData);

        // Trigger hitstop effect
        if (enableHitstop)
        {
            TriggerHitstop(evt.attackData);
        }
    }
    #endregion

    #region Camera Shake
    private void TriggerCameraShakeForAttack(AttackSO attack)
    {
        float amplitude, frequency, duration;

        // Determine shake intensity based on attack properties
        if (attack.isFinisher)
        {
            amplitude = finisherHitAmplitude;
            frequency = finisherHitFrequency;
            duration = finisherHitDuration;
        }
        else if (attack.damage >= 30f) // Heavy attack threshold
        {
            amplitude = heavyHitAmplitude;
            frequency = heavyHitFrequency;
            duration = heavyHitDuration;
        }
        else // Light attack
        {
            amplitude = lightHitAmplitude;
            frequency = lightHitFrequency;
            duration = lightHitDuration;
        }

        Shake(duration, amplitude, frequency);
    }

    public void Shake(float duration, float amplitude, float frequency)
    {
        if (noise == null)
        {
            Debug.LogWarning("HitEffects: Cannot shake camera - noise component not found!");
            return;
        }

        // Set shake timer
        shakeTimer = duration;

        // Apply shake
        noise.AmplitudeGain = amplitude;
        noise.FrequencyGain = frequency;

        //Debug.Log($"HitEffects: Camera shake - amplitude {amplitude}, frequency {frequency}, duration {duration}s");
    }

    public void StopShake()
    {
        shakeTimer = 0f;
        if (noise != null)
        {
            noise.AmplitudeGain = 0f;
            noise.FrequencyGain = 0f;
        }
    }
    #endregion

    #region Hitstop (Freeze Frame Effect)
    private void TriggerHitstop(AttackSO attack)
    {
        if (TimeSlowManager.Instance == null)
        {
            Debug.LogWarning("HitEffects: TimeSlowManager not found! Hitstop disabled.");
            return;
        }

        // Use attack's hitstop value if available, otherwise use default
        float hitstopDuration = attack.hitstop > 0 ? attack.hitstop : defaultHitstopDuration;

        // Apply multiplier for finisher moves
        if (attack.isFinisher)
        {
            hitstopDuration *= finisherHitstopMultiplier;
        }

        // Cancel any existing hitstop
        if (currentHitstopId >= 0)
        {
            TimeSlowManager.Instance.RemoveTimeSlowEffect(currentHitstopId);
        }

        // Add new hitstop effect
        currentHitstopId = TimeSlowManager.Instance.AddTimeSlowEffect(
            $"Hitstop_{attack.animationTrigger}",
            hitstopTimeScale,
            hitstopDuration
        );

        //Debug.Log($"HitEffects: Hitstop triggered for {hitstopDuration}s at {hitstopTimeScale}x speed");
    }

    /// <summary>
    /// Manually trigger a hitstop effect with custom parameters
    /// </summary>
    public void TriggerCustomHitstop(float duration, float timeScale = 0.05f)
    {
        if (!enableHitstop || TimeSlowManager.Instance == null)
            return;

        if (currentHitstopId >= 0)
        {
            TimeSlowManager.Instance.RemoveTimeSlowEffect(currentHitstopId);
        }

        currentHitstopId = TimeSlowManager.Instance.AddTimeSlowEffect(
            "CustomHitstop",
            timeScale,
            duration
        );
    }

    public void CancelHitstop()
    {
        if (currentHitstopId >= 0 && TimeSlowManager.Instance != null)
        {
            TimeSlowManager.Instance.RemoveTimeSlowEffect(currentHitstopId);
            currentHitstopId = -1;
        }
    }
    #endregion

    #region Update
    private void FixedUpdate()
    {
        // Update camera shake timer
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.fixedDeltaTime;

            if (shakeTimer <= 0f)
            {
                // Timer expired, reset shake
                if (noise != null)
                {
                    noise.AmplitudeGain = 0f;
                    noise.FrequencyGain = 0f;
                }
                shakeTimer = 0f;
            }
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Manually trigger camera shake with default settings
    /// </summary>
    public void TriggerLightShake()
    {
        Shake(lightHitDuration, lightHitAmplitude, lightHitFrequency);
    }

    /// <summary>
    /// Manually trigger heavy camera shake
    /// </summary>
    public void TriggerHeavyShake()
    {
        Shake(heavyHitDuration, heavyHitAmplitude, heavyHitFrequency);
    }

    /// <summary>
    /// Manually trigger finisher camera shake
    /// </summary>
    public void TriggerFinisherShake()
    {
        Shake(finisherHitDuration, finisherHitAmplitude, finisherHitFrequency);
    }
    #endregion
}
