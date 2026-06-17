using UnityEngine;

public class MusicPlayerM : MonoBehaviour
{
    private static MusicPlayerM instance;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
