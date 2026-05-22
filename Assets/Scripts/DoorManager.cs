using UnityEngine;

public class DoorManager : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Scene loaded, checking for door spawn...");
        
        string lastExitUsed = PlayerPrefs.GetString("LastExitUsed", "");
        
        // If player came through a door
        if (!string.IsNullOrEmpty(lastExitUsed))
        {
            Debug.Log("Player came through door: " + lastExitUsed);
            
            // Find the door with matching ID in this scene
            GameObject[] allDoors = GameObject.FindGameObjectsWithTag("Door");
            
            foreach (GameObject door in allDoors)
            {
                NextLevel doorScript = door.GetComponent<NextLevel>();
                if (doorScript != null && doorScript.exitID == lastExitUsed)
                {
                    // Move player to this door
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        player.transform.position = door.transform.position;
                        Debug.Log("Player spawned at door: " + lastExitUsed);
                    }
                    
                    // Clear the saved door
                    PlayerPrefs.DeleteKey("LastExitUsed");
                    return;
                }
            }
            
            Debug.LogWarning("No matching door found for: " + lastExitUsed);
        }
        else
        {
            Debug.Log("Player starts at default position");
            // Player stays at their position in the scene
        }
    }
}