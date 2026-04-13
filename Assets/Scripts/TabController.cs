using UnityEngine;
using UnityEngine.UI;


public class TabController : MonoBehaviour
{
    public Image[] tabImages;
    public GameObject[] pages;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ActivateTab(0);
    }

    // Update is called once per frame
    public void ActivateTab(int tabNo)
    {
    for(int i = 0; i < pages.Length; i++)
    {
        if(i == tabNo)
        {
            tabImages[tabNo].color = Color.white;
            pages[tabNo].SetActive(true);
        }
        else
        {
            tabImages[i].color = Color.gray;
            pages[i].SetActive(false);
        }   
    }
    }
}
