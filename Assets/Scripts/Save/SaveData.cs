using System;

/// <summary>
/// PATCH 5 CHANGES:
/// - Added PlayerStats fields (STR/INT/LUK/END/WIS, level, XP, stat points)
/// - Old saves will load with default stat values (all 0, level 1) — safe migration
/// </summary>
[Serializable]
public class SaveData
{
    // Meta
    public int slotIndex;
    public string saveDate;
    public float totalPlayTime;

    // Player Stats
    public int health;
    public int maxHealth;
    public int coins;
    public int killCount;
    public float chaosMeter;

    // Location
    public string currentScene;
    public float playerPosX;
    public float playerPosY;
    public int facingDirection;

    // Quest
    public int questIndex;
    public string lastQuestDescription;

    // Skills
    public bool hasFireball;
    public bool hasIceBolt;

    // Dark Mode
    public bool isDarkMode;

    // === NEW: Player Stats & Leveling ===
    public int statSTR;
    public int statINT;
    public int statLUK;
    public int statEND;
    public int statWIS;
    public int statLevel;
    public int statCurrentXP;
    public int statUnspentPoints;
    public int statBonusPoints;

    public SaveData()
    {
        slotIndex = -1;
        saveDate = "";
        totalPlayTime = 0f;
        health = 100;
        maxHealth = 100;
        coins = 0;
        killCount = 0;
        chaosMeter = 0f;
        currentScene = "GameScene";
        playerPosX = 0f;
        playerPosY = 0f;
        facingDirection = 1;
        questIndex = 0;
        lastQuestDescription = "Explore the world";
        hasFireball = true;
        hasIceBolt = true;
        isDarkMode = false;

        // Stats defaults (new game)
        statSTR = 0;
        statINT = 0;
        statLUK = 0;
        statEND = 0;
        statWIS = 0;
        statLevel = 1;
        statCurrentXP = 0;
        statUnspentPoints = 0;
        statBonusPoints = 0;
    }
}
