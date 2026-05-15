using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Attach to Player. Press Q to consume the smallest health item from inventory.
/// </summary>
public class HealthItemUser : MonoBehaviour
{
    [Header("Keybind")]
    public KeyCode useKey = KeyCode.Q;

    [Header("Feedback")]
    public AudioClip useSound;
    public float useSoundVolume = 0.6f;

    private Player player;

    void Start()
    {
        player = GetComponent<Player>();
        if (player == null)
        {
            Debug.LogError("HealthItemUser: No Player component found on this GameObject!");
        }
    }

    void Update()
    {
        if (player == null) return;

        // Respect dialogue, pause, player disabled, etc.
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive) return;
        if (GameManager.Instance != null && GameManager.Instance.isPaused) return;
        if (!player.enabled) return;

        if (Input.GetKeyDown(useKey))
        {
            TryUseHealthItem();
        }
    }

    void TryUseHealthItem()
    {
        if (InventoryController.Instance == null)
        {
            Debug.LogWarning("InventoryController.Instance not found!");
            return;
        }

        // Find all slots that contain a UsableHealthItem
        var candidates = InventoryController.Instance.GetAllSlotsWithComponent<UsableHealthItem>();

        if (candidates.Count == 0)
        {
            Debug.Log("No health items in inventory.");
            return;
        }

        // Pick the one with smallest healAmount (e.g., Small Potion 20 before Big Potion 50)
        var best = candidates.OrderBy(item => item.component.healAmount).First();

        // Use it
        int healAmount = best.component.healAmount;
        player.Heal(healAmount);

        // IMPORTANT: Remove the SPECIFIC item from the SPECIFIC slot, not by name
        // This ensures we only remove the health item we found
        bool removed = InventoryController.Instance.RemoveItemFromSlot(best.slot, 1);
        
        if (!removed)
        {
            Debug.LogError($"Failed to remove health item from slot!");
            return;
        }

        // Play sound if assigned
        if (useSound != null && player.audioSource != null)
            player.PlaySFX(useSound, useSoundVolume, 1f);

        Debug.Log($"Used {best.component.GetDisplayName()}, restored {healAmount} HP.");
    }
}