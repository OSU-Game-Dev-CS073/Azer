using UnityEngine;
using TMPro;

public class Collectibles : MonoBehaviour
{
    [Header("Collectible Settings")]
    public string itemName = "Item";
    public int value = 1;
    public CollectibleType itemType = CollectibleType.Coin;
    public GameObject uiPrefab;   // Drag the INVENTORY UI PREFAB here

    [Header("Pickup Settings")]
    public PickupMode pickupMode = PickupMode.Auto;
    public KeyCode interactKey = KeyCode.E;
    public string pickupPromptText = "Press E to pick up";

    [Header("Audio")]
    public AudioClip collectSound;

    public enum CollectibleType { Coin, Diamond, Crystal, Food, Potion, QuestItem }
    public enum PickupMode { Auto, Manual }

    private bool playerInRange;
    private Player currentPlayer;
    private GameObject promptUI;
    private bool isBeingCollected = false; // Prevent double collection

    void Start()
    {
        if (pickupMode == PickupMode.Manual)
            CreatePickupPrompt();
    }

    void Update()
    {
        if (pickupMode == PickupMode.Manual && playerInRange && currentPlayer != null)
        {
            if (promptUI != null && !promptUI.activeSelf)
                promptUI.SetActive(true);
            if (Input.GetKeyDown(interactKey))
                ManualPickup();
        }
        else
        {
            if (promptUI != null && promptUI.activeSelf)
                promptUI.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (isBeingCollected) return; // Prevent double collection
        
        Player p = other.GetComponent<Player>();
        if (p == null) return;

        if (pickupMode == PickupMode.Auto)
            Collect(p);
        else
        {
            playerInRange = true;
            currentPlayer = p;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            currentPlayer = null;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    public void ManualPickup()
    {
        if (currentPlayer != null && !isBeingCollected)
            Collect(currentPlayer);
    }

    private void Collect(Player player)
    {
        if (isBeingCollected) return;
        isBeingCollected = true;
        
        // Currency items - go directly to player money
        if (itemType == CollectibleType.Coin || itemType == CollectibleType.Diamond || itemType == CollectibleType.Crystal)
        {
            int moneyValue = value;
            if (itemType == CollectibleType.Diamond) moneyValue *= 10;
            if (itemType == CollectibleType.Crystal) moneyValue *= 5;
            player.AddMoney(moneyValue);
            
            if (collectSound != null)
                player.PlaySFX(collectSound, 0.5f, 1.5f);
                
            Destroy(gameObject);
            return;
        }
        
        // HEALTH ITEMS (Food, Potion) - add to inventory for later use
        if (itemType == CollectibleType.Food || itemType == CollectibleType.Potion)
        {
            if (uiPrefab == null)
            {
                Debug.LogError($"Collectible '{itemName}' has no uiPrefab assigned! Cannot add to inventory.");
                isBeingCollected = false;
                return;
            }

            // Ensure the UI prefab has UsableHealthItem component
            UsableHealthItem healthItem = uiPrefab.GetComponent<UsableHealthItem>();
            if (healthItem == null)
            {
                Debug.LogWarning($"Adding UsableHealthItem to {uiPrefab.name}. Set healAmount in Inspector.");
                healthItem = uiPrefab.AddComponent<UsableHealthItem>();
                healthItem.healAmount = value; // Use the 'value' field as fallback heal amount
                healthItem.displayName = itemName;
            }

            // Add to inventory
            if (InventoryController.Instance == null)
            {
                Debug.LogError("InventoryController.Instance is null!");
                isBeingCollected = false;
                return;
            }

            bool added = InventoryController.Instance.AddItem(uiPrefab, itemName, itemType);
            
            if (added)
            {
                if (collectSound != null)
                    player.PlaySFX(collectSound, 0.5f, 1.5f);
                    
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Inventory full - cannot pick up health item.");
                isBeingCollected = false;
            }
            return;
        }
        
        // Quest items and other items - add to inventory
        if (uiPrefab == null)
        {
            Debug.LogError($"Collectible '{itemName}' has no uiPrefab assigned!");
            isBeingCollected = false;
            return;
        }

        if (InventoryController.Instance == null)
        {
            Debug.LogError("InventoryController.Instance is null!");
            isBeingCollected = false;
            return;
        }

        bool success = InventoryController.Instance.AddItem(uiPrefab, itemName, itemType);
        
        if (success)
        {
            if (collectSound != null)
                player.PlaySFX(collectSound, 0.5f, 1.5f);
                
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Inventory full - cannot pick up item.");
            isBeingCollected = false;
        }
    }

    private void CreatePickupPrompt()
    {
        promptUI = new GameObject("PickupPrompt");
        promptUI.transform.SetParent(transform);
        promptUI.transform.localPosition = new Vector3(0, 1.5f, 0);
        Canvas canvas = promptUI.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = promptUI.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(2, 0.5f);
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(promptUI.transform);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = pickupPromptText;
        tmp.fontSize = 0.4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineWidth = 0.1f;
        promptUI.SetActive(false);
    }

    void OnDestroy()
    {
        if (promptUI != null) Destroy(promptUI);
    }
}