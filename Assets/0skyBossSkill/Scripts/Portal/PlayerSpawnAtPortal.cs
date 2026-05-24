using UnityEngine;

public class PlayerSpawnAtPortal : MonoBehaviour
{
    private void Start()
    {
        if (string.IsNullOrEmpty(PortalSpawnData.nextSpawnPointName))
        {
            return;
        }

        GameObject spawnPoint = GameObject.Find(PortalSpawnData.nextSpawnPointName);

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.transform.position;
        }
        else
        {
            //can not find Portal start position
            Debug.LogWarning("找不到 Portal 出生点: " + PortalSpawnData.nextSpawnPointName);
        }

        PortalSpawnData.nextSpawnPointName = "";
    }
}
