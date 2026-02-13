using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Developer testing panel. Opens from Pause Menu.
/// 
/// FEATURES:
/// - Teleport to any scene (button list)
/// - Set Gold (counter with -/+)
/// - Full Health (button)
/// - Full Chaos Meter (button)
/// - Quest Index setter (counter with -/+)
/// - Infinite Jump toggle
/// 
/// EXPANDABLE: Add new items in BuildContent(). Use the helper methods at the bottom.
/// 
/// ARCHITECTURE: No TMP_InputField (broken when created via code). No ScrollRect/Mask 
/// (unreliable programmatic). Uses a simple manual-scroll approach with mouse wheel.
/// </summary>
public class DevPanel : MonoBehaviour
{
    public static DevPanel Instance { get; private set; }

    private GameObject devPanelRoot;
    private RectTransform contentRect; // the scrollable content area
    private bool isOpen = false;
    private float scrollOffset = 0f;
    private float contentHeight = 0f;
    private float viewHeight = 0f;

    // Dev flags
    public bool infiniteJump = false;

    // Layout tracking
    private float cursorY = 0f; // tracks current Y position as we add items

    // Colors
    private Color bgColor = new Color(0.04f, 0.02f, 0.08f, 0.96f);
    private Color headerColor = new Color(1f, 0.5f, 0.2f, 1f);
    private Color sectionColor = new Color(0.65f, 0.3f, 1f, 1f);
    private Color btnNormal = new Color(0.2f, 0.06f, 0.12f, 0.9f);
    private Color btnHover = new Color(0.35f, 0.08f, 0.45f, 0.95f);
    private Color btnPress = new Color(0.65f, 0.25f, 0.05f, 1f);
    private Color textColor = new Color(0.9f, 0.85f, 0.75f, 1f);
    private Color goldColor = new Color(1f, 0.82f, 0.3f, 1f);
    private Color warnColor = new Color(1f, 0.3f, 0.3f, 1f);
    private Color toggleOn = new Color(0.3f, 0.9f, 0.3f, 1f);
    private Color toggleOff = new Color(0.5f, 0.5f, 0.5f, 1f);
    private Color numBg = new Color(0.08f, 0.04f, 0.12f, 0.9f);
    private Color rowBg = new Color(0.08f, 0.04f, 0.10f, 0.5f);
    private Color applyColor = new Color(0.15f, 0.35f, 0.15f, 0.9f);
    private Color applyHover = new Color(0.2f, 0.5f, 0.2f, 1f);

    private List<string> sceneNames = new List<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CacheSceneNames();
        }
        else
        {
            Destroy(this);
        }
    }

    private void CacheSceneNames()
    {
        sceneNames.Clear();
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            sceneNames.Add(System.IO.Path.GetFileNameWithoutExtension(path));
        }
    }

    void Update()
    {
        if (!isOpen || contentRect == null) return;

        // Mouse wheel scrolling
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            scrollOffset -= scroll * 600f;
            scrollOffset = Mathf.Clamp(scrollOffset, 0f, Mathf.Max(0f, contentHeight - viewHeight));
            contentRect.anchoredPosition = new Vector2(0, scrollOffset);
        }
    }

    // ============ PUBLIC ============

    public void Open()
    {
        if (devPanelRoot == null)
        {
            BuildPanel();
        }
        devPanelRoot.SetActive(true);
        isOpen = true;
        scrollOffset = 0f;
        if (contentRect != null)
            contentRect.anchoredPosition = new Vector2(0, 0);
    }

    public void Close()
    {
        if (devPanelRoot != null)
            devPanelRoot.SetActive(false);
        isOpen = false;
    }

    public bool IsOpen => isOpen;

    // ============ BUILD ============

    private void BuildPanel()
    {
        // Canvas at sort order 300 (above everything)
        GameObject canvasObj = new GameObject("DevPanelCanvas");
        canvasObj.transform.SetParent(transform);
        DontDestroyOnLoad(canvasObj);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Root
        devPanelRoot = MkObj("DevRoot", canvasObj.transform);
        Stretch(devPanelRoot.GetComponent<RectTransform>());

        // Click-outside overlay to close
        GameObject overlay = MkObj("Overlay", devPanelRoot.transform);
        overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        Stretch(overlay.GetComponent<RectTransform>());
        Button ovBtn = overlay.AddComponent<Button>();
        ovBtn.transition = Selectable.Transition.None;
        ovBtn.onClick.AddListener(Close);

        // ---- SIDE PANEL (right side) ----
        GameObject panel = MkObj("Panel", devPanelRoot.transform);
        panel.AddComponent<Image>().color = bgColor;
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1, 0);
        panelRt.anchorMax = new Vector2(1, 1);
        panelRt.pivot = new Vector2(1, 0.5f);
        panelRt.offsetMin = new Vector2(-450, 10);
        panelRt.offsetMax = new Vector2(-10, -10);

        // Purple border effect
        GameObject border = MkObj("Border", panel.transform);
        border.AddComponent<Image>().color = new Color(0.5f, 0.15f, 0.5f, 0.4f);
        border.GetComponent<Image>().raycastTarget = false;
        Stretch(border.GetComponent<RectTransform>());
        GameObject inner = MkObj("Inner", border.transform);
        inner.AddComponent<Image>().color = bgColor;
        inner.GetComponent<Image>().raycastTarget = false;
        RectTransform iRt = inner.GetComponent<RectTransform>();
        iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one;
        iRt.offsetMin = new Vector2(2, 2); iRt.offsetMax = new Vector2(-2, -2);

        // Header text
        MakeLabel(panel.transform, "DEV PANEL", 28, headerColor, FontStyles.Bold,
            0, 1, 1, 1, 0.5f, 1, 0, -8, 0, 38);

        // Warning text
        MakeLabel(panel.transform, "Testing Only — Changes are live", 12, warnColor, FontStyles.Italic,
            0, 1, 1, 1, 0.5f, 1, 0, -44, 0, 18);

        // ---- VIEWPORT (clips content) ----
        GameObject viewport = MkObj("Viewport", panel.transform);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = new Vector2(0, 0);
        vpRt.anchorMax = new Vector2(1, 1);
        vpRt.offsetMin = new Vector2(8, 48);   // bottom: room for close button
        vpRt.offsetMax = new Vector2(-8, -66);  // top: room for header
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<RectMask2D>(); // clips children to this rect

        // ---- CONTENT (tall, scrollable via mouse wheel) ----
        GameObject content = MkObj("Content", viewport.transform);
        contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        // Width stretches to parent; height set after building content

        // Store viewport height for scroll clamping
        // (approximate — actual is set at runtime by layout)
        viewHeight = 800f; // will be overridden

        // ============ BUILD ALL CONTENT ============
        cursorY = -5f; // start 5px from top
        BuildContent(content.transform);
        contentHeight = -cursorY + 10f;
        contentRect.sizeDelta = new Vector2(0, contentHeight);

        // Try to get real viewport height
        Canvas.ForceUpdateCanvases();
        viewHeight = vpRt.rect.height > 0 ? vpRt.rect.height : 700f;

        // ---- CLOSE BUTTON (bottom of panel) ----
        GameObject closeObj = MkObj("CloseBtn", panel.transform);
        RectTransform closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.5f, 0);
        closeRt.anchorMax = new Vector2(0.5f, 0);
        closeRt.pivot = new Vector2(0.5f, 0);
        closeRt.anchoredPosition = new Vector2(0, 8);
        closeRt.sizeDelta = new Vector2(200, 34);
        Image closeImg = closeObj.AddComponent<Image>();
        closeImg.color = new Color(0.4f, 0.08f, 0.08f, 0.9f);
        Button closeBtn = closeObj.AddComponent<Button>();
        SetColors(closeBtn, new Color(0.4f, 0.08f, 0.08f, 0.9f),
            new Color(0.6f, 0.12f, 0.12f, 1f), new Color(0.8f, 0.15f, 0.15f, 1f));
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(Close);
        MenuSFX.WireButton(closeBtn);
        MakeCenteredLabel(closeObj.transform, "CLOSE", 17, Color.white, FontStyles.Bold);

        devPanelRoot.SetActive(false);
    }

    // ============ ALL CONTENT BUILT HERE ============

    private void BuildContent(Transform parent)
    {
        float W = 410f; // usable width

        // ═══════════════════════════════════════
        // SCENE TELEPORT
        // ═══════════════════════════════════════
        AddSectionHeader(parent, "SCENE TELEPORT", W);

        foreach (string sceneName in sceneNames)
        {
            if (sceneName == "MainMenu") continue;
            string s = sceneName;
            AddActionButton(parent, s, W, () =>
            {
                Close();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.isGameStarted = true;
                    GameManager.Instance.isPaused = false;
                }
                Time.timeScale = 1f;
                SceneManager.LoadScene(s);
            });
        }

        cursorY -= 8f;

        // ═══════════════════════════════════════
        // PLAYER CHEATS
        // ═══════════════════════════════════════
        AddSectionHeader(parent, "PLAYER", W);

        AddActionButton(parent, "Full Health", W, () =>
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.SetPlayerHealth(GameManager.Instance.playerMaxHealth);
            UIManager.Instance?.RefreshAllDisplays();
        });

        AddActionButton(parent, "Full Chaos Meter", W, () =>
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.chaosMeter = GameManager.Instance.chaosMax;
            GameManager.Instance.chaosReady = true;
            UIManager.Instance?.UpdateChaosMeter(1f, true);
            UIManager.Instance?.ShowChaosReadyPrompt(true);
        });

        // Gold: [-] [value] [+] [APPLY]
        AddNumberRow(parent, "Gold", W, 0, 99999, 50,
            () => GameManager.Instance != null ? GameManager.Instance.playerCoins : 0,
            (v) =>
            {
                if (GameManager.Instance == null) return;
                GameManager.Instance.playerCoins = v;
                UIManager.Instance?.UpdateCoinDisplay(v);
            });

        // Stat Points: [-] [amount] [+] [ADD]
        AddNumberRow(parent, "Add Stat Pts", W, 1, 50, 1,
            () => 1,
            (v) =>
            {
                if (GameManager.Instance == null) return;
                GameManager.Instance.stats.bonusStatPoints += v;
                Debug.Log($"[DevPanel] Added {v} stat points. Total available: {GameManager.Instance.stats.AvailablePoints()}");
            });

        cursorY -= 8f;

        // ═══════════════════════════════════════
        // QUEST
        // ═══════════════════════════════════════
        AddSectionHeader(parent, "QUEST", W);

        AddNumberRow(parent, "Quest Index", W, 0, 50, 1,
            () => GameManager.Instance != null ? GameManager.Instance.questIndex : 0,
            (v) =>
            {
                if (QuestSystem.Instance != null)
                    QuestSystem.Instance.SetQuestIndex(v);
            });

        cursorY -= 8f;

        // ═══════════════════════════════════════
        // TOGGLES
        // ═══════════════════════════════════════
        AddSectionHeader(parent, "TOGGLES", W);

        AddToggleRow(parent, "Infinite Jump", W, infiniteJump, (v) => { infiniteJump = v; });

        cursorY -= 12f;

        // ═══════════════════════════════════════
        // ADD MORE FEATURES HERE
        // ═══════════════════════════════════════
        // AddSectionHeader(parent, "DEBUG", W);
        // AddActionButton(parent, "Kill All Enemies", W, () => { ... });
        // AddToggleRow(parent, "God Mode", W, false, (v) => { godMode = v; });
    }

    // ============ WIDGET HELPERS ============

    private void AddSectionHeader(Transform parent, string title, float W)
    {
        float H = 26f;
        GameObject obj = MkObj($"Sec_{title}", parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(5, cursorY);
        rt.sizeDelta = new Vector2(W, H);

        TextMeshProUGUI t = obj.AddComponent<TextMeshProUGUI>();
        t.text = $"— {title} —";
        t.fontSize = 15;
        t.color = sectionColor;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;

        cursorY -= (H + 4f);
    }

    private void AddActionButton(Transform parent, string label, float W, UnityEngine.Events.UnityAction onClick)
    {
        float H = 33f;
        GameObject obj = MkObj($"Btn_{label}", parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(5, cursorY);
        rt.sizeDelta = new Vector2(W, H);

        Image img = obj.AddComponent<Image>();
        img.color = btnNormal;

        Button btn = obj.AddComponent<Button>();
        SetColors(btn, btnNormal, btnHover, btnPress);
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        MenuSFX.WireButton(btn);

        MakeCenteredLabel(obj.transform, label, 16, goldColor, FontStyles.Normal);

        cursorY -= (H + 4f);
    }

    private void AddNumberRow(Transform parent, string label, float W, int min, int max, int step,
        System.Func<int> getVal, System.Action<int> setVal)
    {
        float H = 36f;
        int val = getVal();

        // Row background
        GameObject row = MkObj($"Num_{label}", parent);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0, 1); rowRt.anchorMax = new Vector2(0, 1);
        rowRt.pivot = new Vector2(0, 1);
        rowRt.anchoredPosition = new Vector2(5, cursorY);
        rowRt.sizeDelta = new Vector2(W, H);
        row.AddComponent<Image>().color = rowBg;

        // Label (left 28%)
        GameObject lbl = MkObj("L", row.transform);
        RectTransform lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 0); lr.anchorMax = new Vector2(0.28f, 1);
        lr.offsetMin = new Vector2(8, 0); lr.offsetMax = Vector2.zero;
        TextMeshProUGUI lt = lbl.AddComponent<TextMeshProUGUI>();
        lt.text = label;
        lt.fontSize = 14;
        lt.color = textColor;
        lt.alignment = TextAlignmentOptions.MidlineLeft;

        // [-] button
        GameObject minGO = MkBtnInRow(row.transform, "-", 0.29f, 0.39f, H);
        // Value display
        GameObject valGO = MkObj("Val", row.transform);
        RectTransform valRt = valGO.GetComponent<RectTransform>();
        valRt.anchorMin = new Vector2(0.40f, 0.08f); valRt.anchorMax = new Vector2(0.60f, 0.92f);
        valRt.offsetMin = Vector2.zero; valRt.offsetMax = Vector2.zero;
        valGO.AddComponent<Image>().color = numBg;
        GameObject valTGO = MkObj("VT", valGO.transform);
        TextMeshProUGUI valTxt = valTGO.AddComponent<TextMeshProUGUI>();
        valTxt.text = val.ToString();
        valTxt.fontSize = 15;
        valTxt.color = Color.white;
        valTxt.alignment = TextAlignmentOptions.Center;
        Stretch(valTGO.GetComponent<RectTransform>());

        // [+] button
        GameObject plusGO = MkBtnInRow(row.transform, "+", 0.61f, 0.71f, H);

        // [APPLY] button
        GameObject applyGO = MkObj("Apply", row.transform);
        RectTransform apRt = applyGO.GetComponent<RectTransform>();
        apRt.anchorMin = new Vector2(0.73f, 0.08f); apRt.anchorMax = new Vector2(0.98f, 0.92f);
        apRt.offsetMin = Vector2.zero; apRt.offsetMax = Vector2.zero;
        Image apImg = applyGO.AddComponent<Image>();
        apImg.color = applyColor;
        Button apBtn = applyGO.AddComponent<Button>();
        SetColors(apBtn, applyColor, applyHover, new Color(0.3f, 0.7f, 0.3f, 1f));
        apBtn.targetGraphic = apImg;
        MakeCenteredLabel(applyGO.transform, "APPLY", 13, Color.white, FontStyles.Bold);

        // Wire [-]
        Button minBtn = minGO.GetComponent<Button>();
        minBtn.onClick.AddListener(() =>
        {
            val = Mathf.Max(min, val - step);
            valTxt.text = val.ToString();
        });
        MenuSFX.WireButton(minBtn);

        // Wire [+]
        Button plusBtn = plusGO.GetComponent<Button>();
        plusBtn.onClick.AddListener(() =>
        {
            val = Mathf.Min(max, val + step);
            valTxt.text = val.ToString();
        });
        MenuSFX.WireButton(plusBtn);

        // Wire [APPLY]
        apBtn.onClick.AddListener(() => { setVal(val); });
        MenuSFX.WireButton(apBtn);

        cursorY -= (H + 4f);
    }

    private void AddToggleRow(Transform parent, string label, float W, bool initial, System.Action<bool> onChanged)
    {
        float H = 34f;
        bool val = initial;

        GameObject row = MkObj($"Tog_{label}", parent);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0, 1); rowRt.anchorMax = new Vector2(0, 1);
        rowRt.pivot = new Vector2(0, 1);
        rowRt.anchoredPosition = new Vector2(5, cursorY);
        rowRt.sizeDelta = new Vector2(W, H);
        row.AddComponent<Image>().color = rowBg;

        // Label
        GameObject lbl = MkObj("L", row.transform);
        RectTransform lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 0); lr.anchorMax = new Vector2(0.65f, 1);
        lr.offsetMin = new Vector2(12, 0); lr.offsetMax = Vector2.zero;
        TextMeshProUGUI lt = lbl.AddComponent<TextMeshProUGUI>();
        lt.text = label;
        lt.fontSize = 16;
        lt.color = textColor;
        lt.alignment = TextAlignmentOptions.MidlineLeft;

        // Toggle button
        GameObject togGO = MkObj("Tog", row.transform);
        RectTransform togRt = togGO.GetComponent<RectTransform>();
        togRt.anchorMin = new Vector2(1, 0.5f);
        togRt.anchorMax = new Vector2(1, 0.5f);
        togRt.pivot = new Vector2(1, 0.5f);
        togRt.anchoredPosition = new Vector2(-10, 0);
        togRt.sizeDelta = new Vector2(70, 26);

        Image togImg = togGO.AddComponent<Image>();
        togImg.color = val ? toggleOn : toggleOff;

        GameObject togTGO = MkObj("T", togGO.transform);
        TextMeshProUGUI togTxt = togTGO.AddComponent<TextMeshProUGUI>();
        togTxt.text = val ? "ON" : "OFF";
        togTxt.fontSize = 14;
        togTxt.color = Color.white;
        togTxt.alignment = TextAlignmentOptions.Center;
        togTxt.fontStyle = FontStyles.Bold;
        Stretch(togTGO.GetComponent<RectTransform>());

        Button togBtn = togGO.AddComponent<Button>();
        togBtn.targetGraphic = togImg;
        togBtn.onClick.AddListener(() =>
        {
            val = !val;
            togImg.color = val ? toggleOn : toggleOff;
            togTxt.text = val ? "ON" : "OFF";
            onChanged?.Invoke(val);
        });
        MenuSFX.WireButton(togBtn);

        cursorY -= (H + 4f);
    }

    // ============ LOW-LEVEL HELPERS ============

    private GameObject MkBtnInRow(Transform parent, string text, float aMin, float aMax, float rowH)
    {
        GameObject go = MkObj($"Btn{text}", parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMin, 0.08f);
        rt.anchorMax = new Vector2(aMax, 0.92f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = btnNormal;
        Button btn = go.AddComponent<Button>();
        SetColors(btn, btnNormal, btnHover, btnPress);
        btn.targetGraphic = img;

        MakeCenteredLabel(go.transform, text, 20, Color.white, FontStyles.Bold);
        return go;
    }

    private void MakeLabel(Transform parent, string text, int size, Color color, FontStyles style,
        float aMinX, float aMinY, float aMaxX, float aMaxY,
        float pivX, float pivY, float posX, float posY, float sizeX, float sizeY)
    {
        GameObject go = MkObj("Lbl", parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, aMinY);
        rt.anchorMax = new Vector2(aMaxX, aMaxY);
        rt.pivot = new Vector2(pivX, pivY);
        rt.anchoredPosition = new Vector2(posX, posY);
        rt.sizeDelta = new Vector2(sizeX, sizeY);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
    }

    private void MakeCenteredLabel(Transform parent, string text, int size, Color color, FontStyles style)
    {
        GameObject go = MkObj("T", parent);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        Stretch(go.GetComponent<RectTransform>());
    }

    private void SetColors(Button btn, Color normal, Color hover, Color pressed)
    {
        ColorBlock cb = btn.colors;
        cb.normalColor = normal;
        cb.highlightedColor = hover;
        cb.pressedColor = pressed;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
    }

    private GameObject MkObj(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
