using UnityEngine;

/// <summary>
/// Attach to any health item prefab (world pickup OR UI prefab used in shops/inventory).
/// Defines how much HP this item restores when used.
/// </summary>
public class UsableHealthItem : MonoBehaviour
{
    [Tooltip("Amount of health restored when this item is used.")]
    public int healAmount = 20;

    [Tooltip("Optional name override (if empty, uses gameObject.name).")]
    public string displayName = "";

    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;
    }
}