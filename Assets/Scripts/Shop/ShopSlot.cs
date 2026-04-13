using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlot : MonoBehaviour
{
    public GameObject currentItem;
    public int itemPrice;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI nameText;
    public bool isShopSlot = true;

    private void Awake()
    {
        // Auto-find text components if not assigned
        if (priceText == null)
            priceText = GetComponentInChildren<TextMeshProUGUI>();
        
        if (nameText == null)
        {
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var txt in allTexts)
            {
                if (txt != priceText)
                {
                    nameText = txt;
                    break;
                }
            }
        }

        // Apply consistent styling
        if (priceText != null)
        {
            priceText.fontSize = 14;
            priceText.color = Color.yellow;
            priceText.alignment = TextAlignmentOptions.Center;
        }
        if (nameText != null)
        {
            nameText.fontSize = 12;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.Center;
        }

        // Ensure the slot's image can receive raycasts
        var img = GetComponent<Image>();
        if (img != null) img.raycastTarget = true;
    }

    public void UpdatePriceDisplay()
    {
        if (priceText != null && currentItem != null)
            priceText.text = itemPrice.ToString();
    }

    public void UpdateNameDisplay(string name)
    {
        if (nameText != null)
            nameText.text = name;
    }

    public void SetItem(GameObject item, int price)
    {
        currentItem = item;
        itemPrice = price;
        UpdatePriceDisplay();

        // Scale the item's UI image to fit the slot
        if (item != null)
        {
            RectTransform rt = item.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(60, 60); // Adjust as needed
                rt.anchoredPosition = Vector2.zero;
            }
            // Ensure the item's image is enabled
            Image img = item.GetComponent<Image>();
            if (img != null) img.enabled = true;
        }
    }
}