using UnityEngine;

public class MenuUIManager : MonoBehaviour
{
    public static MenuUIManager Instance;

    [Header("UI Panels")]
    public GameObject menuPanel;
    public GameObject shopCanvas;

    [Header("Inventory")]
    public InventoryController inventoryController;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (shopCanvas != null) shopCanvas.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (menuPanel == null) return;
        bool newState = !menuPanel.activeSelf;
        menuPanel.SetActive(newState);
        if (newState && inventoryController != null)
            inventoryController.OnInventoryOpened();
        // Close shop if menu opened?
        if (newState && shopCanvas != null && shopCanvas.activeSelf)
            shopCanvas.SetActive(false);
    }

    public void OpenShop()
    {
        if (shopCanvas == null) return;
        shopCanvas.SetActive(true);
        // Do NOT close the menu - instead, ensure the items page is active
        // The player inventory grid should remain visible
    }

    public void CloseShop()
    {
        if (shopCanvas != null)
            shopCanvas.SetActive(false);
    }

    public bool IsMenuOpen()
    {
        return menuPanel != null && menuPanel.activeSelf;
    }
}