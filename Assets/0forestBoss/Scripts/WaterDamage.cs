using UnityEngine;

public class WaterDamage : MonoBehaviour
{
    [Header("Water Damage Settings")]
    [Tooltip("Damage dealt each time the interval passes.")]
    public int damageAmount = 10;

    [Tooltip("How often the player takes damage while staying in the water.")]
    public float damageInterval = 1f;

    private float damageTimer = 0f;
    private Player playerInWater;

    private void Update()
    {
        if (playerInWater == null)
            return;

        damageTimer += Time.deltaTime;

        if (damageTimer >= damageInterval)
        {
            playerInWater.TakeDamage(damageAmount);
            damageTimer = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();

        if (player != null)
        {
            playerInWater = player;
            damageTimer = 0f;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();

        if (player != null && player == playerInWater)
        {
            playerInWater = null;
            damageTimer = 0f;
        }
    }
}
