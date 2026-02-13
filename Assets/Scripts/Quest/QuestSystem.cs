using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// QuestSystem manages the main story progression via a "quest string" (integer index).
/// Starts at 0, increases as the player progresses.
/// Attach to the GameManager GameObject.
/// </summary>
public class QuestSystem : MonoBehaviour
{
    public static QuestSystem Instance { get; private set; }

    [System.Serializable]
    public class QuestEntry
    {
        public int index;
        public string title;
        public string description;
        public string targetScene; // Which scene this quest is relevant in (optional)
        public string targetNPC;   // NPC to talk to (optional)

        public QuestEntry(int idx, string t, string desc, string scene = "", string npc = "")
        {
            index = idx;
            title = t;
            description = desc;
            targetScene = scene;
            targetNPC = npc;
        }
    }

    // The quest database - add your quests here later
    private List<QuestEntry> questDatabase = new List<QuestEntry>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeQuests();
        }
        else
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// Define all quests here. You'll fill this in with your narrative later.
    /// Quest index must match the list index.
    /// </summary>
    private void InitializeQuests()
    {
        // ============ PLACEHOLDER QUESTS ============
        // Replace these with your actual story beats
        questDatabase.Add(new QuestEntry(0,
            "Awakening",
            "Explore the surrounding area",
            "GameScene"));

        questDatabase.Add(new QuestEntry(1,
            "The Path Forward",
            "Find the way through the forest",
            "Level2"));

        questDatabase.Add(new QuestEntry(2,
            "Town of Whispers",
            "Talk to the villagers in town",
            "Level3", "Villager"));

        questDatabase.Add(new QuestEntry(3,
            "Into the Depths",
            "Enter the cave and survive",
            "Level4"));

        questDatabase.Add(new QuestEntry(4,
            "Swamp Crossing",
            "Cross the dangerous swamp",
            "Level5"));

        questDatabase.Add(new QuestEntry(5,
            "The Hidden Path",
            "Find the secret passage",
            "Level5a"));

        questDatabase.Add(new QuestEntry(6,
            "Rat King's Domain",
            "Clear the underground tunnels",
            "Level6"));

        questDatabase.Add(new QuestEntry(7,
            "The Final Test",
            "Reach the end of the castle",
            "Level7"));

        questDatabase.Add(new QuestEntry(8,
            "To Be Continued...",
            "Your journey continues...",
            ""));
    }

    // ============ PUBLIC API ============

    /// <summary>
    /// Get the current quest index from GameManager.
    /// </summary>
    public int CurrentIndex => GameManager.Instance != null ? GameManager.Instance.questIndex : 0;

    /// <summary>
    /// Get the current quest entry.
    /// </summary>
    public QuestEntry GetCurrentQuest()
    {
        int idx = CurrentIndex;
        if (idx >= 0 && idx < questDatabase.Count)
            return questDatabase[idx];
        return questDatabase[questDatabase.Count - 1]; // Return last if out of range
    }

    /// <summary>
    /// Get the display text for the HUD.
    /// </summary>
    public string GetCurrentQuestText()
    {
        QuestEntry quest = GetCurrentQuest();
        return $"{quest.title}: {quest.description}";
    }

    /// <summary>
    /// Advance to the next quest. Call this when a quest is completed.
    /// </summary>
    public void AdvanceQuest()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.questIndex++;
        Debug.Log($"Quest advanced to index {GameManager.Instance.questIndex}: {GetCurrentQuestText()}");

        // Update HUD
        UIManager.Instance?.UpdateQuestDisplay(GetCurrentQuestText());
    }

    /// <summary>
    /// Set the quest index directly (e.g., from save data).
    /// </summary>
    public void SetQuestIndex(int index)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.questIndex = index;
        UIManager.Instance?.UpdateQuestDisplay(GetCurrentQuestText());
    }

    /// <summary>
    /// Check if the player is on a specific quest index.
    /// Useful for NPC dialogue branching.
    /// </summary>
    public bool IsOnQuest(int index)
    {
        return CurrentIndex == index;
    }

    /// <summary>
    /// Check if the player has completed a specific quest.
    /// </summary>
    public bool HasCompletedQuest(int index)
    {
        return CurrentIndex > index;
    }

    /// <summary>
    /// Get a quest by index (for dialogue/NPC reference).
    /// </summary>
    public QuestEntry GetQuest(int index)
    {
        if (index >= 0 && index < questDatabase.Count)
            return questDatabase[index];
        return null;
    }

    /// <summary>
    /// Total number of quests defined.
    /// </summary>
    public int TotalQuests => questDatabase.Count;
}
