using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    public Image[] tabImages;
    public GameObject[] pages;
    
    void Start()
    {
        // Validate arrays before proceeding
        if (tabImages.Length != pages.Length)
        {
            Debug.LogError($"TabController: tabImages length ({tabImages.Length}) doesn't match pages length ({pages.Length})");
            return;
        }
        
        ActivateTab(0);
    }
    
    public void ActivateTab(int tabNo)
    {
        // Check if tabNo is valid
        if (tabNo < 0 || tabNo >= pages.Length)
        {
            Debug.LogError($"ActivateTab: Invalid tab index {tabNo}. Valid range: 0-{pages.Length - 1}");
            return;
        }
        
        // Loop through all tabs
        for (int i = 0; i < pages.Length; i++)
        {
            if (i == tabNo)
            {
                // Activate the selected tab
                tabImages[i].color = Color.white;
                pages[i].SetActive(true);
            }
            else
            {
                // Deactivate other tabs
                tabImages[i].color = Color.gray;
                pages[i].SetActive(false);
            }
        }
    }
}