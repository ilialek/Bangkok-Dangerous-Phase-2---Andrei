using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Cinemachine;

/// <summary>
/// TimeSlow system that slows down the entire game (including player, enemies, physics, animations)
/// when an enemy is killed. Uses Time.timeScale to affect all delta time-based calculations.
/// </summary>
public class TimeSlow : MonoBehaviour
{
    [Header("Time Slow Settings")]
    [SerializeField] private float slowMotionTimeScale = 0.3f;
    [SerializeField] private float slowMotionDuration = 1.0f;
    [SerializeField] private float slowMotionZoom = 15f;

    private Coroutine timeSlowCoroutine;
    private Volume timeSlowVolume;
    private CinemachineCamera cinemachineCamera;

    private void OnEnable()
    {
        EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
    }

    private void OnDisable()
    {
        EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
    }

    private void Start()
    {
        // Get the volume component from this GameObject
        timeSlowVolume = GetComponent<Volume>();
        
        // Get the cinemachine camera from the scene
        cinemachineCamera = FindObjectOfType<CinemachineCamera>();
    }

    private void OnEnemyKilled(EnemyKilledEvent evt)
    {
        // Trigger time slow on enemy kill
        if (timeSlowCoroutine != null)
        {
            StopCoroutine(timeSlowCoroutine);
        }
        timeSlowCoroutine = StartCoroutine(TimeSlowRoutine());
    }

    private IEnumerator TimeSlowRoutine()
    {
        // Store original camera FOV
        float originalFOV = 60f;
        if (cinemachineCamera != null)
        {
            originalFOV = cinemachineCamera.Lens.FieldOfView;
        }
        
        // Apply time slow - affects EVERYTHING using Time.deltaTime:
        // - Player movement and animations
        // - Enemy movement and animations
        // - Physics
        // - Particle systems
        // - All animations
        Time.timeScale = slowMotionTimeScale;
        
        // Set camera zoom to slowmotion zoom value
        if (cinemachineCamera != null)
        {
            var lens = cinemachineCamera.Lens;
            lens.FieldOfView = slowMotionZoom;
            cinemachineCamera.Lens = lens;
        }
        
        // Enable time slow volume
        if (timeSlowVolume != null)
        {
            timeSlowVolume.enabled = true;
        }
        
        Debug.Log($"TimeSlow: Game time scaled to {slowMotionTimeScale}x for {slowMotionDuration}s (affects player, enemies, physics, and all animations)");

        // Wait for the duration (using unscaled delta time so it's not affected by time scale)
        yield return new WaitForSecondsRealtime(slowMotionDuration);

        // Restore normal time
        Time.timeScale = 1f;
        
        // Restore camera FOV
        if (cinemachineCamera != null)
        {
            var lens = cinemachineCamera.Lens;
            lens.FieldOfView = originalFOV;
            cinemachineCamera.Lens = lens;
        }
        
        // Disable time slow volume
        if (timeSlowVolume != null)
        {
            timeSlowVolume.enabled = false;
        }
        
        Debug.Log("TimeSlow: Time scale restored to 1x - all game systems back to normal speed");
        timeSlowCoroutine = null;
    }
}
