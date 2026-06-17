using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject optionOverlay;
    public GameObject audioPage;
    public GameObject otherPage;

    // ========= 主菜单按钮 =========
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OpenSettings()
    {
        optionOverlay.SetActive(true);

        // 默认打开 Audio 页
        ShowAudioPage();
    }

    public void CloseSettings()
    {
        optionOverlay.SetActive(false);
    }

    public void ExitGame()
    {
        Debug.Log("Exit button pressed");

        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    // ========= Option 内 Tab 切换 =========
    public void ShowAudioPage()
    {
        audioPage.SetActive(true);
        otherPage.SetActive(false);
    }

    public void ShowOtherPage()
    {
        audioPage.SetActive(false);
        otherPage.SetActive(true);
    }
}


