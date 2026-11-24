using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LabeledToggleSwitch : MonoBehaviour
{
    public enum ToggleChannel
    {
        Music,
        Sfx,
        Notifications
    }

    [Header("Label / Channel")]
    [SerializeField] private ToggleChannel channel = ToggleChannel.Music;
    [SerializeField] private string customSaveKey = "";

    [Header("Optional Target Audio Source")]
    [SerializeField] private AudioSource targetAudioSource;

    [Header("Button & Icon Visual Targets")]
    [SerializeField] private SpriteRenderer buttonSpriteRenderer;
    [SerializeField] private Image buttonImage;
    [SerializeField] private SpriteRenderer iconSpriteRenderer;
    [SerializeField] private Image iconImage;

    [Header("On / Off Sprites")]
    [SerializeField] private Sprite buttonOnSprite;
    [SerializeField] private Sprite buttonOffSprite;
    [SerializeField] private Sprite iconOnSprite;
    [SerializeField] private Sprite iconOffSprite;

    [Header("Click Pop Animation")]
    [SerializeField] private bool animateOnToggle = true;
    [SerializeField] private float punchScale = 1.14f;
    [SerializeField] private float animDuration = 0.14f;

    public bool IsOn { get; private set; } = true;

    private Vector3 baseScale;
    private Coroutine animRoutine;

    private string SaveKey
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(customSaveKey))
            {
                return customSaveKey.Trim();
            }
            return $"Setting_{channel}_On";
        }
    }

    void Awake()
    {
        baseScale = transform.localScale;
        IsOn = PlayerPrefs.GetInt(SaveKey, 1) == 1;
        ApplyState(playAnim: false);
    }

    // General command (can be called directly or from channel-specific wrappers)
    public void Toggle()
    {
        IsOn = !IsOn;
        PlayerPrefs.SetInt(SaveKey, IsOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplyState(playAnim: true);
    }

    // Separate named commands for Music, SFX, and Notifications
    public void ToggleMusic()
    {
        channel = ToggleChannel.Music;
        Toggle();
    }

    public void ToggleSfx()
    {
        channel = ToggleChannel.Sfx;
        Toggle();
    }

    public void ToggleNotifications()
    {
        channel = ToggleChannel.Notifications;
        Toggle();
    }

    private void ApplyState(bool playAnim)
    {
        ApplyAudioMute();
        ApplySprites();

        if (playAnim && animateOnToggle && gameObject.activeInHierarchy)
        {
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(PunchScaleRoutine());
        }
    }

    private void ApplyAudioMute()
    {
        if (channel == ToggleChannel.Music)
        {
            AudioSource bgm = targetAudioSource;
            if (bgm == null)
            {
                MainmenuBgmController controller = FindFirstObjectByType<MainmenuBgmController>();
                if (controller != null) bgm = controller.GetComponent<AudioSource>();
            }
            if (bgm != null) bgm.mute = !IsOn;
        }
        else if (channel == ToggleChannel.Sfx)
        {
            AudioSource sfx = targetAudioSource;
            if (sfx == null && SfxPlayer.Instance != null)
            {
                sfx = SfxPlayer.Instance.GetComponent<AudioSource>();
            }
            if (sfx != null) sfx.mute = !IsOn;
        }
    }

    private void ApplySprites()
    {
        Sprite targetButtonSprite = IsOn ? buttonOnSprite : buttonOffSprite;
        if (targetButtonSprite != null)
        {
            if (buttonSpriteRenderer != null) buttonSpriteRenderer.sprite = targetButtonSprite;
            if (buttonImage != null) buttonImage.sprite = targetButtonSprite;
        }

        Sprite targetIconSprite = IsOn ? iconOnSprite : iconOffSprite;
        if (targetIconSprite != null)
        {
            if (iconSpriteRenderer != null) iconSpriteRenderer.sprite = targetIconSprite;
            if (iconImage != null) iconImage.sprite = targetIconSprite;
        }
    }

    private IEnumerator PunchScaleRoutine()
    {
        float half = animDuration * 0.5f;
        Vector3 peakScale = baseScale * punchScale;

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            transform.localScale = Vector3.Lerp(baseScale, peakScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            transform.localScale = Vector3.Lerp(peakScale, baseScale, t);
            yield return null;
        }

        transform.localScale = baseScale;
        animRoutine = null;
    }
}