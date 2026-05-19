using UnityEngine;

/// <summary>
/// 适配当前 Player.TakeDamage；在 BossSkill 内补水平击退，不修改 azerPlayer。
/// </summary>
public static class BossSkillDamageHelper
{
    const float KnockbackSpeedX = 10f;

    public static void ApplyHit(Player player, Vector2 horizontalKnockDir, float damage)
    {
        if (player == null) return;
        int amount = Mathf.Max(1, Mathf.RoundToInt(damage));
        player.TakeDamage(amount);
        if (player.rb != null)
        {
            float vx = Mathf.Sign(horizontalKnockDir.x) * KnockbackSpeedX;
            player.rb.linearVelocity = new Vector2(vx, player.rb.linearVelocity.y);
        }
    }
}
