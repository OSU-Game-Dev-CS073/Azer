using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Boss2 技能调度：按顺序循环执行四个技能
/// 1. 普通弹幕 2. 地刺攻击1(锁定) 3. 地刺攻击2(全屏) 4. 遁入虚空
/// 技能之间间隔可配；血量 ≤10% 时进入狂暴模式
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Boss2SkillControl : MonoBehaviour
{
    #region 枚举
    private enum SkillType
    {
        BulletBall,
        Spike1,
        Spike2,
        VoidAmbush
    }

    private enum Phase
    {
        Ready,
        Idle,
        BulletBall,
        Spike1,
        Spike2,
        VoidAmbush,
        Enrage
    }
    #endregion

    #region 音效
    [Header("音效")]
    [Tooltip("技能1 发射小球")]
    public AudioClip sfxBulletBall;
    [Tooltip("技能1/4 小球发射激光")]
    public AudioClip sfxLaserShot;
    [Tooltip("技能2 地刺出现")]
    public AudioClip sfxSpike1;
    [Tooltip("技能3 全屏地刺出现")]
    public AudioClip sfxSpike2;
    [Tooltip("技能4 Boss 遁入虚空（隐藏）")]
    public AudioClip sfxVoidAmbush;
    [Tooltip("技能4 Boss 出现")]
    public AudioClip sfxVoidAppear;
    [Tooltip("技能4 红圈旋转（循环）")]
    public AudioClip sfxVoidCircleLoop;
    [Tooltip("音效播放器预制体（留空则自动生成）")]
    public GameObject sfxPrefab;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("BGM")]
    [Tooltip("正常战斗 BGM（进入战斗后开始循环播放）")]
    public AudioClip normalBGM;
    [Tooltip("狂暴模式 BGM（血量低于阈值后切换）")]
    public AudioClip enrageBGM;
    [Range(0f, 1f)] public float bgmVolume = 0.8f;
    [SerializeField] private AudioSource bgmSource;
    #endregion

    #region 技能预制体
    [Header("技能预制体")]
    [Tooltip("弹幕小球预制体（带 HomingBulletBall 组件）")]
    public GameObject bulletBallPrefab;
    [Tooltip("技能2 地板预警标记（纯视觉，红色闪烁）")]
    public GameObject spike1WarningPrefab;
    [Tooltip("地刺预制体（用于技能2锁定地刺，带 Collider2D+SpikeDamage+Animator）")]
    public GameObject spikePrefab;
    [Tooltip("技能3 全屏预警标记（纯视觉，红色长条闪烁）")]
    public GameObject floorWarningPrefab;
    [Tooltip("地刺预制体（用于技能3全屏地刺，矮版，带 Collider2D+SpikeDamage+Animator）")]
    public GameObject floorSpikePrefab;
    [Tooltip("虚空突袭红圈预警预制体")]
    public GameObject voidWarningPrefab;
    [Tooltip("虚空突袭小球预制体（带 HomingBulletBall；留空则复用技能1的弹幕小球）")]
    public GameObject voidBulletBallPrefab;
    #endregion

    #region 场景引用
    [Header("场景引用")]
    [Tooltip("子弹/弹幕生成点（通常为 Boss 中心或嘴部）")]
    [SerializeField] private Transform mouthSpawn;
    [Tooltip("玩家 Transform；不拖则运行时按 Tag=Player 自动寻找")]
    [SerializeField] private Transform playerTarget;
    #endregion

    #region 出场 / Ready
    [Header("出场动画")]
    [Tooltip("玩家进入此距离后 Boss 播放 Ready 出场动画")]
    public float readyTriggerDistance = 15f;
    #endregion

    #region 基础设置
    [Header("基础设置")]
    [Tooltip("地面 Y 坐标（地刺 / 预警生成用）")]
    public float groundY = -4f;
    [SerializeField] private float flyingLinearDrag = 0f;
    #endregion

    #region 技能1 - 普通弹幕
    [Header("技能1 - 普通弹幕")]
    [Tooltip("小球飞行速度")]
    public float bulletBallSpeed = 5f;
    [Tooltip("小球飞行多长时间后发射激光（秒）")]
    public float bulletBallFlyDuration = 3f;
    [Tooltip("小球飞行时间浮动范围（秒），实际 = flyDuration + Random.Range(0, flyDurationVariance)")]
    public float bulletBallFlyDurationVariance = 2f;
    [Tooltip("激光存在时间（秒）")]
    public float bulletBallLaserLifetime = 0.3f;
    [Tooltip("小球碰撞伤害")]
    public float bulletBallDamage = 10f;
    [Tooltip("激光伤害")]
    public float bulletBallLaserDamage = 20f;
    [Tooltip("每波发射小球数量")]
    public int bulletBallCount = 2;
    [Tooltip("每颗小球发射间隔（秒）")]
    public float bulletBallInterval = 0.5f;
    [Tooltip("动画第几秒生成弹幕小球（默认动画第 0 秒）")]
    public float skill1SpawnDelay = 0f;
    [Tooltip("音效：动画第几秒播放发射小球音效")]
    public float skill1SfxDelay = 0f;
    #endregion

    #region 技能2 - 地刺攻击1（锁定玩家位置）
    [Header("技能2 - 地刺攻击1（锁定玩家）")]
    [Tooltip("动画第几秒开始地板预警（默认动画第 0 秒）")]
    public float skill2ChargeDelay = 0f;
    [Tooltip("音效：动画第几秒播放地刺音效")]
    public float skill2SfxDelay = 0f;
    [Tooltip("地板预警循环时间（秒）")]
    public float spike1WarningDuration = 3f;
    [Tooltip("地刺伤害")]
    public float spike1Damage = 15f;
    [Tooltip("每根地刺存在时间（秒）")]
    public float spike1Duration = 0.8f;
    #endregion

    #region 技能3 - 地刺攻击2（全屏低刺）
    [Header("技能3 - 地刺攻击2（全屏）")]
    [Tooltip("动画第几秒开始全屏预警（默认动画第 0 秒）")]
    public float skill3ChargeDelay = 0f;
    [Tooltip("音效：动画第几秒播放全屏地刺音效")]
    public float skill3SfxDelay = 0f;
    [Tooltip("全屏预警时间（秒）")]
    public float skill3WarningDuration = 2f;
    [Tooltip("全屏地刺以玩家为中心左右各覆盖多远")]
    public float floorSpikeRange = 16f;
    [Tooltip("地刺数量（均匀分布）")]
    public int floorSpikeCount = 30;
    [Tooltip("全屏地刺存在时间（秒）")]
    public float floorSpikeDuration = 2f;
    [Tooltip("全屏地刺伤害")]
    public float floorSpikeDamage = 12f;
    #endregion

    #region 技能4 - 遁入虚空
    [Header("技能4 - 遁入虚空")]
    [Tooltip("Boss 遁入虚空持续时间（秒），之后出现")]
    public float skill4AppearSfxDelay = 2f;
    [Tooltip("动画第几秒出现红圈")]
    public float skill4CircleDelay = 0.5f;
    [Tooltip("红圈旋转持续时间（秒）")]
    public float voidWarningDuration = 1f;
    [Tooltip("红圈视觉半径")]
    public float voidCircleRadius = 3f;
    [Tooltip("小球生成半径")]
    public float voidBulletSpawnRadius = 4f;
    [Tooltip("红圈周圈小球数量")]
    public int voidBulletCount = 8;
    [Tooltip("小球出现后延迟多久发射激光（秒），0 = 瞬间")]
    public float voidBulletFireDelay = 0f;
    [Tooltip("小球激光伤害")]
    public float voidBulletDamage = 25f;
    #endregion

    #region 技能间隔（普通状态）
    [Header("技能间隔（普通状态）")]
    public float minInterval = 3f;
    public float maxInterval = 5f;
    #endregion

    #region 狂暴模式
    [Header("狂暴模式（血量 ≤ 此百分比触发）")]
    [Range(0f, 1f)] public float enrageHealthPercent = 0.1f;
    [Tooltip("Boss 飞到空中的目标 Y 坐标")]
    public float enrageFlyY = 6f;
    [Tooltip("飞到空中的速度")]
    public float enrageFlySpeed = 8f;
    [Tooltip("狂暴中技能之间间隔最小（秒）")]
    public float enrageMinInterval = 0.5f;
    [Tooltip("狂暴中技能之间间隔最大（秒）")]
    public float enrageMaxInterval = 1f;
    [Tooltip("落地休息时间（秒）")]
    public float enrageRestTime = 10f;
    [Tooltip("每次飞到空中循环 1-2-3 的次数")]
    public int enrageCycleCount = 3;
    [Tooltip("Cinemachine 虚拟摄像机")]
    public CinemachineCamera cineCamera;
    [Tooltip("狂暴时摄像机 Orthographic Size（仅缩放，不改变跟随）")]
    public float enrageCameraLensSize = 13f;
    [Tooltip("狂暴摄像机镜头过渡时间（秒）")]
    public float enrageCameraLensChangeDuration = 1.5f;
    #endregion

    #region 私有变量
    private Rigidbody2D _rb;
    private Transform _mouth;
    private Health _health;
    private EnemyController _enemyController;
    private Animator _animator;
    private Collider2D[] _bossColliders;

    private Phase _currentPhase = Phase.Idle;
    private bool _skillRunning;
    private Coroutine _skillCoroutine;
    private float _idleTimer;
    private int _currentSkillIndex;
    private bool _combatStarted;

    private readonly List<SkillType> _skillSequence = new List<SkillType>
    {
        SkillType.BulletBall,
        SkillType.Spike1,
        SkillType.Spike2,
        SkillType.VoidAmbush
    };

    private float _homeY;
    private bool _isInvincible;
    private bool _readyTriggered;
    private bool _enrageTriggered;
    private Coroutine _enrageCoroutine;

    private float _savedLensSize;
    #endregion

    #region Unity 生命周期
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _health = GetComponent<Health>();
        _enemyController = GetComponent<EnemyController>();
        _animator = GetComponent<Animator>();
        _bossColliders = GetComponentsInChildren<Collider2D>();

        if (_rb != null)
        {
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.linearDamping = Mathf.Max(0f, flyingLinearDrag);
        }

        _mouth = mouthSpawn != null ? mouthSpawn : transform;

        if (mouthSpawn == null)
            Debug.LogWarning($"{nameof(Boss2SkillControl)}: mouthSpawn 未赋值，使用自身 Transform。", this);

        if (playerTarget == null)
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) playerTarget = playerGo.transform;
        }
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.OnDamaged += OnBossDamaged;
            _health.OnDeath += OnBossDeath;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnDamaged -= OnBossDamaged;
            _health.OnDeath -= OnBossDeath;
        }
        if (_skillCoroutine != null)
            StopCoroutine(_skillCoroutine);
        if (_enrageCoroutine != null)
            StopCoroutine(_enrageCoroutine);
        _skillRunning = false;
        _currentPhase = Phase.Idle;
    }

    private void Start()
    {
        _homeY = transform.position.y;
        _combatStarted = false;
        _currentPhase = Phase.Ready;
        _enrageTriggered = false;
        _readyTriggered = false;

        // Ready 阶段：Boss 初始不可见，不追击
        SetBossVisible(false);
        if (_enemyController != null) _enemyController.attacksPlayer = false;

        if (cineCamera != null)
            _savedLensSize = cineCamera.Lens.OrthographicSize;
    }

    private void Update()
    {
        if (_currentPhase == Phase.Ready)
            TickReady();
        else if (_currentPhase == Phase.Idle)
            TickIdle();
    }
    #endregion

    #region Ready 出场
    private void TickReady()
    {
        if (_readyTriggered) return;

        // 检测玩家是否进入 Ready 距离
        float dist = Vector2.Distance(transform.position, GetPlayerPosition());
        if (dist <= readyTriggerDistance)
        {
            _readyTriggered = true;
            StartCoroutine(CoReadySequence());
        }
    }

    private IEnumerator CoReadySequence()
    {
        // 先切 Animator 到 Ready 状态（Boss 仍不可见）
        PlayBossAnimation("Ready");
        yield return null; // 等一帧让 Animator 过渡

        // Boss 现身，此时已在 Ready 动画中
        SetBossVisible(true);

        // 等 Ready 动画播完，Animator 通过 Exit Time 自动回到 Idle
        if (_animator != null)
        {
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            float length = stateInfo.length > 0f ? stateInfo.length : 2f;
            yield return new WaitForSeconds(length);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        // Ready 结束，恢复追击，进入 Idle，开始 BGM
        if (_enemyController != null) _enemyController.attacksPlayer = true;
        PlayBGM(normalBGM);
        _combatStarted = true;
        _idleTimer = 0f;
        _currentPhase = Phase.Idle;
    }
    #endregion

    #region 技能调度
    private void TickIdle()
    {
        if (_skillRunning) return;

        if (!_combatStarted)
        {
            _combatStarted = CheckCombatStarted();
            if (!_combatStarted) return;
            _idleTimer = 0f;
        }

        _idleTimer -= Time.deltaTime;
        if (_idleTimer <= 0f)
            StartNextSkill();
    }

    private bool CheckCombatStarted()
    {
        if (_enemyController == null) return true; // 无 EnemyController 则直接开战

        if (playerTarget == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) playerTarget = go.transform;
        }
        if (playerTarget == null) return false;

        float dist = Vector2.Distance(transform.position, playerTarget.position);
        return dist <= _enemyController.detectionRange;
    }

    private void StartNextSkill()
    {
        if (_skillRunning) return;
        if (_enrageTriggered && _currentPhase != Phase.Enrage) return; // 狂暴中不进入普通技能

        SkillType nextSkill = _skillSequence[_currentSkillIndex];
        _currentSkillIndex = (_currentSkillIndex + 1) % _skillSequence.Count;

        if (_skillCoroutine != null)
            StopCoroutine(_skillCoroutine);
        _skillRunning = true;

        switch (nextSkill)
        {
            case SkillType.BulletBall:
                _currentPhase = Phase.BulletBall;
                _skillCoroutine = StartCoroutine(CoBulletBallSkill());
                break;
            case SkillType.Spike1:
                _currentPhase = Phase.Spike1;
                _skillCoroutine = StartCoroutine(CoSpike1Skill());
                break;
            case SkillType.Spike2:
                _currentPhase = Phase.Spike2;
                _skillCoroutine = StartCoroutine(CoSpike2Skill());
                break;
            case SkillType.VoidAmbush:
                _currentPhase = Phase.VoidAmbush;
                _skillCoroutine = StartCoroutine(CoVoidAmbushSkill());
                break;
        }
    }

    private void FinishCurrentSkill()
    {
        _skillCoroutine = null;
        _skillRunning = false;
        float interval = Random.Range(minInterval, maxInterval);
        _idleTimer = interval;
        _rb.linearVelocity = Vector2.zero;

        // 如果不在狂暴中，回到 Idle
        if (_currentPhase != Phase.Enrage)
            _currentPhase = Phase.Idle;
    }
    #endregion

    #region 技能1 - 普通弹幕
    private IEnumerator CoBulletBallSkill()
    {
        _rb.linearVelocity = Vector2.zero;

        PlayBossAnimation("PlaySkill1");

        // 音效：按配置时间播放
        if (skill1SfxDelay > 0f)
            yield return new WaitForSeconds(skill1SfxDelay);
        PlaySfx(sfxBulletBall, _mouth.position);

        // 等待小球生成时间（如果音效时间晚于生成时间，则从音效时间算起）
        float remainingDelay = skill1SpawnDelay - skill1SfxDelay;
        if (remainingDelay > 0f)
            yield return new WaitForSeconds(remainingDelay);

        if (bulletBallPrefab == null || _mouth == null)
        {
            Debug.LogError("Boss2SkillControl: 弹幕预制体或生成点为空！", this);
            yield return new WaitForSeconds(0.5f);
            FinishCurrentSkill();
            yield break;
        }

        int count = Mathf.Max(1, bulletBallCount);
        for (int i = 0; i < count; i++)
        {
            SpawnBulletBall();
            if (i < count - 1 && bulletBallInterval > 0f)
                yield return new WaitForSeconds(bulletBallInterval);
        }

        yield return new WaitForSeconds(0.3f);
        FinishCurrentSkill();
    }

    private void SpawnBulletBall()
    {
        Vector2 dir = GetAimToPlayer();
        Vector3 spawnPos = _mouth.position + (Vector3)(dir * 0.5f);
        GameObject ball = Instantiate(bulletBallPrefab, spawnPos, Quaternion.identity);

        var homing = ball.GetComponent<HomingBulletBall>();
        if (homing == null)
            homing = ball.AddComponent<HomingBulletBall>();

        homing.Initialize(
            dir,
            bulletBallSpeed,
            bulletBallFlyDuration + Random.Range(0f, bulletBallFlyDurationVariance),
            bulletBallLaserLifetime,
            bulletBallDamage,
            bulletBallLaserDamage
        );
        homing.laserSFX = sfxLaserShot;

        ApplyProjectileVisualFacing(ball, dir);
    }
    #endregion

    #region 技能2 - 地刺攻击1（锁定玩家位置）
    private IEnumerator CoSpike1Skill()
    {
        _rb.linearVelocity = Vector2.zero;

        // 阶段1：Boss 动画 + 地板预警（循环闪烁）
        PlayBossAnimation("PlaySkill2");

        if (skill2ChargeDelay > 0f)
            yield return new WaitForSeconds(skill2ChargeDelay);

        if (spikePrefab == null)
        {
            Debug.LogError("Boss2SkillControl: 地刺预制体为空！", this);
            yield return new WaitForSeconds(0.5f);
            FinishCurrentSkill();
            yield break;
        }

        // 地板预警闪烁（跟随玩家移动）
        Vector2 playerPos = GetPlayerPosition();
        float spikeY = groundY;
        yield return StartCoroutine(ShowSpikeWarning(playerPos, spike1WarningDuration));

        // 阶段2：锁定预警结束时的玩家位置，地刺在此固定位置一次凸起
        // 音效：第 skill2SfxDelay 秒播放（默认与刺同步，即 chargeDelay + warningDuration）
        float sfxWait = skill2SfxDelay - (skill2ChargeDelay + spike1WarningDuration);
        if (sfxWait > 0f)
            yield return new WaitForSeconds(sfxWait);
        PlaySfx(sfxSpike1, transform.position);

        Vector2 lockedPos = GetPlayerPosition();
        float lockedSpikeY = groundY;

        Vector3 spawnPos = new Vector3(lockedPos.x, lockedSpikeY, 0f);
        GameObject spike = Instantiate(spikePrefab, spawnPos, Quaternion.identity);
        SetupSpikeDamage(spike, spike1Damage);
        Destroy(spike, spike1Duration);

        yield return new WaitForSeconds(spike1Duration);
        FinishCurrentSkill();
    }

    private IEnumerator ShowSpikeWarning(Vector2 position, float duration)
    {
        GameObject warning;
        if (spike1WarningPrefab != null)
        {
            warning = Instantiate(spike1WarningPrefab, position, Quaternion.identity);
        }
        else
        {
            warning = new GameObject("SpikeWarning");
            warning.transform.position = position;
            var sr = warning.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(32, Color.red, 0.6f);
            sr.sortingOrder = 10;
            warning.transform.localScale = Vector3.one * 0.5f;
        }

        var sr2 = warning.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (warning == null) yield break;
            elapsed += Time.deltaTime;

            // 预警期间跟随玩家脚底移动
            Vector2 playerPos = GetPlayerPosition();
            warning.transform.position = new Vector3(playerPos.x, groundY, 0f);

            float alpha = 0.4f + Mathf.PingPong(elapsed * 8f, 0.6f);
            if (sr2 != null) sr2.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }

        if (warning != null) Destroy(warning);
    }

    /// <summary>
    /// 全屏地板预警：以玩家 X 为中心固定位置，Y 在玩家脚底，不跟随移动。
    /// </summary>
    private IEnumerator ShowFloorWarning(float duration)
    {
        int markerCount = Mathf.Max(2, floorSpikeCount);
        float range = floorSpikeRange;
        float stepX = (range * 2f) / (markerCount - 1);
        float feetY = groundY;
        float centerX = GetPlayerPosition().x; // 锁定，不跟随
        float startX = centerX - range;

        var markers = new System.Collections.Generic.List<GameObject>();

        for (int i = 0; i < markerCount; i++)
        {
            float x = startX + i * stepX;
            Vector3 pos = new Vector3(x, feetY, 0f);
            GameObject marker;
            if (floorWarningPrefab != null)
            {
                marker = Instantiate(floorWarningPrefab, pos, Quaternion.identity);
            }
            else
            {
                marker = new GameObject("FloorWarning");
                marker.transform.position = pos;
                var sr = marker.AddComponent<SpriteRenderer>();
                sr.sprite = CreateColoredSpritePlain(32, 8, Color.red);
                sr.sortingOrder = 8;
                marker.transform.localScale = new Vector3(stepX / 2f, 0.3f, 1f);
            }
            markers.Add(marker);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 0.3f + Mathf.PingPong(elapsed * 6f, 0.5f);

            for (int i = 0; i < markerCount; i++)
            {
                var m = markers[i];
                if (m == null) continue;
                var s = m.GetComponent<SpriteRenderer>();
                if (s != null) s.color = new Color(1f, 0f, 0f, alpha);
            }
            yield return null;
        }

        foreach (var m in markers)
        {
            if (m != null) Destroy(m);
        }
    }
    #endregion

    #region 技能3 - 地刺攻击2（全屏低刺）
    private IEnumerator CoSpike2Skill()
    {
        _rb.linearVelocity = Vector2.zero;

        // 阶段1：Boss 动画 + 全屏预警
        PlayBossAnimation("PlaySkill3");

        if (skill3ChargeDelay > 0f)
            yield return new WaitForSeconds(skill3ChargeDelay);

        GameObject prefab = floorSpikePrefab != null ? floorSpikePrefab : spikePrefab;
        if (prefab == null)
        {
            Debug.LogError("Boss2SkillControl: 全屏地刺预制体为空！", this);
            yield return new WaitForSeconds(0.5f);
            FinishCurrentSkill();
            yield break;
        }

        // 全屏预警（地板大范围闪烁，位置固定不跟随）
        if (skill3WarningDuration > 0f)
            yield return StartCoroutine(ShowFloorWarning(skill3WarningDuration));

        // 阶段2：全屏低地刺一次生成（与预警相同位置，固定）
        float sfxWait3 = skill3SfxDelay - (skill3ChargeDelay + skill3WarningDuration);
        if (sfxWait3 > 0f)
            yield return new WaitForSeconds(sfxWait3);
        PlaySfx(sfxSpike2, transform.position);

        float feetY = groundY;
        float lockedX = GetPlayerPosition().x;
        float startX = lockedX - floorSpikeRange;
        float endX = lockedX + floorSpikeRange;
        int count = Mathf.Max(1, floorSpikeCount);
        float stepX = (endX - startX) / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float x = startX + i * stepX;
            Vector3 spawnPos = new Vector3(x, feetY, 0f);
            GameObject spike = Instantiate(prefab, spawnPos, Quaternion.identity);

            SetupSpikeDamage(spike, floorSpikeDamage);
            Destroy(spike, floorSpikeDuration);
        }

        yield return new WaitForSeconds(floorSpikeDuration + 0.3f);
        FinishCurrentSkill();
    }
    #endregion

    #region 技能4 - 遁入虚空
    private IEnumerator CoVoidAmbushSkill()
    {
        _rb.linearVelocity = Vector2.zero;

        // ===== 轨道 A：Boss 动画（独立，不影响红圈/小球）=====
        StartCoroutine(CoBossVoidAnimation());

        // ===== 轨道 B：红圈 + 小球（核心技能流程）=====
        // 1. 等待红圈出现时间
        yield return new WaitForSeconds(skill4CircleDelay);

        // 2. 锁定位置，生成红圈
        Vector2 center = GetPlayerPosition();
        GameObject warningCircle = null;
        SpriteRenderer circleSr = null;
        if (voidWarningPrefab != null)
        {
            warningCircle = Instantiate(voidWarningPrefab, center, Quaternion.identity);
            warningCircle.transform.localScale = Vector3.one * (voidCircleRadius * 2f);
            var vwc = warningCircle.GetComponent<VoidWarningCircle>();
            if (vwc != null) vwc.enabled = false;
            circleSr = warningCircle.GetComponent<SpriteRenderer>();
        }
        else
        {
            warningCircle = CreateWarningCircle(center, voidCircleRadius);
            circleSr = warningCircle.GetComponent<SpriteRenderer>();
        }
        if (sfxVoidCircleLoop != null)
        {
            var ca = warningCircle.AddComponent<AudioSource>();
            ca.clip = sfxVoidCircleLoop; ca.loop = true;
            ca.volume = Mathf.Clamp01(sfxVolume); ca.spatialBlend = 0f; ca.Play();
        }

        // 3. 红圈旋转 voidWarningDuration 秒
        float t = 0f;
        while (t < voidWarningDuration)
        {
            t += Time.deltaTime;
            warningCircle.transform.Rotate(0f, 0f, 180f * Time.deltaTime);
            if (circleSr != null)
                circleSr.color = new Color(1f, 0.1f, 0.1f, 0.3f + Mathf.PingPong(t * 4f, 0.5f));
            yield return null;
        }

        // 4. 红圈结束 → 四周生成小球
        int bulletCount = Mathf.Max(4, voidBulletCount);
        float radius = voidBulletSpawnRadius;
        GameObject ballP = voidBulletBallPrefab != null ? voidBulletBallPrefab : bulletBallPrefab;
        var spawnedBalls = new System.Collections.Generic.List<HomingBulletBall>();
        if (ballP != null)
        {
            for (int i = 0; i < bulletCount; i++)
            {
                float angle = (360f / bulletCount) * i * Mathf.Deg2Rad;
                Vector3 sp = new Vector3(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius, 0f);
                GameObject ball = Instantiate(ballP, sp, Quaternion.identity);
                var homing = ball.GetComponent<HomingBulletBall>();
                if (homing == null) homing = ball.AddComponent<HomingBulletBall>();
                homing.speed = 0f; homing.flyDuration = float.MaxValue;
                homing.laserLifetime = 0.3f; homing.ballDamage = 0f;
                homing.laserDamage = voidBulletDamage; homing.laserSFX = sfxLaserShot; homing.laserPrefab = null;
                spawnedBalls.Add(homing);
            }
        }

        // 5. 小球朝圆心发射激光
        if (voidBulletFireDelay > 0f) yield return new WaitForSeconds(voidBulletFireDelay);
        foreach (var homing in spawnedBalls)
            if (homing != null) homing.FireLaserImmediately(center);

        if (warningCircle != null) Destroy(warningCircle);
        yield return new WaitForSeconds(0.6f);
        FinishCurrentSkill();
    }

    /// <summary>
    /// 轨道 A：Boss 遁入虚空 + 出现的纯动画序列，与红圈/小球完全独立。
    /// </summary>
    private IEnumerator CoBossVoidAnimation()
    {
        PlaySfx(sfxVoidAmbush, transform.position);
        yield return StartCoroutine(PlayAnimationRoutine("TeleportIn"));

        yield return new WaitForSeconds(skill4AppearSfxDelay);
        PlaySfx(sfxVoidAppear, transform.position);
        PlayBossAnimation("TeleportOut");
    }
    #endregion

    #region 狂暴模式
    private void OnBossDamaged()
    {
        if (_enrageTriggered) return;
        if (_health == null) return;

        float hpPercent = (float)_health.currentHealth / _health.maxHealth;
        if (hpPercent <= enrageHealthPercent)
        {
            _enrageTriggered = true;

            // 停止当前技能
            if (_skillCoroutine != null)
                StopCoroutine(_skillCoroutine);
            _skillRunning = false;

            _enrageCoroutine = StartCoroutine(CoEnrageLoop());
        }
    }

    private IEnumerator CoEnrageLoop()
    {
        SwitchToEnrageBGM();

        while (true)
        {
            // 起飞前放大镜头
            if (cineCamera != null)
                yield return StartCoroutine(CoCameraTransition(enrageCameraLensSize, enrageCameraLensChangeDuration));

            // 飞到空中，关闭追击（不改 EnemyController 其它行为）
            _currentPhase = Phase.Enrage;
            SetInvincible(true);
            if (_enemyController != null) _enemyController.attacksPlayer = false;
            yield return StartCoroutine(FlyToY(enrageFlyY, enrageFlySpeed));

            // 空中连续循环释放 1-2-3
            for (int cycle = 0; cycle < enrageCycleCount; cycle++)
            {
                // 技能1：弹幕
                yield return StartCoroutine(CoBulletBallSkill());
                yield return new WaitForSeconds(Random.Range(enrageMinInterval, enrageMaxInterval));

                // 技能2：锁定地刺
                yield return StartCoroutine(CoSpike1Skill());
                yield return new WaitForSeconds(Random.Range(enrageMinInterval, enrageMaxInterval));

                // 技能3：全屏地刺
                yield return StartCoroutine(CoSpike2Skill());

                if (cycle < enrageCycleCount - 1)
                    yield return new WaitForSeconds(Random.Range(enrageMinInterval, enrageMaxInterval));
            }

            // 落地，恢复追击
            yield return StartCoroutine(FlyToY(_homeY, enrageFlySpeed));
            SetInvincible(false);
            if (_enemyController != null) _enemyController.attacksPlayer = true;

            // 落地后恢复镜头
            if (cineCamera != null)
                yield return StartCoroutine(CoCameraRestore());

            // 休息（可被攻击）
            _currentPhase = Phase.Idle;
            float restElapsed = 0f;
            while (restElapsed < enrageRestTime)
            {
                if (_health != null && _health.IsDead) yield break;
                restElapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator FlyToY(float targetY, float speed)
    {
        float tol = 0.05f;
        while (Mathf.Abs(_rb.position.y - targetY) > tol)
        {
            float dy = targetY - _rb.position.y;
            float vy = Mathf.Clamp(dy, -1f, 1f) * speed;
            _rb.linearVelocity = new Vector2(0f, vy);
            yield return null;
        }
        _rb.linearVelocity = Vector2.zero;
        _rb.position = new Vector2(_rb.position.x, targetY);
    }

    private void SetInvincible(bool invincible)
    {
        _isInvincible = invincible;
        if (_bossColliders != null)
        {
            foreach (var col in _bossColliders)
            {
                if (col != null) col.enabled = !invincible;
            }
        }
    }

    private void SetBossVisible(bool visible)
    {
        // 只切换渲染，不动 Animator（保持动画状态机运行）
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = visible;

        if (!visible && _bossColliders != null)
        {
            foreach (var col in _bossColliders)
            {
                if (col != null && !_isInvincible) col.enabled = false;
            }
        }
        else if (visible && _bossColliders != null && !_isInvincible)
        {
            foreach (var col in _bossColliders)
            {
                if (col != null) col.enabled = true;
            }
        }
    }

    private IEnumerator CoCameraTransition(float targetLens, float duration)
    {
        if (cineCamera == null) yield break;

        float startLens = cineCamera.Lens.OrthographicSize;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curved = t * t * (3f - 2f * t);

            var lens = cineCamera.Lens;
            lens.OrthographicSize = Mathf.Lerp(startLens, targetLens, curved);
            cineCamera.Lens = lens;

            yield return null;
        }

        var finalLens = cineCamera.Lens;
        finalLens.OrthographicSize = targetLens;
        cineCamera.Lens = finalLens;
    }

    private IEnumerator CoCameraRestore()
    {
        if (cineCamera == null) yield break;

        float startLens = cineCamera.Lens.OrthographicSize;
        float elapsed = 0f;
        float dur = Mathf.Max(0.1f, enrageCameraLensChangeDuration);

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float curved = t * t * (3f - 2f * t);
            var lens = cineCamera.Lens;
            lens.OrthographicSize = Mathf.Lerp(startLens, _savedLensSize, curved);
            cineCamera.Lens = lens;
            yield return null;
        }

        var finalLens = cineCamera.Lens;
        finalLens.OrthographicSize = _savedLensSize;
        cineCamera.Lens = finalLens;
    }
    #endregion

    #region 动画控制
    /// <summary>
    /// 设置 Animator Trigger 参数并等待动画播放完（如果 animator 存在）。
    /// 调用方可以用 yield return StartCoroutine(PlayAnimationRoutine(...)) 来等动画。
    /// </summary>
    private void PlayBossAnimation(string triggerName)
    {
        if (_animator == null || string.IsNullOrEmpty(triggerName)) return;
        _animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// 播放动画并等待完整播放一轮后返回。
    /// </summary>
    private System.Collections.IEnumerator PlayAnimationRoutine(string triggerName)
    {
        if (_animator == null || string.IsNullOrEmpty(triggerName)) yield break;

        _animator.SetTrigger(triggerName);
        // 等一帧让 Animator 完成状态过渡
        yield return null;

        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        float duration = stateInfo.length > 0f ? stateInfo.length : 0.5f;
        yield return new WaitForSeconds(duration);
    }

    private void OnBossDeath()
    {
        StopBGM();

        if (_skillCoroutine != null)
            StopCoroutine(_skillCoroutine);
        if (_enrageCoroutine != null)
            StopCoroutine(_enrageCoroutine);
        _skillRunning = false;

        if (cineCamera != null)
            StartCoroutine(CoCameraRestore());

        PlayBossAnimation("Death");
    }

    private void OnDestroy()
    {
        if (cineCamera != null)
        {
            var lens = cineCamera.Lens;
            lens.OrthographicSize = _savedLensSize;
            cineCamera.Lens = lens;
        }
    }
    #endregion

    #region 辅助方法
    private Vector2 GetAimToPlayer()
    {
        if (playerTarget == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) playerTarget = go.transform;
        }

        if (playerTarget == null)
            return transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

        Vector2 from = _mouth != null ? (Vector2)_mouth.position : (Vector2)transform.position;
        Vector2 to = playerTarget.position;
        Vector2 dir = to - from;
        if (dir.sqrMagnitude < 0.0001f)
            return transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

        // 同步外观朝向
        float sign = Mathf.Sign(dir.x);
        if (sign != 0 && Mathf.Sign(transform.localScale.x) != sign)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * sign, transform.localScale.y, transform.localScale.z);

        return dir.normalized;
    }

    private Vector2 GetPlayerPosition()
    {
        if (playerTarget == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) playerTarget = go.transform;
        }
        return playerTarget != null ? (Vector2)playerTarget.position : (Vector2)transform.position;
    }

    private void SetupSpikeDamage(GameObject spike, float damage)
    {
        var spikeDmg = spike.GetComponent<SpikeDamage>();
        if (spikeDmg == null)
            spikeDmg = spike.AddComponent<SpikeDamage>();
        spikeDmg.damage = damage;
    }

    private void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        float vol = Mathf.Clamp01(bgmVolume);

        if (bgmSource == null)
            bgmSource = GetComponent<AudioSource>();

        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        // 如果已经在播同一首 BGM 则不动
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = vol;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.Play();
    }

    private void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
    }

    private void SwitchToEnrageBGM()
    {
        if (enrageBGM != null)
            PlayBGM(enrageBGM);
    }

    private void PlaySfx(AudioClip clip, Vector3 pos)
    {
        if (clip == null) return;
        float vol = Mathf.Clamp01(sfxVolume);

        GameObject go;
        if (sfxPrefab != null)
        {
            go = Instantiate(sfxPrefab, pos, Quaternion.identity);
        }
        else
        {
            go = new GameObject($"_SFX_{clip.name}");
            go.transform.position = pos;
        }

        var src = go.GetComponent<AudioSource>();
        if (src == null) src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = vol;
        src.clip = clip;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    private static void ApplyProjectileVisualFacing(GameObject projectile, Vector2 moveDir)
    {
        if (projectile == null) return;
        if (moveDir.sqrMagnitude < 0.0001f) return;

        float z = Mathf.Atan2(moveDir.y, Mathf.Abs(moveDir.x)) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, z);

        var sr = projectile.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.flipX = moveDir.x < 0f;
    }

    private GameObject CreateWarningCircle(Vector2 position, float radius)
    {
        GameObject circle = new GameObject("VoidWarningCircle");
        circle.transform.position = position;
        var sr = circle.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(64, new Color(1f, 0.1f, 0.1f, 0.5f), 0.8f);
        sr.sortingOrder = 10;
        circle.transform.localScale = Vector3.one * (radius * 2f);
        return circle;
    }

    private Sprite CreateCircleSprite(int size, Color color, float fillRatio)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        float radius = center * fillRatio;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius && dist >= radius - 3f)
                    pixels[y * size + x] = color;
                else
                    pixels[y * size + x] = Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateColoredSpritePlain(int width, int height, Color color)
    {
        var tex = new Texture2D(width, height);
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
    #endregion
}
