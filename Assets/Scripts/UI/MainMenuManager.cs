using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Main Menu — styled to match the Azer: Path of Salvation concept art.
/// 
/// SETUP:
/// 1. In MainMenu scene, add your background image as a UI Image on a separate Canvas
///    (sorting order 0), OR as a SpriteRenderer behind the camera.
/// 2. This script's canvas is sorting order 50, so it layers on top.
/// 3. For music: add an AudioSource to this GameObject (or a MusicManager),
///    assign your clip, check Play On Awake + Loop.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    private Canvas menuCanvas;
    private GameObject mainPanel;
    private GameObject loadPanel;

    // ========== COLOR PALETTE (from concept art) ==========
    // Background: semi-transparent dark so the background IMAGE shows through
    private Color overlayColor = new Color(0.02f, 0.01f, 0.03f, 0.55f);

    // Buttons: dark crimson base (like the dark armor/cloak)
    private Color btnNormal = new Color(0.28f, 0.06f, 0.08f, 0.88f);
    private Color btnHover = new Color(0.40f, 0.08f, 0.50f, 0.95f);   // purple glow on hover
    private Color btnPressed = new Color(0.70f, 0.30f, 0.05f, 1f);    // fire orange on click
    private Color btnBorder = new Color(0.55f, 0.12f, 0.55f, 0.6f);   // subtle purple border

    // Text: warm gold (matching the logo)
    private Color goldText = new Color(1f, 0.82f, 0.3f, 1f);
    private Color brightGold = new Color(1f, 0.9f, 0.45f, 1f);

    // Title glow: purple accent
    private Color purpleAccent = new Color(0.65f, 0.3f, 1f, 1f);
    private Color subtitleColor = new Color(0.75f, 0.55f, 0.95f, 0.85f);

    // Load panel
    private Color panelColor = new Color(0.06f, 0.02f, 0.10f, 0.92f);
    private Color slotEmpty = new Color(0.12f, 0.05f, 0.12f, 0.5f);
    private Color slotFilled = new Color(0.25f, 0.06f, 0.10f, 0.85f);

    void Start()
    {
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("_GameSystems");
            gm.AddComponent<GameManager>();
            gm.AddComponent<UIManager>();
            gm.AddComponent<QuestSystem>();
            gm.AddComponent<DialogueSystem>();
        }

        if (EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        BuildMenuUI();
    }

    void BuildMenuUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("MainMenuCanvas");
        menuCanvas = canvasObj.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Semi-transparent overlay (NOT opaque — your background image shows through)
        GameObject overlay = CreateUI("Overlay", canvasObj.transform);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = overlayColor;
        overlayImg.raycastTarget = false;
        Stretch(overlay.GetComponent<RectTransform>());

        // ========== MAIN PANEL ==========
        mainPanel = CreateUI("MainPanel", canvasObj.transform);
        RectTransform mpRt = mainPanel.GetComponent<RectTransform>();
        mpRt.anchorMin = new Vector2(0.5f, 0.5f);
        mpRt.anchorMax = new Vector2(0.5f, 0.5f);
        mpRt.sizeDelta = new Vector2(520, 320);

        // Buttons (positioned lower to leave room for background logo)
        float btnY = -30;
        CreateStyledButton("New Game", mainPanel.transform, btnY, OnNewGame);
        btnY -= 75;
        CreateStyledButton("Load Game", mainPanel.transform, btnY, OnLoadGame);
        btnY -= 75;
        CreateStyledButton("Exit", mainPanel.transform, btnY, OnExit);

        // Version text (bottom)
        GameObject verObj = CreateUI("Version", mainPanel.transform);
        TextMeshProUGUI verText = verObj.AddComponent<TextMeshProUGUI>();
        verText.text = "v0.2";
        verText.fontSize = 14;
        verText.color = new Color(0.5f, 0.35f, 0.6f, 0.5f);
        verText.alignment = TextAlignmentOptions.Center;
        RectTransform verRt = verObj.GetComponent<RectTransform>();
        verRt.anchorMin = new Vector2(0, 0);
        verRt.anchorMax = new Vector2(1, 0);
        verRt.pivot = new Vector2(0.5f, 0);
        verRt.anchoredPosition = new Vector2(0, 5);
        verRt.sizeDelta = new Vector2(0, 25);

        // ========== LOAD PANEL (hidden) ==========
        BuildLoadPanel(canvasObj.transform);
    }

    void CreateStyledButton(string label, Transform parent, float yPos, UnityEngine.Events.UnityAction onClick)
    {
        // Outer container (for border effect)
        GameObject container = CreateUI($"BtnContainer_{label}", parent);
        RectTransform containerRt = container.GetComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 1);
        containerRt.anchorMax = new Vector2(0.5f, 1);
        containerRt.pivot = new Vector2(0.5f, 1);
        containerRt.anchoredPosition = new Vector2(0, yPos);
        containerRt.sizeDelta = new Vector2(340, 58);

        // Border glow
        Image borderImg = container.AddComponent<Image>();
        borderImg.color = btnBorder;
        borderImg.raycastTarget = false;

        // Inner button
        GameObject btnObj = CreateUI($"Btn_{label}", container.transform);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = Vector2.zero;
        btnRt.anchorMax = Vector2.one;
        btnRt.offsetMin = new Vector2(2, 2);
        btnRt.offsetMax = new Vector2(-2, -2);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = btnNormal;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = btnNormal;
        cb.highlightedColor = btnHover;
        cb.pressedColor = btnPressed;
        cb.selectedColor = btnNormal;
        cb.fadeDuration = 0.15f;
        btn.colors = cb;
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(onClick);

        // Wire SFX (hover + click sounds)
        MenuSFX.WireButton(btn);

        // Button text
        GameObject textObj = CreateUI("Text", btnObj.transform);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 26;
        text.color = goldText;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        Stretch(textObj.GetComponent<RectTransform>());
    }

    void BuildLoadPanel(Transform canvasTransform)
    {
        loadPanel = CreateUI("LoadPanel", canvasTransform);
        RectTransform lpRt = loadPanel.GetComponent<RectTransform>();
        lpRt.anchorMin = new Vector2(0.5f, 0.5f);
        lpRt.anchorMax = new Vector2(0.5f, 0.5f);
        lpRt.sizeDelta = new Vector2(560, 520);

        // Panel background
        Image panelBg = loadPanel.AddComponent<Image>();
        panelBg.color = panelColor;

        // Border
        GameObject border = CreateUI("Border", loadPanel.transform);
        Image borderImg = border.AddComponent<Image>();
        borderImg.color = new Color(0.45f, 0.1f, 0.45f, 0.4f);
        borderImg.raycastTarget = false;
        Stretch(border.GetComponent<RectTransform>());
        // Inner fill
        GameObject inner = CreateUI("Inner", border.transform);
        Image innerImg = inner.AddComponent<Image>();
        innerImg.color = panelColor;
        RectTransform innerRt = inner.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(2, 2);
        innerRt.offsetMax = new Vector2(-2, -2);

        // Load Title
        GameObject loadTitle = CreateUI("LoadTitle", loadPanel.transform);
        TextMeshProUGUI loadTitleText = loadTitle.AddComponent<TextMeshProUGUI>();
        loadTitleText.text = "LOAD GAME";
        loadTitleText.fontSize = 34;
        loadTitleText.color = brightGold;
        loadTitleText.alignment = TextAlignmentOptions.Center;
        loadTitleText.fontStyle = FontStyles.Bold;
        RectTransform ltRt = loadTitle.GetComponent<RectTransform>();
        ltRt.anchorMin = new Vector2(0, 1);
        ltRt.anchorMax = new Vector2(1, 1);
        ltRt.pivot = new Vector2(0.5f, 1);
        ltRt.anchoredPosition = new Vector2(0, -15);
        ltRt.sizeDelta = new Vector2(0, 45);

        // Decorative line
        GameObject line = CreateUI("DecorLine", loadPanel.transform);
        Image lineImg = line.AddComponent<Image>();
        lineImg.color = new Color(0.55f, 0.15f, 0.55f, 0.4f);
        lineImg.raycastTarget = false;
        RectTransform lineRt = line.GetComponent<RectTransform>();
        lineRt.anchorMin = new Vector2(0.5f, 1);
        lineRt.anchorMax = new Vector2(0.5f, 1);
        lineRt.pivot = new Vector2(0.5f, 0.5f);
        lineRt.anchoredPosition = new Vector2(0, -65);
        lineRt.sizeDelta = new Vector2(200, 2);

        // Save Slot Buttons
        float slotY = -80;
        for (int i = 0; i < 3; i++)
        {
            int slot = i;
            SaveData peek = SaveSystem.PeekSlot(i);
            string label;

            if (peek != null)
            {
                int minutes = Mathf.FloorToInt(peek.totalPlayTime / 60f);
                label = $"Slot {i + 1}: {peek.currentScene}  |  HP: {peek.health}  |  Coins: {peek.coins}\n" +
                        $"Quest #{peek.questIndex}  |  {minutes}min  |  {peek.saveDate}";
            }
            else
            {
                label = $"Slot {i + 1}:  — Empty —";
            }

            CreateSlotButton(label, loadPanel.transform, slotY, peek != null, () =>
            {
                if (SaveSystem.SlotExists(slot))
                {
                    GameManager.Instance.LoadGame(slot);
                }
            });
            slotY -= 105;
        }

        // Back Button
        CreateStyledButton("Back", loadPanel.transform, slotY - 10, () =>
        {
            loadPanel.SetActive(false);
            mainPanel.SetActive(true);
        });

        loadPanel.SetActive(false);
    }

    void CreateSlotButton(string label, Transform parent, float yPos, bool hasData, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUI("SlotBtn", parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, yPos);
        rt.sizeDelta = new Vector2(490, 90);

        Image img = btnObj.AddComponent<Image>();
        img.color = hasData ? slotFilled : slotEmpty;

        Button btn = btnObj.AddComponent<Button>();
        btn.interactable = hasData;
        ColorBlock cb = btn.colors;
        cb.normalColor = hasData ? slotFilled : slotEmpty;
        cb.highlightedColor = btnHover;
        cb.pressedColor = btnPressed;
        cb.disabledColor = new Color(0.08f, 0.04f, 0.08f, 0.35f);
        cb.fadeDuration = 0.15f;
        btn.colors = cb;
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        // Wire SFX
        MenuSFX.WireButton(btn);

        GameObject textObj = CreateUI("Text", btnObj.transform);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 16;
        text.color = hasData ? goldText : new Color(0.45f, 0.3f, 0.5f, 0.6f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        Stretch(textObj.GetComponent<RectTransform>());
    }

    // ========== CALLBACKS ==========

    void OnNewGame()
    {
        GameManager.Instance.StartNewGame();
    }

    void OnLoadGame()
    {
        mainPanel.SetActive(false);
        loadPanel.SetActive(true);
    }

    void OnExit()
    {
        GameManager.Instance.QuitGame();
    }

    // ========== HELPERS ==========

    private GameObject CreateUI(string name, Transform parent)
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
