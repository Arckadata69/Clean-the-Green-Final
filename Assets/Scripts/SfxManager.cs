using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance { get; private set; }

    [Header("Default Fallback SFX")]
    [SerializeField] private AudioClip defaultClickClip;
    [SerializeField] [Range(0f, 1f)] private float defaultVolume = 1f;

    private AudioSource sfxSource;

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
    }

    public void PlayClick(AudioClip clip = null, float volumeScale = 1f, float pitchMin = 0.96f, float pitchMax = 1.04f)
    {
        AudioClip clipToPlay = clip != null ? clip : defaultClickClip;
        if (clipToPlay == null) return;

        sfxSource.pitch = Random.Range(pitchMin, pitchMax);
        sfxSource.PlayOneShot(clipToPlay, defaultVolume * Mathf.Clamp01(volumeScale));
    }
}