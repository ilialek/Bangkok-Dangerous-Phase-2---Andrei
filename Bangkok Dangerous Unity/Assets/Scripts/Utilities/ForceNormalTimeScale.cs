using UnityEngine;

/// <summary>
/// Emergency script to force Time.timeScale to 1 at scene start
/// Add this to any GameObject in your scene to ensure game starts at normal speed
/// </summary>
public class ForceNormalTimeScale : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ForceTimeScaleBeforeScene()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Debug.Log("ForceNormalTimeScale - BEFORE SCENE: Set Time.timeScale to 1");
    }
    
    private void Awake()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Debug.Log("ForceNormalTimeScale - AWAKE: Set Time.timeScale to 1");
    }

    private void Start()
    {
        if (Time.timeScale != 1f)
        {
            Debug.LogError($"ForceNormalTimeScale - START: Time.timeScale was {Time.timeScale}! Forcing to 1");
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
        else
        {
            Debug.Log("ForceNormalTimeScale - START: Time.timeScale is correctly at 1");
        }
    }
}
