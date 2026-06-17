using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// MenuSFX plays hover and click sounds on ALL UI buttons game-wide.
/// 
/// SETUP:
/// 1. Add this component to the MainMenuManager GameObject in your MainMenu scene
/// 2. Assign your AudioClips to "Hover Clip" and "Click Clip" in the Inspector
/// 3. That's it — every button created by UIManager, MainMenuManager, and DevPanel
///    will automatically play these sounds.
/// 
/// This component uses DontDestroyOnLoad so it persists across all scenes.
/// </summary>
public class MenuSFX : MonoBehaviour
{
    public static MenuSFX Instance { get; private set; }

    [Header("Assign your SFX clips here")]
    [Tooltip("Played when mouse hovers over any menu button")]
    public AudioClip hoverClip;

    [Tooltip("Played when any menu button is clicked")]
    public AudioClip clickClip;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float hoverVolume = 0.5f;
    [Range(0f, 1f)]
    public float clickVolume = 0.7f;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Create a dedicated AudioSource for UI sounds
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
        else if (Instance != this)
        {
            // Transfer clips if the existing instance has none but this one does
            if (Instance.hoverClip == null && hoverClip != null)
                Instance.hoverClip = hoverClip;
            if (Instance.clickClip == null && clickClip != null)
                Instance.clickClip = clickClip;

            Destroy(this);
        }
    }

    public void PlayHover()
    {
        if (audioSource != null && hoverClip != null)
        {
            audioSource.PlayOneShot(hoverClip, hoverVolume);
        }
    }

    public void PlayClick()
    {
        if (audioSource != null && clickClip != null)
        {
            audioSource.PlayOneShot(clickClip, clickVolume);
        }
    }

    /// <summary>
    /// Call this on any Button to wire up hover + click SFX.
    /// Works even if no clips are assigned yet (will be silent until assigned).
    /// </summary>
    public static void WireButton(Button button)
    {
        if (button == null) return;

        // Click SFX via onClick
        button.onClick.AddListener(() =>
        {
            if (Instance != null) Instance.PlayClick();
        });

        // Hover SFX via EventTrigger
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry hoverEntry = new EventTrigger.Entry();
        hoverEntry.eventID = EventTriggerType.PointerEnter;
        hoverEntry.callback.AddListener((data) =>
        {
            if (Instance != null) Instance.PlayHover();
        });
        trigger.triggers.Add(hoverEntry);
    }
}
