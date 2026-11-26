using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    public enum TransitionStyle
    {
        BlackoutFade,
        PulseScaleFade
    }

    [Header("Persistence")]
    [SerializeField] private bool makePersistent = true;

    [Header("Overlay UI")]
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private Image overlayImage;
    [SerializeField] private RectTransform pulseTarget;

    [Header("Transition Settings")]
    [SerializeField] private TransitionStyle transitionStyle = TransitionStyle.BlackoutFade;
    [SerializeField] private Color overlayColor = Color.black;
    [SerializeField] [Min(0.05f)] private float fadeOutDuration = 0.35f;
    [SerializeField] [Min(0f)] private float holdBlackDuration = 0.08f;
    [SerializeField] [Min(0.05f)] private float fadeInDuration = 0.35f;
    [SerializeField] private float pulseStartScale = 1.25f;
    [SerializeField] private bool fallbackDirectLoadIfMissingOverlay = true;

    private bool isTransitioning = false;

    void Awake()
    {
        if (makePersistent)
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance == null)
        {
            Instance = this;
        }

        ApplyOverlayColor();
        SetOverlayAlpha(0f, blocksRaycasts: false);
    }

    public void LoadScene(string sceneName)
    {
        if (isTransitioning || string.IsNullOrWhiteSpace(sceneName)) return;

        if (overlayCanvasGroup == null && fallbackDirectLoadIfMissingOverlay)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        StartCoroutine(TransitionRoutine(() => SceneManager.LoadSceneAsync(sceneName)));
    }

    public void LoadScene(int buildIndex)
    {
        if (isTransitioning || buildIndex < 0) return;

        if (overlayCanvasGroup == null && fallbackDirectLoadIfMissingOverlay)
        {
            SceneManager.LoadScene(buildIndex);
            return;
        }

        StartCoroutine(TransitionRoutine(() => SceneManager.LoadSceneAsync(buildIndex)));
    }

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SetOverlayColor(Color newColor)
    {
        overlayColor = newColor;
        ApplyOverlayColor();
    }

    private void ApplyOverlayColor()
    {
        if (overlayImage != null)
        {
            overlayImage.color = overlayColor;
        }
    }

    private void SetOverlayAlpha(float alpha, bool blocksRaycasts)
    {
        if (overlayCanvasGroup == null) return;

        overlayCanvasGroup.alpha = Mathf.Clamp01(alpha);
        overlayCanvasGroup.blocksRaycasts = blocksRaycasts;
        overlayCanvasGroup.interactable = false;
    }

    private IEnumerator TransitionRoutine(Func<AsyncOperation> startAsyncLoad)
    {
        isTransitioning = true;
        ApplyOverlayColor();

        yield return StartCoroutine(AnimateOut());

        AsyncOperation asyncLoad = startAsyncLoad != null ? startAsyncLoad.Invoke() : null;
        while (asyncLoad != null && !asyncLoad.isDone)
        {
            yield return null;
        }

        if (holdBlackDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdBlackDuration);
        }

        yield return StartCoroutine(AnimateIn());
        isTransitioning = false;
    }

    private IEnumerator AnimateOut()
    {
        if (overlayCanvasGroup == null) yield break;

        overlayCanvasGroup.blocksRaycasts = true;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeOutDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            overlayCanvasGroup.alpha = t;

            if (transitionStyle == TransitionStyle.PulseScaleFade && pulseTarget != null)
            {
                float scale = Mathf.Lerp(pulseStartScale, 1f, t);
                pulseTarget.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        overlayCanvasGroup.alpha = 1f;
        if (pulseTarget != null) pulseTarget.localScale = Vector3.one;
    }

    private IEnumerator AnimateIn()
    {
        if (overlayCanvasGroup == null) yield break;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeInDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            overlayCanvasGroup.alpha = 1f - t;

            if (transitionStyle == TransitionStyle.PulseScaleFade && pulseTarget != null)
            {
                float scale = Mathf.Lerp(1f, pulseStartScale, t);
                pulseTarget.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        overlayCanvasGroup.alpha = 0f;
        overlayCanvasGroup.blocksRaycasts = false;
    }
}