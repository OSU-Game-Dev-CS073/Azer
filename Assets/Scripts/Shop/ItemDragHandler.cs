using System.Collections;
using System.Collections.Generic;
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPosition = rectTransform.position;
        
        // Detach from slot and move to root
        transform.SetParent(parentCanvas.transform);
        transform.SetAsLastSibling(); // Put on top of everything
        
        // Make it visible and not block raycasts
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 1f; // Keep fully visible
        
        // Slightly enlarge for visual feedback
        rectTransform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        
        Debug.Log($"Started dragging: {gameObject.name}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Move the item with the mouse
        if (rectTransform != null)
        {
            rectTransform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
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
            // Valid drop on a different slot
            if (dropSlot.currentItem != null && dropSlot.currentItem != gameObject)
            {
                // Slot has an item - swap items
                GameObject itemToSwap = dropSlot.currentItem;
                
                // Move swapped item to original slot
                itemToSwap.transform.SetParent(originalSlot.transform);
                originalSlot.currentItem = itemToSwap;
                itemToSwap.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                
                // Ensure swapped item is visible
                Image swappedImage = itemToSwap.GetComponent<Image>();
                if (swappedImage != null) swappedImage.enabled = true;
            }
            else
            {
                originalSlot.currentItem = null;
            }

            // Move this item into drop slot
            transform.SetParent(dropSlot.transform);
            dropSlot.currentItem = gameObject;
        }
        else
        {
            // No valid drop - return to original slot
            transform.SetParent(originalParent);
        }

        // Reset position and scale
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        
        // Restore raycast blocking
        canvasGroup.blocksRaycasts = true;
        
        // Ensure item is visible
        if (itemImage != null) itemImage.enabled = true;
        
        Debug.Log($"Ended dragging: {gameObject.name}");
    }
}