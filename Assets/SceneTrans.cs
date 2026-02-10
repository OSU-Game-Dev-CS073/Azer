using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [Tooltip("Exact name of the scene to load (must be in Scene List).")]
    public string sceneToLoad;

    [Tooltip("Name of the spawn point in the NEXT scene.")]
    public string spawnPointName;

    // This will remember the spawn we want to use after loading the scene
    public static string NextSpawnPointName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            NextSpawnPointName = spawnPointName;
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
