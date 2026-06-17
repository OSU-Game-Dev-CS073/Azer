using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages background music across all scenes.
/// Crossfades between tracks when scenes change.
/// 
/// Works with SceneMusic components — drop a SceneMusic into each scene
/// and assign that scene's AudioClip. MusicManager handles the rest.
/// 
/// If no SceneMusic exists in a scene, music fades to silence.
/// If the same clip is already playing (e.g. multiple scenes share music),
/// it keeps playing without restarting.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    public float crossfadeDuration = 1.5f;

    // Two AudioSources for crossfading
    private AudioSource sourceA;
    private AudioSource sourceB;
    private bool sourceAActive = true;
    private Coroutine crossfadeRoutine;

    // Track what's currently playing
    private AudioClip currentClip;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Create two AudioSources for crossfading
            sourceA = gameObject.AddComponent<AudioSource>();
            sourceA.playOnAwake = false;
            sourceA.loop = true;
            sourceA.spatialBlend = 0f;
            sourceA.volume = 0f;

            sourceB = gameObject.AddComponent<AudioSource>();
            sourceB.playOnAwake = false;
            sourceB.loop = true;
            sourceB.spatialBlend = 0f;
            sourceB.volume = 0f;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Give SceneMusic components a frame to register
        StartCoroutine(CheckSceneMusicDelayed());
    }

    private IEnumerator CheckSceneMusicDelayed()
    {
        yield return null; // wait 1 frame for SceneMusic.Start() to run

        // SceneMusic.Start() calls MusicManager.PlayMusic()
        // If no SceneMusic registered, fade to silence
        SceneMusic sm = FindAnyObjectByType<SceneMusic>();
        if (sm == null)
        {
            FadeToSilence();
        }
    }

    // ============ PUBLIC API ============

    /// <summary>
    /// Play a music clip. If the same clip is already playing, does nothing.
    /// Crossfades from current track to the new one.
    /// Called by SceneMusic components automatically.
    /// </summary>
    public void PlayMusic(AudioClip clip, float volumeOverride = -1f)
    {
        if (clip == null)
        {
            FadeToSilence();
            return;
        }

        // Same clip already playing — don't restart
        if (currentClip == clip)
        {
            return;
        }

        float targetVol = volumeOverride >= 0 ? volumeOverride : musicVolume;
        currentClip = clip;

        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);
        crossfadeRoutine = StartCoroutine(CrossfadeTo(clip, targetVol));
    }

    /// <summary>Fade current music to silence.</summary>
    public void FadeToSilence()
    {
        if (currentClip == null) return;
        currentClip = null;

        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);
        crossfadeRoutine = StartCoroutine(FadeOut());
    }

    /// <summary>Stop music immediately (no fade).</summary>
    public void StopImmediate()
    {
        currentClip = null;
        sourceA.Stop(); sourceA.volume = 0f;
        sourceB.Stop(); sourceB.volume = 0f;
    }

    /// <summary>Set master music volume. Affects currently playing source.</summary>
    public void SetVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        AudioSource active = sourceAActive ? sourceA : sourceB;
        if (active.isPlaying)
            active.volume = musicVolume;
    }

    // ============ CROSSFADE ============

    private IEnumerator CrossfadeTo(AudioClip newClip, float targetVolume)
    {
        AudioSource fadeOut = sourceAActive ? sourceA : sourceB;
        AudioSource fadeIn = sourceAActive ? sourceB : sourceA;
        sourceAActive = !sourceAActive;

        // Start new clip
        fadeIn.clip = newClip;
        fadeIn.volume = 0f;
        fadeIn.Play();

        // Crossfade
        float elapsed = 0f;
        float startVol = fadeOut.volume;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / crossfadeDuration;

            fadeOut.volume = Mathf.Lerp(startVol, 0f, t);
            fadeIn.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        fadeOut.Stop();
        fadeOut.volume = 0f;
        fadeIn.volume = targetVolume;
        crossfadeRoutine = null;
    }

    private IEnumerator FadeOut()
    {
        AudioSource active = sourceAActive ? sourceA : sourceB;
        float startVol = active.volume;
        float elapsed = 0f;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            active.volume = Mathf.Lerp(startVol, 0f, elapsed / crossfadeDuration);
            yield return null;
        }

        active.Stop();
        active.volume = 0f;
        crossfadeRoutine = null;
    }
}
