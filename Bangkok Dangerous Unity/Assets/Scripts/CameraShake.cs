using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera cinemachine;
    [SerializeField]
    private CinemachineBasicMultiChannelPerlin noise;
    public static CameraShake Instance;

    [Header("Hit Shake Settings")]
    [SerializeField] private float hitAmplitude = 15.0f;
    [SerializeField] private float hitFrequency = 2.0f;
    [SerializeField] private float hitDuration = 0.3f;

    [Header("Kill Zoom Settings")]
    [SerializeField] private float killZoomAmount = 5f;
    [SerializeField] private float killZoomDuration = 0.5f;
    [SerializeField] private float killSideOffset = 2f;
    [SerializeField] private float killDutchAngle = 15f;
    [SerializeField] private float killOrbitalHorizontalAxis = 30f;

    private Coroutine shakeCoroutine;
    private Coroutine zoomCoroutine;
    [SerializeField]
    private CinemachineOrbitalFollow orbitalFollow;

    public void Awake()
    {
        Instance = this;
        
        // Get Cinemachine camera if not assigned
        if (cinemachine == null)
        {
            cinemachine = GetComponent<CinemachineCamera>();
        }
        
        // Get noise component if not assigned
        if (noise == null)
        {
            noise = GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
        }
        
        // Get orbital follow component if not assigned
        if (orbitalFollow == null)
        {
            orbitalFollow = GetComponentInChildren<CinemachineOrbitalFollow>();
        }
        
        // If still not found, try to find it in the scene
        if (noise == null)
        {
            noise = FindObjectOfType<CinemachineBasicMultiChannelPerlin>();
            if (noise != null)
            {
                Debug.Log("CameraShake: Found CinemachineBasicMultiChannelPerlin in scene");
            }
        }
        
        if (noise == null)
        {
            Debug.LogError("CameraShake: Failed to find CinemachineBasicMultiChannelPerlin component! Make sure it's assigned in the inspector or exists in the scene.");
        }
    }

    private void OnEnable()
    {
        EventBus<PlayerAttackHitEvent>.Subscribe(OnPlayerAttackHit);
        EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
    }

    private void OnDisable()
    {
        EventBus<PlayerAttackHitEvent>.Unsubscribe(OnPlayerAttackHit);
        EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
    }

    private void OnPlayerAttackHit(PlayerAttackHitEvent evt)
    {
        PlayHitShake();
    }

    private void OnEnemyKilled(EnemyKilledEvent evt)
    {
        PlayKillZoom();
    }

    /// <summary>
    /// Plays a kill zoom effect - camera zooms in then back out
    /// </summary>
    private void PlayKillZoom()
    {
        if (cinemachine == null)
        {
            Debug.LogWarning("CameraShake: Cinemachine camera not found!");
            return;
        }

        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
        }

        zoomCoroutine = StartCoroutine(KillZoomRoutine());
    }

    /// <summary>
    /// Triggers a camera shake when the player lands a hit
    /// Sets amplitude and frequency to specified values for the duration, then resets to 0
    /// </summary>
    public void PlayHitShake()
    {
        if (noise == null)
        {
            Debug.LogWarning("CameraShake: Cinemachine Basic Multi Channel Perlin noise component not found!");
            return;
        }

        // Stop any existing shake
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(HitShakeRoutine());
    }

    /// <summary>
    /// General purpose shake method for other systems (enemies, etc.)
    /// </summary>
    public void Shake(float duration, float amplitude)
    {
        Shake(duration, amplitude, amplitude);
    }

    /// <summary>
    /// General purpose shake method with separate amplitude and frequency control
    /// </summary>
    public void Shake(float duration, float amplitude, float frequency)
    {
        if (noise == null)
        {
            Debug.LogWarning("CameraShake: Cinemachine Basic Multi Channel Perlin noise component not found!");
            return;
        }

        // Stop any existing shake
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(GeneralShakeRoutine(duration, amplitude, frequency));
    }

    private IEnumerator HitShakeRoutine()
    {
        if (noise == null && cinemachine == null) yield break;
        
        // Store original position if we're going to shake the camera directly
        Vector3 originalPosition = cinemachine != null ? cinemachine.transform.localPosition : Vector3.zero;
        
        // Set noise gains if available
        if (noise != null)
        {
            noise.AmplitudeGain = hitAmplitude;
            noise.FrequencyGain = hitFrequency;
            Debug.Log($"CameraShake: Hit shake started via noise (amplitude: {hitAmplitude}, frequency: {hitFrequency}, duration: {hitDuration}s)");
        }
        
        // Also shake the camera transform directly for more noticeable effect
        float elapsedTime = 0f;
        while (elapsedTime < hitDuration)
        {
            if (cinemachine != null)
            {
                float x = Random.Range(-hitAmplitude * 0.01f, hitAmplitude * 0.01f);
                float y = Random.Range(-hitAmplitude * 0.01f, hitAmplitude * 0.01f);
                float z = Random.Range(-hitAmplitude * 0.01f, hitAmplitude * 0.01f);
                cinemachine.transform.localPosition = originalPosition + new Vector3(x, y, z);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset to original position and zero noise gains
        if (cinemachine != null)
        {
            cinemachine.transform.localPosition = originalPosition;
        }
        if (noise != null)
        {
            noise.AmplitudeGain = 0f;
            noise.FrequencyGain = 0f;
        }

        Debug.Log("CameraShake: Hit shake ended");
        shakeCoroutine = null;
    }

    private IEnumerator KillZoomRoutine()
    {
        Vector3 originalPosition = cinemachine.transform.localPosition;
        Quaternion originalRotation = cinemachine.transform.localRotation;
        
        // Store original orbital horizontal axis if available
        float originalOrbitalAxis = 0f;
        if (orbitalFollow != null)
        {
            originalOrbitalAxis = orbitalFollow.HorizontalAxis.Value;
        }
        
        // Calculate side offset using camera right vector (alternates left/right)
        Vector3 sideOffset = cinemachine.transform.right * killSideOffset;
        
        // Move to side and apply dutch angle
        float elapsedTime = 0f;
        float zoomInDuration = killZoomDuration * 0.5f;
        while (elapsedTime < zoomInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / zoomInDuration;
            
            cinemachine.transform.localPosition = Vector3.Lerp(originalPosition, originalPosition + sideOffset, t);
            
            // Apply dutch angle (rotation around Z axis)
            Quaternion dutchRotation = originalRotation * Quaternion.Euler(0, 0, killDutchAngle * t);
            cinemachine.transform.localRotation = dutchRotation;
            
            // Change orbital horizontal axis for side view
            if (orbitalFollow != null)
            {
                var horizontalAxis = orbitalFollow.HorizontalAxis;
                horizontalAxis.Value = Mathf.Lerp(originalOrbitalAxis, killOrbitalHorizontalAxis, t);
                orbitalFollow.HorizontalAxis = horizontalAxis;
            }
            
            yield return null;
        }

        // Move back and remove dutch angle
        elapsedTime = 0f;
        float zoomOutDuration = killZoomDuration * 0.5f;
        while (elapsedTime < zoomOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / zoomOutDuration;
            
            cinemachine.transform.localPosition = Vector3.Lerp(originalPosition + sideOffset, originalPosition, t);
            
            // Remove dutch angle
            Quaternion dutchRotation = originalRotation * Quaternion.Euler(0, 0, killDutchAngle * (1f - t));
            cinemachine.transform.localRotation = dutchRotation;
            
            // Restore original orbital horizontal axis
            if (orbitalFollow != null)
            {
                var horizontalAxis = orbitalFollow.HorizontalAxis;
                horizontalAxis.Value = Mathf.Lerp(killOrbitalHorizontalAxis, originalOrbitalAxis, t);
                orbitalFollow.HorizontalAxis = horizontalAxis;
            }
            
            yield return null;
        }

        // Ensure position, rotation, and orbital axis are reset to original
        cinemachine.transform.localPosition = originalPosition;
        cinemachine.transform.localRotation = originalRotation;
        if (orbitalFollow != null)
        {
            var horizontalAxis = orbitalFollow.HorizontalAxis;
            horizontalAxis.Value = originalOrbitalAxis;
            orbitalFollow.HorizontalAxis = horizontalAxis;
        }
        Debug.Log("CameraShake: Kill effect ended");
        zoomCoroutine = null;
    }

    private IEnumerator GeneralShakeRoutine(float duration, float amplitude, float frequency)
    {
        if (noise == null && cinemachine == null) yield break;
        
        // Store original position if we're going to shake the camera directly
        Vector3 originalPosition = cinemachine != null ? cinemachine.transform.localPosition : Vector3.zero;
        
        // Set noise gains if available
        if (noise != null)
        {
            noise.AmplitudeGain = amplitude;
            noise.FrequencyGain = frequency;
            Debug.Log($"CameraShake: Shake started via noise (amplitude: {amplitude}, frequency: {frequency}, duration: {duration}s)");
        }
        
        // Also shake the camera transform directly for more noticeable effect
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            if (cinemachine != null)
            {
                float x = Random.Range(-amplitude * 0.01f, amplitude * 0.01f);
                float y = Random.Range(-amplitude * 0.01f, amplitude * 0.01f);
                float z = Random.Range(-amplitude * 0.01f, amplitude * 0.01f);
                cinemachine.transform.localPosition = originalPosition + new Vector3(x, y, z);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset to original position and zero noise gains
        if (cinemachine != null)
        {
            cinemachine.transform.localPosition = originalPosition;
        }
        if (noise != null)
        {
            noise.AmplitudeGain = 0f;
            noise.FrequencyGain = 0f;
        }

        Debug.Log("CameraShake: Shake ended");
        shakeCoroutine = null;
    }
}
