using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PATCH 5 CHANGES:
/// - Added PlayerStats (STR/INT/LUK/END/WIS + leveling)
/// - Chaos mode now activated with K key (was T)
/// - Chaos meter fills with WIS multiplier
/// - Dark mode is now a timed transformation (10s countdown)
/// - XP system: enemies give XP, leveling grants stat points
/// - Max health derived from END stat
/// - Stats saved/loaded with save system
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool isGameStarted = false;
    public bool isPaused = false;
    public int currentSaveSlot = -1;

    [Header("Player Data - Live")]
    public int playerHealth = 100;
    public int playerMaxHealth = 100;
    public int playerCoins = 0;
    public int killCount = 0;
    public int facingDirection = 1;

    [Header("Chaos / Dark Mode")]
    public float chaosMeter = 0f;
    public float chaosMax = 100f;
    public bool isDarkMode = false;
    public float darkModeTimer = 0f;
    public float darkModeDuration = 10f;
    public bool chaosReady = false; // meter is full, waiting for K press

    [Header("Quest")]
    public int questIndex = 0;

    [Header("Skills")]
    public bool hasFireball = true;
    public bool hasIceBolt = true;

    [Header("Play Time")]
    public float totalPlayTime = 0f;

    // === STAT SYSTEM ===
    [Header("Player Stats")]
    public PlayerStats stats = new PlayerStats();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (!isGameStarted) return;

        totalPlayTime += Time.unscaledDeltaTime;

        // ESC — pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SceneManager.GetActiveScene().name == "MainMenu") return;
            if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive) return;

            if (isPaused) ResumeGame();
            else PauseGame();
        }

        // K — activate chaos mode when meter is full
        if (Input.GetKeyDown(KeyCode.K) && !isPaused)
        {
            if (chaosReady && !isDarkMode)
            {
                ActivateDarkMode();
            }
        }

        // Dark mode countdown
        if (isDarkMode)
        {
            darkModeTimer -= Time.deltaTime;
            UIManager.Instance?.UpdateChaosTimer(darkModeTimer, darkModeDuration);

            if (darkModeTimer <= 0f)
            {
                DeactivateDarkMode();
            }
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isPaused)
        {
            Time.timeScale = 1f;
            isPaused = false;
        }
        if (scene.name == "MainMenu")
            isGameStarted = false;
    }

    // ============ PAUSE ============

    public void PauseGame()
    {
        if (!isGameStarted) return;
        isPaused = true;
        Time.timeScale = 0f;
        UIManager.Instance?.ShowPauseMenu();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        UIManager.Instance?.HidePauseMenu();
    }

    // ============ GAME FLOW ============

    public void StartNewGame()
    {
        playerHealth = 100;
        playerMaxHealth = 100;
        playerCoins = 0;
        killCount = 0;
        chaosMeter = 0f;
        chaosReady = false;
        isDarkMode = false;
        darkModeTimer = 0f;
        questIndex = 0;
        hasFireball = true;
        hasIceBolt = true;
        totalPlayTime = 0f;
        facingDirection = 1;
        currentSaveSlot = -1;
        isGameStarted = true;

        stats.Reset();
        RecalculateMaxHealth();

        SceneManager.LoadScene("GameScene");
    }

    public void LoadGame(int slot)
    {
        SaveData data = SaveSystem.Load(slot);
        if (data == null) return;

        currentSaveSlot = slot;
        ApplySaveData(data);
        isGameStarted = true;
        SceneManager.LoadScene(data.currentScene);
    }

    public void SaveGame(int slot)
    {
        SaveData data = CreateSaveData();
        SaveSystem.Save(slot, data);
        currentSaveSlot = slot;
    }

    public void QuickSave()
    {
        if (currentSaveSlot >= 0) { SaveGame(currentSaveSlot); return; }
        for (int i = 0; i < 3; i++)
        {
            if (!SaveSystem.SlotExists(i)) { SaveGame(i); return; }
        }
        SaveGame(0);
    }

    // ============ SAVE / LOAD ============

    private SaveData CreateSaveData()
    {
        SaveData d = new SaveData();
        d.health = playerHealth;
        d.maxHealth = playerMaxHealth;
        d.coins = playerCoins;
        d.killCount = killCount;
        d.chaosMeter = chaosMeter;
        d.isDarkMode = isDarkMode;
        d.questIndex = questIndex;
        d.hasFireball = hasFireball;
        d.hasIceBolt = hasIceBolt;
        d.totalPlayTime = totalPlayTime;
        d.facingDirection = facingDirection;
        d.currentScene = SceneManager.GetActiveScene().name;
        d.lastQuestDescription = QuestSystem.Instance != null
            ? QuestSystem.Instance.GetCurrentQuestText() : "Explore the world";

        // Stats
        d.statSTR = stats.STR;
        d.statINT = stats.INT;
        d.statLUK = stats.LUK;
        d.statEND = stats.END;
        d.statWIS = stats.WIS;
        d.statLevel = stats.level;
        d.statCurrentXP = stats.currentXP;
        d.statUnspentPoints = stats.unspentPoints;
        d.statBonusPoints = stats.bonusStatPoints;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            d.playerPosX = player.transform.position.x;
            d.playerPosY = player.transform.position.y;
        }
        return d;
    }

    private void ApplySaveData(SaveData d)
    {
        playerHealth = d.health;
        playerMaxHealth = d.maxHealth;
        playerCoins = d.coins;
        killCount = d.killCount;
        chaosMeter = d.chaosMeter;
        isDarkMode = d.isDarkMode;
        questIndex = d.questIndex;
        hasFireball = d.hasFireball;
        hasIceBolt = d.hasIceBolt;
        totalPlayTime = d.totalPlayTime;
        facingDirection = d.facingDirection;

        // Stats
        stats.STR = d.statSTR;
        stats.INT = d.statINT;
        stats.LUK = d.statLUK;
        stats.END = d.statEND;
        stats.WIS = d.statWIS;
        stats.level = d.statLevel;
        stats.currentXP = d.statCurrentXP;
        stats.unspentPoints = d.statUnspentPoints;
        stats.bonusStatPoints = d.statBonusPoints;

        RecalculateMaxHealth();
        chaosReady = (chaosMeter >= chaosMax) && !isDarkMode;
    }

    public Vector2? GetSavedPosition()
    {
        if (currentSaveSlot < 0) return null;
        SaveData data = SaveSystem.PeekSlot(currentSaveSlot);
        if (data != null && data.currentScene == SceneManager.GetActiveScene().name)
            return new Vector2(data.playerPosX, data.playerPosY);
        return null;
    }

    // ============ STATS ============

    /// <summary>Recalculate max HP from END stat. Heals by the difference if HP increased.</summary>
    public void RecalculateMaxHealth()
    {
        int oldMax = playerMaxHealth;
        playerMaxHealth = stats.GetMaxHealth();
        if (playerMaxHealth > oldMax)
            playerHealth += (playerMaxHealth - oldMax);
        playerHealth = Mathf.Clamp(playerHealth, 0, playerMaxHealth);
        UIManager.Instance?.UpdateHealthBar((float)playerHealth / playerMaxHealth);
    }

    // ============ XP / LEVELING ============

    /// <summary>Give XP to the player. Shows level-up popup if leveled.</summary>
    public void GiveXP(int amount)
    {
        int levelsGained = stats.AddXP(amount);
        UIManager.Instance?.UpdateXPBar(stats.currentXP, stats.XPToNext(), stats.level);

        if (levelsGained > 0)
        {
            RecalculateMaxHealth();
            UIManager.Instance?.ShowLevelUp(stats.level);
        }
    }

    // ============ CHAOS / DARK MODE ============

    public void OnEnemyKilled(int xpReward = 25)
    {
        killCount++;
        AddChaos(10f);
        GiveXP(xpReward);
    }

    public void AddChaos(float amount)
    {
        if (isDarkMode) return; // can't fill during dark mode

        float scaledAmount = amount * stats.GetChaosRate();
        chaosMeter = Mathf.Clamp(chaosMeter + scaledAmount, 0f, chaosMax);

        bool wasFull = chaosReady;
        chaosReady = chaosMeter >= chaosMax;

        UIManager.Instance?.UpdateChaosMeter(chaosMeter / chaosMax, chaosReady);

        // Show prompt when meter first fills
        if (chaosReady && !wasFull)
        {
            UIManager.Instance?.ShowChaosReadyPrompt(true);
        }
    }

    public bool IsChaosMaxed() => chaosReady;

    public void ActivateDarkMode()
    {
        if (!chaosReady) return;

        isDarkMode = true;
        chaosReady = false;
        darkModeTimer = darkModeDuration;

        UIManager.Instance?.ShowChaosReadyPrompt(false);
        UIManager.Instance?.ShowDarkModeFlash();
        UIManager.Instance?.SetChaosTimerMode(true);

        Player player = FindAnyObjectByType<Player>();
        if (player != null)
            player.ActivateDarkMode();
    }

    public void DeactivateDarkMode()
    {
        isDarkMode = false;
        darkModeTimer = 0f;
        chaosMeter = 0f;
        chaosReady = false;

        UIManager.Instance?.SetChaosTimerMode(false);
        UIManager.Instance?.UpdateChaosMeter(0f, false);

        Player player = FindAnyObjectByType<Player>();
        if (player != null)
            player.DeactivateDarkMode();
    }

    // ============ PLAYER HEALTH ============

    public void AddCoins(int amount)
    {
        playerCoins += amount;
        UIManager.Instance?.UpdateCoinDisplay(playerCoins);
    }

    public void SetPlayerHealth(int hp)
    {
        playerHealth = Mathf.Clamp(hp, 0, playerMaxHealth);
        UIManager.Instance?.UpdateHealthBar((float)playerHealth / playerMaxHealth);
    }

    // ============ NAVIGATION ============

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        isGameStarted = false;
        // Reset dark mode if active
        if (isDarkMode) DeactivateDarkMode();
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
