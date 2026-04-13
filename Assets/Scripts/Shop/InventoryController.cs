using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryController : MonoBehaviour
{
    public static InventoryController Instance;

    [Header("Slot Settings")]
    public GameObject slotPrefab;
    public Transform itemsPageParent;
    public int initialSlotCount = 20;

    [Header("Stacking")]
    public bool enableStacking = true;
    public int maxStackSize = 99;

    private List<Slot> slots = new List<Slot>();
    private Dictionary<Slot, InventoryItem> slotItems = new Dictionary<Slot, InventoryItem>();
    private bool hasLoaded = false;

    private class InventoryItem
    {
        public GameObject uiPrefab;
        public string itemName;
        public int quantity;
        public Collectibles.CollectibleType itemType;
        public string uiPrefabName;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        CreateSlots();
        if (GameManager.Instance != null && GameManager.Instance.inventorySlots.Count > 0 && !hasLoaded)
        {
            LoadFromSlotData(GameManager.Instance.inventorySlots);
            hasLoaded = true;
        }
    }

    void CreateSlots()
    {
        if (slotPrefab == null) { Debug.LogError("Slot Prefab not assigned!"); return; }
        if (itemsPageParent == null) { Debug.LogError("Items Page Parent not assigned!"); return; }

        foreach (Transform child in itemsPageParent) Destroy(child.gameObject);
        slots.Clear();
        slotItems.Clear();

        for (int i = 0; i < initialSlotCount; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, itemsPageParent);
            Slot slot = slotGO.GetComponent<Slot>();
            if (slot == null) slot = slotGO.AddComponent<Slot>();
            slots.Add(slot);
        }
    }

    public bool AddItem(GameObject itemPrefab, string itemName, Collectibles.CollectibleType itemType = Collectibles.CollectibleType.QuestItem)
    {
        if (itemPrefab == null) return false;

        // Try stacking
        if (enableStacking)
        {
            foreach (var kvp in slotItems)
            {
                if (kvp.Value.itemName == itemName && kvp.Value.uiPrefab == itemPrefab && kvp.Value.quantity < maxStackSize)
                {
                    kvp.Value.quantity++;
                    UpdateSlotDisplay(kvp.Key, kvp.Value);
                    SaveToGameManager();
                    return true;
                }
            }
        }

        // Find empty slot
        foreach (Slot slot in slots)
        {
            if (slot.IsEmpty())
            {
                GameObject newItem = Instantiate(itemPrefab, slot.transform);
                newItem.name = itemName;
                
                // Resize the item sprite
                RectTransform rect = newItem.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(50, 50);
                    rect.anchoredPosition = Vector2.zero;
                }
                Image img = newItem.GetComponent<Image>();
                if (img != null) img.enabled = true;
                if (newItem.GetComponent<ItemDragHandler>() == null)
                    newItem.AddComponent<ItemDragHandler>();
                
                slot.SetItem(newItem);
                slotItems[slot] = new InventoryItem
                {
                    uiPrefab = itemPrefab,
                    itemName = itemName,
                    quantity = 1,
                    itemType = itemType,
                    uiPrefabName = itemPrefab.name
                };
                UpdateSlotDisplay(slot, slotItems[slot]);
                SaveToGameManager();
                return true;
            }
        }
        return false;
    }

public bool RemoveItem(string itemName, int amount = 1)
{
    Debug.Log($"[Inventory] RemoveItem called: itemName='{itemName}', amount={amount}");
    Debug.Log($"[Inventory] Current slotItems count: {slotItems.Count}");
    
    foreach (var kvp in slotItems)
    {
        Debug.Log($"[Inventory] Checking item: '{kvp.Value.itemName}' (quantity={kvp.Value.quantity})");
        
        if (kvp.Value.itemName == itemName)
        {
            Debug.Log($"[Inventory] Found match! Current quantity: {kvp.Value.quantity}");
            
            kvp.Value.quantity -= amount;
            Debug.Log($"[Inventory] New quantity: {kvp.Value.quantity}");
            
            if (kvp.Value.quantity <= 0)
            {
                Debug.Log($"[Inventory] Quantity <= 0, clearing slot");
                kvp.Key.ClearSlot();
                slotItems.Remove(kvp.Key);
            }
            else
            {
                UpdateSlotDisplay(kvp.Key, kvp.Value);
            }
            
            SaveToGameManager();
            Debug.Log($"[Inventory] RemoveItem SUCCESS for {itemName}");
            return true;
        }
    }
    
    Debug.LogWarning($"[Inventory] RemoveItem FAILED: '{itemName}' not found in inventory!");
    return false;
}

public bool RemoveItemFromSlot(Slot slot, int amount = 1)
{
    if (slotItems.ContainsKey(slot))
    {
        var data = slotItems[slot];
        data.quantity -= amount;
        if (data.quantity <= 0)
        {
            slot.ClearSlot();
            slotItems.Remove(slot);
        }
        else
        {
            UpdateSlotDisplay(slot, data);
        }
        SaveToGameManager();
        return true;
    }
    return false;
}

    public int GetItemCount(string itemName)
    {
        int total = 0;
        foreach (var kvp in slotItems)
            if (kvp.Value.itemName == itemName) total += kvp.Value.quantity;
        return total;
    }

    private void UpdateSlotDisplay(Slot slot, InventoryItem data)
    {
        if (data.quantity > 1)
        {
            TextMeshProUGUI text = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (text == null)
            {
                GameObject textGO = new GameObject("QuantityText");
                textGO.transform.SetParent(slot.transform);
                text = textGO.AddComponent<TextMeshProUGUI>();
                RectTransform textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(1, 1);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.pivot = new Vector2(1, 1);
                textRect.anchoredPosition = new Vector2(-5, -5);
                textRect.sizeDelta = new Vector2(30, 20);
                text.fontSize = 14;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.TopRight;
                text.fontStyle = FontStyles.Bold;
                text.outlineWidth = 0.2f;
                text.outlineColor = Color.black;
            }
            text.text = data.quantity.ToString();
            text.enabled = true;
        }
        else
        {
            TextMeshProUGUI text = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.enabled = false;
        }
    }

    public void OnInventoryOpened()
    {
        RefreshInventoryUI();
    }

    public void RefreshInventoryUI()
    {
        foreach (Slot slot in slots)
            slot.ClearSlot();
        slotItems.Clear();
        if (GameManager.Instance != null && GameManager.Instance.inventorySlots.Count > 0)
            LoadFromSlotData(GameManager.Instance.inventorySlots);
    }

    public List<GameManager.InventorySlotData> GetAllSlotData()
    {
        List<GameManager.InventorySlotData> list = new List<GameManager.InventorySlotData>();
        foreach (var kvp in slotItems)
        {
            list.Add(new GameManager.InventorySlotData
            {
                itemName = kvp.Value.itemName,
                uiPrefabName = kvp.Value.uiPrefabName,
                quantity = kvp.Value.quantity,
                itemType = kvp.Value.itemType
            });
        }
        return list;
    }

    public void LoadFromSlotData(List<GameManager.InventorySlotData> data)
    {
        if (data == null || data.Count == 0) return;
        foreach (var itemData in data)
        {
            string path = "Prefabs/" + itemData.uiPrefabName;
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null) continue;
            for (int i = 0; i < itemData.quantity; i++)
                AddItemWithoutSync(prefab, itemData.itemName, itemData.itemType);
        }
        SaveToGameManager();
    }

    private void AddItemWithoutSync(GameObject prefab, string name, Collectibles.CollectibleType type)
    {
        if (enableStacking)
        {
            foreach (var kvp in slotItems)
            {
                if (kvp.Value.itemName == name && kvp.Value.uiPrefab == prefab && kvp.Value.quantity < maxStackSize)
                {
                    kvp.Value.quantity++;
                    UpdateSlotDisplay(kvp.Key, kvp.Value);
                    return;
                }
            }
        }
        foreach (Slot slot in slots)
        {
            if (slot.IsEmpty())
            {
                GameObject newItem = Instantiate(prefab, slot.transform);
                newItem.name = name;
                RectTransform rect = newItem.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(50, 50);
                    rect.anchoredPosition = Vector2.zero;
                }
                Image img = newItem.GetComponent<Image>();
                if (img != null) img.enabled = true;
                if (newItem.GetComponent<ItemDragHandler>() == null) newItem.AddComponent<ItemDragHandler>();
                slot.SetItem(newItem);
                slotItems[slot] = new InventoryItem
                {
                    uiPrefab = prefab,
                    itemName = name,
                    quantity = 1,
                    itemType = type,
                    uiPrefabName = prefab.name
                };
                UpdateSlotDisplay(slot, slotItems[slot]);
                return;
            }
        }
    }

public void SaveToGameManager()
{
    if (GameManager.Instance != null)
    {
        GameManager.Instance.inventorySlots = GetAllSlotData();
        Debug.Log($"[Inventory] Saved to GameManager. Items count: {GameManager.Instance.inventorySlots.Count}");
    }
}
}