using UnityEngine;

public class BossArenaTrigger : MonoBehaviour
{
    public GameObject bossArenaWall;
    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        if (bossArenaWall != null)
            bossArenaWall.SetActive(true);
    }
}
