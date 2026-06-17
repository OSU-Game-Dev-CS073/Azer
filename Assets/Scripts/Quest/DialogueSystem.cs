using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// DialogueSystem handles NPC dialogue with typing effect.
/// FIX: Added ESC to close dialogue.
/// FIX: Added cooldown after dialogue ends to prevent instant re-trigger.
/// FIX: Updated prompt text to show ESC hint.
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    // UI Elements
    private GameObject dialoguePanel;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI dialogueText;
    private TextMeshProUGUI continuePrompt;

    // State
    private Queue<string> sentences = new Queue<string>();
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private string currentSentence = "";
    private Coroutine typingCoroutine;

    // Cooldown to prevent re-trigger (fixes infinite loop)
    private float dialogueEndCooldown = 0f;
    private const float COOLDOWN_DURATION = 0.3f;

    // Settings
    private float typingSpeed = 0.03f;

    // Callback when dialogue ends
    private System.Action onDialogueEnd;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    void Start()
    {
        BuildDialogueUI();
    }

    void Update()
    {
        // Tick cooldown
        if (dialogueEndCooldown > 0f)
        {
            dialogueEndCooldown -= Time.unscaledDeltaTime;
        }

        if (!isDialogueActive) return;

        // ESC to close dialogue immediately
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EndDialogue();
            return;
        }

        // E or Enter to advance dialogue
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping)
            {
                // Skip typing, show full sentence
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                dialogueText.text = currentSentence;
                isTyping = false;
                continuePrompt.gameObject.SetActive(true);
            }
            else
            {
                DisplayNextSentence();
            }
        }
    }

    private void BuildDialogueUI()
    {
        // Create our own canvas for dialogue (always on top)
        GameObject canvasObj = new GameObject("DialogueCanvas");
        canvasObj.transform.SetParent(transform);
        DontDestroyOnLoad(canvasObj);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Dialogue Panel (bottom of screen)
        dialoguePanel = new GameObject("DialoguePanel");
        dialoguePanel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRt = dialoguePanel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.1f, 0);
        panelRt.anchorMax = new Vector2(0.9f, 0);
        panelRt.pivot = new Vector2(0.5f, 0);
        panelRt.anchoredPosition = new Vector2(0, 30);
        panelRt.sizeDelta = new Vector2(0, 180);

        Image panelBg = dialoguePanel.AddComponent<Image>();
        panelBg.color = new Color(0.02f, 0.02f, 0.08f, 0.92f);

        // Border effect
        GameObject border = new GameObject("Border");
        border.transform.SetParent(dialoguePanel.transform, false);
        RectTransform borderRt = border.AddComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(3, 3);
        borderRt.offsetMax = new Vector2(-3, -3);
        Image borderImg = border.AddComponent<Image>();
        borderImg.color = new Color(0.05f, 0.05f, 0.15f, 0.95f);

        // NPC Name
        GameObject nameObj = new GameObject("NPCName");
        nameObj.transform.SetParent(dialoguePanel.transform, false);
        nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "";
        nameText.fontSize = 22;
        nameText.color = new Color(0.4f, 0.7f, 1f, 1f);
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 1);
        nameRt.anchorMax = new Vector2(1, 1);
        nameRt.pivot = new Vector2(0, 1);
        nameRt.anchoredPosition = new Vector2(20, -12);
        nameRt.sizeDelta = new Vector2(-40, 30);

        // Dialogue Text
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(dialoguePanel.transform, false);
        dialogueText = textObj.AddComponent<TextMeshProUGUI>();
        dialogueText.text = "";
        dialogueText.fontSize = 20;
        dialogueText.color = Color.white;
        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        dialogueText.enableWordWrapping = true;
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = new Vector2(20, 30);
        textRt.offsetMax = new Vector2(-20, -45);

        // Continue Prompt - UPDATED text to show ESC hint
        GameObject promptObj = new GameObject("ContinuePrompt");
        promptObj.transform.SetParent(dialoguePanel.transform, false);
        continuePrompt = promptObj.AddComponent<TextMeshProUGUI>();
        continuePrompt.text = "[E] Continue    [ESC] Close";
        continuePrompt.fontSize = 14;
        continuePrompt.color = new Color(0.6f, 0.6f, 0.8f, 0.8f);
        continuePrompt.alignment = TextAlignmentOptions.BottomRight;
        RectTransform promptRt = promptObj.GetComponent<RectTransform>();
        promptRt.anchorMin = new Vector2(0, 0);
        promptRt.anchorMax = new Vector2(1, 0);
        promptRt.pivot = new Vector2(1, 0);
        promptRt.anchoredPosition = new Vector2(-15, 8);
        promptRt.sizeDelta = new Vector2(0, 25);

        dialoguePanel.SetActive(false);
    }

    // ============ PUBLIC API ============

    public void StartDialogue(string npcName, string[] lines, System.Action onEnd = null)
    {
        if (isDialogueActive) return;

        // FIX: Don't start if still in cooldown from previous dialogue
        if (dialogueEndCooldown > 0f) return;

        isDialogueActive = true;
        onDialogueEnd = onEnd;

        nameText.text = npcName;
        sentences.Clear();

        foreach (string line in lines)
        {
            sentences.Enqueue(line);
        }

        dialoguePanel.SetActive(true);

        // Pause player input
        Player player = Object.FindAnyObjectByType<Player>();
        if (player != null)
        {
            player.SetInputEnabled(false);
        }

        DisplayNextSentence();
    }

    private void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentSentence = sentences.Dequeue();
        continuePrompt.gameObject.SetActive(false);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(currentSentence));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in sentence)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        continuePrompt.gameObject.SetActive(true);
    }

    private void EndDialogue()
    {
        // Stop any typing
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        isDialogueActive = false;
        isTyping = false;
        dialoguePanel.SetActive(false);

        // FIX: Set cooldown so NPCInteractable doesn't immediately re-trigger
        dialogueEndCooldown = COOLDOWN_DURATION;

        // Re-enable player input
        Player player = Object.FindAnyObjectByType<Player>();
        if (player != null)
        {
            player.SetInputEnabled(true);
        }

        onDialogueEnd?.Invoke();
        onDialogueEnd = null;
    }

    /// <summary>
    /// Returns true if dialogue is active. Also returns true during cooldown
    /// to prevent NPCInteractable from re-triggering.
    /// </summary>
    public bool IsDialogueActive => isDialogueActive || dialogueEndCooldown > 0f;
}
