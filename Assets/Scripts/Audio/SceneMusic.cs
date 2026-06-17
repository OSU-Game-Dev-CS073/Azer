using UnityEngine;

/// <summary>
/// Drop this component on any GameObject in a scene to set that scene's music.
/// When the scene loads, it tells MusicManager to play this clip.
/// 
/// SETUP:
///   1. In each scene, create an empty GameObject (e.g. "SceneMusic")
///   2. Add this component
///   3. Drag your music AudioClip into the "Music Clip" field
///   4. Optionally adjust volume (0-1)
///   5. Done — MusicManager handles crossfading automatically
/// 
/// NOTES:
///   - If two scenes use the SAME clip, music continues without restarting
///   - If a scene has NO SceneMusic, music fades to silence
///   - Only one SceneMusic should exist per scene
///   - The AudioClip should be set to "Load In Background" for large files
/// </summary>
public class SceneMusic : MonoBehaviour
{
    [Header("Scene Music")]
    [Tooltip("The music track for this scene. Leave empty for silence.")]
    public AudioClip musicClip;

    [Tooltip("Volume for this track (0-1). Uses MusicManager default if -1.")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    void Start()
    {
        if (MusicManager.Instance != null && musicClip != null)
        {
            MusicManager.Instance.PlayMusic(musicClip, volume);
        }
    }
}
