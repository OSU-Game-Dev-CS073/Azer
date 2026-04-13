using UnityEngine;
using TMPro;

public class Collectibles : MonoBehaviour
{
    [Header("Collectible Settings")]
    public string itemName = "Item";
    public int value = 1;
    public CollectibleType itemType = CollectibleType.Coin;
    public GameObject uiPrefab;   // Drag the INVENTORY UI PREFAB here (e.g., HealthPotion_UI)

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
        if (currentPlayer != null)
            Collect(currentPlayer);
    }

    private void Collect(Player player)
    {
        // Currency goes directly to player money
        if (itemType == CollectibleType.Coin || itemType == CollectibleType.Diamond || itemType == CollectibleType.Crystal)
        {
            int moneyValue = value;
            if (itemType == CollectibleType.Diamond) moneyValue *= 10;
            if (itemType == CollectibleType.Crystal) moneyValue *= 5;
            player.AddMoney(moneyValue);
        }
        // Health items heal directly
        else if (itemType == CollectibleType.Food || itemType == CollectibleType.Potion)
        {
            player.Heal(value);
        }
        // All other items (QuestItem, etc.) go to inventory
        else
        {
            if (uiPrefab == null)
            {
                Debug.LogError($"Collectible '{itemName}' has no uiPrefab assigned! Drag the inventory UI prefab into the uiPrefab field.");
                return;
            }

            if (InventoryController.Instance == null)
            {
                Debug.LogError("InventoryController.Instance is null!");
                return;
            }

            if (!InventoryController.Instance.AddItem(uiPrefab, itemName, itemType))
            {
                Debug.Log("Inventory full – cannot pick up.");
                return;
            }
        }

        if (collectSound != null)
            player.PlaySFX(collectSound, 0.5f, 1.5f);

        Destroy(gameObject);
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