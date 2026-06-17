using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// PATCH 6: Added MusicManager to auto-init.
/// </summary>
public class GameBootstrapper : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoBootstrap()
    {
        if (GameManager.Instance == null)
        {
            GameObject go = new GameObject("_GameSystems");
            go.AddComponent<GameManager>();
            go.AddComponent<UIManager>();
            go.AddComponent<QuestSystem>();
            go.AddComponent<DialogueSystem>();
            go.AddComponent<DevPanel>();
            go.AddComponent<StatPanelUI>();
            go.AddComponent<MusicManager>();
            DontDestroyOnLoad(go);
            Debug.Log("GameBootstrapper: Core systems initialized");
        }

        SceneManager.sceneLoaded += EnsureEventSystem;
    }

    private static bool hasRedirected = false;

    static void EnsureEventSystem(Scene scene, LoadSceneMode mode)
    {
        if (EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(eventSystem);
        }

        // Force start at MainMenu if game hasn't been started through proper flow
        if (!hasRedirected && scene.name != "MainMenu" && GameManager.Instance != null && !GameManager.Instance.isGameStarted)
        {
            hasRedirected = true;
            Debug.Log($"GameBootstrapper: Redirecting from '{scene.name}' to MainMenu");
            SceneManager.LoadScene("MainMenu");
            return;
        }

        if (scene.name != "MainMenu" && GameManager.Instance != null && !GameManager.Instance.isGameStarted)
        {
            GameManager.Instance.isGameStarted = true;
        }
    }
}
