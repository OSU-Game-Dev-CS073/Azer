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

    // ========== REFRESH METHODS (using your reference logic) ==========
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

    // Iterate over actual inventory slots
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
            Item originalItem = inventorySlot.currentItem.GetComponent<Item>();
            if (originalItem != null)
            {
                // Pass the inventorySlot so we know which slot to remove from when selling
                CreateShopSlot(playerInventoryGrid, originalItem.ID, originalItem.quantity, false, inventorySlot);
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
        // Shop item: get data from NPC's itemReferences
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
        // Player item: get data from the inventory item
        if (originalSlot == null || originalSlot.currentItem == null)
        {
            Destroy(slotObj);
            return;
        }
        Item invItem = originalSlot.currentItem.GetComponent<Item>();
        if (invItem == null)
        {
            Destroy(slotObj);
            return;
        }
        itemPrefab = invItem.uiPrefab;
        price = invItem.GetSellPrice();
        displayName = invItem.Name;
        
        // Store the original slot reference on the handler for selling
        // We'll do this after creating the item instance
    }

    if (itemPrefab == null)
    {
        Debug.LogError($"No uiPrefab for item {displayName}");
        Destroy(slotObj);
        return;
    }

    // Instantiate the visual item inside the slot
    GameObject itemInstance = Instantiate(itemPrefab, slotObj.transform);
    RectTransform itemRect = itemInstance.GetComponent<RectTransform>();
    if (itemRect != null)
    {
        itemRect.anchoredPosition = Vector2.zero;
        itemRect.sizeDelta = new Vector2(60, 60);
    }

    // Disable drag on shop items
    ItemDragHandler drag = itemInstance.GetComponent<ItemDragHandler>();
    if (drag != null) drag.enabled = false;

    // Setup the slot
    slot.SetItem(itemInstance, price);
    slot.UpdateNameDisplay(displayName);
    slot.isShopSlot = isShop;

    // Add click handler to the item
    ShopItemHandler handler = itemInstance.GetComponent<ShopItemHandler>();
    if (handler == null) handler = itemInstance.AddComponent<ShopItemHandler>();
    
    // Pass the originalSlot for selling
    if (!isShop && originalSlot != null)
    {
        handler.Initialize(isShop, null, null, originalSlot);
    }
    else
    {
        handler.Initialize(isShop, null, isShop ? currentShop.GetCurrentStock().Find(s => s.itemID == itemID) : null, null);
    }
}
    // ========== CREATE SHOP SLOT (with proper display scaling) ==========

    // ========== MONEY & BUY/SELL ==========
    public void UpdateMoneyDisplay()
    {
        if (playerMoneyText != null && CurrencyController.Instance != null)
            playerMoneyText.text = CurrencyController.Instance.GetGold().ToString();
    }

    public bool TryBuyItem(ShopNPC.ShopStockItem stockItem, int price)
    {
        if (CurrencyController.Instance == null || currentShop == null) return false;

        if (CurrencyController.Instance.GetGold() < price)
        {
            Debug.Log("Not enough gold!");
            return false;
        }

        Item itemData = currentShop.GetItemByID(stockItem.itemID);
        if (itemData == null || itemData.uiPrefab == null)
        {
            Debug.LogError($"Cannot buy item ID {stockItem.itemID}: missing UI prefab!");
            return false;
        }

        if (currentShop.RemoveFromShopStock(stockItem.itemID, 1))
        {
            CurrencyController.Instance.SpendGold(price);
            bool added = InventoryController.Instance.AddItem(itemData.uiPrefab, itemData.Name, Collectibles.CollectibleType.QuestItem);
            if (added)
            {
                UpdateMoneyDisplay();
                RefreshShopDisplay();
                RefreshPlayerInventoryDisplay();
                return true;
            }
            else
            {
                // Inventory full – refund
                CurrencyController.Instance.AddGold(price);
                Debug.Log("Inventory full!");
            }
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
            // Optional: add item back to shop stock
            // int itemID = GetItemIDByName(playerItem.itemName);
            // if (itemID != -1) currentShop?.AddToStock(itemID, 1);
            return true;
        }
        return false;
    }
}