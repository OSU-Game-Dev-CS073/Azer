using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Transform originalParent;
    CanvasGroup canvasGroup;
    RectTransform rectTransform;
    Image itemImage;
    Vector3 originalPosition;
    Canvas parentCanvas;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        itemImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            parentCanvas = FindFirstObjectByType<Canvas>();
        }
    }

    void Update()
    {
        // Disable dragging if shop is open
        if (ShopController.Instance != null && ShopController.Instance.shopPanel != null && ShopController.Instance.shopPanel.activeSelf)
        {
            canvasGroup.blocksRaycasts = true;
            return;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Don't allow dragging when shop is open
        if (ShopController.Instance != null && ShopController.Instance.shopPanel != null && ShopController.Instance.shopPanel.activeSelf)
        {
            eventData.pointerDrag = null;
            return;
        }
        
        originalParent = transform.parent;
        originalPosition = rectTransform.position;
        
        transform.SetParent(parentCanvas.transform);
        transform.SetAsLastSibling();
        
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 1f;
        rectTransform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ShopController.Instance != null && ShopController.Instance.shopPanel != null && ShopController.Instance.shopPanel.activeSelf)
            return;
            
        if (rectTransform != null)
        {
            rectTransform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ShopController.Instance != null && ShopController.Instance.shopPanel != null && ShopController.Instance.shopPanel.activeSelf)
            return;
            
        Slot dropSlot = eventData.pointerEnter?.GetComponent<Slot>();
        if (dropSlot == null)
        {
            GameObject dropItem = eventData.pointerEnter;
            if (dropItem != null)
            {
                dropSlot = dropItem.GetComponentInParent<Slot>();
            }
        }
        
        Slot originalSlot = originalParent.GetComponent<Slot>();

        if (dropSlot != null && dropSlot != originalSlot)
        {
            if (dropSlot.currentItem != null && dropSlot.currentItem != gameObject)
            {
                GameObject itemToSwap = dropSlot.currentItem;
                itemToSwap.transform.SetParent(originalSlot.transform);
                originalSlot.currentItem = itemToSwap;
                itemToSwap.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                
                Image swappedImage = itemToSwap.GetComponent<Image>();
                if (swappedImage != null) swappedImage.enabled = true;
            }
            else
            {
                originalSlot.currentItem = null;
            }

            transform.SetParent(dropSlot.transform);
            dropSlot.currentItem = gameObject;
        }
        else
        {
            transform.SetParent(originalParent);
        }

        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        canvasGroup.blocksRaycasts = true;
        
        if (itemImage != null) itemImage.enabled = true;
    }
}