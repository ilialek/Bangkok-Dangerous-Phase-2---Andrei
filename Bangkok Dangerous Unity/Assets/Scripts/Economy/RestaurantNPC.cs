using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RestaurantNPC : MonoBehaviour
{
    [SerializeField] private float interactionRange = 5f;
    [SerializeField] private KeyCode interactKey = KeyCode.Q;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Transform itemButtonContainer; // Where to create buttons
    [SerializeField] private HealingItemData[] foodItems; // Food items to sell
    [SerializeField] private int[] foodPrices; // Price for each food item
    [SerializeField] private Font buttonTextFont; // Font for button text
    [SerializeField] private Text moneyAmountText; // Text to display player money
    [SerializeField] private GameObject tooltipPanel; // Panel for hover tooltip
    [SerializeField] private Text tooltipText; // Text component in tooltip
    [SerializeField] private GameObject interactionPrompt; // UI prompt to press Q/A
    [SerializeField] private GameObject cutscenePlaceholderPanel; // Cutscene placeholder panel shown after ordering
    
    private Player player;
    private bool menuOpen = false;
    private MonoBehaviour[] playerScriptsToDisable;
    
    // Static list to track all active RestaurantNPC instances
    private static System.Collections.Generic.List<RestaurantNPC> activeRestaurants = new System.Collections.Generic.List<RestaurantNPC>();

    private void Start()
    {
        player = FindObjectOfType<Player>();
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // Register this restaurant
        activeRestaurants.Add(this);

        // Cache player scripts (excluding UI management scripts)
        if (player != null)
        {
            var allScripts = player.GetComponents<MonoBehaviour>();
            var scriptsToCache = new System.Collections.Generic.List<MonoBehaviour>();
            
            foreach (var script in allScripts)
            {
                if (script != this && 
                    script.GetType().Name != "InteractionPromptManager" && 
                    !script.GetType().Name.Contains("UI") &&
                    !script.GetType().Name.Contains("Canvas"))
                {
                    scriptsToCache.Add(script);
                }
            }
            playerScriptsToDisable = scriptsToCache.ToArray();
        }
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
        if (cutscenePlaceholderPanel != null)
            cutscenePlaceholderPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // Unregister this restaurant when destroyed
        activeRestaurants.Remove(this);
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        bool inRange = distance <= interactionRange;

        // Show/hide interaction prompt based on range from ANY restaurant
        if (interactionPrompt != null && !menuOpen)
        {
            bool anyRestaurantInRange = false;
            foreach (RestaurantNPC restaurant in activeRestaurants)
            {
                if (restaurant != null && restaurant.IsPlayerInRange())
                {
                    anyRestaurantInRange = true;
                    break;
                }
            }
            interactionPrompt.SetActive(anyRestaurantInRange);
        }

        // Press Q or A (Xbox) to open menu
        if (inRange && (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.JoystickButton0)) && !menuOpen)
        {
            menuOpen = true;
            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                
                // Hide interaction prompt
                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(false);
                }
                
                // Update money display
                UpdateMoneyDisplay();
                
                // Create dynamic buttons for food items
                CreateFoodButtons();
                
                // Disable player input/movement and camera
                if (playerScriptsToDisable != null)
                {
                    foreach (MonoBehaviour mb in playerScriptsToDisable)
                    {
                        if (mb != null)
                            mb.enabled = false;
                    }
                }
            }
        }

        // Close menu with Escape
        if (menuOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            menuOpen = false;
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                
                // Show interaction prompt again
                if (interactionPrompt != null && player != null)
                {
                    distance = Vector3.Distance(transform.position, player.transform.position);
                    interactionPrompt.SetActive(distance <= interactionRange);
                }
                
                // Re-enable player input/movement
                if (playerScriptsToDisable != null)
                {
                    foreach (MonoBehaviour mb in playerScriptsToDisable)
                    {
                        if (mb != null)
                            mb.enabled = true;
                    }
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    private void UpdateMoneyDisplay()
    {
        if (moneyAmountText != null)
        {
            SimpleProgression progression = SimpleProgression.Instance;
            if (progression != null)
            {
                moneyAmountText.text = "฿" + progression.GetScore().ToString();
            }
        }
    }

    private void CreateFoodButtons()
    {
        if (itemButtonContainer == null)
        {
            Debug.LogError("RestaurantNPC: itemButtonContainer not assigned! Assign the button container in the inspector.");
            return;
        }

        if (foodItems == null || foodItems.Length == 0)
        {
            Debug.LogError("RestaurantNPC: foodItems not assigned or empty!");
            return;
        }

        // Clear existing buttons
        foreach (Transform child in itemButtonContainer)
            Destroy(child.gameObject);

        // Create button for each food item
        for (int i = 0; i < foodItems.Length; i++)
        {
            try
            {
                HealingItemData food = foodItems[i];
                if (food == null) continue;

                string itemName = food.itemName ?? "Unknown Food";
                GameObject buttonGO = new GameObject($"FoodButton_{itemName}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                buttonGO.transform.SetParent(itemButtonContainer, false);

                // Setup layout
                LayoutElement le = buttonGO.GetComponent<LayoutElement>();
                le.preferredHeight = 200f;
                le.preferredWidth = 200f;

                // Setup button visuals
                Image img = buttonGO.GetComponent<Image>();
                img.color = Color.white;
                
                // Set button sprite from food item
                if (food.itemSprite != null)
                {
                    img.sprite = food.itemSprite;
                    img.type = Image.Type.Simple;
                }

                Button btn = buttonGO.GetComponent<Button>();
                ColorBlock colors = btn.colors;
                colors.normalColor = new Color(0.6f, 0.6f, 0.6f); // Darker until hover
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(0.4f, 0.4f, 0.4f);
                btn.colors = colors;

                // Create child GameObject for text
                GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
                textGO.transform.SetParent(buttonGO.transform, false);
                RectTransform textRT = textGO.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = new Vector2(10, 5);
                textRT.offsetMax = new Vector2(-10, -5);

                // Add text component
                Text buttonText = textGO.AddComponent<Text>();
                if (buttonText == null)
                {
                    Debug.LogError("Failed to add Text component!");
                    Destroy(textGO);
                    continue;
                }

                int price = (foodPrices != null && i < foodPrices.Length) ? foodPrices[i] : 50; // Default 50 if not set
                
                // Display: name and price
                string displayText = itemName + "\n<color=#00FF00>฿" + price.ToString() + "</color>";
                buttonText.text = displayText;
                
                if (buttonTextFont != null)
                {
                    buttonText.font = buttonTextFont;
                }
                
                buttonText.fontSize = 18;
                buttonText.alignment = TextAnchor.MiddleCenter;
                buttonText.color = Color.white;

                // Add hover events to show tooltip
                int foodIndex = i;
                EventTrigger trigger = buttonGO.AddComponent<EventTrigger>();
                
                // On pointer enter (hover)
                EventTrigger.Entry entryEnter = new EventTrigger.Entry();
                entryEnter.eventID = EventTriggerType.PointerEnter;
                entryEnter.callback.AddListener((data) => { 
                    RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
                    ShowTooltip(foodIndex, buttonRect); 
                });
                trigger.triggers.Add(entryEnter);
                
                // On pointer exit (stop hover)
                EventTrigger.Entry entryExit = new EventTrigger.Entry();
                entryExit.eventID = EventTriggerType.PointerExit;
                entryExit.callback.AddListener((data) => { HideTooltip(); });
                trigger.triggers.Add(entryExit);

                // Add click handler
                btn.onClick.AddListener(() => OrderFood(foodIndex));
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error creating food button: " + ex.Message);
            }
        }
    }

    private void ShowTooltip(int foodIndex, RectTransform buttonRect)
    {
        if (tooltipPanel == null || tooltipText == null) return;
        if (foodIndex < 0 || foodIndex >= foodItems.Length) return;

        HealingItemData food = foodItems[foodIndex];
        int healAmount = food.healAmount;
        float healDuration = food.healDuration;
        float speedMultiplier = food.speedMultiplier;
        float damageMultiplier = food.damageMultiplier;

        string tooltipContent = "";
        
        // Add description if available
        if (!string.IsNullOrEmpty(food.itemDescription))
        {
            tooltipContent += "<i>" + food.itemDescription + "</i>\n\n";
        }
        
        tooltipContent += "<b>Effects:</b>\n";
        
        // Only add heal if there's healing
        if (healAmount > 0)
        {
            tooltipContent += "<color=#00FF00>Heal: +" + healAmount.ToString() + " HP</color>\n";
        }
        
        // Only add duration if there's a duration
        if (healDuration > 0)
        {
            tooltipContent += "Duration: " + healDuration.ToString("F1") + "s\n";
        }
        
        // Only add speed if multiplier is greater than 1
        if (speedMultiplier > 1f)
        {
            float speedIncrease = (speedMultiplier - 1f) * 100f;
            tooltipContent += "<color=#0000FF>+" + speedIncrease.ToString("F0") + "% faster</color>\n";
        }
        
        // Only add damage if multiplier is greater than 1
        if (damageMultiplier > 1f)
        {
            float damageIncrease = (damageMultiplier - 1f) * 100f;
            tooltipContent += "<color=#FF0000>+" + damageIncrease.ToString("F0") + "% more damage</color>\n";
        }
        
        tooltipText.text = tooltipContent;
        tooltipPanel.SetActive(true);
        
        // Position tooltip diagonally below the button
        if (buttonRect != null)
        {
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
                Vector3 buttonPos = buttonRect.position;
                float offsetX = 180f;
                float offsetY = -180f;
                tooltipRect.position = new Vector3(buttonPos.x + offsetX, buttonPos.y + offsetY, buttonPos.z);
            }
        }
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    private void OrderFood(int foodIndex)
    {
        if (foodIndex < 0 || foodIndex >= foodItems.Length)
            return;

        HealingItemData food = foodItems[foodIndex];
        string itemName = food.itemName ?? "Unknown Food";
        
        // Get price
        int price = (foodPrices != null && foodIndex < foodPrices.Length) ? foodPrices[foodIndex] : 50;
        
        // Check if player has enough money
        SimpleProgression progression = SimpleProgression.Instance;
        if (progression == null)
        {
            Debug.LogError("SimpleProgression not found!");
            return;
        }
        
        if (progression.GetScore() < price)
        {
            Debug.Log("Not enough money to order " + itemName);
            return;
        }
        
        // Deduct money
        progression.SpendMoney(price);
        
        // Update money display
        UpdateMoneyDisplay();
        
        // Immediately consume the food (apply its effects)
        PlayerHealthSystem healthSystem = player.GetComponent<PlayerHealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.ConsumeHealingItem(food);
        }
        else
        {
            Debug.LogError("PlayerHealthSystem not found!");
        }

        // Close menu and show cutscene placeholder
        StartCoroutine(ShowCutscenePlaceholder());
    }

    private System.Collections.IEnumerator ShowCutscenePlaceholder()
    {
        // Close the menu
        menuOpen = false;
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        // Show cutscene placeholder
        if (cutscenePlaceholderPanel != null)
        {
            cutscenePlaceholderPanel.SetActive(true);
        }

        // Wait for 5 seconds
        yield return new WaitForSeconds(5f);

        // Close cutscene placeholder
        if (cutscenePlaceholderPanel != null)
        {
            cutscenePlaceholderPanel.SetActive(false);
        }

        // Re-enable player scripts
        if (playerScriptsToDisable != null)
        {
            foreach (MonoBehaviour mb in playerScriptsToDisable)
            {
                if (mb != null)
                    mb.enabled = true;
            }
        }

        // Show interaction prompt if still in range
        if (interactionPrompt != null && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            interactionPrompt.SetActive(distance <= interactionRange);
        }

        // Restore cursor state
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Helper method to check if player is in range
    private bool IsPlayerInRange()
    {
        if (player == null) return false;
        float distance = Vector3.Distance(transform.position, player.transform.position);
        return distance <= interactionRange;
    }
}
