using UnityEngine;

/// <summary>
/// Player stat system with 5 core stats and leveling.
/// Plain [Serializable] data class stored on GameManager and in SaveData.
/// 
/// ══════════════════════════════════════════════
///  STAT           EFFECT PER POINT
/// ──────────────────────────────────────────────
///  STR  Strength  +2 min/max melee damage (base 5-10)
///  INT  Intellect +3 bonus spell damage (on top of spell base)
///  LUK  Luck      +2% crit chance (cap 60%), crits = 2x dmg
///  END  Endurance +15 max HP (base 100)
///  WIS  Wisdom    +5% chaos fill rate bonus
/// ──────────────────────────────────────────────
///  LEVELING: Each level = +3 stat points
///  XP to next: 80 + (level * 20)
/// ══════════════════════════════════════════════
/// </summary>
[System.Serializable]
public class PlayerStats
{
    public int STR = 0;
    public int INT = 0;
    public int LUK = 0;
    public int END = 0;
    public int WIS = 0;

    public int level = 1;
    public int currentXP = 0;
    public int unspentPoints = 0;
    public int bonusStatPoints = 0;

    // Constants
    public const int BASE_MIN_DMG = 5;
    public const int BASE_MAX_DMG = 10;
    public const int BASE_HP = 100;
    public const int STR_PER = 2;
    public const int INT_PER = 3;
    public const float LUK_PER = 0.02f;
    public const float LUK_CAP = 0.60f;
    public const int END_PER = 15;
    public const float WIS_PER = 0.05f;

    // Derived
    public int GetMinDamage() => BASE_MIN_DMG + (STR * STR_PER);
    public int GetMaxDamage() => BASE_MAX_DMG + (STR * STR_PER);
    public int GetMaxHealth() => BASE_HP + (END * END_PER);
    public float GetCritChance() => Mathf.Min(LUK_CAP, LUK * LUK_PER);
    public float GetChaosRate() => 1f + (WIS * WIS_PER);
    public int GetSpellBonus() => INT * INT_PER;

    public int RollMeleeDamage(out bool crit)
    {
        int d = Random.Range(GetMinDamage(), GetMaxDamage() + 1);
        crit = Random.value < GetCritChance();
        return crit ? d * 2 : d;
    }

    public int RollSpellDamage(int spellBase, out bool crit)
    {
        int d = spellBase + GetSpellBonus();
        crit = Random.value < GetCritChance();
        return crit ? d * 2 : d;
    }

    // Leveling
    public int XPToNext() => 80 + (level * 20);

    public int AddXP(int amount)
    {
        currentXP += amount;
        int gained = 0;
        while (currentXP >= XPToNext())
        {
            currentXP -= XPToNext();
            level++;
            unspentPoints += 3;
            gained++;
        }
        return gained;
    }

    public int AvailablePoints() => unspentPoints + bonusStatPoints;

    public bool SpendPoint(string stat)
    {
        if (AvailablePoints() <= 0) return false;
        switch (stat)
        {
            case "STR": STR++; break;
            case "INT": INT++; break;
            case "LUK": LUK++; break;
            case "END": END++; break;
            case "WIS": WIS++; break;
            default: return false;
        }
        if (bonusStatPoints > 0) bonusStatPoints--;
        else unspentPoints--;
        return true;
    }

    public void Reset()
    {
        STR = 0; INT = 0; LUK = 0; END = 0; WIS = 0;
        level = 1; currentXP = 0; unspentPoints = 0; bonusStatPoints = 0;
    }

    public static string GetDescription(string stat)
    {
        switch (stat)
        {
            case "STR": return "Increases melee damage.\n+2 min/max per point.";
            case "INT": return "Increases spell damage.\n+3 bonus per point.";
            case "LUK": return "Crit hit chance.\n+2% per point (cap 60%).\nCrits = 2x damage.";
            case "END": return "Increases max health.\n+15 HP per point.";
            case "WIS": return "Chaos fills faster.\n+5% fill rate per point.";
            default: return "";
        }
    }

    public static Color GetStatColor(string stat)
    {
        switch (stat)
        {
            case "STR": return new Color(1f, 0.3f, 0.2f);
            case "INT": return new Color(0.3f, 0.5f, 1f);
            case "LUK": return new Color(1f, 0.85f, 0.2f);
            case "END": return new Color(0.2f, 0.85f, 0.3f);
            case "WIS": return new Color(0.7f, 0.3f, 1f);
            default: return Color.white;
        }
    }
}
