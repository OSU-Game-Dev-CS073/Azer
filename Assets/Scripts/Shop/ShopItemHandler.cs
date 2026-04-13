using UnityEngine;
using UnityEngine.EventSystems;
// next time i need to add action scripts for each item type
// q for quick heal potions, and need to figure out how to handle stacking world items and shop items


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
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (isShopItem)
                BuyItem();
            else
                SellItem();
        }
    }

    private void BuyItem()
    {
        if (shopStockItem == null) return;
        ShopSlot slot = GetComponentInParent<ShopSlot>();
        if (slot == null) return;
        ShopController.Instance.TryBuyItem(shopStockItem, slot.itemPrice);
    }

    private void SellItem()
    {
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
        if (slot == null) return;

        int sellPrice = invItem.GetSellPrice();

        // Remove using the slot reference
        bool removed = InventoryController.Instance.RemoveItemFromSlot(originalInventorySlot, 1);
        if (removed)
        {
            CurrencyController.Instance.AddGold(sellPrice);
            ShopController.Instance.UpdateMoneyDisplay();
            ShopController.Instance.RefreshPlayerInventoryDisplay();
            ShopController.Instance.RefreshShopDisplay();
            Debug.Log($"Sold {invItem.Name} for {sellPrice} gold!");
        }
        else
        {
            Debug.LogError($"Failed to remove {invItem.Name} from inventory.");
        }
    }
}