using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonPngClickEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public enum PulseMode
    {
        PunchOutAndBack,
        SquashDownAndPop
    }

    [Header("Single Clicked PNG (Sprite Swap)")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;
    [SerializeField] private float clickFlashDuration = 0.12f;

    [Header("Optional Multi-PNG Frame Sequence")]
    [Tooltip("Leave empty to use just Normal Sprite + Pressed Sprite above.")]
    [SerializeField] private Sprite[] clickFrameSprites;
    [SerializeField] private float frameInterval = 0.05f;

    [Header("Optional Pulse Animation")]
    [SerializeField] private bool enablePulseAnimation = true;
    [Tooltip("Leave empty to pulse this GameObject's Transform.")]
    [SerializeField] private Transform pulseTarget;
    [SerializeField] private PulseMode pulseMode = PulseMode.PunchOutAndBack;
    [SerializeField] private float pulseScaleMultiplier = 1.15f;
    [SerializeField] [Min(0.02f)] private float pulseDuration = 0.14f;

    [Header("World 2D Object Support")]
    [SerializeField] private bool supportWorldMouseDown = true;

    private Image uiImage;
    private SpriteRenderer worldSpriteRenderer;
    private Button uiButton;
    private Coroutine spriteRoutine;
    private Coroutine pulseRoutine;
    private Vector3 baseScale;

    void Awake()
    {
        uiImage = GetComponent<Image>();
        worldSpriteRenderer = GetComponent<SpriteRenderer>();
        uiButton = GetComponent<Button>();

        if (pulseTarget == null)
        {
            pulseTarget = transform;
        }
        baseScale = pulseTarget.localScale;

        Sprite current = GetCurrentSprite();
        if (normalSprite == null && current != null)
        {
            normalSprite = current;
        }

        if (uiButton != null)
        {
            uiButton.onClick.AddListener(TriggerClickPng);
        }
    }

    void OnDestroy()
    {
        if (uiButton != null)
        {
            uiButton.onClick.RemoveListener(TriggerClickPng);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (clickFrameSprites != null && clickFrameSprites.Length > 0) return;
        if (pressedSprite != null)
        {
            SetSprite(pressedSprite);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (clickFrameSprites != null && clickFrameSprites.Length > 0) return;
        if (normalSprite != null)
        {
            SetSprite(normalSprite);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (uiButton == null)
        {
            TriggerClickPng();
        }
    }

    void OnMouseDown()
    {
        if (!supportWorldMouseDown || uiButton != null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        TriggerClickPng();
    }

    public void TriggerClickPng()
    {
        if (!gameObject.activeInHierarchy) return;

        if (spriteRoutine != null)
        {
            StopCoroutine(spriteRoutine);
        }

        if (clickFrameSprites != null && clickFrameSprites.Length > 0)
        {
            spriteRoutine = StartCoroutine(PlayFrameSequence());
        }
        else if (pressedSprite != null)
        {
            spriteRoutine = StartCoroutine(FlashPressedSprite());
        }

        if (enablePulseAnimation)
        {
            PlayPulseAnimation();
        }
    }

    public void PlayPulseAnimation()
    {
        if (!gameObject.activeInHierarchy || pulseTarget == null) return;

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
        }

        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator FlashPressedSprite()
    {
        SetSprite(pressedSprite);
        yield return new WaitForSecondsRealtime(clickFlashDuration);

        if (normalSprite != null)
        {
            SetSprite(normalSprite);
        }

        spriteRoutine = null;
    }

    private IEnumerator PlayFrameSequence()
    {
        for (int i = 0; i < clickFrameSprites.Length; i++)
        {
            if (clickFrameSprites[i] != null)
            {
                SetSprite(clickFrameSprites[i]);
            }
            yield return new WaitForSecondsRealtime(frameInterval);
        }

        if (normalSprite != null)
        {
            SetSprite(normalSprite);
        }

        spriteRoutine = null;
    }

    private IEnumerator PulseRoutine()
    {
        float halfDuration = Mathf.Max(0.01f, pulseDuration * 0.5f);
        float targetFactor = pulseMode == PulseMode.SquashDownAndPop
            ? Mathf.Min(pulseScaleMultiplier, 0.99f)
            : pulseScaleMultiplier;

        Vector3 peakScale = baseScale * targetFactor;
        Vector3 currentStart = pulseTarget.localScale;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            pulseTarget.localScale = Vector3.Lerp(currentStart, peakScale, t);
            yield return null;
        }

        pulseTarget.localScale = peakScale;

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            pulseTarget.localScale = Vector3.Lerp(peakScale, baseScale, t);
            yield return null;
        }

        pulseTarget.localScale = baseScale;
        pulseRoutine = null;
    }

    private Sprite GetCurrentSprite()
    {
        if (uiImage != null) return uiImage.sprite;
        if (worldSpriteRenderer != null) return worldSpriteRenderer.sprite;
        return null;
    }

    private void SetSprite(Sprite sprite)
    {
        if (uiImage != null)
        {
            uiImage.sprite = sprite;
        }
        else if (worldSpriteRenderer != null)
        {
            worldSpriteRenderer.sprite = sprite;
        }
    }
}