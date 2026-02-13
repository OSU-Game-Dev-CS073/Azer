using UnityEngine;

/// <summary>
/// PATCH 5 CHANGE: Spell damage now uses PlayerStats.RollSpellDamage()
/// INT stat adds +3 bonus damage per point on top of spell base damage.
/// LUK crit also applies to spells.
/// </summary>
public class SkillSystem : MonoBehaviour
{
    [Header("Fireball")]
    public float fireballCooldown = 1.5f;
    public float fireballSpeed = 12f;
    public int fireballDamage = 25;
    public Color fireballColor = new Color(1f, 0.4f, 0.1f, 1f);
    private float fireballTimer = 0f;

    [Header("Ice Bolt")]
    public float iceBoltCooldown = 2f;
    public float iceBoltSpeed = 15f;
    public int iceBoltDamage = 15;
    public float iceBoltSlowDuration = 2f;
    public Color iceBoltColor = new Color(0.3f, 0.7f, 1f, 1f);
    private float iceBoltTimer = 0f;

    private Player player;

    void Start()
    {
        player = GetComponent<Player>();
    }

    void Update()
    {
        if (fireballTimer > 0) fireballTimer -= Time.deltaTime;
        if (iceBoltTimer > 0) iceBoltTimer -= Time.deltaTime;

        if (GameManager.Instance == null) return;
        if (GameManager.Instance.isDarkMode) return; // skills disabled in dark mode
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive) return;

        if (Input.GetKeyDown(KeyCode.Q) && GameManager.Instance.hasFireball && fireballTimer <= 0)
            CastFireball();

        if (Input.GetKeyDown(KeyCode.R) && GameManager.Instance.hasIceBolt && iceBoltTimer <= 0)
            CastIceBolt();
    }

    void CastFireball()
    {
        fireballTimer = fireballCooldown;
        Vector2 dir = player != null ? new Vector2(player.facingDirection, 0) : Vector2.right;
        Vector3 pos = transform.position + new Vector3(dir.x * 1f, 0.3f, 0);

        // INT stat + crit via PlayerStats
        int dmg = fireballDamage;
        if (GameManager.Instance != null)
        {
            bool crit;
            dmg = GameManager.Instance.stats.RollSpellDamage(fireballDamage, out crit);
            if (crit) UIManager.Instance?.ShowCritPopup();
        }

        SpawnProjectile("Fireball", pos, dir, fireballSpeed, dmg, fireballColor, 0.4f);
    }

    void CastIceBolt()
    {
        iceBoltTimer = iceBoltCooldown;
        Vector2 dir = player != null ? new Vector2(player.facingDirection, 0) : Vector2.right;
        Vector3 pos = transform.position + new Vector3(dir.x * 1f, 0.3f, 0);

        int dmg = iceBoltDamage;
        if (GameManager.Instance != null)
        {
            bool crit;
            dmg = GameManager.Instance.stats.RollSpellDamage(iceBoltDamage, out crit);
            if (crit) UIManager.Instance?.ShowCritPopup();
        }

        GameObject projectile = SpawnProjectile("IceBolt", pos, dir, iceBoltSpeed, dmg, iceBoltColor, 0.3f);
        if (projectile != null)
        {
            Projectile proj = projectile.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.appliesSlow = true;
                proj.slowDuration = iceBoltSlowDuration;
            }
        }
    }

    GameObject SpawnProjectile(string name, Vector3 pos, Vector2 dir, float speed, int damage, Color color, float radius)
    {
        GameObject proj = new GameObject(name);
        proj.transform.position = pos;
        proj.layer = LayerMask.NameToLayer("Player");

        SpriteRenderer sr = proj.AddComponent<SpriteRenderer>();
        sr.color = color;
        sr.sortingLayerName = "player";
        sr.sortingOrder = 10;
        sr.sprite = CreateCircleSprite(radius);

        Rigidbody2D rb = proj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = dir.normalized * speed;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = proj.AddComponent<CircleCollider2D>();
        col.radius = radius;
        col.isTrigger = true;

        Projectile projScript = proj.AddComponent<Projectile>();
        projScript.damage = damage;
        projScript.lifetime = 3f;

        proj.AddComponent<ProjectileVFX>();
        return proj;
    }

    Sprite CreateCircleSprite(float radius)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        float center = size / 2f;
        float r = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist < r)
                {
                    float alpha = Mathf.Clamp01(1f - (dist / r) * 0.3f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / (radius * 2));
    }
}
