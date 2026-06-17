using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// PATCH 5 CHANGES:
/// - Chaos meter now supports timer mode (10s countdown during dark mode)
/// - Flashing "Press [K] to Chaosify!" prompt when meter is full
/// - XP bar + level display below coins
/// - Level-up celebration popup
/// - CRIT! floating text popup
/// - "Stats" button added to pause menu → opens StatPanelUI
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private Canvas mainCanvas;
    private CanvasScaler canvasScaler;
    private GameObject hudRoot;

    // Health
    private Image healthBarFill;
    private TextMeshProUGUI healthText;

    // Chaos
    private Image chaosBarFill;
    private TextMeshProUGUI chaosLabel;
    private TextMeshProUGUI chaosTimerText;
    private GameObject chaosReadyPromptObj;
    private TextMeshProUGUI chaosReadyPromptText;
    private bool chaosIsTimerMode = false;
    private Coroutine chaosFlashCoroutine;

    // Coins
    private TextMeshProUGUI coinText;

    // XP / Level
    private Image xpBarFill;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI xpText;

    // Smooth bar animation (anchor-based — no fillAmount)
    private float healthTarget = 1f;
    private float chaosTarget = 0f;
    private float xpTarget = 0f;
    private float healthTrailVal = 1f;
    private float chaosTrailVal = 0f;
    private float xpTrailVal = 0f;
    private RectTransform healthFillRt;
    private RectTransform healthTrailRt;
    private RectTransform chaosFillRt;
    private RectTransform chaosTrailRt;
    private RectTransform xpFillRt;
    private RectTransform xpTrailRt;

    // Quest
    private TextMeshProUGUI questText;

    // Pause
    private GameObject pauseMenuPanel;

    // Overlays
    private Image darkFlashOverlay;
    private TextMeshProUGUI saveNotification;
    private TextMeshProUGUI levelUpPopup;
    private TextMeshProUGUI critPopup;

    // Colors
    private Color healthColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    private Color healthLowColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    private Color chaosColor = new Color(0.6f, 0.1f, 0.9f, 1f);
    private Color chaosReadyColor = new Color(1f, 0.2f, 0.8f, 1f);
    private Color chaosTimerColor = new Color(1f, 0.4f, 0.1f, 1f);
    private Color panelColor = new Color(0.05f, 0.05f, 0.1f, 0.9f);
    private Color buttonColor = new Color(0.15f, 0.15f, 0.25f, 1f);
    private Color buttonHoverColor = new Color(0.25f, 0.25f, 0.4f, 1f);
    private Color goldColor = new Color(1f, 0.85f, 0.2f, 1f);
    private Color xpColor = new Color(0.3f, 0.7f, 1f, 1f);

    private Color devBtnColor = new Color(0.35f, 0.15f, 0.05f, 1f);
    private Color devBtnHover = new Color(0.5f, 0.2f, 0.08f, 1f);
    private Color statBtnColor = new Color(0.1f, 0.15f, 0.35f, 1f);
    private Color statBtnHover = new Color(0.15f, 0.22f, 0.5f, 1f);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else { Destroy(gameObject); return; }
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            if (hudRoot != null) hudRoot.SetActive(false);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            return;
        }
        if (mainCanvas == null) BuildAllUI();
        else RefreshCanvasCamera();

        if (hudRoot != null) hudRoot.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        RefreshAllDisplays();
        StartCoroutine(CleanupSceneCanvases());
    }

    private IEnumerator CleanupSceneCanvases()
    {
        yield return null;
        if (SceneManager.GetActiveScene().name == "MainMenu") yield break;

        Canvas[] all = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in all)
        {
            if (c == mainCanvas) continue;
            if (c.gameObject.scene == gameObject.scene) continue;
            if (c.sortingOrder == 50) continue; // enemy health bar canvases
            c.gameObject.SetActive(false);
        }
    }

    private void RefreshCanvasCamera()
    {
        if (mainCanvas != null) { mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay; mainCanvas.sortingOrder = 100; }
    }

    void LateUpdate()
    {
        float speed = Time.unscaledDeltaTime * 5f;

        if (healthFillRt != null)
        {
            float cur = healthFillRt.anchorMax.x;
            healthFillRt.anchorMax = new Vector2(Mathf.Lerp(cur, healthTarget, speed), 1f);
        }
        if (healthTrailRt != null)
        {
            healthTrailVal = Mathf.Lerp(healthTrailVal, healthTarget, speed * 0.4f);
            healthTrailRt.anchorMax = new Vector2(healthTrailVal, 1f);
        }

        if (chaosFillRt != null && !chaosIsTimerMode)
        {
            float cur = chaosFillRt.anchorMax.x;
            chaosFillRt.anchorMax = new Vector2(Mathf.Lerp(cur, chaosTarget, speed), 1f);
        }
        if (chaosTrailRt != null && !chaosIsTimerMode)
        {
            chaosTrailVal = Mathf.Lerp(chaosTrailVal, chaosTarget, speed * 0.4f);
            chaosTrailRt.anchorMax = new Vector2(chaosTrailVal, 1f);
        }

        if (xpFillRt != null)
        {
            float cur = xpFillRt.anchorMax.x;
            xpFillRt.anchorMax = new Vector2(Mathf.Lerp(cur, xpTarget, speed), 1f);
        }
        if (xpTrailRt != null)
        {
            xpTrailVal = Mathf.Lerp(xpTrailVal, xpTarget, speed * 0.4f);
            xpTrailRt.anchorMax = new Vector2(xpTrailVal, 1f);
        }
    }

    // ============ BUILD ============

    private void BuildAllUI()
    {
        GameObject canvasObj = new GameObject("AzerGameUI");
        canvasObj.transform.SetParent(transform);
        DontDestroyOnLoad(canvasObj);

        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 100;

        canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        hudRoot = new GameObject("HUD");
        hudRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform hr = hudRoot.AddComponent<RectTransform>();
        Stretch(hr);

        BuildHealthBar();
        BuildChaosMeter();
        BuildCoinDisplay();
        BuildXPBar();
        BuildQuestTracker();
        BuildChaosReadyPrompt();
        BuildPauseMenu();
        BuildDarkFlashOverlay();
        BuildSaveNotification();
        BuildLevelUpPopup();
        BuildCritPopup();
    }

    // ---------- HEALTH BAR ----------
    private void BuildHealthBar()
    {
        GameObject c = MkUI("HealthBar", hudRoot.transform);
        RectTransform rt = c.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20, -20);
        rt.sizeDelta = new Vector2(300, 40);

        MkImage(c.transform, "Bg", new Color(0.15f, 0.15f, 0.15f, 0.8f), true);

        // Trail bar (shows where health WAS)
        GameObject trail = MkUI("Trail", c.transform);
        trail.AddComponent<Image>().color = new Color(0.8f, 0.3f, 0.3f, 0.6f);
        healthTrailRt = trail.GetComponent<RectTransform>();
        healthTrailRt.anchorMin = Vector2.zero; healthTrailRt.anchorMax = Vector2.one;
        healthTrailRt.offsetMin = new Vector2(4, 4); healthTrailRt.offsetMax = new Vector2(-4, -4);

        // Fill bar (actual health)
        GameObject fill = MkUI("Fill", c.transform);
        healthBarFill = fill.AddComponent<Image>();
        healthBarFill.color = healthColor;
        healthFillRt = fill.GetComponent<RectTransform>();
        healthFillRt.anchorMin = Vector2.zero; healthFillRt.anchorMax = Vector2.one;
        healthFillRt.offsetMin = new Vector2(4, 4); healthFillRt.offsetMax = new Vector2(-4, -4);

        GameObject lbl = MkUI("Label", c.transform);
        healthText = lbl.AddComponent<TextMeshProUGUI>();
        healthText.text = "♥ 100/100";
        healthText.fontSize = 18; healthText.color = Color.white;
        healthText.alignment = TextAlignmentOptions.Center;
        Stretch(lbl.GetComponent<RectTransform>());
    }

    // ---------- CHAOS METER ----------
    private void BuildChaosMeter()
    {
        GameObject c = MkUI("ChaosBar", hudRoot.transform);
        RectTransform rt = c.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20, -68);
        rt.sizeDelta = new Vector2(220, 26);

        MkImage(c.transform, "Bg", new Color(0.2f, 0.05f, 0.3f, 0.8f), true);

        // Trail bar
        GameObject cTrail = MkUI("Trail", c.transform);
        cTrail.AddComponent<Image>().color = new Color(0.8f, 0.3f, 0.9f, 0.4f);
        chaosTrailRt = cTrail.GetComponent<RectTransform>();
        chaosTrailRt.anchorMin = Vector2.zero; chaosTrailRt.anchorMax = new Vector2(0, 1);
        chaosTrailRt.offsetMin = new Vector2(3, 3); chaosTrailRt.offsetMax = new Vector2(0, -3);

        // Fill bar
        GameObject fill = MkUI("Fill", c.transform);
        chaosBarFill = fill.AddComponent<Image>();
        chaosBarFill.color = chaosColor;
        chaosFillRt = fill.GetComponent<RectTransform>();
        chaosFillRt.anchorMin = Vector2.zero; chaosFillRt.anchorMax = new Vector2(0, 1);
        chaosFillRt.offsetMin = new Vector2(3, 3); chaosFillRt.offsetMax = new Vector2(0, -3);

        // Normal label ("CHAOS 45%")
        GameObject lbl = MkUI("ChaosLabel", c.transform);
        chaosLabel = lbl.AddComponent<TextMeshProUGUI>();
        chaosLabel.text = "CHAOS 0%";
        chaosLabel.fontSize = 13; chaosLabel.color = new Color(0.8f, 0.7f, 1f);
        chaosLabel.alignment = TextAlignmentOptions.Center;
        Stretch(lbl.GetComponent<RectTransform>());

        // Timer text (shown during dark mode)
        GameObject timerObj = MkUI("TimerText", c.transform);
        chaosTimerText = timerObj.AddComponent<TextMeshProUGUI>();
        chaosTimerText.text = "10.0s";
        chaosTimerText.fontSize = 15; chaosTimerText.color = chaosTimerColor;
        chaosTimerText.alignment = TextAlignmentOptions.Center;
        chaosTimerText.fontStyle = FontStyles.Bold;
        Stretch(timerObj.GetComponent<RectTransform>());
        timerObj.SetActive(false);
    }

    // ---------- COIN DISPLAY ----------
    private void BuildCoinDisplay()
    {
        GameObject c = MkUI("Coins", hudRoot.transform);
        RectTransform rt = c.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20, -100);
        rt.sizeDelta = new Vector2(180, 26);

        MkImage(c.transform, "Bg", new Color(0.1f, 0.1f, 0.15f, 0.7f), true);
        coinText = c.AddComponent<TextMeshProUGUI>();
        coinText.text = "● 0"; coinText.fontSize = 18;
        coinText.color = goldColor; coinText.alignment = TextAlignmentOptions.MidlineLeft;
        coinText.margin = new Vector4(10, 0, 0, 0);
    }

    // ---------- XP BAR + LEVEL ----------
    private void BuildXPBar()
    {
        // Level text
        GameObject lvlObj = MkUI("LevelText", hudRoot.transform);
        RectTransform lvlRt = lvlObj.GetComponent<RectTransform>();
        lvlRt.anchorMin = new Vector2(0, 1); lvlRt.anchorMax = new Vector2(0, 1);
        lvlRt.pivot = new Vector2(0, 1);
        lvlRt.anchoredPosition = new Vector2(20, -130);
        lvlRt.sizeDelta = new Vector2(180, 22);

        levelText = lvlObj.AddComponent<TextMeshProUGUI>();
        levelText.text = "Lv 1"; levelText.fontSize = 18;
        levelText.color = xpColor; levelText.alignment = TextAlignmentOptions.MidlineLeft;
        levelText.margin = new Vector4(4, 0, 0, 0);

        // XP bar
        GameObject bar = MkUI("XPBar", hudRoot.transform);
        RectTransform barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0, 1); barRt.anchorMax = new Vector2(0, 1);
        barRt.pivot = new Vector2(0, 1);
        barRt.anchoredPosition = new Vector2(20, -150);
        barRt.sizeDelta = new Vector2(220, 18);

        MkImage(bar.transform, "Bg", new Color(0.1f, 0.1f, 0.15f, 0.7f), true);

        // Trail bar
        GameObject xTrail = MkUI("Trail", bar.transform);
        xTrail.AddComponent<Image>().color = new Color(0.3f, 0.5f, 0.9f, 0.4f);
        xpTrailRt = xTrail.GetComponent<RectTransform>();
        xpTrailRt.anchorMin = Vector2.zero; xpTrailRt.anchorMax = new Vector2(0, 1);
        xpTrailRt.offsetMin = new Vector2(2, 2); xpTrailRt.offsetMax = new Vector2(0, -2);

        // Fill bar
        GameObject fill = MkUI("Fill", bar.transform);
        xpBarFill = fill.AddComponent<Image>();
        xpBarFill.color = xpColor;
        xpFillRt = fill.GetComponent<RectTransform>();
        xpFillRt.anchorMin = Vector2.zero; xpFillRt.anchorMax = new Vector2(0, 1);
        xpFillRt.offsetMin = new Vector2(2, 2); xpFillRt.offsetMax = new Vector2(0, -2);

        // XP numbers
        GameObject xpObj = MkUI("XPText", bar.transform);
        xpText = xpObj.AddComponent<TextMeshProUGUI>();
        xpText.text = "0/100"; xpText.fontSize = 12;
        xpText.color = Color.white; xpText.alignment = TextAlignmentOptions.Center;
        Stretch(xpObj.GetComponent<RectTransform>());
    }

    // ---------- QUEST TRACKER ----------
    private void BuildQuestTracker()
    {
        GameObject c = MkUI("Quest", hudRoot.transform);
        RectTransform rt = c.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20, -20);
        rt.sizeDelta = new Vector2(350, 60);

        MkImage(c.transform, "Bg", new Color(0.05f, 0.05f, 0.15f, 0.75f), true);

        GameObject t = MkUI("Text", c.transform);
        questText = t.AddComponent<TextMeshProUGUI>();
        questText.text = "► Explore the world";
        questText.fontSize = 16; questText.color = new Color(0.9f, 0.9f, 0.7f);
        questText.alignment = TextAlignmentOptions.TopRight;
        questText.margin = new Vector4(10, 5, 10, 5);
        questText.enableWordWrapping = true;
        Stretch(t.GetComponent<RectTransform>());
    }

    // ---------- CHAOS READY PROMPT (center screen flash) ----------
    private void BuildChaosReadyPrompt()
    {
        chaosReadyPromptObj = MkUI("ChaosPrompt", hudRoot.transform);
        RectTransform rt = chaosReadyPromptObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.75f);
        rt.anchorMax = new Vector2(0.5f, 0.75f);
        rt.sizeDelta = new Vector2(500, 50);

        chaosReadyPromptText = chaosReadyPromptObj.AddComponent<TextMeshProUGUI>();
        chaosReadyPromptText.text = "Press [K] to Chaosify!";
        chaosReadyPromptText.fontSize = 28;
        chaosReadyPromptText.color = chaosReadyColor;
        chaosReadyPromptText.alignment = TextAlignmentOptions.Center;
        chaosReadyPromptText.fontStyle = FontStyles.Bold;
        chaosReadyPromptText.raycastTarget = false;

        chaosReadyPromptObj.SetActive(false);
    }

    // ---------- PAUSE MENU ----------
    private void BuildPauseMenu()
    {
        pauseMenuPanel = MkUI("PauseMenu", mainCanvas.transform);
        Stretch(pauseMenuPanel.GetComponent<RectTransform>());
        pauseMenuPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);

        GameObject panel = MkUI("Panel", pauseMenuPanel.transform);
        panel.AddComponent<Image>().color = panelColor;
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 0.5f); pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(400, 500);

        // Title
        AddLabel(panel.transform, "PAUSED", 36, Color.white, FontStyles.Bold,
            0, 1, 1, 1, 0, -20, 0, 50);

        float y = -90f;
        float sp = 55f;

        MkPauseBtn("Resume", panel.transform, y, () => GameManager.Instance.ResumeGame()); y -= sp;
        MkPauseBtn("Quick Save", panel.transform, y, () => {
            GameManager.Instance.QuickSave();
            ShowSaveNotification("Game Saved!");
        }); y -= sp;
        MkPauseBtn("Stats", panel.transform, y, () => {
            if (StatPanelUI.Instance != null) StatPanelUI.Instance.Open();
        }, statBtnColor, statBtnHover, new Color(0.5f, 0.7f, 1f)); y -= sp;
        MkPauseBtn("Dev Panel", panel.transform, y, () => {
            if (DevPanel.Instance != null) DevPanel.Instance.Open();
        }, devBtnColor, devBtnHover, new Color(1f, 0.6f, 0.2f)); y -= sp;
        MkPauseBtn("Main Menu", panel.transform, y, () => GameManager.Instance.GoToMainMenu()); y -= sp;
        MkPauseBtn("Exit Game", panel.transform, y, () => GameManager.Instance.QuitGame());

        pauseMenuPanel.SetActive(false);
    }

    private void MkPauseBtn(string label, Transform parent, float yPos,
        UnityEngine.Events.UnityAction onClick,
        Color? normalOverride = null, Color? hoverOverride = null, Color? textColorOverride = null)
    {
        GameObject obj = MkUI($"Btn_{label}", parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, yPos);
        rt.sizeDelta = new Vector2(280, 48);

        Color nc = normalOverride ?? buttonColor;
        Color hc = hoverOverride ?? buttonHoverColor;

        Image img = obj.AddComponent<Image>(); img.color = nc;
        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = nc; cb.highlightedColor = hc;
        cb.pressedColor = new Color(0.35f, 0.35f, 0.55f, 1f);
        btn.colors = cb; btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        MenuSFX.WireButton(btn);

        GameObject t = MkUI("T", obj.transform);
        TextMeshProUGUI txt = t.AddComponent<TextMeshProUGUI>();
        txt.text = label; txt.fontSize = 22;
        txt.color = textColorOverride ?? Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        if (normalOverride.HasValue) txt.fontStyle = FontStyles.Bold;
        Stretch(t.GetComponent<RectTransform>());
    }

    // ---------- OVERLAYS ----------
    private void BuildDarkFlashOverlay()
    {
        GameObject f = MkUI("DarkFlash", mainCanvas.transform);
        darkFlashOverlay = f.AddComponent<Image>();
        darkFlashOverlay.color = new Color(0.4f, 0, 0.6f, 0);
        darkFlashOverlay.raycastTarget = false;
        Stretch(f.GetComponent<RectTransform>());
        f.SetActive(false);
    }

    private void BuildSaveNotification()
    {
        GameObject n = MkUI("SaveNotif", hudRoot.transform);
        RectTransform rt = n.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 60);
        rt.sizeDelta = new Vector2(300, 40);
        n.AddComponent<Image>().color = new Color(0.1f, 0.3f, 0.1f, 0.8f);

        GameObject t = MkUI("T", n.transform);
        saveNotification = t.AddComponent<TextMeshProUGUI>();
        saveNotification.text = ""; saveNotification.fontSize = 18;
        saveNotification.color = Color.white; saveNotification.alignment = TextAlignmentOptions.Center;
        Stretch(t.GetComponent<RectTransform>());
        n.SetActive(false);
    }

    private void BuildLevelUpPopup()
    {
        GameObject p = MkUI("LevelUp", hudRoot.transform);
        RectTransform rt = p.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.6f); rt.anchorMax = new Vector2(0.5f, 0.6f);
        rt.sizeDelta = new Vector2(400, 60);

        levelUpPopup = p.AddComponent<TextMeshProUGUI>();
        levelUpPopup.text = ""; levelUpPopup.fontSize = 32;
        levelUpPopup.color = goldColor;
        levelUpPopup.alignment = TextAlignmentOptions.Center;
        levelUpPopup.fontStyle = FontStyles.Bold;
        levelUpPopup.raycastTarget = false;
        p.SetActive(false);
    }

    private void BuildCritPopup()
    {
        GameObject p = MkUI("Crit", hudRoot.transform);
        RectTransform rt = p.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.55f); rt.anchorMax = new Vector2(0.5f, 0.55f);
        rt.sizeDelta = new Vector2(200, 40);

        critPopup = p.AddComponent<TextMeshProUGUI>();
        critPopup.text = "CRIT!"; critPopup.fontSize = 26;
        critPopup.color = new Color(1f, 0.9f, 0.2f);
        critPopup.alignment = TextAlignmentOptions.Center;
        critPopup.fontStyle = FontStyles.Bold;
        critPopup.raycastTarget = false;
        p.SetActive(false);
    }

    // ============ PUBLIC METHODS ============

    public void UpdateHealthBar(float normalized)
    {
        if (healthBarFill == null) return;
        healthTarget = normalized;
        healthBarFill.color = normalized < 0.3f ? healthLowColor : healthColor;
        if (GameManager.Instance != null)
        {
            int cur = Mathf.RoundToInt(normalized * GameManager.Instance.playerMaxHealth);
            healthText.text = $"♥ {cur}/{GameManager.Instance.playerMaxHealth}";
        }
    }

    public void UpdateChaosMeter(float normalized, bool isReady = false)
    {
        if (chaosBarFill == null || chaosIsTimerMode) return;
        chaosTarget = normalized;
        chaosBarFill.color = isReady ? chaosReadyColor : chaosColor;

        int pct = Mathf.RoundToInt(normalized * 100f);
        chaosLabel.text = isReady ? "CHAOS READY!" : $"CHAOS {pct}%";
        chaosLabel.color = isReady ? chaosReadyColor : new Color(0.8f, 0.7f, 1f);
    }

    public void SetChaosTimerMode(bool timerMode)
    {
        chaosIsTimerMode = timerMode;
        if (chaosLabel != null) chaosLabel.gameObject.SetActive(!timerMode);
        if (chaosTimerText != null) chaosTimerText.gameObject.SetActive(timerMode);

        if (timerMode)
        {
            if (chaosFillRt != null) chaosFillRt.anchorMax = new Vector2(1f, 1f);
            chaosBarFill.color = chaosTimerColor;
        }
    }

    public void UpdateChaosTimer(float timeLeft, float duration)
    {
        if (chaosTimerText == null) return;
        chaosTimerText.text = $"CHAOS {timeLeft:F1}s";
        float t = timeLeft / duration;
        if (chaosFillRt != null) chaosFillRt.anchorMax = new Vector2(t, 1f);
        chaosBarFill.color = Color.Lerp(new Color(1f, 0.1f, 0.1f), chaosTimerColor, t);
    }

    public void ShowChaosReadyPrompt(bool show)
    {
        if (chaosReadyPromptObj == null) return;

        if (show)
        {
            chaosReadyPromptObj.SetActive(true);
            if (chaosFlashCoroutine != null) StopCoroutine(chaosFlashCoroutine);
            chaosFlashCoroutine = StartCoroutine(FlashChaosPrompt());
        }
        else
        {
            if (chaosFlashCoroutine != null) { StopCoroutine(chaosFlashCoroutine); chaosFlashCoroutine = null; }
            chaosReadyPromptObj.SetActive(false);
        }
    }

    private IEnumerator FlashChaosPrompt()
    {
        while (true)
        {
            // Fade in/out cycle — doesn't block gameplay
            float cycle = 1.2f;
            float elapsed = 0f;
            while (elapsed < cycle)
            {
                elapsed += Time.deltaTime;
                float alpha = 0.5f + 0.5f * Mathf.Sin(elapsed / cycle * Mathf.PI * 2f);
                if (chaosReadyPromptText != null)
                    chaosReadyPromptText.color = new Color(1f, 0.2f, 0.8f, alpha);
                yield return null;
            }
        }
    }

    public void UpdateCoinDisplay(int coins)
    {
        if (coinText != null) coinText.text = $"● {coins}";
    }

    public void UpdateXPBar(int currentXP, int xpNeeded, int level)
    {
        xpTarget = (float)currentXP / xpNeeded;
        if (xpText != null) xpText.text = $"{currentXP}/{xpNeeded}";
        if (levelText != null) levelText.text = $"Lv {level}";
    }

    public void UpdateQuestDisplay(string desc)
    {
        if (questText != null) questText.text = $"► {desc}";
    }

    public void ShowPauseMenu()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void HidePauseMenu()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (DevPanel.Instance != null && DevPanel.Instance.IsOpen) DevPanel.Instance.Close();
        if (StatPanelUI.Instance != null && StatPanelUI.Instance.IsOpen) StatPanelUI.Instance.Close();
    }

    public void ShowDarkModeFlash() { StartCoroutine(DarkFlashRoutine()); }

    private IEnumerator DarkFlashRoutine()
    {
        if (darkFlashOverlay == null) yield break;
        darkFlashOverlay.gameObject.SetActive(true);
        float d = 0.5f, e = 0f;
        while (e < d) { e += Time.unscaledDeltaTime; darkFlashOverlay.color = new Color(0.4f, 0, 0.6f, Mathf.Lerp(0, 0.8f, e / d)); yield return null; }
        e = 0f;
        while (e < d) { e += Time.unscaledDeltaTime; darkFlashOverlay.color = new Color(0.4f, 0, 0.6f, Mathf.Lerp(0.8f, 0, e / d)); yield return null; }
        darkFlashOverlay.gameObject.SetActive(false);
    }

    public void ShowSaveNotification(string msg) { StartCoroutine(SaveNotifRoutine(msg)); }
    private IEnumerator SaveNotifRoutine(string msg)
    {
        if (saveNotification == null) yield break;
        saveNotification.text = msg;
        saveNotification.transform.parent.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(2f);
        saveNotification.transform.parent.gameObject.SetActive(false);
    }

    public void ShowLevelUp(int newLevel)
    {
        StartCoroutine(LevelUpRoutine(newLevel));
    }

    private IEnumerator LevelUpRoutine(int level)
    {
        if (levelUpPopup == null) yield break;
        levelUpPopup.text = $"LEVEL UP! Lv {level}";
        levelUpPopup.gameObject.SetActive(true);

        float duration = 2.5f, e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float alpha = e < 0.3f ? e / 0.3f : Mathf.Lerp(1f, 0f, (e - 1.5f) / 1f);
            levelUpPopup.color = new Color(goldColor.r, goldColor.g, goldColor.b, Mathf.Clamp01(alpha));
            float scale = 1f + Mathf.Sin(e * 3f) * 0.05f;
            levelUpPopup.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        levelUpPopup.gameObject.SetActive(false);
    }

    public void ShowCritPopup()
    {
        StartCoroutine(CritRoutine());
    }

    private IEnumerator CritRoutine()
    {
        if (critPopup == null) yield break;
        critPopup.gameObject.SetActive(true);
        float d = 0.8f, e = 0f;
        while (e < d)
        {
            e += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, e / d);
            critPopup.color = new Color(1f, 0.9f, 0.2f, a);
            yield return null;
        }
        critPopup.gameObject.SetActive(false);
    }

    public void RefreshAllDisplays()
    {
        if (GameManager.Instance == null) return;
        var gm = GameManager.Instance;
        UpdateHealthBar((float)gm.playerHealth / gm.playerMaxHealth);
        if (!chaosIsTimerMode)
            UpdateChaosMeter(gm.chaosMeter / gm.chaosMax, gm.chaosReady);
        UpdateCoinDisplay(gm.playerCoins);
        UpdateXPBar(gm.stats.currentXP, gm.stats.XPToNext(), gm.stats.level);
        if (QuestSystem.Instance != null)
            UpdateQuestDisplay(QuestSystem.Instance.GetCurrentQuestText());
    }

    // ============ HELPERS ============

    private GameObject MkUI(string name, Transform parent)
    {
        GameObject o = new GameObject(name);
        o.transform.SetParent(parent, false);
        o.AddComponent<RectTransform>();
        return o;
    }

    private void MkImage(Transform parent, string name, Color c, bool stretch)
    {
        GameObject o = MkUI(name, parent);
        o.AddComponent<Image>().color = c;
        if (stretch) Stretch(o.GetComponent<RectTransform>());
    }

    private void AddLabel(Transform parent, string text, int size, Color color, FontStyles style,
        float aMinX, float aMinY, float aMaxX, float aMaxY, float posX, float posY, float sW, float sH)
    {
        GameObject o = MkUI("Lbl", parent);
        TextMeshProUGUI t = o.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        RectTransform rt = o.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, aMinY); rt.anchorMax = new Vector2(aMaxX, aMaxY);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(posX, posY); rt.sizeDelta = new Vector2(sW, sH);
    }

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
