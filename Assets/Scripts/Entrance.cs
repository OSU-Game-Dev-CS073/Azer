using UnityEngine;

public class Entrance : MonoBehaviour
{
    [SerializeField] private string entranceID; // Must match exitID from previous scene
    
    void Start()
    {
        // Check if player should spawn here
        string lastExitUsed = PlayerPrefs.GetString("LastExitUsed", "");
        
        if (lastExitUsed == entranceID)
        {
            // Find player and move to this entrance
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Set position
                player.transform.position = transform.position;
                
                // Set facing direction
                float dirX = PlayerPrefs.GetFloat("ExitDirX", 1f);
                Player playerScript = player.GetComponent<Player>();
                if (playerScript != null)
                {
                    playerScript.facingDirection = (int)Mathf.Sign(dirX);
                    player.transform.localScale = new Vector3(
                        playerScript.baseScale * playerScript.facingDirection,
                        playerScript.baseScale,
                        playerScript.baseScale
                    );
                }
                
                Debug.Log($"Player spawned at entrance: {entranceID}");
                
                // Clear saved data
                PlayerPrefs.DeleteKey("LastExitUsed");
                PlayerPrefs.DeleteKey("ExitDirX");
                PlayerPrefs.DeleteKey("ExitDirY");
            }
        }
    }
}