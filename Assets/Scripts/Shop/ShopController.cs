using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ShopController : MonoBehaviour
{
    public static ShopController Instance;

    [Header("UI")]
    public GameObject shopPanel;
    public Transform shopInventoryGrid;
    public Transform playerInventoryGrid;
    public GameObject shopSlotPrefab;
    public TextMeshProUGUI playerMoneyText;
    public TextMeshProUGUI shopTitleText;

    private ShopNPC currentShop;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        UpdateMoneyDisplay();
    }

    public void OpenShop(ShopNPC shop)
    {
        currentShop = shop;
        shopPanel.SetActive(true);
        if (shopTitleText != null) shopTitleText.text = shop.shopkeeperName + "'s Shop";
        RefreshShopDisplay();
        RefreshPlayerInventoryDisplay();
        UpdateMoneyDisplay();
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        currentShop = null;
    }

    public void RefreshShopDisplay()
    {
        if (currentShop == null) return;
        foreach (Transform child in shopInventoryGrid) Destroy(child.gameObject);

        foreach (var stockItem in currentShop.GetCurrentStock())
        {
            if (stockItem.quantity <= 0) continue;
            CreateShopSlot(shopInventoryGrid, stockItem.itemID, stockItem.quantity, true);
        }
    }

    public void RefreshPlayerInventoryDisplay()
    {
        if (InventoryController.Instance == null) return;
        foreach (Transform child in playerInventoryGrid) Destroy(child.gameObject);

        Transform inventoryParent = InventoryController.Instance.itemsPageParent;
        if (inventoryParent == null)
        {
            Debug.LogWarning("InventoryController itemsPageParent not assigned!");
            return;
        }

        foreach (Transform slotTransform in inventoryParent)
        {
            Slot inventorySlot = slotTransform.GetComponent<Slot>();
            if (inventorySlot?.currentItem != null)
            {
                // Try to get Item component (for selling price)
                Item originalItem = inventorySlot.currentItem.GetComponent<Item>();
                if (originalItem != null)
                {
                    CreateShopSlot(playerInventoryGrid, originalItem.ID, originalItem.quantity, false, inventorySlot);
                }
                else
                {
                    // If no Item component, still show it but cannot sell (or use default price)
                    Debug.LogWarning($"Item '{inventorySlot.currentItem.name}' has no Item component - cannot sell.");
                }
            }
        }
    }

    private void CreateShopSlot(Transform grid, int itemID, int quantity, bool isShop, Slot originalSlot = null)
    {
        if (shopSlotPrefab == null) return;
        GameObject slotObj = Instantiate(shopSlotPrefab, grid);
        ShopSlot slot = slotObj.GetComponent<ShopSlot>();
        if (slot == null)
        {
            Debug.LogError("ShopSlot component missing on prefab!");
            Destroy(slotObj);
            return;
        }

        Item itemData = null;
        GameObject itemPrefab = null;
        int price = 0;
        string displayName = "";

        if (isShop)
        {
            itemData = currentShop.GetItemByID(itemID);
            if (itemData == null)
            {
                Debug.LogError($"Item with ID {itemID} not found in NPC's itemReferences!");
                Destroy(slotObj);
                return;
            }
            itemPrefab = itemData.uiPrefab;
            price = itemData.buyPrice;
            displayName = itemData.Name;
        }
        else
        {
            if (originalSlot == null || originalSlot.currentItem == null)
            {
                Destroy(slotObj);
                return;
            }
            Item invItem = originalSlot.currentItem.GetComponent<Item>();
            if (invItem == null)
            {
                Debug.LogWarning($"Cannot sell '{originalSlot.currentItem.name}' – missing Item component.");
                Destroy(slotObj);
                return;
            }
            itemPrefab = invItem.uiPrefab;
            price = invItem.GetSellPrice();
            displayName = invItem.Name;
        }

        if (itemPrefab == null)
        {
            Debug.LogError($"No uiPrefab for item {displayName}");
            Destroy(slotObj);
            return;
        }

        GameObject itemInstance = Instantiate(itemPrefab, slotObj.transform);
        RectTransform itemRect = itemInstance.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.sizeDelta = new Vector2(60, 60);
        }

        ItemDragHandler drag = itemInstance.GetComponent<ItemDragHandler>();
        if (drag != null) drag.enabled = false;

        slot.SetItem(itemInstance, price);
        slot.UpdateNameDisplay(displayName);
        slot.isShopSlot = isShop;

        ShopItemHandler handler = itemInstance.GetComponent<ShopItemHandler>();
        if (handler == null) handler = itemInstance.AddComponent<ShopItemHandler>();

        if (!isShop && originalSlot != null)
            handler.Initialize(isShop, null, null, originalSlot);
        else
            handler.Initialize(isShop, null, isShop ? currentShop.GetCurrentStock().Find(s => s.itemID == itemID) : null, null);
    }

    public void UpdateMoneyDisplay()
    {
        if (playerMoneyText != null && CurrencyController.Instance != null)
            playerMoneyText.text = CurrencyController.Instance.GetGold().ToString();
    }

    // ==================== FIXED BUY METHOD ====================
    public bool TryBuyItem(ShopNPC.ShopStockItem stockItem, int price)
    {
        Debug.Log($"[SHOP] === TryBuyItem called: ItemID={stockItem.itemID}, Price={price} ===");

        if (CurrencyController.Instance == null)
        {
            Debug.LogError("[SHOP] CurrencyController.Instance is null!");
            return false;
        }
        if (currentShop == null)
        {
            Debug.LogError("[SHOP] currentShop is null!");
            return false;
        }

        if (CurrencyController.Instance.GetGold() < price)
        {
            Debug.Log("[SHOP] Not enough gold!");
            return false;
        }

        Item itemData = currentShop.GetItemByID(stockItem.itemID);
        if (itemData == null)
        {
            Debug.LogError($"[SHOP] Item with ID {stockItem.itemID} not found in shop's itemReferences!");
            return false;
        }

        Debug.Log($"[SHOP] Item found: {itemData.Name}, uiPrefab: {(itemData.uiPrefab != null ? itemData.uiPrefab.name : "NULL")}");

        if (itemData.uiPrefab == null)
        {
            Debug.LogError($"[SHOP] Cannot buy item {itemData.Name}: uiPrefab is null!");
            return false;
        }

        // Check for health component on the UI prefab
        UsableHealthItem healthCheck = itemData.uiPrefab.GetComponent<UsableHealthItem>();
        if (healthCheck != null)
        {
            Debug.Log($"[SHOP] ✅ This is a HEALTH item! Heal amount: {healthCheck.healAmount}");
        }
        else
        {
            Debug.LogWarning($"[SHOP] ⚠️ Item '{itemData.Name}' has NO UsableHealthItem component on its UI prefab.");
            // Optional: auto-add for items that look like health items
            if (itemData.Name.ToLower().Contains("apple") || itemData.Name.ToLower().Contains("potion") || itemData.Name.ToLower().Contains("food"))
            {
                Debug.Log($"[SHOP] Auto-adding UsableHealthItem to {itemData.uiPrefab.name} (fallback). Set healAmount in Inspector!");
                healthCheck = itemData.uiPrefab.AddComponent<UsableHealthItem>();
                healthCheck.healAmount = 20; // default fallback
                healthCheck.displayName = itemData.Name;
            }
        }

        if (currentShop.RemoveFromShopStock(stockItem.itemID, 1))
        {
            Debug.Log("[SHOP] Item removed from shop stock.");
            CurrencyController.Instance.SpendGold(price);
            Debug.Log($"[SHOP] Spent {price} gold. Remaining: {CurrencyController.Instance.GetGold()}");

            // Add to inventory – use the correct item type for health items
            Collectibles.CollectibleType itemType = (healthCheck != null) ? Collectibles.CollectibleType.Potion : Collectibles.CollectibleType.QuestItem;
            bool added = InventoryController.Instance.AddItem(itemData.uiPrefab, itemData.Name, itemType);
            
            if (added)
            {
                Debug.Log($"[SHOP] Item '{itemData.Name}' successfully added to inventory!");

                // Verify the item in inventory has the health component
                var healthItems = InventoryController.Instance.GetAllSlotsWithComponent<UsableHealthItem>();
                Debug.Log($"[SHOP] Total health items in inventory after purchase: {healthItems.Count}");
                foreach (var hi in healthItems)
                    Debug.Log($"[SHOP] -> {hi.component.GetDisplayName()} heals {hi.component.healAmount}");

                UpdateMoneyDisplay();
                RefreshShopDisplay();
                RefreshPlayerInventoryDisplay();
                return true;
            }
            else
            {
                Debug.LogError("[SHOP] Failed to add item to inventory! (Inventory full?) Refunding...");
                CurrencyController.Instance.AddGold(price);
            }
        }
        else
        {
            Debug.LogError($"[SHOP] Failed to remove item from shop stock! ID: {stockItem.itemID}");
        }
        return false;
    }

    public bool TrySellItem(GameManager.InventorySlotData playerItem, int price)
    {
        if (CurrencyController.Instance == null || InventoryController.Instance == null) return false;

        if (InventoryController.Instance.RemoveItem(playerItem.itemName, 1))
        {
            CurrencyController.Instance.AddGold(price);
            UpdateMoneyDisplay();
            RefreshPlayerInventoryDisplay();
            return true;
        }
        return false;
    }
}