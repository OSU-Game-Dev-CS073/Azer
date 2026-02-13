using UnityEngine;

/// <summary>
/// Universal Health component. Used by both player and enemies.
/// Updated to clamp values and provide better event hooks.
/// </summary>
public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    // Events
    public System.Action OnDamaged;
    public System.Action OnHealed;
    public System.Action OnDeath;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Change health by amount. Negative = damage, positive = heal.
    /// </summary>
    public void ChangeHealth(int amount)
    {
        if (isDead) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);

        if (amount < 0)
        {
            OnDamaged?.Invoke();
        }
        else if (amount > 0)
        {
            OnHealed?.Invoke();
        }

        // Death check
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            OnDeath?.Invoke();
            Die();
        }
    }

    /// <summary>
    /// Set health to a specific value.
    /// </summary>
    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            OnDeath?.Invoke();
            Die();
        }
    }

    /// <summary>
    /// Heal to full.
    /// </summary>
    public void FullHeal()
    {
        currentHealth = maxHealth;
        OnHealed?.Invoke();
    }

    /// <summary>
    /// Get health as 0-1 range.
    /// </summary>
    public float GetNormalized()
    {
        return (float)currentHealth / maxHealth;
    }

    public bool IsDead => isDead;

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died!");
    }

    /// <summary>
    /// Reset health (e.g., on respawn).
    /// </summary>
    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
    }
}
