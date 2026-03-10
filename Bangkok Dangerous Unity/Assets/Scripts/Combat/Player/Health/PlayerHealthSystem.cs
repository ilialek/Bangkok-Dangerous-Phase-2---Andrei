using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthSystem : MonoBehaviour
{

    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private float invincibilityDuration = 1.0f;
    private bool isInvincible = false;
    [SerializeField] private InputBufferCombatSystem inputBufferCombatSystem;
    [Header("UI")]
    [Tooltip("Optional: assign a UI Slider to display player health")]
    [SerializeField] private Slider healthSlider;
    [Tooltip("UI Image that shows when healing is active (will pulse)")]
    [SerializeField] private Image healingIndicator;
    [Tooltip("Text to display remaining healing duration")]
    [SerializeField] private Text healingDurationText;
    [Tooltip("Full-screen UI Image for damage vignette effect (should be a red radial gradient)")]
    [SerializeField] private Image damageVignette;
    [Tooltip("How long the damage vignette fades in seconds")]
    [SerializeField] private float vignetteFadeDuration = 0.5f;
    [Tooltip("Health percentage below which vignette stays visible (0-1, e.g., 0.3 = 30%)")]
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [Header("Pickup UI")]
    [Tooltip("Two UI Image slots that stay active. They'll be empty at start and filled with the item sprite when items are picked up.")]
    [SerializeField] private Image pickupSlot1;
    [SerializeField] private Image pickupSlot2;
    [Tooltip("Sprite to show when a slot is empty (optional). If null, the Image.sprite will be set to null to appear empty.")]
    [SerializeField] private Sprite emptySprite;
    [Tooltip("Text to display stack count for slot 1")]
    [SerializeField] private Text stackCountText1;
    [Tooltip("Text to display stack count for slot 2")]
    [SerializeField] private Text stackCountText2;
    [Header("Item Usage Feedback")]
    [Tooltip("Full-screen UI Image for item usage flash effect (white flash)")]
    [SerializeField] private Image itemUsageFlash;
    [Tooltip("Duration of the item usage flash effect in seconds")]
    [SerializeField] private float itemUsageFlashDuration = 0.3f;
    [Tooltip("Audio clip to play when an item is consumed")]
    [SerializeField] private AudioClip itemUsageSound;
    [Tooltip("Scale factor for the item slot during usage feedback animation")]
    [SerializeField] private float itemSlotPeakScale = 1.2f;
    [Tooltip("Duration of the item slot scale animation in seconds")]
    [SerializeField] private float itemSlotAnimationDuration = 0.2f;
    [Header("Critical Health Settings")]
    [Tooltip("Amount of money to deduct per second while in critical health state")]
    [SerializeField] private int moneyDeductPerSecond = 10;

    private HealingItemData slot1Data = null;
    private HealingItemData slot2Data = null;
    private int slot1StackCount = 0;
    private int slot2StackCount = 0;
    private const int maxStackSize = 3;
    private int activeHealingStacks = 0;
    private float indicatorPulseSpeed = 2f; // Speed of the healing indicator pulse
    private Coroutine vignetteCoroutine;
    private AudioSource audioSource;
    
    // Critical health state
    private bool isInCriticalHealth = false;
    private float lastMoneyDeductTime = 0f;
    private SimpleProgression progressionSystem;
    
    // Active item modifiers
    private System.Collections.Generic.List<HealingItemData> activeItems = new System.Collections.Generic.List<HealingItemData>();
    private float activeSpeedMultiplier = 1f;
    private float activeDamageMultiplier = 1f;
    
    public float SpeedMultiplier => activeSpeedMultiplier;
    public float DamageMultiplier => activeDamageMultiplier;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        
        // Get reference to progression system for money deduction
        progressionSystem = FindObjectOfType<SimpleProgression>();
        if (progressionSystem == null)
        {
            Debug.LogWarning("PlayerHealthSystem: Could not find SimpleProgression in scene!");
        }
        
        // Ensure both pickup UI Images start active and empty (or use emptySprite if provided)
        if (pickupSlot1 != null)
        {
            pickupSlot1.gameObject.SetActive(true);
            pickupSlot1.sprite = emptySprite; // will be null if no emptySprite assigned
            slot1Data = null;
            slot1StackCount = 0;
        }
        if (pickupSlot2 != null)
        {
            pickupSlot2.gameObject.SetActive(true);
            pickupSlot2.sprite = emptySprite;
            slot2Data = null;
            slot2StackCount = 0;
        }

        UpdateStackCountDisplay();

        // Initialize healing indicator
        if (healingIndicator != null)
        {
            healingIndicator.gameObject.SetActive(false);
        }

        // Initialize damage vignette as fully transparent
        if (damageVignette != null)
        {
            Color vignetteColor = damageVignette.color;
            vignetteColor.a = 0f;
            damageVignette.color = vignetteColor;
            damageVignette.gameObject.SetActive(true);
        }

        // Initialize item usage flash as fully transparent
        if (itemUsageFlash != null)
        {
            Color flashColor = itemUsageFlash.color;
            flashColor.a = 0f;
            itemUsageFlash.color = flashColor;
            itemUsageFlash.gameObject.SetActive(true);
        }

        // Get or create AudioSource for item usage sound
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && itemUsageSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Press E or Y (Xbox) to consume first slot
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            TryConsumeFirstSlot();
        }
        // Press F or B (Xbox) to consume second slot
        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            TryConsumeSecondSlot();
        }

        // Update healing indicator pulse if healing is active
        if (healingIndicator != null && activeHealingStacks > 0)
        {
            float alpha = 0.5f + (Mathf.Sin(Time.time * indicatorPulseSpeed) * 0.5f);
            Color color = healingIndicator.color;
            color.a = alpha;
            healingIndicator.color = color;
        }

        // Update low-health vignette baseline when not flashing from damage
        if (damageVignette != null && vignetteCoroutine == null)
        {
            UpdateLowHealthVignette();
        }
    }

    private void UpdateLowHealthVignette()
    {
        float healthPercent = (float)currentHealth / maxHealth;
        float targetAlpha = 0f;
        
        if (healthPercent <= lowHealthThreshold)
        {
            // Keep vignette visible when health is low
            targetAlpha = Mathf.Lerp(0.4f, 0f, healthPercent / lowHealthThreshold);
        }

        Color vignetteColor = damageVignette.color;
        vignetteColor.a = Mathf.Lerp(vignetteColor.a, targetAlpha, Time.unscaledDeltaTime * 2f);
        damageVignette.color = vignetteColor;
    }

    // Try consuming the first slot when player presses E
    private void TryConsumeFirstSlot()
    {
        // If slot1 empty, nothing to consume
        if (slot1Data == null)
        {
            return;
        }

        // Play feedback effects
        PlayItemUsageFeedback(pickupSlot1);

        // Start healing over time with item data
        StartCoroutine(HealOverTime(slot1Data));

        // Decrement stack count
        slot1StackCount--;

        // Clear slot if stack is empty
        if (slot1StackCount <= 0)
        {
            if (pickupSlot1 != null)
            {
                pickupSlot1.sprite = emptySprite;
            }
            slot1Data = null;
            slot1StackCount = 0;
        }

        UpdateStackCountDisplay();
    }

    // Try consuming the second slot when player presses F
    private void TryConsumeSecondSlot()
    {
        // If slot2 empty, nothing to consume
        if (slot2Data == null)
        {
            return;
        }

        // Play feedback effects
        PlayItemUsageFeedback(pickupSlot2);

        // Start healing over time with item data
        StartCoroutine(HealOverTime(slot2Data));

        // Decrement stack count
        slot2StackCount--;

        // Clear slot if stack is empty
        if (slot2StackCount <= 0)
        {
            if (pickupSlot2 != null)
            {
                pickupSlot2.sprite = emptySprite;
            }
            slot2Data = null;
            slot2StackCount = 0;
        }

        UpdateStackCountDisplay();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("TakeDamage"))
        {
            TakeDamage(8);
        }
        //add healing item to player
        if(other.gameObject.tag == "HealingItem")
        {
            HealingItem healingItem = other.GetComponent<HealingItem>();
            if (healingItem == null || healingItem.itemData == null)
            {
                Debug.LogWarning("HealingItem has no data assigned!");
                return;
            }

            // Use TryAddHealingItem to handle stacking
            if (TryAddHealingItem(healingItem.itemData))
            {
                healingItem.OnPickedUp();
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        //take damage from enemy collision
        if (collision.gameObject.tag == "Enemy")
        {
            TakeDamage(12);
        }
    }

    public void TakeDamage(int damage)
    {
        // If at critical health (HP = 1), deduct 85 score per hit
        if (currentHealth == 1)
        {
            if (progressionSystem != null)
            {
                progressionSystem.score -= 85;
                progressionSystem.score = Mathf.Max(0, progressionSystem.score);
                Debug.Log($"At critical health! Deducted 85 score from hit. Current score: {progressionSystem.score}");
                UpdateScoreDisplay();
            }
            TriggerDamageVignette();
            return;
        }
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 1, maxHealth);

        UpdateHealthUI();
        
        // Trigger damage vignette effect
        TriggerDamageVignette();
    }

    /// <summary>
    /// <summary>
    /// Directly heal the player by a specific amount
    /// </summary>
    public void DirectHeal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthUI();
    }
    
    private void EnterCriticalHealth()
    {
        isInCriticalHealth = true;
        lastMoneyDeductTime = Time.time;
    }
    
    private void ExitCriticalHealth()
    {
        isInCriticalHealth = false;
    }
    
    private void DeductMoneyWhileCritical()
    {
        // This method is no longer used - damage is deducted directly in TakeDamage()
    }
    
    public void AddMoney(int amount)
    {
        if (progressionSystem == null)
            return;
            
        progressionSystem.score += amount;
        Debug.Log($"Added {amount} score. Current score: {progressionSystem.score}");
    }
    
    public int GetCurrentMoney() => progressionSystem != null ? progressionSystem.score : 0;
    public bool IsInCriticalHealth() => isInCriticalHealth;

    /// <summary>
    /// Attempts to add a healing item to the player's inventory
    /// </summary>
    /// <param name="itemData">The healing item data to add</param>
    /// <returns>True if successfully added, false if inventory is full</returns>
    public bool TryAddHealingItem(HealingItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError("Cannot add null healing item!");
            return false;
        }

        // Try to add to slot 1 if it has the same item and space available
        if (slot1Data == itemData && slot1StackCount < maxStackSize)
        {
            slot1StackCount++;
            UpdateStackCountDisplay();
            return true;
        }

        // Try to add to slot 2 if it has the same item and space available
        if (slot2Data == itemData && slot2StackCount < maxStackSize)
        {
            slot2StackCount++;
            UpdateStackCountDisplay();
            return true;
        }

        // Try to add to empty slot 1
        if (slot1Data == null)
        {
            if (pickupSlot1 != null)
            {
                pickupSlot1.sprite = itemData.itemSprite;
                slot1Data = itemData;
                slot1StackCount = 1;
            }
            UpdateStackCountDisplay();
            return true;
        }

        // Try to add to empty slot 2
        if (slot2Data == null)
        {
            if (pickupSlot2 != null)
            {
                pickupSlot2.sprite = itemData.itemSprite;
                slot2Data = itemData;
                slot2StackCount = 1;
            }
            UpdateStackCountDisplay();
            return true;
        }

        // Both slots are full and neither matches the item being added
        return false;
    }

    // Immediately consumes/applies a healing item's effects without adding to inventory
    // Used by RestaurantNPC to instantly consume food items
    public void ConsumeHealingItem(HealingItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError("Cannot consume null healing item!");
            return;
        }

        StartCoroutine(HealOverTime(itemData));
    }

    void Die()
    {
        if (healthSlider != null) healthSlider.value = 0;
        // Add death logic here (e.g., respawn, game over screen, etc.)
    }

    private void TriggerDamageVignette()
    {
        if (damageVignette == null) return;

        if (vignetteCoroutine != null)
            StopCoroutine(vignetteCoroutine);

        vignetteCoroutine = StartCoroutine(DamageVignetteFade());
    }

    private System.Collections.IEnumerator DamageVignetteFade()
    {
        Color vignetteColor = damageVignette.color;
        
        // Fade in quickly (to about 0.7 alpha for damage flash)
        float elapsed = 0f;
        float fadeInDuration = vignetteFadeDuration * 0.2f; // Fast fade in (20% of total)
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            vignetteColor.a = Mathf.Lerp(vignetteColor.a, 0.7f, elapsed / fadeInDuration);
            damageVignette.color = vignetteColor;
            yield return null;
        }

        // Calculate target alpha based on health
        float healthPercent = (float)currentHealth / maxHealth;
        float targetAlpha = 0f;
        
        if (healthPercent <= lowHealthThreshold)
        {
            // Keep vignette visible when health is low
            // At 0% health: 0.4 alpha, at threshold: 0 alpha, interpolated in between
            targetAlpha = Mathf.Lerp(0.4f, 0f, healthPercent / lowHealthThreshold);
        }

        // Fade out slower to target alpha (either 0 or low-health baseline)
        elapsed = 0f;
        float fadeOutDuration = vignetteFadeDuration * 0.8f; // Slower fade out (80% of total)
        float startAlpha = vignetteColor.a;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            vignetteColor.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeOutDuration);
            damageVignette.color = vignetteColor;
            yield return null;
        }

        // Set final target alpha
        vignetteColor.a = targetAlpha;
        damageVignette.color = vignetteColor;
        vignetteCoroutine = null;
    }

    private void UpdateHealthUI()
    {
        if (healthSlider == null) return;

        healthSlider.wholeNumbers = true;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    private void UpdateScoreDisplay()
    {
        // Access scoreDisplayText through reflection since it's private
        var scoreTextField = progressionSystem.GetType().GetField("scoreDisplayText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (scoreTextField != null)
        {
            var scoreDisplay = scoreTextField.GetValue(progressionSystem);
            if (scoreDisplay is Text textComponent)
            {
                textComponent.text = "฿ " + progressionSystem.score.ToString();
            }
        }
    }

    private System.Collections.IEnumerator HealOverTime(HealingItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError("HealOverTime called with null itemData!");
            yield break;
        }

        // Track this stack for the visual indicator only
        activeHealingStacks++;
        
        // Add item to active list and recalculate modifiers
        activeItems.Add(itemData);
        RecalculateModifiers();

        // Show healing indicator when at least one stack is active
        if (healingIndicator != null)
        {
            healingIndicator.gameObject.SetActive(true);
            // Set the indicator sprite to the current item image
            if (itemData.itemSprite != null)
            {
                healingIndicator.sprite = itemData.itemSprite;
            }
        }

        // Play heal particle from item data if available
        ParticleSystem spawnedParticle = null;
        
        if (itemData.activeParticleEffect != null)
        {
            // Instantiate item-specific particle
            Vector3 particlePosition = transform.position + new Vector3(0, 1, 0);
            Quaternion particleRotation = Quaternion.Euler(-90, 0, 0);
            spawnedParticle = Instantiate(itemData.activeParticleEffect, particlePosition, particleRotation, transform);
            spawnedParticle.Play();
        }

        // Apply time slow to enemies if configured using TimeSlowManager
        // (This feature has been removed)

        float timeElapsed = 0f;
        int totalHealedByThisStack = 0;
        int ticks = Mathf.Max(1, Mathf.RoundToInt(itemData.healDuration / itemData.healTickRate));
        int healPerTick = Mathf.CeilToInt((float)itemData.healAmount / ticks);

        while (timeElapsed < itemData.healDuration)
        {
            // Update duration text
            if (healingDurationText != null)
            {
                float remainingTime = itemData.healDuration - timeElapsed;
                healingDurationText.text = Mathf.Max(0, remainingTime).ToString("F1");
            }

            // Wait for next tick using realtime so time scale doesn't affect the duration
            yield return new WaitForSecondsRealtime(itemData.healTickRate);
            timeElapsed += itemData.healTickRate;

            // Only apply healing if health is below max and item still has healing left
            if (currentHealth < maxHealth && totalHealedByThisStack < itemData.healAmount)
            {
                // Calculate how much this stack should heal this tick (don't exceed remaining for this stack)
                int remainingForStack = itemData.healAmount - totalHealedByThisStack;
                int thisTickHeal = Mathf.Min(healPerTick, remainingForStack);

                // Apply healing (do not multiply by global stacks; each stack heals independently)
                int oldHealth = currentHealth;
                currentHealth = Mathf.Min(currentHealth + thisTickHeal, maxHealth);
                int actualHealed = currentHealth - oldHealth;
                totalHealedByThisStack += actualHealed;

                UpdateHealthUI();
            }
        }

        // Clean up spawned particle
        if (spawnedParticle != null)
        {
            spawnedParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(spawnedParticle.gameObject, 2f);
        }

        activeHealingStacks--;
        
        // Remove item from active list and recalculate modifiers
        activeItems.Remove(itemData);
        RecalculateModifiers();

        // Hide healing indicator if no more active stacks
        if (activeHealingStacks == 0)
        {
            if (healingIndicator != null)
            {
                healingIndicator.gameObject.SetActive(false);
            }
            if (healingDurationText != null)
            {
                healingDurationText.text = "";
            }
        }
    }

    private void RecalculateModifiers()
    {
        // When no active items, reset to defaults
        if (activeItems.Count == 0)
        {
            activeSpeedMultiplier = 1f;
            activeDamageMultiplier = 1f;
            return;
        }

        // Calculate combined modifiers from all active items
        // Using multiplicative stacking: multiple items multiply their effects
        activeSpeedMultiplier = 1f;
        activeDamageMultiplier = 1f;
        
        foreach (var item in activeItems)
        {
            activeSpeedMultiplier *= item.speedMultiplier;
            activeDamageMultiplier *= item.damageMultiplier;
        }
    }

    private void UpdateStackCountDisplay()
    {
        // Update slot 1 stack count
        if (stackCountText1 != null)
        {
            if (slot1StackCount > 0)
            {
                stackCountText1.text = slot1StackCount.ToString();
                stackCountText1.gameObject.SetActive(true);
            }
            else
            {
                stackCountText1.gameObject.SetActive(false);
            }
        }

        // Update slot 2 stack count
        if (stackCountText2 != null)
        {
            if (slot2StackCount > 0)
            {
                stackCountText2.text = slot2StackCount.ToString();
                stackCountText2.gameObject.SetActive(true);
            }
            else
            {
                stackCountText2.gameObject.SetActive(false);
            }
        }
    }

    private void PlayItemUsageFeedback(Image itemSlot)
    {
        // Play audio feedback
        if (audioSource != null && itemUsageSound != null)
        {
            audioSource.PlayOneShot(itemUsageSound);
        }

        // Play white flash feedback
        if (itemUsageFlash != null)
        {
            StartCoroutine(ItemUsageFlashEffect());
        }

        // Play item slot scale animation
        if (itemSlot != null)
        {
            StartCoroutine(ItemSlotScaleAnimation(itemSlot));
        }
    }

    private System.Collections.IEnumerator ItemUsageFlashEffect()
    {
        Color flashColor = itemUsageFlash.color;
        
        // Flash in quickly
        float elapsed = 0f;
        while (elapsed < itemUsageFlashDuration * 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 0.5f, elapsed / (itemUsageFlashDuration * 0.3f));
            flashColor.a = alpha;
            itemUsageFlash.color = flashColor;
            yield return null;
        }

        // Flash out
        elapsed = 0f;
        while (elapsed < itemUsageFlashDuration * 0.7f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0.5f, 0f, elapsed / (itemUsageFlashDuration * 0.7f));
            flashColor.a = alpha;
            itemUsageFlash.color = flashColor;
            yield return null;
        }

        // Ensure alpha is reset to 0
        flashColor.a = 0f;
        itemUsageFlash.color = flashColor;
    }

    private System.Collections.IEnumerator ItemSlotScaleAnimation(Image itemSlot)
    {
        RectTransform rectTransform = itemSlot.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            yield break;
        }

        Vector3 originalScale = rectTransform.localScale;
        Vector3 peakScale = originalScale * itemSlotPeakScale;

        // Scale up
        float elapsed = 0f;
        while (elapsed < itemSlotAnimationDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (itemSlotAnimationDuration * 0.5f);
            rectTransform.localScale = Vector3.Lerp(originalScale, peakScale, t);
            yield return null;
        }

        // Scale back down
        elapsed = 0f;
        while (elapsed < itemSlotAnimationDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (itemSlotAnimationDuration * 0.5f);
            rectTransform.localScale = Vector3.Lerp(peakScale, originalScale, t);
            yield return null;
        }

        // Ensure scale is reset to original
        rectTransform.localScale = originalScale;
    }

}
