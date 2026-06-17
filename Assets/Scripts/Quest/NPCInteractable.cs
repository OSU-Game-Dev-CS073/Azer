using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to any NPC for dialogue interaction.
/// FIX: Prompt now has consistent size regardless of NPC scale.
/// FIX: Won't re-trigger dialogue during cooldown.
/// </summary>
public class NPCInteractable : MonoBehaviour
{
    [Header("NPC Info")]
    public string npcName = "Villager";

    [Header("Default Dialogue (if no quest-specific lines)")]
    [TextArea(2, 4)]
    public string[] defaultDialogue = new string[] { "Hello, traveler." };

    [Header("Quest-Specific Dialogue")]
    public List<QuestDialogue> questDialogues = new List<QuestDialogue>();

    [Header("Quest Advancement")]
    [Tooltip("If > -1, talking to this NPC advances quest to this index when the quest is at advanceFromQuest")]
    public int advanceToQuest = -1;
    public int advanceFromQuest = -1;

    [Header("Interaction")]
    public float interactRange = 2f;
    public KeyCode interactKey = KeyCode.E;

    // Visual prompt
    private bool playerInRange = false;
    private GameObject promptUI;

    [System.Serializable]
    public class QuestDialogue
    {
        public int questIndex;
        [TextArea(2, 4)]
        public string[] lines;
    }

    void Start()
    {
        CreateInteractPrompt();
    }

    void Update()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        playerInRange = dist <= interactRange;

        if (promptUI != null)
        {
            promptUI.SetActive(playerInRange &&
                DialogueSystem.Instance != null &&
                !DialogueSystem.Instance.IsDialogueActive);
        }

        // Interact - check dialogue system exists and isn't active (including cooldown)
        if (playerInRange &&
            Input.GetKeyDown(interactKey) &&
            DialogueSystem.Instance != null &&
            !DialogueSystem.Instance.IsDialogueActive)
        {
            Interact();
        }
    }

    void Interact()
    {
        string[] lines = GetDialogueForCurrentQuest();

        DialogueSystem.Instance.StartDialogue(npcName, lines, () =>
        {
            if (advanceToQuest >= 0 && advanceFromQuest >= 0)
            {
                if (QuestSystem.Instance != null && QuestSystem.Instance.IsOnQuest(advanceFromQuest))
                {
                    QuestSystem.Instance.SetQuestIndex(advanceToQuest);
                    Debug.Log($"NPC {npcName} advanced quest to {advanceToQuest}");
                }
            }
        });
    }

    string[] GetDialogueForCurrentQuest()
    {
        if (QuestSystem.Instance == null) return defaultDialogue;

        int currentQuest = QuestSystem.Instance.CurrentIndex;

        foreach (QuestDialogue qd in questDialogues)
        {
            if (qd.questIndex == currentQuest && qd.lines != null && qd.lines.Length > 0)
            {
                return qd.lines;
            }
        }

        return defaultDialogue;
    }

    void CreateInteractPrompt()
    {
        // FIX: Create prompt NOT parented to NPC to avoid scale inheritance issues.
        // Instead, we'll position it manually in Update/LateUpdate.
        promptUI = new GameObject($"InteractPrompt_{npcName}");
        // Parent to NPC but we'll compensate for scale
        promptUI.transform.SetParent(transform);

        Canvas promptCanvas = promptUI.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.WorldSpace;
        promptCanvas.sortingOrder = 50;

        RectTransform canvasRt = promptUI.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(200f, 50f);

        // FIX: Compensate for parent NPC scale so prompt is always consistent size
        Vector3 parentScale = transform.lossyScale;
        float compensateX = parentScale.x != 0 ? 1f / Mathf.Abs(parentScale.x) : 1f;
        float compensateY = parentScale.y != 0 ? 1f / Mathf.Abs(parentScale.y) : 1f;
        canvasRt.localScale = new Vector3(0.01f * compensateX, 0.01f * compensateY, 0.01f);
        canvasRt.localPosition = new Vector3(0, 2f / Mathf.Abs(parentScale.y != 0 ? parentScale.y : 1f), 0);

        // Text
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(promptUI.transform, false);
        TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "[E] Talk";
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        promptUI.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
