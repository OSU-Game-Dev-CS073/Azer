using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Stat allocation panel. Opens from Pause Menu → Stats button.
/// Shows all 5 stats with descriptions, [+] allocation buttons, and derived stat previews.
/// 
/// Layout:
///   Header: "CHARACTER STATS" + Level + Available Points
///   Per stat row: [Name] [Value] [+] [Description]
///   Bottom: Derived stats summary
/// </summary>
public class StatPanelUI : MonoBehaviour
{
    public static StatPanelUI Instance { get; private set; }

    private GameObject panelRoot;
    private bool isOpen = false;

    // References for live updating
    private TextMeshProUGUI pointsText;
    private TextMeshProUGUI levelInfoText;
    private TextMeshProUGUI derivedText;
    private TextMeshProUGUI[] statValueTexts = new TextMeshProUGUI[5];
    private Button[] plusButtons = new Button[5];

    private readonly string[] statNames = { "STR", "INT", "LUK", "END", "WIS" };

    // Colors
    private Color bgColor = new Color(0.03f, 0.02f, 0.07f, 0.97f);
    private Color rowBg = new Color(0.06f, 0.04f, 0.10f, 0.7f);
    private Color btnColor = new Color(0.15f, 0.35f, 0.15f, 0.9f);
    private Color btnHover = new Color(0.2f, 0.5f, 0.2f, 1f);
    private Color btnDisabled = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    private Color headerColor = new Color(0.5f, 0.7f, 1f, 1f);
    private Color descColor = new Color(0.6f, 0.55f, 0.7f, 0.9f);

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(this); }
    }

    public void Open()
    {
        if (panelRoot == null) Build();
        Refresh();
        panelRoot.SetActive(true);
        isOpen = true;
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        isOpen = false;
    }

    public bool IsOpen => isOpen;

    // ============ BUILD ============

    private void Build()
    {
        GameObject canvasObj = new GameObject("StatPanelCanvas");
        canvasObj.transform.SetParent(transform);
        DontDestroyOnLoad(canvasObj);

        Canvas c = canvasObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 250;

        CanvasScaler s = canvasObj.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        s.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        panelRoot = Mk("Root", canvasObj.transform);
        Stretch(panelRoot.GetComponent<RectTransform>());

        // Overlay
        GameObject ov = Mk("Overlay", panelRoot.transform);
        ov.AddComponent<Image>().color = new Color(0, 0, 0, 0.4f);
        Stretch(ov.GetComponent<RectTransform>());
        Button ovBtn = ov.AddComponent<Button>();
        ovBtn.transition = Selectable.Transition.None;
        ovBtn.onClick.AddListener(Close);

        // Center panel
        GameObject panel = Mk("Panel", panelRoot.transform);
        panel.AddComponent<Image>().color = bgColor;
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 0.5f); pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(620, 560);

        // Border
        GameObject border = Mk("Border", panel.transform);
        border.AddComponent<Image>().color = new Color(0.3f, 0.4f, 0.7f, 0.4f);
        border.GetComponent<Image>().raycastTarget = false;
        Stretch(border.GetComponent<RectTransform>());
        GameObject inner = Mk("Inner", border.transform);
        inner.AddComponent<Image>().color = bgColor;
        inner.GetComponent<Image>().raycastTarget = false;
        RectTransform ir = inner.GetComponent<RectTransform>();
        ir.anchorMin = Vector2.zero; ir.anchorMax = Vector2.one;
        ir.offsetMin = new Vector2(2, 2); ir.offsetMax = new Vector2(-2, -2);

        float curY = -12f;

        // Title
        MkLabel(panel.transform, "CHARACTER STATS", 26, headerColor, FontStyles.Bold,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, curY), new Vector2(0, 32));
        curY -= 36;

        // Level + Points row
        GameObject infoRow = Mk("Info", panel.transform);
        RectTransform infoRt = infoRow.GetComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0, 1); infoRt.anchorMax = new Vector2(1, 1);
        infoRt.pivot = new Vector2(0.5f, 1);
        infoRt.anchoredPosition = new Vector2(0, curY);
        infoRt.sizeDelta = new Vector2(0, 28);

        levelInfoText = MkLabelReturn(infoRow.transform, "", 16, new Color(0.7f, 0.8f, 1f),
            new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(0, 0.5f), new Vector2(15, 0));
        levelInfoText.alignment = TextAlignmentOptions.MidlineLeft;

        pointsText = MkLabelReturn(infoRow.transform, "", 18, new Color(1f, 0.85f, 0.2f),
            new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-15, 0));
        pointsText.alignment = TextAlignmentOptions.MidlineRight;
        pointsText.fontStyle = FontStyles.Bold;

        curY -= 34;

        // Column headers
        MkSmallLabel(panel.transform, "STAT", 12, descColor, 0.02f, 0.12f, curY, 20);
        MkSmallLabel(panel.transform, "VAL", 12, descColor, 0.13f, 0.20f, curY, 20);
        MkSmallLabel(panel.transform, "", 12, descColor, 0.21f, 0.28f, curY, 20);
        MkSmallLabel(panel.transform, "DESCRIPTION", 12, descColor, 0.30f, 0.98f, curY, 20);
        curY -= 24;

        // Stat rows
        for (int i = 0; i < 5; i++)
        {
            BuildStatRow(panel.transform, i, curY);
            curY -= 54;
        }

        curY -= 8;

        // Divider
        GameObject div = Mk("Div", panel.transform);
        div.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.5f, 0.4f);
        RectTransform divRt = div.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.05f, 1); divRt.anchorMax = new Vector2(0.95f, 1);
        divRt.pivot = new Vector2(0.5f, 1);
        divRt.anchoredPosition = new Vector2(0, curY);
        divRt.sizeDelta = new Vector2(0, 2);
        curY -= 10;

        // Derived stats summary
        GameObject derivedObj = Mk("Derived", panel.transform);
        RectTransform drt = derivedObj.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.03f, 1); drt.anchorMax = new Vector2(0.97f, 1);
        drt.pivot = new Vector2(0.5f, 1);
        drt.anchoredPosition = new Vector2(0, curY);
        drt.sizeDelta = new Vector2(0, 70);

        derivedText = derivedObj.AddComponent<TextMeshProUGUI>();
        derivedText.fontSize = 14;
        derivedText.color = new Color(0.75f, 0.75f, 0.85f);
        derivedText.alignment = TextAlignmentOptions.TopLeft;
        derivedText.enableWordWrapping = true;

        // Close button
        GameObject closeObj = Mk("CloseBtn", panel.transform);
        RectTransform crt = closeObj.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0); crt.anchorMax = new Vector2(0.5f, 0);
        crt.pivot = new Vector2(0.5f, 0);
        crt.anchoredPosition = new Vector2(0, 10);
        crt.sizeDelta = new Vector2(180, 34);
        Image ci = closeObj.AddComponent<Image>();
        ci.color = new Color(0.35f, 0.08f, 0.08f, 0.9f);
        Button cb = closeObj.AddComponent<Button>();
        SetBtnColors(cb, ci.color, new Color(0.5f, 0.12f, 0.12f, 1f));
        cb.targetGraphic = ci;
        cb.onClick.AddListener(Close);
        MenuSFX.WireButton(cb);
        MkCenteredLabel(closeObj.transform, "CLOSE", 16, Color.white, FontStyles.Bold);

        panelRoot.SetActive(false);
    }

    private void BuildStatRow(Transform parent, int index, float y)
    {
        string stat = statNames[index];
        Color statColor = PlayerStats.GetStatColor(stat);
        float rowH = 48f;

        // Row bg
        GameObject row = Mk($"Row_{stat}", parent);
        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0.02f, 1); rrt.anchorMax = new Vector2(0.98f, 1);
        rrt.pivot = new Vector2(0.5f, 1);
        rrt.anchoredPosition = new Vector2(0, y);
        rrt.sizeDelta = new Vector2(0, rowH);
        row.AddComponent<Image>().color = rowBg;

        // Stat name
        MkSmallLabel(row.transform, stat, 20, statColor, 0.02f, 0.12f, 0, rowH, true, FontStyles.Bold);

        // Stat value
        GameObject valObj = Mk("Val", row.transform);
        RectTransform vrt = valObj.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0.13f, 0); vrt.anchorMax = new Vector2(0.20f, 1);
        vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
        statValueTexts[index] = valObj.AddComponent<TextMeshProUGUI>();
        statValueTexts[index].text = "0";
        statValueTexts[index].fontSize = 22;
        statValueTexts[index].color = Color.white;
        statValueTexts[index].alignment = TextAlignmentOptions.Center;
        statValueTexts[index].fontStyle = FontStyles.Bold;

        // [+] button
        GameObject plusObj = Mk("Plus", row.transform);
        RectTransform prt = plusObj.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.21f, 0.12f); prt.anchorMax = new Vector2(0.28f, 0.88f);
        prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
        Image pi = plusObj.AddComponent<Image>();
        pi.color = btnColor;
        plusButtons[index] = plusObj.AddComponent<Button>();
        SetBtnColors(plusButtons[index], btnColor, btnHover);
        plusButtons[index].targetGraphic = pi;
        int idx = index; // capture
        plusButtons[index].onClick.AddListener(() => OnPlusClicked(idx));
        MenuSFX.WireButton(plusButtons[index]);
        MkCenteredLabel(plusObj.transform, "+", 22, Color.white, FontStyles.Bold);

        // Description
        GameObject descObj = Mk("Desc", row.transform);
        RectTransform drt = descObj.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.30f, 0); drt.anchorMax = new Vector2(0.98f, 1);
        drt.offsetMin = new Vector2(5, 2); drt.offsetMax = new Vector2(-5, -2);
        TextMeshProUGUI dt = descObj.AddComponent<TextMeshProUGUI>();
        dt.text = PlayerStats.GetDescription(stat);
        dt.fontSize = 12;
        dt.color = descColor;
        dt.alignment = TextAlignmentOptions.MidlineLeft;
        dt.enableWordWrapping = true;
    }

    // ============ LOGIC ============

    private void OnPlusClicked(int index)
    {
        if (GameManager.Instance == null) return;
        string stat = statNames[index];

        if (GameManager.Instance.stats.SpendPoint(stat))
        {
            // If END increased, recalculate max HP
            if (stat == "END")
                GameManager.Instance.RecalculateMaxHealth();

            Refresh();
        }
    }

    private void Refresh()
    {
        if (GameManager.Instance == null) return;
        PlayerStats st = GameManager.Instance.stats;

        int[] values = { st.STR, st.INT, st.LUK, st.END, st.WIS };
        for (int i = 0; i < 5; i++)
        {
            if (statValueTexts[i] != null)
                statValueTexts[i].text = values[i].ToString();

            // Disable + buttons when no points available
            if (plusButtons[i] != null)
            {
                bool canSpend = st.AvailablePoints() > 0;
                plusButtons[i].interactable = canSpend;
                Image img = plusButtons[i].GetComponent<Image>();
                if (img != null) img.color = canSpend ? btnColor : btnDisabled;
            }
        }

        if (levelInfoText != null)
            levelInfoText.text = $"Level {st.level}   XP: {st.currentXP}/{st.XPToNext()}";

        if (pointsText != null)
        {
            int pts = st.AvailablePoints();
            pointsText.text = pts > 0 ? $"Available Points: {pts}" : "No points available";
            pointsText.color = pts > 0 ? new Color(1f, 0.85f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);
        }

        // Derived stats
        if (derivedText != null)
        {
            derivedText.text =
                $"Melee Damage: {st.GetMinDamage()}-{st.GetMaxDamage()}    " +
                $"Spell Bonus: +{st.GetSpellBonus()}    " +
                $"Crit: {st.GetCritChance() * 100f:F0}%\n" +
                $"Max HP: {st.GetMaxHealth()}    " +
                $"Chaos Rate: {st.GetChaosRate():F2}x";
        }
    }

    // ============ HELPERS ============

    private void MkSmallLabel(Transform parent, string text, int size, Color color,
        float aMinX, float aMaxX, float y, float h, bool anchored = false, FontStyles style = FontStyles.Normal)
    {
        GameObject o = Mk("L", parent);
        RectTransform rt = o.GetComponent<RectTransform>();
        if (anchored)
        {
            rt.anchorMin = new Vector2(aMinX, 0); rt.anchorMax = new Vector2(aMaxX, 1);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.anchorMin = new Vector2(aMinX, 1); rt.anchorMax = new Vector2(aMaxX, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(0, h);
        }
        TextMeshProUGUI t = o.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
    }

    private void MkLabel(Transform parent, string text, int size, Color color, FontStyles style,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 sd)
    {
        GameObject o = Mk("L", parent);
        RectTransform rt = o.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = sd;
        TextMeshProUGUI t = o.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
    }

    private TextMeshProUGUI MkLabelReturn(Transform parent, string text, int size, Color color,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos)
    {
        GameObject o = Mk("L", parent);
        RectTransform rt = o.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = Vector2.zero;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        TextMeshProUGUI t = o.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color;
        return t;
    }

    private void MkCenteredLabel(Transform parent, string text, int size, Color color, FontStyles style)
    {
        GameObject o = Mk("T", parent);
        TextMeshProUGUI t = o.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        Stretch(o.GetComponent<RectTransform>());
    }

    private void SetBtnColors(Button btn, Color normal, Color hover)
    {
        ColorBlock cb = btn.colors;
        cb.normalColor = normal; cb.highlightedColor = hover;
        cb.pressedColor = new Color(0.3f, 0.7f, 0.3f, 1f);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
    }

    private GameObject Mk(string name, Transform parent)
    {
        GameObject o = new GameObject(name);
        o.transform.SetParent(parent, false);
        o.AddComponent<RectTransform>();
        return o;
    }

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
