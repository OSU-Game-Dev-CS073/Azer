using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Forest Boss (Griffin) — full controller.
/// Start in tired (rest) → player enters detection zone → scream → fly up → combat cycle.
///   Skill1: fan green bullets (slow, many, spread)  → 播放 attack1 动画
///   Skill2: fast hollow balls (few, aimed)           → 播放 attack2 动画
///   Skill3: tornado from screen edge                 → 无专属动画，保持 idle
///   循环 N 次 Skill1+Skill2 后放一次 Skill3，如此反复。
/// Animator triggers: Rest / Idle / Move / Attack1 / Attack2 / Hurt / Death
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Boss3SkillControl : MonoBehaviour
{
    #region Enums
    private enum BossState { Rest, Scream, FlyUp, Combat, Dead }
    #endregion

    #region Inspector — 引用
    [Header("引用")]
    [Tooltip("Cinemachine 虚拟摄像机")]
    public CinemachineCamera cineCamera;
    [Tooltip("左侧边界碰撞器（战斗前禁用，开战后激活）")]
    public GameObject leftBorder;
    [Tooltip("右侧边界碰撞器（战斗前禁用，开战后激活）")]
    public GameObject rightBorder;
    [Tooltip("子弹生成点（嘴部位置），不拖则用自身 Transform")]
    public Transform mouthSpawn;
    [Tooltip("玩家 Transform，不拖则按 Tag=Player 自动查找")]
    public Transform playerTarget;
    [Tooltip("检测中心点 — 拖一个空 GameObject 放在想让 Boss 检测玩家的位置（例如巢穴入口）")]
    public Transform detectionCenter;

    #region Inspector — 战斗入场
    [Header("战斗入场")]
    [Tooltip("检测半径（从 detectionCenter 到玩家的距离）")]
    public float detectionRange = 10f;
    [Tooltip("贴图是否默认面朝左（勾选 = 面朝左，不勾 = 面朝右）")]
    public bool spriteFacesLeft = false;
    [Tooltip("Boss 战斗时的悬停中心点（拖一个空 GameObject），起飞后先飞到这里")]
    public Transform bossCombatCenter;
    [Tooltip("起飞 / 降落速度")]
    public float flyUpSpeed = 6f;
    [Tooltip("飞到目标点时认为到位的距离（调大避免卡在目标点附近抖动）")]
    public float flyArriveDistance = 1f;
    [Tooltip("飞到 bossCombatCenter 后等待几秒再开始技能循环")]
    public float preCombatWaitTime = 1f;
    [Tooltip("战斗时摄像机 Orthographic Size（越大视野越宽）")]
    public float cameraLensSize = 13f;
    [Tooltip("叫声后额外等待多久再起飞（秒）")]
    public float screamPauseTime = 1f;
    [Tooltip("摄像机镜头切换的过渡时间（秒），0 = 立即切换")]
    public float cameraLensChangeDuration = 1.5f;
    [Tooltip("战斗时摄像机移动到的目标点（和镜头过渡同时进行），不拖则不移位")]
    public Transform cameraTargetPoint;
    #endregion

    [Header("技能预制体")]
    [Tooltip("技能1 绿色扇形子弹预制体")]
    public GameObject greenBulletPrefab;
    [Tooltip("技能2 空心直线球预制体")]
    public GameObject homingBallPrefab;
    [Tooltip("技能3 龙卷风预制体")]
    public GameObject tornadoPrefab;

    [Header("音效")]
    [Tooltip("惊醒叫声")]
    public AudioClip sfxScream;
    [Tooltip("技能1 扇形子弹音效")]
    public AudioClip sfxSkill1;
    [Tooltip("技能2 直线球音效")]
    public AudioClip sfxSkill2;
    [Tooltip("技能3 龙卷风音效")]
    public AudioClip sfxSkill3;
    [Tooltip("战斗背景音乐")]
    public AudioClip bgm;
    [Tooltip("音效音量")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Tooltip("背景音乐音量")]
    [Range(0f, 1f)] public float bgmVolume = 0.8f;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;
    #endregion

    #region Inspector — 技能1（扇形绿色子弹）
    [Header("技能1 — 扇形绿色子弹（量多、速度慢、扇形散射）")]
    [Tooltip("子弹飞行速度")]
    public float skill1BulletSpeed = 4f;
    [Tooltip("子弹最大存活时间（秒）")]
    public float skill1BulletLifetime = 6f;
    [Tooltip("每颗子弹伤害")]
    public float skill1Damage = 8f;
    [Tooltip("波次数")]
    public int skill1Waves = 3;
    [Tooltip("波次间隔（秒）")]
    public float skill1WaveInterval = 0.5f;
    [Tooltip("每波子弹数量")]
    public int skill1BulletCount = 12;
    [Tooltip("散射总角度（度），以水平为中心对称展开")]
    public float skill1SpreadAngle = 90f;
    #endregion

    #region Inspector — 技能2（快速空心球）
    [Header("技能2 — 快速空心球（量少、速度快、瞄准玩家直线发射）")]
    [Tooltip("子弹飞行速度")]
    public float skill2BulletSpeed = 14f;
    [Tooltip("子弹最大存活时间（秒）")]
    public float skill2BulletLifetime = 4f;
    [Tooltip("每颗子弹伤害")]
    public float skill2Damage = 12f;
    [Tooltip("发射总数量")]
    public int skill2ShotCount = 5;
    [Tooltip("每发间隔（秒）")]
    public float skill2ShotInterval = 0.3f;
    #endregion

    #region Inspector — 技能3（龙卷风）
    [Header("技能3 — 龙卷风（从屏幕边缘生成，物理碰撞推动玩家向中间移动）")]
    [Tooltip("龙卷风水平移动速度")]
    public float tornadoSpeed = 3f;
    [Tooltip("龙卷风最大存活时间（秒）")]
    public float tornadoLifetime = 6f;
    [Tooltip("龙卷风碰撞伤害")]
    public float tornadoDamage = 20f;
    [Tooltip("龙卷风生成的 Y 坐标")]
    public float tornadoSpawnY = -2f;
    [Tooltip("龙卷风一次释放数量")]
    public int tornadoCount = 2;
    [Tooltip("龙卷风之间间隔（秒）")]
    public float tornadoInterval = 0.8f;
    #endregion

    #region Inspector — 时机与飞行
    [Header("技能循环时机")]
    [Tooltip("技能之间最小间隔（秒）")]
    public float skillIntervalMin = 2f;
    [Tooltip("技能之间最大间隔（秒）")]
    public float skillIntervalMax = 4f;
    [Tooltip("技能 1→2→3 完整序列循环多少次后落地")]
    public int skillSequenceLoops = 3;
    #endregion

    #region Inspector — 战斗飞行 / 落地
    [Header("战斗飞行 / 落地")]
    [Tooltip("Boss 战斗时左右飞行的速度")]
    public float combatFlySpeedX = 3f;
    [Tooltip("Boss 飞行时离边界的最小距离，防止只露半个身体")]
    public float combatBorderMargin = 3f;
    [Tooltip("Boss 休息 / 落地时的 Y 坐标")]
    public float restY = 0f;
    [Tooltip("落地停留时间（秒）")]
    public float landRestTime = 2f;
    #endregion

    #region Private State
    private Rigidbody2D _rb;
    private Health _health;
    private EnemyController _enemyCtrl;
    private Animator _anim;
    private SpriteRenderer _sr;

    private BossState _state;
    private bool _combatStarted;
    private bool _isCasting;
    private bool _isLanding;
    private int _loopCount;
    private Coroutine _combatRoutine;
    private float _patrolDir = 1f;

    private float _savedLensSize;
    private Vector3 _savedCameraPos;
    private Transform _savedFollow;
    #endregion

    #region ===== Unity Lifecycle =====
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _health = GetComponent<Health>();
        _enemyCtrl = GetComponent<EnemyController>();
        _anim = GetComponent<Animator>();
        _sr = GetComponent<SpriteRenderer>();

        if (_rb != null)
        {
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        if (mouthSpawn == null) mouthSpawn = transform;

        // sfxSource: 如果用户拖的不可用（GameObject 禁用），就自动建一个
        if (sfxSource == null || !sfxSource.isActiveAndEnabled)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null || !sfxSource.isActiveAndEnabled)
                sfxSource = gameObject.AddComponent<AudioSource>();
        }
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // 2D 音效，不受距离影响

        if (playerTarget == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) playerTarget = go.transform;
        }
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.OnDamaged += OnDamaged;
            _health.OnDeath += OnDeath;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnDamaged -= OnDamaged;
            _health.OnDeath -= OnDeath;
        }
        StopAllCoroutines();
    }

    private void Start()
    {
        // 禁止 EnemyController 自带的 AI
        if (_enemyCtrl != null)
            _enemyCtrl.attacksPlayer = false;

        SetBorders(false);

        if (cineCamera != null)
        {
            _savedLensSize = cineCamera.Lens.OrthographicSize;
            _savedCameraPos = cineCamera.transform.position;
            _savedFollow = cineCamera.Follow;
        }

        _state = BossState.Rest;
        // 初始休息位置
        Vector3 pos = transform.position;
        pos.y = restY;
        transform.position = pos;
        TrigAnim("Rest");
    }

    private void Update()
    {
        // 非飞行阶段锁死速度
        if (_state != BossState.FlyUp && _state != BossState.Combat)
            _rb.linearVelocity = Vector2.zero;

        switch (_state)
        {
            case BossState.Rest:
                UpdatePreCombat();
                break;
            case BossState.Combat:
                UpdateCombatMove();
                break;
        }

        // 战斗飞行时才限制 X 不超出屏幕
        if (_state == BossState.Combat)
            ClampToScreenX();
    }
    #endregion

    #region ===== Pre-Combat =====
    private void UpdatePreCombat()
    {
        if (_combatStarted) return;

        // 休息时面朝玩家
        FacePlayer();

        if (CheckPlayerInRange())
        {
            _combatStarted = true;
            Debug.Log("[Boss3] 检测到玩家进入范围，开始进入战斗");
            StartCoroutine(CoEnterCombat());
        }
    }

    private bool CheckPlayerInRange()
    {
        if (playerTarget == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) playerTarget = go.transform;
        }
        if (playerTarget == null) return false;

        Vector2 center = detectionCenter != null ? (Vector2)detectionCenter.position : (Vector2)transform.position;
        return Vector2.Distance(center, playerTarget.position) <= detectionRange;
    }
    #endregion

    #region ===== Combat Entry =====
    private IEnumerator CoEnterCombat()
    {
        Debug.Log("[Boss3] === CoEnterCombat 开始 ====");
        _state = BossState.Scream;
        _rb.linearVelocity = Vector2.zero;

        // 叫声 → idle → 等音频播完 → move 飞空
        Debug.Log($"[Boss3] 准备播放叫声: sfxScream={(sfxScream != null ? sfxScream.name : "NULL")}, sfxSource={sfxSource}, volume={sfxVolume}");
        PlaySfx(sfxScream);
        TrigAnim("Idle");

        float screamLen = sfxScream != null ? sfxScream.length : 1.5f;
        yield return new WaitForSeconds(screamLen + screamPauseTime);

        // 叫声结束，开始 BGM
        PlayBGM(bgm);

        // 起飞 — move 状态 + 摄像机同步
        _state = BossState.FlyUp;
        TrigAnim("Move");
        if (cineCamera != null)
        {
            cineCamera.Follow = null;
            StartCoroutine(CoCameraTransition(cameraTargetPoint, cameraLensSize, cameraLensChangeDuration));
        }

        if (bossCombatCenter != null)
            yield return StartCoroutine(FlyToPosition(bossCombatCenter.position, flyUpSpeed));
        else
            Debug.LogWarning("[Boss3] bossCombatCenter 未赋值，Boss 无法飞行！");

        // 到达战斗区域，立刻切为面朝玩家
        FacePlayer();

        SetBorders(true);

        // 到位后稍等再开打
        if (preCombatWaitTime > 0f)
            yield return new WaitForSeconds(preCombatWaitTime);

        _state = BossState.Combat;
        _loopCount = 0;
        _patrolDir = 1f;
        _combatRoutine = StartCoroutine(CoCombatLoop());
    }

    private IEnumerator CoCombatLoop()
    {
        while (_state == BossState.Combat)
        {
            _loopCount++;

            // Skill1
            TrigAnim("Attack1");
            yield return StartCoroutine(CoSkill1());
            TrigAnim("Move");
            yield return new WaitForSeconds(Random.Range(skillIntervalMin, skillIntervalMax));

            // Skill2
            TrigAnim("Attack2");
            yield return StartCoroutine(CoSkill2());
            TrigAnim("Move");
            yield return new WaitForSeconds(Random.Range(skillIntervalMin, skillIntervalMax));

            // Skill3
            yield return StartCoroutine(CoSkill3());
            yield return new WaitForSeconds(Random.Range(skillIntervalMin, skillIntervalMax));

            // 循环 N 次后落地
            if (_loopCount >= skillSequenceLoops)
            {
                Debug.Log($"[Boss3] 触发落地: _loopCount={_loopCount}, skillSequenceLoops={skillSequenceLoops}");
                _loopCount = 0;
                _isLanding = true;
                yield return StartCoroutine(CoLandAndTakeOff());
                _isLanding = false;
            }
        }
    }

    private IEnumerator CoLandAndTakeOff()
    {
        // 落地 X：左边界往右 3~6，或右边界往左 3~6，随机二选一
        float leftBx = leftBorder != null ? leftBorder.transform.position.x : GetScreenLeft();
        float rightBx = rightBorder != null ? rightBorder.transform.position.x : GetScreenRight();
        float randomX;
        if (Random.value < 0.5f)
            randomX = Random.Range(leftBx + 3f, leftBx + 6f);
        else
            randomX = Random.Range(rightBx - 6f, rightBx - 3f);

        Vector2 landPos = new Vector2(randomX, restY);

        Debug.Log($"[Boss3] 开始落地: 当前位置={transform.position}, 目标={landPos}, _isCasting={_isCasting}, restY={restY}, flySpeed={flyUpSpeed}, arriveDist={flyArriveDistance}");

        // 飞向落地
        yield return StartCoroutine(FlyToPosition(landPos, flyUpSpeed));

        Debug.Log($"[Boss3] 落地到达: 当前位置={transform.position}");

        // 停止移动，等一帧确保完全停住
        _rb.linearVelocity = Vector2.zero;
        yield return null;

        // 地面休息 — tired
        Debug.Log($"[Boss3] 触发 Rest 动画, flyArriveDistance={flyArriveDistance}");
        TrigAnim("Rest");
        yield return null;
        Debug.Log($"[Boss3] Rest 后动画={_anim?.GetCurrentAnimatorStateInfo(0).shortNameHash}");
        yield return new WaitForSeconds(landRestTime);

        // 重新起飞 — 叫声 → move → 飞回
        PlaySfx(sfxScream);
        float screamLen = sfxScream != null ? sfxScream.length : 1.5f;
        yield return new WaitForSeconds(screamLen);
        TrigAnim("Move");
        if (bossCombatCenter != null)
            yield return StartCoroutine(FlyToPosition(bossCombatCenter.position, flyUpSpeed));
        else
            Debug.LogWarning("[Boss3] bossCombatCenter 未赋值，Boss 无法飞行！");
    }
    #endregion

    #region ===== Skill 1 — Fan Green Bullets =====
    private IEnumerator CoSkill1()
    {
        _isCasting = true;
        _rb.linearVelocity = Vector2.zero;
        PlaySfx(sfxSkill1);

        if (greenBulletPrefab == null)
        {
            Debug.LogError("Boss3SkillControl: greenBulletPrefab not assigned!", this);
            yield break;
        }

        for (int w = 0; w < skill1Waves; w++)
        {
            SpawnFanVolley();
            if (w < skill1Waves - 1 && skill1WaveInterval > 0f)
                yield return new WaitForSeconds(skill1WaveInterval);
        }
        _isCasting = false;
    }

    private void SpawnFanVolley()
    {
        Vector2 forward = GetAimToPlayer().normalized;
        int count = Mathf.Max(1, skill1BulletCount);
        float range = Mathf.Max(0f, skill1SpreadAngle);
        float step = count <= 1 ? 0f : range / (count - 1);
        float startAngle = -range * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float a = startAngle + i * step;
            Vector2 dir = (Vector2)(Quaternion.Euler(0f, 0f, a) * forward);
            GameObject bullet = Instantiate(greenBulletPrefab, mouthSpawn.position, Quaternion.identity);
            ApplyProjectileSetup(bullet, dir, skill1BulletSpeed, skill1Damage, skill1BulletLifetime);
        }
    }
    #endregion

    #region ===== Skill 2 — Fast Aimed Balls =====
    private IEnumerator CoSkill2()
    {
        _isCasting = true;
        _rb.linearVelocity = Vector2.zero;

        if (homingBallPrefab == null)
        {
            Debug.LogError("Boss3SkillControl: homingBallPrefab not assigned!", this);
            yield break;
        }

        int shots = Mathf.Max(1, skill2ShotCount);
        float interval = Mathf.Max(0f, skill2ShotInterval);

        for (int i = 0; i < shots; i++)
        {
            PlaySfx(sfxSkill2);

            Vector2 dir = GetAimToPlayer();
            GameObject ball = Instantiate(homingBallPrefab, mouthSpawn.position, Quaternion.identity);
            ApplyProjectileSetup(ball, dir, skill2BulletSpeed, skill2Damage, skill2BulletLifetime);

            if (i < shots - 1 && interval > 0f)
                yield return new WaitForSeconds(interval);
        }
        _isCasting = false;
    }
    #endregion

    #region ===== Skill 3 — Tornado =====
    private IEnumerator CoSkill3()
    {
        _isCasting = true;
        _rb.linearVelocity = Vector2.zero;
        PlaySfx(sfxSkill3);

        if (tornadoPrefab == null)
        {
            Debug.LogError("Boss3SkillControl: tornadoPrefab not assigned!", this);
            _isCasting = false;
            yield break;
        }

        int count = Mathf.Max(1, tornadoCount);
        for (int i = 0; i < count; i++)
        {
            SpawnTornado();

            if (i < count - 1 && tornadoInterval > 0f)
                yield return new WaitForSeconds(tornadoInterval);
        }

        _isCasting = false;
    }

    private void SpawnTornado()
    {
        float leftX = leftBorder != null ? leftBorder.transform.position.x : GetScreenLeft();
        float rightX = rightBorder != null ? rightBorder.transform.position.x : GetScreenRight();

        float playerX = playerTarget != null ? playerTarget.position.x : transform.position.x;
        float distToLeft = Mathf.Abs(playerX - leftX);
        float distToRight = Mathf.Abs(playerX - rightX);

        float spawnX;
        Vector2 moveDir;

        if (distToLeft <= distToRight)
        {
            spawnX = leftX;
            moveDir = Vector2.right;
        }
        else
        {
            spawnX = rightX;
            moveDir = Vector2.left;
        }

        Vector3 spawnPos = new Vector3(spawnX, tornadoSpawnY, 0f);
        GameObject tornado = Instantiate(tornadoPrefab, spawnPos, Quaternion.identity);

        var ts = tornado.GetComponent<ForestBossTornado>();
        if (ts == null) ts = tornado.AddComponent<ForestBossTornado>();
        ts.Initialize(moveDir, tornadoSpeed, tornadoDamage, tornadoLifetime);
    }
    #endregion

    #region ===== Combat Fly & Face =====
    private void UpdateCombatMove()
    {
        // 落地 / 施法期间不移动
        if (_isCasting || _isLanding)
        {
            if (!_isLanding) FacePlayer(); // 落地时朝向由 FlyToPosition 控制
            return;
        }

        // 左右边界 — 用激活的边界碰撞器的 X
        float leftX = leftBorder != null && leftBorder.activeSelf ? leftBorder.transform.position.x : GetScreenLeft();
        float rightX = rightBorder != null && rightBorder.activeSelf ? rightBorder.transform.position.x : GetScreenRight();
        // 留余量，Boss 不全贴边界
        leftX += combatBorderMargin;
        rightX -= combatBorderMargin;

        // 到边界就反向
        float x = transform.position.x;
        if (x >= rightX) _patrolDir = -1f;
        else if (x <= leftX) _patrolDir = 1f;

        // 水平移动
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x + _patrolDir * combatFlySpeedX * Time.deltaTime, leftX, rightX);
        // 高度锁在 bossCombatCenter 的 Y
        if (bossCombatCenter != null)
            pos.y = bossCombatCenter.position.y;
        transform.position = pos;

        // 面朝玩家
        FacePlayer();
    }
    #endregion

    #region ===== Utility =====
    private Vector2 GetAimToPlayer()
    {
        if (playerTarget == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) playerTarget = go.transform;
        }
        if (playerTarget == null) return GetHorizontalAim();

        Vector2 dir = playerTarget.position - mouthSpawn.position;
        if (dir.sqrMagnitude < 0.0001f) return GetHorizontalAim();
        return dir.normalized;
    }

    private void FacePlayer()
    {
        if (playerTarget == null) return;
        float dx = playerTarget.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.05f) return;
        float sx = Mathf.Sign(dx) * (spriteFacesLeft ? -1f : 1f);
        if (Mathf.Sign(transform.localScale.x) != sx)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * sx, transform.localScale.y, transform.localScale.z);
    }

    private Vector2 GetHorizontalAim()
    {
        if (playerTarget != null)
            return playerTarget.position.x >= transform.position.x ? Vector2.right : Vector2.left;
        return transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
    }

    private void ApplyProjectileSetup(GameObject obj, Vector2 dir, float speed, float damage, float lifetime)
    {
        float z = Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, z);
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = dir.x < 0f;

        var dmg = obj.GetComponent<ForestBossProjectileDamage>();
        if (dmg == null) dmg = obj.AddComponent<ForestBossProjectileDamage>();
        dmg.damage = damage;
        dmg.knockbackMode = ForestBossProjectileDamage.KnockbackMode.HorizontalFromVelocity;

        var rbProj = obj.GetComponent<Rigidbody2D>();
        if (rbProj != null) rbProj.linearVelocity = dir * speed;

        Destroy(obj, lifetime);
    }

    private IEnumerator FlyToPosition(Vector2 target, float speed)
    {
        // 面向目标
        float dx = target.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.1f)
        {
            float sx = Mathf.Sign(dx) * (spriteFacesLeft ? -1f : 1f);
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * sx, transform.localScale.y, transform.localScale.z);
        }

        while (Vector2.Distance(_rb.position, target) > flyArriveDistance)
        {
            Vector2 delta = target - _rb.position;
            _rb.linearVelocity = delta.normalized * Mathf.Min(speed, delta.magnitude / Time.deltaTime);
            yield return null;
        }
        _rb.linearVelocity = Vector2.zero;
        _rb.position = target;
    }

    private void SetBorders(bool active)
    {
        // 左边：开战后激活；右边：始终保持激活
        if (leftBorder != null) leftBorder.SetActive(active);
    }

    private IEnumerator CoCameraTransition(Transform targetPoint, float targetLens, float duration)
    {
        if (cineCamera == null) yield break;

        float startLens = cineCamera.Lens.OrthographicSize;
        Vector3 startPos = cineCamera.transform.position;
        bool movePosition = targetPoint != null;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curved = t * t * (3f - 2f * t);

            // Lens
            var lens = cineCamera.Lens;
            lens.OrthographicSize = Mathf.Lerp(startLens, targetLens, curved);
            cineCamera.Lens = lens;

            // Position
            if (movePosition)
                cineCamera.transform.position = Vector3.Lerp(startPos, targetPoint.position, curved);

            yield return null;
        }

        var finalLens = cineCamera.Lens;
        finalLens.OrthographicSize = targetLens;
        cineCamera.Lens = finalLens;
        if (movePosition)
            cineCamera.transform.position = targetPoint.position;
    }

    private void ClampToScreenX()
    {
        float left = GetScreenLeft();
        float right = GetScreenRight();
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, left, right);
        transform.position = pos;
    }

    private float GetScreenLeft()
    {
        if (Camera.main == null) return -10f;
        return Camera.main.transform.position.x - Camera.main.orthographicSize * Camera.main.aspect;
    }

    private float GetScreenRight()
    {
        if (Camera.main == null) return 10f;
        return Camera.main.transform.position.x + Camera.main.orthographicSize * Camera.main.aspect;
    }

    private void TrigAnim(string trigger)
    {
        if (_anim != null && !string.IsNullOrEmpty(trigger))
            _anim.SetTrigger(trigger);
    }
    #endregion

    #region ===== Damage / Death =====
    private void OnDamaged()
    {
        StartCoroutine(CoPlayHurt());
    }

    private IEnumerator CoPlayHurt()
    {
        TrigAnim("Hurt");
        yield return new WaitForSeconds(0.5f);
        if (_state == BossState.Combat)
            TrigAnim("Move");
    }

    private void OnDeath()
    {
        _state = BossState.Dead;
        StopAllCoroutines();

        SetBorders(false);
        StopBGM();

        // 摄像机：镜头平滑退回 + 完成后恢复跟随玩家
        if (cineCamera != null)
            StartCoroutine(CoCameraRestore());

        TrigAnim("Death");

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Kinematic;

        Destroy(gameObject, 3f);
    }

    private IEnumerator CoCameraRestore()
    {
        if (cineCamera == null) yield break;

        float startLens = cineCamera.Lens.OrthographicSize;
        float elapsed = 0f;
        float dur = Mathf.Max(0.1f, cameraLensChangeDuration);

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
        cineCamera.Follow = _savedFollow;
    }
    #endregion

    #region ===== Audio =====
    private void PlaySfx(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[Boss3] PlaySfx: clip 为 null，跳过");
            return;
        }
        if (sfxSource == null)
        {
            Debug.LogWarning("[Boss3] PlaySfx: sfxSource 为 null，跳过");
            return;
        }
        Debug.Log($"[Boss3] PlaySfx 播放: {clip.name}, volume={Mathf.Clamp01(sfxVolume)}, source={sfxSource.name}, enabled={sfxSource.enabled}, isActiveAndEnabled={sfxSource.isActiveAndEnabled}");
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume));
    }

    private void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.spatialBlend = 0f;
        }
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.volume = Mathf.Clamp01(bgmVolume);
        bgmSource.loop = true;
        bgmSource.Play();
    }

    private void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
    }
    #endregion

    #region ===== Cleanup =====
    private void OnDestroy()
    {
        SetBorders(false);
        if (cineCamera != null)
        {
            var lens = cineCamera.Lens;
            lens.OrthographicSize = _savedLensSize;
            cineCamera.Lens = lens;
            cineCamera.Follow = _savedFollow;
        }
    }
    #endregion
}
