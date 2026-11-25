using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance { get; private set; }

    private const string SfxSaveKey = "Setting_Sfx_On";

    [Serializable]
    public struct NamedSfx
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float volumeScale;
    }

    [Header("Default & Reusable Clips")]
    [SerializeField] private AudioClip defaultClickClip;
    [SerializeField] [Range(0f, 1f)] private float defaultVolume = 1f;
    [SerializeField] private List<NamedSfx> soundLibrary = new List<NamedSfx>();

    private AudioSource sfxSource;
    private readonly Dictionary<string, NamedSfx> clipLookup = new Dictionary<string, NamedSfx>(StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = GetComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        RebuildLibrary();
        SyncMuteWithSavedSetting();
    }

    private void RebuildLibrary()
    {
        clipLookup.Clear();
        foreach (NamedSfx entry in soundLibrary)
        {
            if (!string.IsNullOrWhiteSpace(entry.key) && entry.clip != null && !clipLookup.ContainsKey(entry.key))
            {
                clipLookup.Add(entry.key.Trim(), entry);
            }
        }
    }

    public bool IsSfxEnabled()
    {
        bool savedOn = PlayerPrefs.GetInt(SfxSaveKey, 1) == 1;
        return savedOn && (sfxSource == null || !sfxSource.mute);
    }

    public void SyncMuteWithSavedSetting()
    {
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) return;
        sfxSource.mute = PlayerPrefs.GetInt(SfxSaveKey, 1) != 1;
    }

    // Change the default clip at runtime or in code when switching contexts
    public void SetDefaultClip(AudioClip newClip)
    {
        defaultClickClip = newClip;
    }

    // Play a specific clip passed directly from any script or event
    public void PlayClip(AudioClip clip, float volumeScale = 1f, float pitchMin = 0.96f, float pitchMax = 1.04f)
    {
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null || !IsSfxEnabled() || clip == null) return;

        sfxSource.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
        sfxSource.PlayOneShot(clip, defaultVolume * Mathf.Clamp01(volumeScale));
    }

    // Play a reusable sound from Sound Library by name (e.g., "PopupOpen", "Success", "TrashPickup")
    public void PlayNamedSfx(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (clipLookup.Count == 0 && soundLibrary.Count > 0) RebuildLibrary();

        if (clipLookup.TryGetValue(key.Trim(), out NamedSfx entry))
        {
            float scale = entry.volumeScale <= 0f ? 1f : entry.volumeScale;
            PlayClip(entry.clip, scale);
        }
    }

    // Keep compatibility with existing button click calls
    public void PlayClick(AudioClip clip = null, float volumeScale = 1f, float pitchMin = 0.96f, float pitchMax = 1.04f)
    {
        AudioClip target = clip != null ? clip : defaultClickClip;
        PlayClip(target, volumeScale, pitchMin, pitchMax);
    }
}