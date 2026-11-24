using System.Collections;
using UnityEngine;

public class SimpleToggleButton : MonoBehaviour
{
    [SerializeField] private string saveKey = "Setting_Music_On";
    [SerializeField] private AudioSource audioToMute;

    [SerializeField] private SpriteRenderer buttonRenderer;
    [SerializeField] private SpriteRenderer iconRenderer;

    [SerializeField] private Sprite buttonOn;
    [SerializeField] private Sprite buttonOff;
    [SerializeField] private Sprite iconOn;
    [SerializeField] private Sprite iconOff;

    [SerializeField] private float popScale = 1.14f;
    [SerializeField] private float popTime = 0.14f;

    public bool IsOn { get; private set; } = true;
    private Vector3 startScale;
    private Coroutine popRoutine;

    void Awake()
    {
        startScale = transform.localScale;
        IsOn = PlayerPrefs.GetInt(saveKey, 1) == 1;
        Apply(animate: false);
    }

    public void Toggle()
    {
        IsOn = !IsOn;
        PlayerPrefs.SetInt(saveKey, IsOn ? 1 : 0);
        PlayerPrefs.Save();
        Apply(animate: true);
    }

    private void Apply(bool animate)
    {
        if (audioToMute != null) audioToMute.mute = !IsOn;
        if (buttonRenderer != null) buttonRenderer.sprite = IsOn ? buttonOn : buttonOff;
        if (iconRenderer != null) iconRenderer.sprite = IsOn ? iconOn : iconOff;

        if (animate && gameObject.activeInHierarchy)
        {
            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(PopRoutine());
        }
    }

    private IEnumerator PopRoutine()
    {
        float half = popTime * 0.5f;
        Vector3 big = startScale * popScale;

        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            transform.localScale = Vector3.Lerp(startScale, big, t / half);
            yield return null;
        }
        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            transform.localScale = Vector3.Lerp(big, startScale, t / half);
            yield return null;
        }

        transform.localScale = startScale;
        popRoutine = null;
    }
}