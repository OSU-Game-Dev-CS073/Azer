using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// SceneController - handles scene transitions with animation.
/// Updated to work with GameManager and MainMenu.
/// </summary>
public class SceneController : MonoBehaviour
{
    [SerializeField] public Animator transitionAnim;
    public static SceneController instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            FindTransitionAnimator();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Invoke(nameof(FindTransitionAnimator), 0.1f);
    }

    void FindTransitionAnimator()
    {
        GameObject transitionObject = GameObject.Find("Scene Transition");

        if (transitionObject == null)
        {
            Animator[] allAnimators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (Animator anim in allAnimators)
            {
                if (anim.runtimeAnimatorController != null &&
                    anim.runtimeAnimatorController.name == "Scene Transition")
                {
                    transitionObject = anim.gameObject;
                    break;
                }
            }
        }

        if (transitionObject != null)
        {
            transitionAnim = transitionObject.GetComponent<Animator>();
            if (transitionAnim != null)
            {
                VerifyAnimatorParameters();
            }
        }
        else
        {
            transitionAnim = null;
        }
    }

    void VerifyAnimatorParameters()
    {
        bool hasEnd = false;
        bool hasStart = false;

        foreach (AnimatorControllerParameter param in transitionAnim.parameters)
        {
            if (param.name == "End") hasEnd = true;
            if (param.name == "Start") hasStart = true;
        }

        if (!hasEnd || !hasStart)
        {
            transitionAnim = null;
        }
    }

    public void NextLevel()
    {
        StartCoroutine(LoadLevelByIndex());
    }

    public void LoadSceneByName(string sceneName)
    {
        StartCoroutine(LoadLevelByName(sceneName));
    }

    IEnumerator LoadLevelByIndex()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (transitionAnim != null && VerifyAnimatorHasParameters())
        {
            transitionAnim.SetTrigger("End");
            yield return new WaitForSeconds(1);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        SceneManager.LoadSceneAsync(nextSceneIndex);

        if (transitionAnim != null)
        {
            transitionAnim.SetTrigger("Start");
        }
    }

    IEnumerator LoadLevelByName(string sceneName)
    {
        if (!DoesSceneExist(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' does not exist in Build Settings!");
            yield break;
        }

        if (transitionAnim != null && VerifyAnimatorHasParameters())
        {
            transitionAnim.SetTrigger("End");
            yield return new WaitForSeconds(1);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        if (transitionAnim != null)
        {
            transitionAnim.SetTrigger("Start");
        }
    }

    bool VerifyAnimatorHasParameters()
    {
        if (transitionAnim == null) return false;

        bool hasEnd = false;
        bool hasStart = false;

        foreach (AnimatorControllerParameter param in transitionAnim.parameters)
        {
            if (param.name == "End") hasEnd = true;
            if (param.name == "Start") hasStart = true;
        }

        return hasEnd && hasStart;
    }

    private bool DoesSceneExist(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (sceneNameInBuild == sceneName)
            {
                return true;
            }
        }
        return false;
    }
}
