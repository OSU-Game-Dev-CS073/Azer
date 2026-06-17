using UnityEngine;

/// <summary>
/// Updated collectibles - coins now go through GameManager for persistence.
/// </summary>
public class Collectibles : MonoBehaviour
{
    public AudioClip coinCollect;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (gameObject.CompareTag("coin"))
            {
                Player player = collision.gameObject.GetComponent<Player>();
                if (player != null)
                {
                    // Use GameManager for coin tracking
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddCoins(1);
                    }

                    player.PlaySFX(coinCollect, 0.4f, 1.75f);
                    Destroy(gameObject);
                }
            }
        }
    }
}
