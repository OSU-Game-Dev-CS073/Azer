using UnityEngine;
using UnityEngine.EventSystems;

public class ShopItemHandler : MonoBehaviour, IPointerClickHandler
{
    private bool isShopItem;
    private ShopNPC.ShopStockItem shopStockItem;
    private Slot originalInventorySlot;

    public void Initialize(bool isShop, GameManager.InventorySlotData playerItem, ShopNPC.ShopStockItem shopItem, Slot originalSlot = null)
    {
        isShopItem = isShop;
        shopStockItem = shopItem;
        originalInventorySlot = originalSlot;
        Debug.Log($"[ShopItemHandler] Initialize - isShopItem={isShopItem}, hasStockItem={shopItem != null}, hasOriginalSlot={originalSlot != null}");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[ShopItemHandler] OnPointerClick FIRED! isShopItem={isShopItem}, button={eventData.button}, object={gameObject.name}");
        
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (isShopItem)
            {
                Debug.Log("[ShopItemHandler] Calling BuyItem");
                BuyItem();
            }
            else
            {
                Debug.Log("[ShopItemHandler] Calling SellItem");
                SellItem();
            }
        }
    }

    private void BuyItem()
    {
        Debug.Log("[ShopItemHandler] BuyItem executing");
        if (shopStockItem == null) 
        {
            Debug.LogError("[ShopItemHandler] shopStockItem is null!");
            return;
        }
        ShopSlot slot = GetComponentInParent<ShopSlot>();
        if (slot == null) 
        {
            Debug.LogError("[ShopItemHandler] ShopSlot not found!");
            return;
        }
        Debug.Log($"[ShopItemHandler] Attempting to buy item ID: {shopStockItem.itemID}, Price: {slot.itemPrice}");
        ShopController.Instance.TryBuyItem(shopStockItem, slot.itemPrice);
    }

    private void SellItem()
    {
        Debug.Log("[ShopItemHandler] SellItem executing");
        if (originalInventorySlot == null)
        {
            Debug.LogError("[ShopItemHandler] Cannot sell: originalInventorySlot is null.");
            return;
        }

        Item invItem = originalInventorySlot.currentItem?.GetComponent<Item>();
        if (invItem == null)
        {
            Debug.LogError("[ShopItemHandler] No Item component on inventory item.");
            return;
        }

        ShopSlot slot = GetComponentInParent<ShopSlot>();
        if (slot == null) 
        {
            Debug.LogError("[ShopItemHandler] ShopSlot not found for selling!");
            return;
        }

        int sellPrice = invItem.GetSellPrice();
        Debug.Log($"[ShopItemHandler] Selling {invItem.Name} for {sellPrice} gold");

        bool removed = InventoryController.Instance.RemoveItemFromSlot(originalInventorySlot, 1);
        if (removed)
        {
            CurrencyController.Instance.AddGold(sellPrice);
            ShopController.Instance.UpdateMoneyDisplay();
            ShopController.Instance.RefreshPlayerInventoryDisplay();
            ShopController.Instance.RefreshShopDisplay();
            Debug.Log($"[ShopItemHandler] Successfully sold {invItem.Name} for {sellPrice} gold!");
        }
        else
        {
            Debug.LogError($"[ShopItemHandler] Failed to remove {invItem.Name} from inventory.");
        }
    }
}