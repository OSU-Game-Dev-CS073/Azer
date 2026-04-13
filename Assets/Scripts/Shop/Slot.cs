using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public GameObject currentItem;   // The UI item in this slot

    public void SetItem(GameObject item)
    {
        currentItem = item;
        item.transform.SetParent(transform);
        RectTransform rt = item.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = Vector2.zero;
        else Debug.LogError("Item has no RectTransform!");

        Image img = item.GetComponent<Image>();
        if (img != null) img.enabled = true;
    }

    public void ClearSlot()
    {
        if (currentItem != null) Destroy(currentItem);
        currentItem = null;
    }

    public bool IsEmpty() => currentItem == null;
}