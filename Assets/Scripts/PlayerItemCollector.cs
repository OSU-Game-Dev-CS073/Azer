using UnityEngine;

public class PlayerItemCollector : MonoBehaviour
{
    private InventoryController inventoryController;

    void Start()
    {
        inventoryController = FindObjectOfType<InventoryController>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Item"))
        {
            // Get the Collectibles component (not Item)
            Collectibles collectible = collision.GetComponent<Collectibles>();
            if (collectible != null)
            {
                // Add the item using the UI prefab from Collectibles
                bool itemAdded = inventoryController.AddItem(
                    collectible.uiPrefab, 
                    collectible.itemName, 
                    collectible.itemType
                );

                if (itemAdded)
                {
                    Destroy(collision.gameObject);
                }
            }
            else
            {
                Debug.LogError("Item missing Collectibles component!");
            }
        }
    }
}