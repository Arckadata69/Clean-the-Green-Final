using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMController : MonoBehaviour
{
    public static BGMController Instance { get; private set; }

    private const string MusicSaveKey = "Setting_Music_On";

    public enum BgmTrack
    {
        MainMenu,
        Forest,
        Beach,
        City,
        MiniGame,
        ShopAlmanac
    }

    [Serializable]
    public struct NamedBgm
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float volumeScale;
    }

    [Header("Default & Reusable BGM")]
    [SerializeField] private AudioClip defaultBgmClip;
    [SerializeField] [Range(0f, 1f)] private float defaultVolume = 1f;
    [SerializeField] private List<NamedBgm> bgmLibrary = new List<NamedBgm>();

    [Header("Optional Enum Track Mapping")]
    [SerializeField] private AudioClip mainMenuClip;
    [SerializeField] private AudioClip forestClip;
    [SerializeField] private AudioClip beachClip;
    [SerializeField] private AudioClip cityClip;
    [SerializeField] private AudioClip miniGameClip;
    [SerializeField] private AudioClip shopAlmanacClip;

    [Header("Startup")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private BgmTrack startupTrack = BgmTrack.MainMenu;

    private AudioSource bgmSource;
    private readonly Dictionary<string, NamedBgm> bgmLookup =
        new Dictionary<string, NamedBgm>(StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = GetComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        RebuildLibrary();
        SyncMuteWithSavedSetting();
    }

    void Start()
    {
        if (playOnStart)
        {
            AudioClip startClip = GetTrackClip(startupTrack);
            if (startClip == null) startClip = defaultBgmClip;
            if (startClip != null) PlayClip(startClip);
        }
    }

    private void RebuildLibrary()
    {
        bgmLookup.Clear();
        foreach (NamedBgm entry in bgmLibrary)
        {
            if (!string.IsNullOrWhiteSpace(entry.key) &&
                entry.clip != null &&
                !bgmLookup.ContainsKey(entry.key.Trim()))
            {
                bgmLookup.Add(entry.key.Trim(), entry);
            }
        }
    }

    public bool IsMusicEnabled()
    {
        bool savedOn = PlayerPrefs.GetInt(MusicSaveKey, 1) == 1;
        return savedOn && (bgmSource == null || !bgmSource.mute);
    }

    public void SyncMuteWithSavedSetting()
    {
        if (bgmSource == null) bgmSource = GetComponent<AudioSource>();
        if (bgmSource == null) return;
        bgmSource.mute = PlayerPrefs.GetInt(MusicSaveKey, 1) != 1;
    }

    public void SetDefaultClip(AudioClip newClip)
    {
        defaultBgmClip = newClip;
    }

    public void PlayClip(AudioClip clip, float volumeScale = 1f)
    {
        if (bgmSource == null) bgmSource = GetComponent<AudioSource>();
        if (bgmSource == null) return;

        AudioClip target = clip != null ? clip : defaultBgmClip;
        if (target == null) return;

        float scale = volumeScale <= 0f ? 1f : Mathf.Clamp01(volumeScale);
        bgmSource.volume = defaultVolume * scale;

        if (bgmSource.clip == target && bgmSource.isPlaying) return;

        bgmSource.clip = target;
        bgmSource.Play();
    }

    public void PlayNamedBgm(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (bgmLookup.Count == 0 && bgmLibrary.Count > 0) RebuildLibrary();

        if (bgmLookup.TryGetValue(key.Trim(), out NamedBgm entry))
        {
            PlayClip(entry.clip, entry.volumeScale);
        }
    }

    public void PlayTrack(BgmTrack track)
    {
        AudioClip clip = GetTrackClip(track);
        if (clip != null)
        {
            PlayClip(clip);
        }
    }

    public void StopBgm()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    private AudioClip GetTrackClip(BgmTrack track)
    {
        switch (track)
        {
            case BgmTrack.MainMenu: return mainMenuClip;
            case BgmTrack.Forest: return forestClip;
            case BgmTrack.Beach: return beachClip;
            case BgmTrack.City: return cityClip;
            case BgmTrack.MiniGame: return miniGameClip;
            case BgmTrack.ShopAlmanac: return shopAlmanacClip;
            default: return defaultBgmClip;
        }
    }
}