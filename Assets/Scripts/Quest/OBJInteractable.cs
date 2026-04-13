using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class OBJInteractable : MonoBehaviour
{
    [Header("Object Info")]
    public string objectName = "Object";

    [Header("Interaction Type")]
    public InteractionType interactionType = InteractionType.Readable;
    
    public enum InteractionType
    {
        Readable,
        Door
    }

    [Header("Readable Settings")]
    [TextArea(3, 10)]
    public string[] readableText = new string[] { "This is a readable object." };

    [Header("Quest-Specific Text")]
    public List<QuestReadable> questReadables = new List<QuestReadable>();

    [Header("Door Settings")]
    public string targetSceneName = "NextLevel";
    public string doorDestinationID = "";

    [Header("Key Lock Settings")]
    public bool requiresKey = false;
    public string requiredKeyName = "Key";
    public bool consumeKeyOnUse = true;
    public string lockedMessage = "The door is locked. You need a key.";

    [Header("Quest Lock Settings")]
    public bool requireQuestToUnlock = false;
    public int requiredQuestIndex = -1;
    public string questLockedMessage = "The door is locked by a mysterious force.";

    [Header("Quest Advancement")]
    public int advanceToQuest = -1;
    public int advanceFromQuest = -1;

    [Header("Interaction Settings")]
    public float interactRange = 2f;
    public KeyCode interactKey = KeyCode.E;
    public string promptText = "[E] Read";

    [Header("Visual Effects")]
    public bool highlightWhenInRange = false;
    public Color highlightColor = Color.yellow;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    [Header("Audio")]
    public AudioClip interactionSound;
    public float soundVolume = 0.6f;

    private bool playerInRange = false;
    private GameObject promptUI;
    private AudioSource audioSource;

    [System.Serializable]
    public class QuestReadable
    {
        public int questIndex;
        [TextArea(3, 10)]
        public string[] lines;
    }

    void Start()
    {
        CreateInteractPrompt();
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && interactionSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        UpdatePromptText();
    }

    void Update()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        playerInRange = dist <= interactRange;

        if (highlightWhenInRange && spriteRenderer != null)
            spriteRenderer.color = playerInRange ? highlightColor : originalColor;

        if (promptUI != null)
        {
            bool canInteract = playerInRange;
            if (interactionType == InteractionType.Readable)
                canInteract = canInteract && DialogueSystem.Instance != null && !DialogueSystem.Instance.IsDialogueActive;
            promptUI.SetActive(canInteract);
        }

        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            bool canInteract = true;
            if (interactionType == InteractionType.Readable)
                canInteract = DialogueSystem.Instance != null && !DialogueSystem.Instance.IsDialogueActive;
            
            if (canInteract) Interact();
        }
    }

    void Interact()
    {
        PlayInteractionSound();
        
        switch (interactionType)
        {
            case InteractionType.Readable:
                HandleReadableInteraction();
                break;
            case InteractionType.Door:
                HandleDoorInteraction();
                break;
        }
    }
    
    void HandleReadableInteraction()
    {
        string[] lines = GetTextForCurrentQuest();
        DialogueSystem.Instance.StartDialogue(objectName, lines, () => OnAfterInteraction());
    }
    
    void HandleDoorInteraction()
    {
        Debug.Log($"[Door] HandleDoorInteraction called on {objectName}");
        
        // Check quest lock first
        if (requireQuestToUnlock && requiredQuestIndex >= 0)
        {
            if (QuestSystem.Instance == null || !QuestSystem.Instance.IsOnQuest(requiredQuestIndex))
            {
                ShowLockedMessage(questLockedMessage);
                return;
            }
        }

        // Check key lock
        if (requiresKey && !string.IsNullOrEmpty(requiredKeyName))
        {
            Debug.Log($"[Door] Checking key: {requiredKeyName}");
            
            if (InventoryController.Instance == null)
            {
                Debug.LogError("[Door] InventoryController.Instance is null!");
                ShowLockedMessage(lockedMessage);
                return;
            }

            int keyCount = InventoryController.Instance.GetItemCount(requiredKeyName);
            Debug.Log($"[Door] Current key count for '{requiredKeyName}': {keyCount}");
            
            if (keyCount <= 0)
            {
                Debug.Log($"[Door] No key found! Cannot open door.");
                ShowLockedMessage(lockedMessage);
                return;
            }

            // Consume the key
            if (consumeKeyOnUse)
            {
                Debug.Log($"[Door] Attempting to remove 1 key...");
                bool removed = InventoryController.Instance.RemoveItem(requiredKeyName, 1);
                
                if (removed)
                {
                    Debug.Log($"[Door] Key removed successfully!");
                    
                    // Force save to GameManager
                    InventoryController.Instance.SaveToGameManager();
                    
                    // Verify removal
                    int remaining = InventoryController.Instance.GetItemCount(requiredKeyName);
                    Debug.Log($"[Door] Remaining keys after removal: {remaining}");
                }
                else
                {
                    Debug.LogError($"[Door] Failed to remove key '{requiredKeyName}' from inventory!");
                    ShowLockedMessage("Something went wrong...");
                    return;
                }
            }
        }

        // Unlocked – proceed to scene transition
        Debug.Log($"[Door] Door unlocked! Loading scene: {targetSceneName}");
        
        // Final save before scene change
        if (InventoryController.Instance != null)
            InventoryController.Instance.SaveToGameManager();
        
        if (SceneController.instance != null)
        {
            if (!string.IsNullOrEmpty(doorDestinationID) && GameManager.Instance != null)
            {
                var method = GameManager.Instance.GetType().GetMethod("SetDoorEntrance");
                if (method != null)
                    method.Invoke(GameManager.Instance, new object[] { doorDestinationID });
            }
            SceneController.instance.LoadSceneByName(targetSceneName);
        }
        else
        {
            if (GameManager.Instance != null)
            {
                var loadMethod = GameManager.Instance.GetType().GetMethod("LoadScene");
                if (loadMethod != null)
                {
                    loadMethod.Invoke(GameManager.Instance, new object[] { targetSceneName });
                    return;
                }
            }
            Debug.LogWarning($"SceneController not found. Directly loading scene: {targetSceneName}");
            SceneManager.LoadScene(targetSceneName);
        }
        
        OnAfterInteraction();
    }

    void ShowLockedMessage(string message)
    {
        if (DialogueSystem.Instance != null && !string.IsNullOrEmpty(message))
        {
            string[] msg = new string[] { message };
            DialogueSystem.Instance.StartDialogue(objectName, msg, null);
        }
        else
        {
            Debug.Log($"Door locked: {message}");
        }
    }
    
    void OnAfterInteraction()
    {
        if (advanceToQuest >= 0 && advanceFromQuest >= 0)
        {
            if (QuestSystem.Instance != null && QuestSystem.Instance.IsOnQuest(advanceFromQuest))
            {
                QuestSystem.Instance.SetQuestIndex(advanceToQuest);
                Debug.Log($"Interacting with {objectName} advanced quest to {advanceToQuest}");
            }
        }
    }
    
    string[] GetTextForCurrentQuest()
    {
        if (QuestSystem.Instance == null) return readableText;
        int currentQuest = QuestSystem.Instance.CurrentIndex;
        foreach (QuestReadable qr in questReadables)
            if (qr.questIndex == currentQuest && qr.lines != null && qr.lines.Length > 0)
                return qr.lines;
        return readableText;
    }
    
    void UpdatePromptText()
    {
        if (interactionType == InteractionType.Door)
        {
            promptText = "[E] Enter";
            if (requiresKey) promptText = "[E] Unlock Door";
        }
        else
        {
            promptText = "[E] Read";
        }
        
        if (promptUI != null)
        {
            TMPro.TextMeshProUGUI text = promptUI.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null) text.text = promptText;
        }
    }
    
    void PlayInteractionSound()
    {
        if (audioSource != null && interactionSound != null)
            audioSource.PlayOneShot(interactionSound, soundVolume);
    }
    
    void CreateInteractPrompt()
    {
        promptUI = new GameObject($"InteractPrompt_{objectName}");
        promptUI.transform.SetParent(transform);
        
        Canvas promptCanvas = promptUI.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.WorldSpace;
        promptCanvas.sortingOrder = 50;
        
        RectTransform canvasRt = promptUI.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(200f, 50f);
        
        Vector3 parentScale = transform.lossyScale;
        float compensateX = parentScale.x != 0 ? 1f / Mathf.Abs(parentScale.x) : 1f;
        float compensateY = parentScale.y != 0 ? 1f / Mathf.Abs(parentScale.y) : 1f;
        canvasRt.localScale = new Vector3(0.01f * compensateX, 0.01f * compensateY, 0.01f);
        canvasRt.localPosition = new Vector3(0, 2f / Mathf.Abs(parentScale.y != 0 ? parentScale.y : 1f), 0);
        
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(promptUI.transform, false);
        TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = promptText;
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
        Gizmos.color = interactionType == InteractionType.Door ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}