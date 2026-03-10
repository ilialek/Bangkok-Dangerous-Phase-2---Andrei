using UnityEngine;

/// <summary>
/// Manages time scale modifications from multiple sources (items, abilities, etc.)
/// Ensures the slowest active time scale is applied and properly restored
/// </summary>
public class TimeSlowManager : MonoBehaviour
{
    public static TimeSlowManager Instance;

    private System.Collections.Generic.List<TimeSlowEffect> activeEffects = new System.Collections.Generic.List<TimeSlowEffect>();
    private const float normalTimeScale = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Update all active effects and remove expired ones
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].remainingDuration -= Time.unscaledDeltaTime;
            
            if (activeEffects[i].remainingDuration <= 0f)
            {
                //Debug.Log($"TimeSlow effect '{activeEffects[i].source}' expired");
                activeEffects.RemoveAt(i);
            }
        }

        // Apply the slowest active time scale
        UpdateTimeScale();
    }

    /// <summary>
    /// Adds a new time slow effect. Returns an ID that can be used to cancel it early.
    /// </summary>
    public int AddTimeSlowEffect(string source, float timeScale, float duration)
    {
        TimeSlowEffect effect = new TimeSlowEffect
        {
            id = GetNextId(),
            source = source,
            timeScale = Mathf.Clamp01(timeScale),
            remainingDuration = duration
        };

        activeEffects.Add(effect);
        //Debug.Log($"TimeSlow added: '{source}' at {timeScale}x for {duration}s");
        UpdateTimeScale();
        
        return effect.id;
    }

    /// <summary>
    /// Removes a specific time slow effect by ID
    /// </summary>
    public void RemoveTimeSlowEffect(int id)
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i].id == id)
            {
                Debug.Log($"TimeSlow removed: '{activeEffects[i].source}'");
                activeEffects.RemoveAt(i);
                UpdateTimeScale();
                return;
            }
        }
    }

    private void UpdateTimeScale()
    {
        if (activeEffects.Count == 0)
        {
            // No active effects, restore normal time
            if (Time.timeScale != normalTimeScale)
            {
                Time.timeScale = normalTimeScale;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                //Debug.Log("TimeSlow: Restored to normal time");
            }
        }
        else
        {
            // Find the slowest active time scale
            float slowest = 1f;
            foreach (var effect in activeEffects)
            {
                if (effect.timeScale < slowest)
                {
                    slowest = effect.timeScale;
                }
            }

            // Apply the slowest time scale
            if (Mathf.Abs(Time.timeScale - slowest) > 0.001f)
            {
                Time.timeScale = slowest;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                //Debug.Log($"TimeSlow: Applied {slowest}x time scale");
            }
        }
    }

    private int nextId = 1;
    private int GetNextId()
    {
        return nextId++;
    }

    private class TimeSlowEffect
    {
        public int id;
        public string source;
        public float timeScale;
        public float remainingDuration;
    }
}
