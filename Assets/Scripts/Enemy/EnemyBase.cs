using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PATCH 5 CHANGE: Added xpReward field. Enemies now give XP on death.
/// Default 25 XP for mobs. Set higher for bosses in Inspector.
/// </summary>
public class EnemyBase : MonoBehaviour
{
    public enum EnemyCategory { Mob, Boss }

    [Header("Enemy Config")]
    public EnemyCategory category = EnemyCategory.Mob;
    public string enemyName = "Enemy";

    [Header("Rewards")]
    [Tooltip("XP given to player on death. Mobs ~25, Bosses ~100-200.")]
    public int xpReward = 25;

    [Header("Health")]
    public Health health;

    [Header("Health Bar")]
    public bool showHealthBar = true;
    public Vector3 healthBarOffset = new Vector3(0, 1.5f, 0);

    private GameObject healthBarObj;
    private Image healthBarFill;
    private Image healthBarBg;
    private TextMeshProUGUI nameLabel;

    private float slowTimer = 0f;
    private bool isSlowed = false;

    protected bool isDead = false;
    private SpriteRenderer spriteRenderer;

    protected virtual void Awake()
    {
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected virtual void OnEnable()
    {
        if (health != null)
        {
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }
    }

    protected virtual void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= OnDamaged;
            health.OnDeath -= OnDeath;
        }
    }

    protected virtual void Start()
    {
        if (showHealthBar)
            CreateHealthBar();
    }

    protected virtual void Update()
    {
        if (healthBarObj != null)
            healthBarObj.transform.position = transform.position + healthBarOffset;

        if (isSlowed)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0) RemoveSlow();
        }
    }

    // ============ HEALTH BAR ============

    private void CreateHealthBar()
    {
        healthBarObj = new GameObject($"{enemyName}_HealthBar");
        healthBarObj.transform.position = transform.position + healthBarOffset;

        Canvas canvas = healthBarObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        RectTransform canvasRt = healthBarObj.GetComponent<RectTransform>();
        float barWidth = category == EnemyCategory.Boss ? 3f : 1.5f;
        float barHeight = category == EnemyCategory.Boss ? 0.5f : 0.25f;
        canvasRt.sizeDelta = new Vector2(barWidth, barHeight);
        canvasRt.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        GameObject bgObj = new GameObject("Bg");
        bgObj.transform.SetParent(healthBarObj.transform, false);
        healthBarBg = bgObj.AddComponent<Image>();
        healthBarBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(healthBarObj.transform, false);
        healthBarFill = fillObj.AddComponent<Image>();
        healthBarFill.color = category == EnemyCategory.Boss
            ? new Color(0.9f, 0.2f, 0.1f, 1f)
            : new Color(0.8f, 0.3f, 0.3f, 1f);
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        healthBarFill.fillAmount = 1f;
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0); fillRt.anchorMax = new Vector2(1, 1);
        fillRt.offsetMin = new Vector2(2, 2); fillRt.offsetMax = new Vector2(-2, -2);

        if (category == EnemyCategory.Boss)
        {
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(healthBarObj.transform, false);
            nameLabel = nameObj.AddComponent<TextMeshProUGUI>();
            nameLabel.text = enemyName;
            nameLabel.fontSize = 14;
            nameLabel.color = Color.white;
            nameLabel.alignment = TextAlignmentOptions.Center;
            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 1); nameRt.anchorMax = new Vector2(1, 1);
            nameRt.pivot = new Vector2(0.5f, 0);
            nameRt.anchoredPosition = new Vector2(0, 5);
            nameRt.sizeDelta = new Vector2(0, 20);
        }

        if (category == EnemyCategory.Mob)
            healthBarObj.SetActive(false);
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null || health == null) return;
        healthBarFill.fillAmount = (float)health.currentHealth / health.maxHealth;
        if (category == EnemyCategory.Mob && healthBarObj != null && health.currentHealth < health.maxHealth)
            healthBarObj.SetActive(true);
    }

    // ============ DAMAGE / DEATH ============

    protected virtual void OnDamaged()
    {
        UpdateHealthBar();
        if (spriteRenderer != null) StartCoroutine(DamageFlash());
    }

    protected virtual void OnDeath()
    {
        if (isDead) return;
        isDead = true;

        // Give XP and chaos to player
        if (GameManager.Instance != null)
            GameManager.Instance.OnEnemyKilled(xpReward);

        if (healthBarObj != null) Destroy(healthBarObj);
        Destroy(gameObject, 1f);
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = isSlowed ? Color.cyan : original;
    }

    // ============ SLOW ============

    public void ApplySlow(float duration)
    {
        isSlowed = true;
        slowTimer = duration;
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.5f, 0.8f, 1f, 1f);
    }

    private void RemoveSlow()
    {
        isSlowed = false;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    public float GetSpeedMultiplier() => isSlowed ? 0.4f : 1f;
    public bool IsDead => isDead;

    void OnDestroy()
    {
        if (healthBarObj != null) Destroy(healthBarObj);
    }
}
