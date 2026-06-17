using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    private void Start()
    {
        // If no spawn point was requested, do nothing
        if (string.IsNullOrEmpty(SceneTransition.NextSpawnPointName))
            return;

        // Find the spawn point in this scene
        GameObject spawn = GameObject.Find(SceneTransition.NextSpawnPointName);
        if (spawn == null)
        {
            Debug.LogWarning("Spawn point not found: " + SceneTransition.NextSpawnPointName);
            return;
        }

        // Find the player (the persistent one)
        if (PlayerSingleton.Instance != null)
        {
            PlayerSingleton.Instance.transform.position = spawn.transform.position;
        }
        else
        {
            Debug.LogWarning("No PlayerSingleton instance found in scene.");
        }
    }
}
