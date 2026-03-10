using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Very small progression/score manager used for playtests.
/// Subscribes to EnemyKilledEvent and awards a point per kill.
/// Optionally shows the score in a UI Text component.
/// </summary>
public class SimpleProgression : MonoBehaviour
{
    [Header("Score")]
    [Tooltip("Current player score (points awarded per enemy kill)")]
    [SerializeField] public int score = 0;

    [Header("Score Display")]
    [Tooltip("UI Text component to display total score")]
    [SerializeField] private Text scoreDisplayText;
    [Tooltip("UI Text component to display last kill earned (temporary)")]
    [SerializeField] private Text lastKillEarnedText;
    [Tooltip("Time to show the last kill earned before adding to total")]
    [SerializeField] private float lastKillDisplayDuration = 2f;
    [Tooltip("Minimum score to award per kill")]
    [SerializeField] private int minScorePerKill = 70;
    [Tooltip("Maximum score to award per kill")]
    [SerializeField] private int maxScorePerKill = 200;

    // singleton for easy access
    public static SimpleProgression Instance { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize score display
        if (scoreDisplayText != null)
        {
            scoreDisplayText.text = "฿ " + score.ToString();
        }
    }

    private void LateUpdate()
    {
        // Simple protection: just ensure Text component is enabled
        if (scoreDisplayText != null && !scoreDisplayText.enabled)
        {
            scoreDisplayText.enabled = true;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
    }

    protected void OnDisable()
    {
        // Safely unsubscribe from events
        EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
    }

    private void OnEnemyKilled(EnemyKilledEvent evt)
    {
        // Award random score between min and max
        int scoreAwarded = Random.Range(minScorePerKill, maxScorePerKill + 1);
        
        // Show the earned amount in last kill text
        if (lastKillEarnedText != null)
        {
            lastKillEarnedText.text = "+ ฿" + scoreAwarded.ToString();
        }
        
        // Update total score after delay
        StartCoroutine(AddScoreAfterDelay(scoreAwarded));

        Debug.Log($"SimpleProgression: enemy killed ({evt.enemy?.name ?? "unknown"}). Score awarded: {scoreAwarded}. Total will be: {score + scoreAwarded}");
    }

    private System.Collections.IEnumerator AddScoreAfterDelay(int scoreAmount)
    {
        yield return new WaitForSeconds(lastKillDisplayDuration);
        
        score += scoreAmount;
        
        // Update score display
        if (scoreDisplayText != null)
        {
            scoreDisplayText.text = "฿ " + score.ToString();
        }

        // Clear the last kill earned text (don't disable the gameobject)
        if (lastKillEarnedText != null)
        {
            lastKillEarnedText.text = "";
        }
    }

    /// <summary>
    /// Spawns a floating text popup showing the score at a world position.
    /// The text drifts upward and fades out over time.
    /// </summary>
    private void ShowFloatingScore(int scoreAmount, Vector3 worldPosition)
    {
        // Create a new GameObject with Text component
        GameObject popup = new GameObject("FloatingScore");
        
        // Try to parent to Canvas if available, otherwise use world space
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            popup.transform.SetParent(canvas.transform, false);
        }
        
        // Add Text component
        Text textComponent = popup.AddComponent<Text>();
        textComponent.text = "Dropped: ฿" + scoreAmount.ToString();
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = 40;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.color = new Color(0f, 1f, 0f, 1f); // Green for money
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        // Add outline for better visibility
        Outline outline = popup.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);
        
        // Get RectTransform and position it at the world position
        RectTransform rectTransform = popup.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 100);
        
        // Convert world position to canvas position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        rectTransform.position = screenPos;
        
        // Animate the popup
        StartCoroutine(AnimateFloatingScore(popup, 2f));
    }

    private System.Collections.IEnumerator AnimateFloatingScore(GameObject popup, float duration)
    {
        RectTransform rectTransform = popup.GetComponent<RectTransform>();
        Text textComponent = popup.GetComponent<Text>();
        
        float elapsedTime = 0f;
        Vector3 startPos = rectTransform.localPosition;
        Color startColor = textComponent.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // Drift upward
            rectTransform.localPosition = startPos + Vector3.up * 50f * progress;

            // Fade out
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(1f, 0f, progress);
            textComponent.color = newColor;

            yield return null;
        }

        // Clean up
        Destroy(popup);
    }

    /// <summary>
    /// Gets the current score (money)
    /// </summary>
    public int GetScore()
    {
        return score;
    }

    /// <summary>
    /// Adds money to the score
    /// </summary>
    public void AddMoney(int amount)
    {
        score += amount;
        
        // Update score display
        if (scoreDisplayText != null)
        {
            scoreDisplayText.text = "฿ " + score.ToString();
        }

        Debug.Log($"Player gained ฿{amount}. Total: ฿{score}");
    }

    /// <summary>
    /// Spends money from the score
    /// </summary>
    public bool SpendMoney(int amount)
    {
        if (score < amount)
            return false;

        score -= amount;
        
        // Update score display
        if (scoreDisplayText != null)
        {
            scoreDisplayText.text = "฿ " + score.ToString();
        }

        Debug.Log($"Player spent ฿{amount}. Remaining: ฿{score}");
        return true;
    }

}
