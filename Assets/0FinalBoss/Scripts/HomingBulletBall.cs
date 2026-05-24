using UnityEngine;

/// <summary>
/// Boss2 技能1 弹幕小球：飞向玩家位置，飞行一定时间后向玩家发射激光，然后自毁。
/// 小球自身碰撞也造成伤害。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class HomingBulletBall : MonoBehaviour
{
    [Header("运行时设置（由 Boss2SkillControl 初始化）")]
    public float speed = 5f;
    public float flyDuration = 3f;
    public float laserLifetime = 0.3f;
    public float ballDamage = 10f;
    public float laserDamage = 20f;

    [Header("激光音效（可选）")]
    public AudioClip laserSFX;

    [Header("激光预制体（可选，留空则动态生成）")]
    public GameObject laserPrefab;

    private Rigidbody2D _rb;
    private Animator _animator;
    private Transform _playerTarget;
    private float _timer;
    private bool _laserFired;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        TryFindPlayer();
    }

    public void Initialize(Vector2 initialDirection, float speed_, float flyDuration_,
        float laserLifetime_, float ballDamage_, float laserDamage_)
    {
        speed = speed_;
        flyDuration = Mathf.Max(0.5f, flyDuration_);
        laserLifetime = laserLifetime_;
        ballDamage = ballDamage_;
        laserDamage = laserDamage_;

        if (_rb != null)
            _rb.linearVelocity = initialDirection.normalized * speed;
    }

    private void TryFindPlayer()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) _playerTarget = playerGo.transform;
    }

    private void Update()
    {
        if (_laserFired) return;

        _timer += Time.deltaTime;
        if (_timer >= flyDuration)
        {
            FireLaserAndDestroy();
            return;
        }

        // 持续追踪玩家方向
        if (_playerTarget == null) TryFindPlayer();
        if (_playerTarget != null && _rb != null)
        {
            Vector2 dir = ((Vector2)_playerTarget.position - _rb.position).normalized;
            _rb.linearVelocity = dir * speed;

            // 旋转朝向移动方向
            float angle = Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg;
            if (dir.x < 0) angle = 180f - angle;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void FireLaserAndDestroy()
    {
        if (_laserFired) return;
        _laserFired = true;

        if (_rb != null) _rb.linearVelocity = Vector2.zero;
        PlayAnimation("Fire");
        if (laserSFX != null) AudioSource.PlayClipAtPoint(laserSFX, transform.position);

        if (_playerTarget == null) TryFindPlayer();
        Vector2 targetPos = _playerTarget != null ? (Vector2)_playerTarget.position : (Vector2)transform.position + Vector2.right;
        Vector2 from = transform.position;
        Vector2 dir = (targetPos - from).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        float distance = Vector2.Distance(from, targetPos);

        SpawnStationaryBeam(from, dir, distance);
        Destroy(gameObject, 0.1f);
    }

    public void FireLaserImmediately(Vector2 targetPos)
    {
        if (_laserFired) return;
        _laserFired = true;

        if (_rb != null) _rb.linearVelocity = Vector2.zero;
        PlayAnimation("Fire");

        Vector2 from = transform.position;
        Vector2 dir = (targetPos - from).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        float distance = Vector2.Distance(from, targetPos);

        SpawnStationaryBeam(from, dir, distance);
        Destroy(gameObject, 0.1f);
    }

    /// <summary>
    /// 生成一道固定光束：从小球位置向目标方向延伸，不移动，短暂停留后消失。
    /// </summary>
    private void SpawnStationaryBeam(Vector2 origin, Vector2 direction, float distance)
    {
        float beamLength = Mathf.Min(distance, 20f);

        if (laserPrefab != null)
        {
            // 光束从小球位置射出，朝向玩家
            GameObject laser = Instantiate(laserPrefab, origin, Quaternion.identity);
            SetupStationaryBeam(laser, direction, beamLength);
        }
        else
        {
            GameObject laser = CreateBeam(direction, beamLength);
            laser.transform.position = origin;
            SetupStationaryBeam(laser, direction, beamLength);
        }
    }

    private void SetupStationaryBeam(GameObject beam, Vector2 direction, float length)
    {
        float angle = Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
        if (direction.x < 0) angle = 180f - angle;
        beam.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 不移动 —— 移除 Rigidbody2D 速度
        var beamRb = beam.GetComponent<Rigidbody2D>();
        if (beamRb != null)
        {
            beamRb.linearVelocity = Vector2.zero;
            beamRb.bodyType = RigidbodyType2D.Kinematic;
        }

        // 光束长度适配
        var col = beam.GetComponent<BoxCollider2D>();
        if (col != null) col.size = new Vector2(length, col.size.y);

        var dmg = beam.GetComponent<BossSkillProjectileDamage>();
        if (dmg == null) dmg = beam.AddComponent<BossSkillProjectileDamage>();
        dmg.damage = laserDamage;
        dmg.knockbackMode = BossSkillProjectileDamage.KnockbackMode.HorizontalFromVelocity;

        Destroy(beam, Mathf.Max(0.1f, laserLifetime));
    }

    private GameObject CreateBeam(Vector2 direction, float length)
    {
        GameObject beam = new GameObject("LaserBeam");
        beam.transform.position = transform.position;

        var sr = beam.AddComponent<SpriteRenderer>();
        sr.sprite = CreateBeamSprite(length);
        sr.color = new Color(1f, 0.3f, 0.3f);
        sr.sortingOrder = 5;

        var rb = beam.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = beam.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(length, 0.3f);

        return beam;
    }

    private Sprite CreateBeamSprite(float length)
    {
        int w = Mathf.Max(16, Mathf.RoundToInt(length * 32));
        int h = 8;
        var tex = new Texture2D(w, h);
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float alpha = 1f - Mathf.Abs(y - h / 2f) / (h / 2f) * 0.5f;
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f));
    }

    private void PlayAnimation(string triggerName)
    {
        if (_animator != null && !string.IsNullOrEmpty(triggerName))
            _animator.SetTrigger(triggerName);
    }

    #region 小球碰撞伤害
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_laserFired) return;
        if (other == null || !other.CompareTag("Player")) return;
        if (!other.TryGetComponent<Player>(out var player)) return;

        Vector2 knockDir = _rb != null && _rb.linearVelocity.x >= 0 ? Vector2.right : Vector2.left;
        BossSkillDamageHelper.ApplyHit(player, knockDir, ballDamage);

        // 命中后不销毁，继续飞行（小球可多次碰撞？或者只碰一次）
        // 这里选择命中后仍继续飞行直到发射激光
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; // 防止重复碰撞
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_laserFired) return;
        if (collision.collider == null || !collision.collider.CompareTag("Player")) return;
        if (!collision.collider.TryGetComponent<Player>(out var player)) return;

        Vector2 knockDir = transform.position.x < collision.transform.position.x ? Vector2.right : Vector2.left;
        BossSkillDamageHelper.ApplyHit(player, knockDir, ballDamage);

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }
    #endregion
}
