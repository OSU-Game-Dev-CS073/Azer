using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 技能调度：按顺序循环执行四个技能
/// 1. 激光射击 2. 冲撞 3. 散射子弹 4. 陨石坠落
/// 技能之间间隔 3-5 秒
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossSkillControl : MonoBehaviour
{
    #region 枚举与数据结构
    private enum SkillType
    {
        Laser,      // 激光射击
        Dash,       // 冲撞
        ScatterShot,// 散射子弹
        Meteor      // 陨石坠落
    }

    private enum Phase
    {
        Idle,       // 等待下一个技能
        Laser,
        Dash,
        ScatterShot,
        Meteor
    }
    #endregion

    #region 序列化字段 - 调试与预制体
    // 调试日志开关已移除：按需求固定行为运行

    [Header("Sound Effects/音效（拖拽 AudioClip）")]
    [Tooltip("直射：每发一颗子弹播放一次（短音效，哒哒哒）")]
    public AudioClip sfxLaserShot;
    [Tooltip("散射：每一波(每波)播放一次")]
    public AudioClip sfxScatterVolley;
    [Tooltip("冲撞：每次释放播放一次")]
    public AudioClip sfxDash;
    [Tooltip("陨石：每次技能播放一次")]
    public AudioClip sfxMeteor;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Tooltip("可选：拖一个 AudioSource（推荐）；不拖则运行时临时创建并在播放结束后销毁")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Skill Prefabs / 技能预制体")]
    [Tooltip("激光子弹预制体（直线移动，飞出屏幕后自毁）")]
    public GameObject laserPrefab;
    [Tooltip("散射子弹预制体（多个角度发射）")]
    public GameObject scatterShotPrefab;   // 原 fireballPrefab
    [Tooltip("陨石预制体（从上方掉落，带碰撞伤害）")]
    public GameObject meteorPrefab;

    [Header("Scene References / 场景引用")]
    [Tooltip("子弹生成点（通常为Boss的嘴部）")]
    [SerializeField] private Transform mouthSpawn;
    [Tooltip("冲撞的目标（通常拖玩家 Transform）；不拖则运行时按 Tag=Player 自动寻找")]
    [SerializeField] private Transform playerTarget;

    // 开战后才开始技能：固定使用 EnemyController.detectionRange 作为进入战斗阈值

    [Header("Flying & Rigidbody Settings / 飞行&刚体设置")]
    // 固定为飞行 Boss：gravityScale=0、freezeRotation=true
    [SerializeField] private float flyingLinearDrag = 0f;

    [Header("Return Height After Dash/ Dash 后回到高度")]
    // 固定：开局记录 homeY；每次冲撞结束后缓慢回到该高度
    [Tooltip("回到 homeY 的速度（单位/秒）")]
    public float returnToHomeYSpeed = 6f;
    [Tooltip("回到 homeY 的允许误差")]
    public float returnToHomeYTolerance = 0.05f;

    [Header("Laser Recovery/激光后摇")]
    [Tooltip("激光发射后等待多久再切下一个技能（秒）")]
    public float laserCastTime = 0.5f;

    [Header("技能参数 - 激光")]
    [Tooltip("激光移动速度")]
    public float laserSpeed = 20f;
    [Tooltip("激光存在最长时间（秒），防止飞出屏幕后残留")]
    public float laserLifetime = 3f;
    [Tooltip("直射子弹伤害")]
    public float laserDamage = 8f;
    [Tooltip("直射连发次数")]
    public int laserShotCount = 10;
    [Tooltip("每发之间间隔（秒）")]
    public float laserShotInterval = 0.2f;

    [Header("技能参数 - 冲撞")]
    public float dashSpeed = 12f;
    public float dashDamage = 20f;
    [Tooltip("不使用空气墙时，冲撞持续多久后自动结束")]
    public float dashDuration = 2.5f;
    // 不使用空气墙结束冲撞：固定按 dashDuration 结束

    [Header("技能参数 - 散射子弹")]
    [Tooltip("散射子弹波数")]
    public int scatterVolleyCount = 3;
    [Tooltip("每波之间的间隔（秒）")]
    public float scatterVolleyDelay = 0.6f;
    [Tooltip("子弹散射角度范围（度）")]
    public float scatterAngleRange = 60f;
    [Tooltip("子弹数量")]
    public int bulletCountPerVolley = 7;
    [Tooltip("散射子弹飞行速度（Rigidbody2D）")]
    public float scatterBulletSpeed = 10f;
    [Tooltip("散射子弹生命周期（秒）")]
    public float scatterBulletLifetime = 5f;
    [Tooltip("散射子弹出生点相对嘴的前向偏移（模拟原项目：Mouth.position + dir * 1.0f）")]
    public float scatterSpawnOffset = 1.0f;
    // 散射固定为：以水平为中心，上下对称展开（不再提供 Legacy 开关/步进）
    [Tooltip("散射子弹伤害")]
    public float scatterDamage = 8f;

    [Header("技能参数 - 陨石坠落")]
    [Tooltip("陨石波数")]
    public int meteorWaves = 5;
    [Tooltip("每波陨石数量")]
    public int meteorPerWave = 6;
    [Tooltip("波次间隔（秒）")]
    public float meteorWaveDelay = 0.2f;
    [Tooltip("陨石生成X偏移范围（以玩家当前 X 为中心）")]
    public float meteorSpawnXMin = -12f;
    public float meteorSpawnXMax = 12f;
    [Tooltip("陨石生成Y坐标")]
    public float meteorSpawnY = 8f;
    [Tooltip("陨石伤害")]
    public float meteorDamage = 15f;

    [Header("技能间隔")]
    [Tooltip("技能结束后等待的最小时间（秒）")]
    public float minInterval = 3f;
    [Tooltip("技能结束后等待的最大时间（秒）")]
    public float maxInterval = 5f;

    #endregion

    #region 私有变量
    private Rigidbody2D _rb;
    private Transform _mouth;
    private Vector3 _initialScale;
    private Phase _currentPhase = Phase.Idle;
    private bool _skillRunning;
    private Coroutine _skillCoroutine;
    private float _idleTimer;
    private int _currentSkillIndex;
    private readonly List<SkillType> _skillSequence = new List<SkillType>
    {
        SkillType.Laser,
        SkillType.Dash,
        SkillType.ScatterShot,
        SkillType.Meteor
    };
    private float _dashTimer;          // 冲撞计时
    private bool _dashEnded;           // 空气墙触发结束（由碰撞回调置位）
    private Vector2 _dashDir;          // 冲撞方向（锁定目标方向）
    private float _homeY;
    private EnemyController _enemyController;
    private bool _combatStarted;
    #endregion

    #region Unity 生命周期
    private void OnEnable() { }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _enemyController = GetComponent<EnemyController>();
        if (_rb != null)
        {
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.linearDamping = Mathf.Max(0f, flyingLinearDrag);
        }

        _mouth = mouthSpawn != null ? mouthSpawn : transform;
        _initialScale = transform.localScale;

        if (mouthSpawn == null)
            Debug.LogWarning($"{nameof(BossSkillControl)}: mouthSpawn 未赋值，将使用自身 Transform 作为生成点。", this);

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (playerTarget == null)
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) playerTarget = playerGo.transform;
        }

        // 需求：无需可选调试开关，避免控制台噪声
    }

    private void Start()
    {
        // 初始进入 Idle，等待一段时间后开始第一个技能
        _homeY = transform.position.y;
        _combatStarted = false; // 固定：开战后才开始技能
        _idleTimer = Random.Range(minInterval, maxInterval);
        _currentPhase = Phase.Idle;
    }

    private void Update()
    {
        switch (_currentPhase)
        {
            case Phase.Idle:
                TickIdle();
                break;
            case Phase.Dash:
                ApplyDashVelocity();
                TickDash();
                break;
            // 其他技能阶段在协程中处理，不需要 Update 逻辑
        }
    }
    #endregion

    #region 技能调度核心
    private void TickIdle()
    {
        if (_skillRunning) return;

        if (!_combatStarted)
        {
            _combatStarted = CheckCombatStarted();
            if (!_combatStarted)
                return; // 未开战：不走技能计时
            // 需求：进入战斗后立刻开始释放技能（不等待）
            _idleTimer = 0f;
        }

        _idleTimer -= Time.deltaTime;
        if (_idleTimer <= 0f)
        {
            StartNextSkill();
        }
    }

    private bool CheckCombatStarted()
    {
        if (playerTarget == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) playerTarget = go.transform;
        }
        if (playerTarget == null) return false;

        float dist = Vector2.Distance(transform.position, playerTarget.position);

        // 优先：使用同物体 EnemyController 的 detectionRange 作为“进入战斗”阈值
        if (_enemyController == null)
        {
            Debug.LogError($"{nameof(BossSkillControl)}: 缺少 EnemyController，无法按进入战斗状态启动技能。", this);
            return false;
        }

        return dist <= _enemyController.detectionRange;
    }

    /// <summary>
    /// 按顺序开始下一个技能
    /// </summary>
    private void StartNextSkill()
    {
        if (_skillRunning) return;

        SkillType nextSkill = _skillSequence[_currentSkillIndex];
        _currentSkillIndex = (_currentSkillIndex + 1) % _skillSequence.Count;

        // 固定行为：不输出过程日志

        // 停止所有旧协程
        if (_skillCoroutine != null)
            StopCoroutine(_skillCoroutine);
        _skillRunning = true;

        switch (nextSkill)
        {
            case SkillType.Laser:
                _currentPhase = Phase.Laser;
                _skillCoroutine = StartCoroutine(CoLaserSkill());
                break;
            case SkillType.Dash:
                _currentPhase = Phase.Dash;
                _dashTimer = 0f;
                _dashEnded = false;
                _dashDir = GetDashDirectionToPlayer();
                _skillCoroutine = StartCoroutine(CoDashSkill());
                break;
            case SkillType.ScatterShot:
                _currentPhase = Phase.ScatterShot;
                _skillCoroutine = StartCoroutine(CoScatterShotSkill());
                break;
            case SkillType.Meteor:
                _currentPhase = Phase.Meteor;
                _skillCoroutine = StartCoroutine(CoMeteorSkill());
                break;
        }
    }

    /// <summary>
    /// 当前技能结束，进入等待间隔状态
    /// </summary>
    private void FinishCurrentSkill()
    {
        // 注意：需要在协程内部 StopCoroutine 自己，避免状态不同步/重入被打断
        _skillCoroutine = null;
        _skillRunning = false;
        _currentPhase = Phase.Idle;
        float interval = Random.Range(minInterval, maxInterval);
        _idleTimer = interval;
        // 确保停止所有移动
        _rb.linearVelocity = Vector2.zero;

        // 固定行为：不输出过程日志
    }
    #endregion

    #region 具体技能实现（协程）

    /// <summary>
    /// 技能1：激光射击 — 连续发射一条直线激光，飞出屏幕后自动销毁
    /// </summary>
    private IEnumerator CoLaserSkill()
    {
        // 固定行为：不输出过程日志
        _rb.linearVelocity = Vector2.zero;

        if (laserPrefab == null || _mouth == null)
        {
            Debug.LogError($"{nameof(BossSkillControl)}: 激光预制体或生成点为空，无法发射激光！", this);
            yield return new WaitForSeconds(laserCastTime);
            FinishCurrentSkill();
            yield break;
        }

        int shots = Mathf.Max(1, laserShotCount);
        float interval = Mathf.Max(0f, laserShotInterval);

        for (int s = 0; s < shots; s++)
        {
            // 每一发都确保朝向玩家（玩家跑到左边时也会立刻改向）
            Vector2 direction = GetAimToPlayerCenterFromMouth();

            PlaySfx(sfxLaserShot, _mouth != null ? _mouth.position : transform.position);
            GameObject laser = Instantiate(laserPrefab, _mouth.position, Quaternion.identity);
            ApplyProjectileVisualFacing(laser, direction);
            // 直射伤害
            {
                var dmg = laser.GetComponent<BossSkillProjectileDamage>();
                if (dmg == null) dmg = laser.AddComponent<BossSkillProjectileDamage>();
                dmg.damage = laserDamage;
                dmg.knockbackMode = BossSkillProjectileDamage.KnockbackMode.HorizontalFromVelocity;
                // BossSkillProjectileDamage 命中后必定销毁，不需要 hitOnce
            }
            LaserProjectile laserScript = laser.GetComponent<LaserProjectile>();
            if (laserScript != null)
            {
                laserScript.Initialize(direction, laserSpeed, laserLifetime);
            }
            else
            {
                var rbLaser = laser.GetComponent<Rigidbody2D>();
                if (rbLaser != null)
                    rbLaser.linearVelocity = direction * laserSpeed;
                Destroy(laser, laserLifetime);
            }

            if (s < shots - 1 && interval > 0f)
                yield return new WaitForSeconds(interval);
        }

        // 后摇
        if (laserCastTime > 0f)
            yield return new WaitForSeconds(laserCastTime);
        FinishCurrentSkill();
    }

    private Vector2 GetAimToPlayerCenterFromMouth()
    {
        if (playerTarget == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) playerTarget = go.transform;
        }

        if (playerTarget == null)
            return GetHorizontalAimToPlayer();

        Vector2 from = _mouth != null ? (Vector2)_mouth.position : (Vector2)transform.position;
        Vector2 to = playerTarget.position;
        Vector2 dir = to - from;
        if (dir.sqrMagnitude < 0.0001f)
            return GetHorizontalAimToPlayer();

        // 同步外观朝向（只翻转 X，不改变其它缩放）
        float sign = Mathf.Sign(dir.x);
        if (sign != 0 && Mathf.Sign(transform.localScale.x) != sign)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * sign, transform.localScale.y, transform.localScale.z);

        return dir.normalized;
    }

    private Vector2 GetHorizontalAimToPlayer()
    {
        if (playerTarget == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) playerTarget = go.transform;
        }

        Vector2 dir = Vector2.right;
        if (playerTarget != null)
            dir = (playerTarget.position.x >= transform.position.x) ? Vector2.right : Vector2.left;
        else
            dir = (transform.localScale.x >= 0) ? Vector2.right : Vector2.left;

        // 同步外观朝向（只翻转 X，不改变其它缩放）
        float sign = Mathf.Sign(dir.x);
        if (sign != 0 && Mathf.Sign(transform.localScale.x) != sign)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * sign, transform.localScale.y, transform.localScale.z);

        return dir;
    }

    /// <summary>
    /// 技能2：冲撞 — Boss向当前朝向高速移动，撞到玩家造成伤害，碰到空气墙或持续超时后结束
    /// </summary>
    private IEnumerator CoDashSkill()
    {
        // 固定行为：不输出过程日志
        _rb.linearVelocity = Vector2.zero; // 重置速度
        PlaySfx(sfxDash, transform.position);

        // 确保Boss面向移动方向（可选：已经由之前的技能决定，但为了安全）
        // 不主动翻转，保持现有方向

        float elapsed = 0f;

        // 等待结束条件：超时 或 空气墙触发（由碰撞回调置位 _dashEnded）
        while (elapsed < dashDuration && !_dashEnded)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Dash 结束：先停住，再回到 homeY（为下一次冲撞做准备）
        _rb.linearVelocity = Vector2.zero;
        yield return StartCoroutine(CoReturnToHomeY());

        FinishCurrentSkill();
    }

    private IEnumerator CoReturnToHomeY()
    {
        // 若刚体不存在（极少），直接跳过
        if (_rb == null) yield break;

        float speed = Mathf.Max(0.01f, returnToHomeYSpeed);
        float tol = Mathf.Max(0.001f, returnToHomeYTolerance);

        // 缓慢靠近目标高度；保证 x 速度为 0，避免飘移
        while (Mathf.Abs(_rb.position.y - _homeY) > tol)
        {
            float dy = _homeY - _rb.position.y;
            float vy = Mathf.Clamp(dy, -1f, 1f) * speed;
            _rb.linearVelocity = new Vector2(0f, vy);
            yield return null;
        }

        // 对齐到目标高度（避免无限逼近）
        _rb.linearVelocity = Vector2.zero;
        _rb.position = new Vector2(_rb.position.x, _homeY);
    }

    // 冲撞期间持续施加速度（在Update中调用）
    private void ApplyDashVelocity()
    {
        if (_currentPhase != Phase.Dash) return;
        if (_dashDir == Vector2.zero)
            _dashDir = GetDashDirectionToPlayer();
        _rb.linearVelocity = _dashDir.normalized * dashSpeed;
    }

    private Vector2 GetDashDirectionToPlayer()
    {
        // 锁定冲撞方向：优先朝玩家位置；找不到玩家就按当前朝向左右
        if (playerTarget == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) playerTarget = go.transform;
        }

        Vector2 dir;
        if (playerTarget != null)
        {
            dir = (playerTarget.position - transform.position);
            if (dir.sqrMagnitude < 0.0001f)
                dir = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            dir = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        }

        // 可选：让贴图朝向玩家（仅影响外观）
        if (dir.x != 0)
        {
            float sign = Mathf.Sign(dir.x);
            if (Mathf.Sign(transform.localScale.x) != sign)
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * sign, transform.localScale.y, transform.localScale.z);
        }

        return dir.normalized;
    }

    private void TickDash()
    {
        if (_currentPhase != Phase.Dash) return;
        // Dash 的结束由协程计时/空气墙触发控制，这里无需逻辑
    }

    /// <summary>
    /// 技能3：散射子弹 — 类似于魂斗罗散弹枪，多波次多角度发射子弹
    /// </summary>
    private IEnumerator CoScatterShotSkill()
    {
        // 固定行为：不输出过程日志
        _rb.linearVelocity = Vector2.zero;

        for (int wave = 0; wave < scatterVolleyCount; wave++)
        {
            SpawnScatterVolley();
            if (wave < scatterVolleyCount - 1)
                yield return new WaitForSeconds(scatterVolleyDelay);
        }

        yield return new WaitForSeconds(0.2f); // 略微停顿
        FinishCurrentSkill();
    }

    private void SpawnScatterVolley()
    {
        if (scatterShotPrefab == null)
        {
            Debug.LogError($"{nameof(BossSkillControl)}: 散射子弹预制体未赋值！", this);
            return;
        }

        if (_mouth == null) return;

        PlaySfx(sfxScatterVolley, _mouth.position);

        // 固定：以水平(0°)为中心，上下对称展开扇形
        Vector2 forward = GetHorizontalAimToPlayer().normalized;
        int count = Mathf.Max(1, bulletCountPerVolley);
        float range = Mathf.Max(0f, scatterAngleRange);
        float step = (count <= 1) ? 0f : (range / (count - 1));
        float startAngle = -range * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + i * step;
            Vector2 dir = (Vector2)(Quaternion.Euler(0f, 0f, angle) * forward);

            GameObject bullet = Instantiate(scatterShotPrefab, null);
            bullet.transform.position = _mouth.position + (Vector3)(dir * scatterSpawnOffset);
            ApplyProjectileVisualFacing(bullet, dir);
            // 散射伤害
            {
                var dmg = bullet.GetComponent<BossSkillProjectileDamage>();
                if (dmg == null) dmg = bullet.AddComponent<BossSkillProjectileDamage>();
                dmg.damage = scatterDamage;
                dmg.knockbackMode = BossSkillProjectileDamage.KnockbackMode.HorizontalFromVelocity;
            }

            var rbBullet = bullet.GetComponent<Rigidbody2D>();
            if (rbBullet != null)
                rbBullet.linearVelocity = dir.normalized * scatterBulletSpeed;

            Destroy(bullet, scatterBulletLifetime);
        }
    }

    /// <summary>
    /// 技能4：陨石坠落 — 从屏幕上方掉落多波陨石
    /// </summary>
    private IEnumerator CoMeteorSkill()
    {
        // 固定行为：不输出过程日志
        _rb.linearVelocity = Vector2.zero;
        PlaySfx(sfxMeteor, transform.position);

        for (int wave = 0; wave < meteorWaves; wave++)
        {
            for (int i = 0; i < meteorPerWave; i++)
            {
                SpawnOneMeteor();
            }
            if (wave < meteorWaves - 1)
                yield return new WaitForSeconds(meteorWaveDelay);
        }

        yield return new WaitForSeconds(0.5f);
        FinishCurrentSkill();
    }

    private void SpawnOneMeteor()
    {
        if (meteorPrefab == null)
        {
            Debug.LogError($"{nameof(BossSkillControl)}: 陨石预制体未赋值！", this);
            return;
        }

        float randomOffsetX = Random.Range(meteorSpawnXMin, meteorSpawnXMax);
        // 以玩家为中心生成（玩家不存在则回退到 Boss）
        float centerX = playerTarget != null ? playerTarget.position.x : transform.position.x;
        float x = centerX + randomOffsetX;
        Vector3 spawnPos = new Vector3(x, meteorSpawnY, 0f);
        GameObject meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);
        // 陨石伤害 + 落地爆炸销毁
        {
            var impact = meteor.GetComponent<BossSkillMeteorImpact>();
            if (impact == null) impact = meteor.AddComponent<BossSkillMeteorImpact>();
            impact.damage = meteorDamage;
        }
        // 假设陨石自带向下移动或重力脚本，这里仅生成
        // 可选：添加上下移动组件
        MeteorMovement move = meteor.GetComponent<MeteorMovement>();
        if (move == null)
        {
            // 默认增加一个简单的下落逻辑
            var rb = meteor.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.down * 8f;
        }
        Destroy(meteor, 8f); // 超出屏幕销毁
    }
    #endregion

    #region 碰撞处理
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 固定：不依赖空气墙结束冲撞

        // 冲撞对玩家造成伤害
        if (_currentPhase == Phase.Dash && collision.collider.CompareTag("Player"))
        {
            if (collision.collider.TryGetComponent<Player>(out var player))
            {
                Vector2 knockDir = transform.position.x < collision.transform.position.x ? Vector2.right : Vector2.left;
                // 假设存在辅助类，若没有可自定义伤害逻辑
                BossSkillDamageHelper.ApplyHit(player, knockDir, dashDamage);
                // 固定行为：不输出过程日志
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 固定：不依赖空气墙结束冲撞
    }
    #endregion

    #region 辅助方法
    private void PlaySfx(AudioClip clip, Vector3 pos)
    {
        if (clip == null) return;

        float vol = Mathf.Clamp01(sfxVolume);

        // 推荐路径：复用已有 AudioSource，避免频繁创建对象（机枪音效尤其重要）
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, vol);
            return;
        }

        // 兜底：临时创建 AudioSource 并在播放结束后销毁
        var go = new GameObject($"_SFX_{clip.name}");
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D 声音（如需 3D 可改为 1）
        src.volume = vol;
        src.clip = clip;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    private static void ApplyProjectileVisualFacing(GameObject projectile, Vector2 moveDir)
    {
        if (projectile == null) return;
        if (moveDir.sqrMagnitude < 0.0001f) return;

        // 让“默认朝右”的图片转向实际移动方向（避免向左时看起来倒着/反着）
        bool left = moveDir.x < 0f;
        float x = Mathf.Abs(moveDir.x);
        float z = Mathf.Atan2(moveDir.y, x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, z);

        var sr = projectile.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.flipX = left;
    }

    private void OnDisable()
    {
        if (_skillCoroutine != null)
            StopCoroutine(_skillCoroutine);
        _skillRunning = false;
        _currentPhase = Phase.Idle;
        _dashEnded = false;
    }
    #endregion
}
