using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopNPC : MonoBehaviour
{
    [SerializeField] private float interactionRange = 5f;
    [SerializeField] private KeyCode interactKey = KeyCode.Q;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Transform itemButtonContainer; // Where to create buttons
    [SerializeField] private HealingItemData[] shopItems; // Items to sell
    [SerializeField] private int[] itemPrices; // Price for each item
    [SerializeField] private Font buttonTextFont; // Font for button text
    [SerializeField] private Text moneyAmountText; // Text to display player money
    [SerializeField] private GameObject tooltipPanel; // Panel for hover tooltip
    [SerializeField] private Text tooltipText; // Text component in tooltip
    [SerializeField] private GameObject interactionPrompt; // UI prompt to press A/Q
    [SerializeField] private float discountChance = 0.3f; // 30% chance for a discount
    [SerializeField] private float discountPercentage = 0.25f; // 25% off the item
    
    private Player player;
    private bool menuOpen = false;
    private MonoBehaviour[] playerScriptsToDisable;
    
    // Discount tracking
    private int discountedItemIndex = -1; // -1 means no discount
    private int discountedPrice = 0;
    
    // Static list to track all active ShopNPC instances
    private static System.Collections.Generic.List<ShopNPC> activeShops = new System.Collections.Generic.List<ShopNPC>();

    private void Start()
    {
        player = FindObjectOfType<Player>();
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // Register this shop
        activeShops.Add(this);
        
        // Initialize discount for this shop
        ApplyRandomDiscount();

        // Cache player scripts (excluding this ShopNPC and any UI management scripts)
        if (player != null)
        {
            var allScripts = player.GetComponents<MonoBehaviour>();
            var scriptsToCache = new System.Collections.Generic.List<MonoBehaviour>();
            
            foreach (var script in allScripts)
            {
                // Don't disable ShopNPC, UI scripts, or PersistentText
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
    }

    private void OnDestroy()
    {
        // Unregister this shop when destroyed
        activeShops.Remove(this);
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        bool inRange = distance <= interactionRange;

        // Show/hide interaction prompt based on range from ANY shop
        if (interactionPrompt != null && !menuOpen)
        {
            // Check if player is in range of ANY shop
            bool anyShopInRange = false;
            foreach (ShopNPC shop in activeShops)
            {
                if (shop != null && shop.IsPlayerInRange())
                {
                    anyShopInRange = true;
                    break;
                }
            }
            interactionPrompt.SetActive(anyShopInRange);
        }

        // Press Q or A (Xbox) to open menu
        if (inRange && (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.JoystickButton0)) && !menuOpen)
        {
            menuOpen = true;
            Time.timeScale = 0f; // Freeze game time
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
                
                // Create dynamic buttons for shop items
                CreateShopButtons();
                
                // Disable player input/movement and camera (but not UI scripts)
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
            Time.timeScale = 1f; // Resume game time
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
        Gizmos.color = Color.green;
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

    private void CreateShopButtons()
    {
        if (itemButtonContainer == null)
        {
            Debug.LogError("ShopNPC: itemButtonContainer not assigned! Assign the button container in the inspector.");
            return;
        }

        if (shopItems == null || shopItems.Length == 0)
        {
            Debug.LogError("ShopNPC: shopItems not assigned or empty!");
            return;
        }

        // Clear existing buttons
        foreach (Transform child in itemButtonContainer)
            Destroy(child.gameObject);

        // Create button for each item
        for (int i = 0; i < shopItems.Length; i++)
        {
            try
            {
                HealingItemData item = shopItems[i];
                if (item == null) continue;

                string itemName = item.itemName ?? "Unknown Item";
                GameObject buttonGO = new GameObject($"ItemButton_{itemName}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                buttonGO.transform.SetParent(itemButtonContainer, false);

                // Setup layout
                LayoutElement le = buttonGO.GetComponent<LayoutElement>();
                le.preferredHeight = 200f;
                le.preferredWidth = 200f;

                // Setup button visuals
                Image img = buttonGO.GetComponent<Image>();
                img.color = Color.white;
                
                // Set button sprite from item
                if (item.itemSprite != null)
                {
                    img.sprite = item.itemSprite;
                    img.type = Image.Type.Simple;
                }

                Button btn = buttonGO.GetComponent<Button>();
                ColorBlock colors = btn.colors;
                colors.normalColor = new Color(0.6f, 0.6f, 0.6f); // Darker until hover
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(0.4f, 0.4f, 0.4f);
                btn.colors = colors;

                // Create child GameObject for text (can't have both Image and Text on same GO)
                GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
                textGO.transform.SetParent(buttonGO.transform, false);
                RectTransform textRT = textGO.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = new Vector2(10, 5); // Small padding on left and bottom
                textRT.offsetMax = new Vector2(-10, -5); // Small padding on right and top

                // Add text component
                Text buttonText = textGO.AddComponent<Text>();
                if (buttonText == null)
                {
                    Debug.LogError("Failed to add Text component!");
                    Destroy(textGO);
                    continue;
                }

                int price = (itemPrices != null && i < itemPrices.Length) ? itemPrices[i] : 50; // Default 50 if not set
                
                // Check if this item has a discount
                if (i == discountedItemIndex)
                {
                    price = discountedPrice;
                    // Simple display: just name and price with discount indicator
                    string displayText = itemName + "\n<color=#FFD700>฿" + price.ToString() + " (SALE!)</color>";
                    buttonText.text = displayText;
                }
                else
                {
                    // Simple display: just name and price
                    string displayText = itemName + "\n<color=#00FF00>฿" + price.ToString() + "</color>";
                    buttonText.text = displayText;
                }
                
                if (buttonTextFont != null)
                {
                    buttonText.font = buttonTextFont;
                }
                
                buttonText.fontSize = 18;
                buttonText.alignment = TextAnchor.MiddleCenter;
                buttonText.color = Color.white;

                // Add hover events to show tooltip
                int itemIndex = i;
                EventTrigger trigger = buttonGO.AddComponent<EventTrigger>();
                
                // On pointer enter (hover)
                EventTrigger.Entry entryEnter = new EventTrigger.Entry();
                entryEnter.eventID = EventTriggerType.PointerEnter;
                entryEnter.callback.AddListener((data) => { 
                    RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
                    ShowTooltip(itemIndex, buttonRect); 
                });
                trigger.triggers.Add(entryEnter);
                
                // On pointer exit (stop hover)
                EventTrigger.Entry entryExit = new EventTrigger.Entry();
                entryExit.eventID = EventTriggerType.PointerExit;
                entryExit.callback.AddListener((data) => { HideTooltip(); });
                trigger.triggers.Add(entryExit);

                // Add click handler
                btn.onClick.AddListener(() => BuyItem(itemIndex));
            }
            catch (System.Exception ex)
            {
            }
        }
    }

    private void ShowTooltip(int itemIndex, RectTransform buttonRect)
    {
        if (tooltipPanel == null || tooltipText == null) return;
        if (itemIndex < 0 || itemIndex >= shopItems.Length) return;

        HealingItemData item = shopItems[itemIndex];
        int healAmount = item.healAmount;
        float healDuration = item.healDuration;
        float speedMultiplier = item.speedMultiplier;
        float damageMultiplier = item.damageMultiplier;

        string tooltipContent = "";
        
        // Add description if available
        if (!string.IsNullOrEmpty(item.itemDescription))
        {
            tooltipContent += "<i>" + item.itemDescription + "</i>\n\n";
        }
        
        tooltipContent += "<b>Stats:</b>\n";
        
        // Only add heal if there's healing (always green)
        if (healAmount > 0)
        {
            tooltipContent += "<color=#00FF00>Heal: +" + healAmount.ToString() + " HP</color>\n";
        }
        
        // Only add duration if there's a duration
        if (healDuration > 0)
        {
            tooltipContent += "Duration: " + healDuration.ToString("F1") + "s\n";
        }
        
        // Only add speed if multiplier is greater than 1 (increase) - blue
        if (speedMultiplier > 1f)
        {
            float speedIncrease = (speedMultiplier - 1f) * 100f;
            tooltipContent += "<color=#0000FF>+" + speedIncrease.ToString("F0") + "% faster</color>\n";
        }
        
        // Only add damage if multiplier is greater than 1 (increase) - red
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
                // Get button position and offset diagonally (right and down)
                Vector3 buttonPos = buttonRect.position;
                float offsetX = 180f; // Move right
                float offsetY = -180f; // Move down
                tooltipRect.position = new Vector3(buttonPos.x + offsetX, buttonPos.y + offsetY, buttonPos.z);
            }
        }
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    private void BuyItem(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= shopItems.Length)
            return;

        HealingItemData item = shopItems[itemIndex];
        string itemName = item.itemName ?? "Unknown Item";
        
        // Get price (use discounted price if available)
        int price = (itemPrices != null && itemIndex < itemPrices.Length) ? itemPrices[itemIndex] : 50;
        if (itemIndex == discountedItemIndex)
        {
            price = discountedPrice;
        }
        
        // Check if player has enough money
        SimpleProgression progression = SimpleProgression.Instance;
        if (progression == null)
        {
            Debug.LogError("SimpleProgression not found!");
            return;
        }
        
        if (progression.GetScore() < price)
        {
            return;
        }
        
        // Deduct money
        progression.SpendMoney(price);
        
        // Update money display
        UpdateMoneyDisplay();
        
        // Give item to player
        PlayerHealthSystem healthSystem = player.GetComponent<PlayerHealthSystem>();
        if (healthSystem != null)
        {
            if (healthSystem.TryAddHealingItem(item))
            {
            }
            else
            {
                // Refund if inventory is full
                progression.AddMoney(price);
                UpdateMoneyDisplay();
            }
        }
        else
        {
            Debug.LogError("PlayerHealthSystem not found!");
        }
    }

    // Helper method to check if player is in range
    private bool IsPlayerInRange()
    {
        if (player == null) return false;
        float distance = Vector3.Distance(transform.position, player.transform.position);
        return distance <= interactionRange;
    }

    // Apply a random discount to one item in the shop
    private void ApplyRandomDiscount()
    {
        // Check if we should apply a discount
        if (Random.value > discountChance)
        {
            discountedItemIndex = -1;
            return;
        }
        
        // Pick a random item to discount
        if (shopItems != null && shopItems.Length > 0)
        {
            discountedItemIndex = Random.Range(0, shopItems.Length);
            
            // Calculate discounted price
            int originalPrice = (itemPrices != null && discountedItemIndex < itemPrices.Length) 
                ? itemPrices[discountedItemIndex] 
                : 50;
            discountedPrice = Mathf.Max(1, Mathf.RoundToInt(originalPrice * (1f - discountPercentage)));
        }
    }

    // Check if any menu is open
    private static bool IsAnyMenuOpen()
    {
        foreach (ShopNPC shop in activeShops)
        {
            if (shop != null && shop.menuOpen)
                return true;
        }
        return false;
    }
}
