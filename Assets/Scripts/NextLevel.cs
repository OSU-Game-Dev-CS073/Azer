using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevel : MonoBehaviour
{
    [SerializeField] public string targetSceneName; // Scene to load
    [SerializeField] public string exitID; // Unique ID for this exit
    [SerializeField] public Vector2 exitDirection = Vector2.right; // Direction player faces after spawn
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log($"Player entered exit: {exitID}, loading scene: {targetSceneName}");
            
            // Save which exit we're using
            PlayerPrefs.SetString("LastExitUsed", exitID);
            PlayerPrefs.SetFloat("ExitDirX", exitDirection.x);
            PlayerPrefs.SetFloat("ExitDirY", exitDirection.y);
            PlayerPrefs.Save();
            
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                SceneController.instance.LoadSceneByName(targetSceneName);
            }
            else
            {
                Debug.LogError("No target scene name specified for this exit!");
            }
        }
    }
}