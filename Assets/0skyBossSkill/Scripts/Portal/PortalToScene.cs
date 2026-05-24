using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalToScene : MonoBehaviour
{
    [Header("目标场景名称")]
    [SerializeField] private string targetSceneName;

    [Header("目标场景中的出生点名称")]
    [SerializeField] private string targetSpawnPointName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PortalSpawnData.nextSpawnPointName = targetSpawnPointName;
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
