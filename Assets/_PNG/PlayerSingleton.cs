using UnityEngine;

public class PlayerSingleton : MonoBehaviour
{
    public static PlayerSingleton Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);      // If another player exists, kill this one
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep this player between scenes
    }
}
