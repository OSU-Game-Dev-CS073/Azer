using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // <-- ADD THIS for CanvasScaler

public class ShopNPC : MonoBehaviour
{
    [Header("NPC Settings")]
    public string shopkeeperName = "Merchant";
    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 2f;

    [Header("Shop Stock")]
    public List<ShopStockItem> defaultShopStock = new List<ShopStockItem>();
    private List<ShopStockItem> currentShopStock = new List<ShopStockItem>();

    [Header("Item References")]
    public List<Item> itemReferences;  // Drag your Item prefabs here

    [System.Serializable]
    public class ShopStockItem
    {
        public int itemID;
        public int quantity;
    }

    // UI Prompt
    private GameObject promptUI;
    private bool playerInRange;

    void Start()
    {
        InitializeShop();
        CreateInteractPrompt();
    }

    private void InitializeShop()
    {
        currentShopStock = new List<ShopStockItem>();
        foreach (var item in defaultShopStock)
        {
            currentShopStock.Add(new ShopStockItem
            {
                itemID = item.itemID,
                quantity = item.quantity
            });
        }
        Debug.Log($"[ShopNPC] Initialized with {currentShopStock.Count} items");
    }

    void Update()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Check distance
        float dist = Vector2.Distance(transform.position, player.transform.position);
        playerInRange = dist <= interactRange;

        // Show/hide prompt
        if (promptUI != null)
            promptUI.SetActive(playerInRange);

        // Interact
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            Debug.Log($"[ShopNPC] Interacting with {shopkeeperName}");
            OpenShop();
        }
    }

    private void OpenShop()
    {
        if (ShopController.Instance == null)
        {
            Debug.LogError("[ShopNPC] ShopController.Instance is null! Make sure ShopController exists in the scene.");
            return;
        }

        if (ShopController.Instance.shopPanel == null)
        {
            Debug.LogError("[ShopNPC] ShopController's shopPanel is null! Assign it in the Inspector.");
            return;
        }

        Debug.Log("[ShopNPC] Opening shop...");
        ShopController.Instance.OpenShop(this);
    }

    private void CreateInteractPrompt()
    {
        promptUI = new GameObject("InteractPrompt");
        promptUI.transform.SetParent(transform);
        promptUI.transform.localPosition = new Vector3(0, 1.5f, 0);

        // Add Canvas
        Canvas canvas = promptUI.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Add Canvas Scaler for consistent size
        CanvasScaler scaler = promptUI.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        // Add RectTransform
        RectTransform canvasRect = promptUI.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1.5f, 0.4f);

        // Add Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(promptUI.transform);
        
        var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = $"[{interactKey}] Talk";
        tmp.fontSize = 0.25f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineWidth = 0.05f;
        tmp.outlineColor = Color.black;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        promptUI.SetActive(false);
        Debug.Log("[ShopNPC] Interact prompt created");
    }

    // Public methods for ShopController
    public List<ShopStockItem> GetCurrentStock() => currentShopStock;
    public Item GetItemByID(int id) => itemReferences?.Find(i => i.ID == id);
    public bool RemoveFromShopStock(int itemID, int quantity)
    {
        var existing = currentShopStock.Find(s => s.itemID == itemID);
        if (existing != null && existing.quantity >= quantity)
        {
            existing.quantity -= quantity;
            if (existing.quantity <= 0) currentShopStock.Remove(existing);
            return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}