using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IconPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Target Icon Transform")]
    [Tooltip("Drag the child Icon transform here (e.g., Icon under Almanac).")]
    [SerializeField] private Transform iconTarget;

    [Header("Pressed Motion (No Extra PNG Needed)")]
    [SerializeField] private float pressedScaleMultiplier = 0.88f;
    [SerializeField] private Vector3 pressedLocalOffset = new Vector3(0f, -0.03f, 0f);
    [SerializeField] [Min(0.01f)] private float animDuration = 0.08f;
    [SerializeField] private bool supportWorldMouseEvents = true;

    [Header("Pressed Tint (Grey / Black)")]
    [Tooltip("Enable to tint the icon darker/grey when pressed.")]
    [SerializeField] private bool tintOnPress = true;
    [Tooltip("Target tint when pressed (e.g. light grey (0.65, 0.65, 0.65, 1) or dark/black (0.25, 0.25, 0.25, 1)).")]
    [SerializeField] private Color pressedTintColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    private Vector3 iconBaseScale;
    private Vector3 iconBaseLocalPos;
    private Color iconBaseColor = Color.white;
    private bool hasColorTarget = false;
    private SpriteRenderer iconSpriteRenderer;
    private Image iconImage;
    private Coroutine animRoutine;

    void Awake()
    {
        if (iconTarget == null)
        {
            Transform found = transform.Find("Icon");
            if (found != null) iconTarget = found;
        }

        if (iconTarget != null)
        {
            iconBaseScale = iconTarget.localScale;
            iconBaseLocalPos = iconTarget.localPosition;

            iconSpriteRenderer = iconTarget.GetComponent<SpriteRenderer>();
            iconImage = iconTarget.GetComponent<Image>();

            if (iconSpriteRenderer != null)
            {
                iconBaseColor = iconSpriteRenderer.color;
                hasColorTarget = true;
            }
            else if (iconImage != null)
            {
                iconBaseColor = iconImage.color;
                hasColorTarget = true;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StartIconAnimation(pressed: true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StartIconAnimation(pressed: false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartIconAnimation(pressed: false);
    }

    void OnMouseDown()
    {
        if (!supportWorldMouseEvents) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        StartIconAnimation(pressed: true);
    }

    void OnMouseUp()
    {
        if (!supportWorldMouseEvents) return;
        StartIconAnimation(pressed: false);
    }

    public void PulseIconOnce()
    {
        if (iconTarget == null || !gameObject.activeInHierarchy) return;
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(PulseRoutine());
    }

    private void StartIconAnimation(bool pressed)
    {
        if (iconTarget == null || !gameObject.activeInHierarchy) return;

        Vector3 targetScale = pressed ? iconBaseScale * pressedScaleMultiplier : iconBaseScale;
        Vector3 targetPos = pressed ? iconBaseLocalPos + pressedLocalOffset : iconBaseLocalPos;
        Color targetColor = (pressed && tintOnPress) ? pressedTintColor : iconBaseColor;

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateTo(targetScale, targetPos, targetColor));
    }

    private IEnumerator AnimateTo(Vector3 targetScale, Vector3 targetPos, Color targetColor)
    {
        Vector3 startScale = iconTarget.localScale;
        Vector3 startPos = iconTarget.localPosition;
        Color startColor = GetCurrentIconColor();
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, animDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            iconTarget.localScale = Vector3.Lerp(startScale, targetScale, t);
            iconTarget.localPosition = Vector3.Lerp(startPos, targetPos, t);

            if (tintOnPress && hasColorTarget)
            {
                SetIconColor(Color.Lerp(startColor, targetColor, t));
            }

            yield return null;
        }

        iconTarget.localScale = targetScale;
        iconTarget.localPosition = targetPos;
        if (tintOnPress && hasColorTarget)
        {
            SetIconColor(targetColor);
        }

        animRoutine = null;
    }

    private IEnumerator PulseRoutine()
    {
        yield return AnimateTo(
            iconBaseScale * pressedScaleMultiplier,
            iconBaseLocalPos + pressedLocalOffset,
            tintOnPress ? pressedTintColor : iconBaseColor
        );
        yield return AnimateTo(iconBaseScale, iconBaseLocalPos, iconBaseColor);
        animRoutine = null;
    }

    private Color GetCurrentIconColor()
    {
        if (iconSpriteRenderer != null) return iconSpriteRenderer.color;
        if (iconImage != null) return iconImage.color;
        return iconBaseColor;
    }

    private void SetIconColor(Color color)
    {
        if (iconSpriteRenderer != null)
        {
            iconSpriteRenderer.color = color;
        }
        else if (iconImage != null)
        {
            iconImage.color = color;
        }
    }
}