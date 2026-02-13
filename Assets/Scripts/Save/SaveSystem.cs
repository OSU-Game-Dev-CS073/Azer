using UnityEngine;
using System.IO;

public static class SaveSystem
{
    private static string SavePath => Application.persistentDataPath;
    private const int MAX_SLOTS = 3;

    private static string GetFilePath(int slot)
    {
        return Path.Combine(SavePath, $"azer_save_slot{slot}.json");
    }

    public static void Save(int slot, SaveData data)
    {
        if (slot < 0 || slot >= MAX_SLOTS)
        {
            Debug.LogError($"Invalid save slot: {slot}");
            return;
        }

        data.slotIndex = slot;
        data.saveDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        string json = JsonUtility.ToJson(data, true);
        string path = GetFilePath(slot);

        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"Game saved to slot {slot} at {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save: {e.Message}");
        }
    }

    public static SaveData Load(int slot)
    {
        if (slot < 0 || slot >= MAX_SLOTS)
        {
            Debug.LogError($"Invalid save slot: {slot}");
            return null;
        }

        string path = GetFilePath(slot);

        if (!File.Exists(path))
        {
            Debug.Log($"No save file found in slot {slot}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"Game loaded from slot {slot}");
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load: {e.Message}");
            return null;
        }
    }

    public static bool SlotExists(int slot)
    {
        return File.Exists(GetFilePath(slot));
    }

    public static void DeleteSlot(int slot)
    {
        string path = GetFilePath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"Deleted save slot {slot}");
        }
    }

    /// <summary>
    /// Returns brief info for displaying in the load menu. Null if slot is empty.
    /// </summary>
    public static SaveData PeekSlot(int slot)
    {
        return Load(slot); // Same as load, just used for display
    }
}
