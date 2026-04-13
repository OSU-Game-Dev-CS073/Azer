using UnityEngine;
//working
public class MenuController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuCanvas;
    public GameObject shopCanvas;
    public InventoryController inventoryController;

    void Start()
    {
        if (menuCanvas != null) menuCanvas.SetActive(false);
        if (shopCanvas != null) shopCanvas.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (IsShopOpen())
            return;

        if (menuCanvas != null)
        {
            bool newState = !menuCanvas.activeSelf;
            menuCanvas.SetActive(newState);

            if (newState && inventoryController != null)
                inventoryController.OnInventoryOpened();
        }
    }

    public bool IsShopOpen()
    {
        return shopCanvas != null && shopCanvas.activeSelf;
    }

    public bool IsInventoryOpen()
    {
        return menuCanvas != null && menuCanvas.activeSelf;
    }
}