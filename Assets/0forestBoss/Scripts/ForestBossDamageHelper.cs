using UnityEngine;

/// <summary>
/// 森林Boss伤害工具 — 对玩家造成伤害并施加水平击退。
/// </summary>
public static class ForestBossDamageHelper
{
    private const float KnockbackSpeedX = 10f;

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
