using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonPngClickEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Single Clicked PNG (Sprite Swap)")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;
    [SerializeField] private float clickFlashDuration = 0.12f;

    [Header("Optional Multi-PNG Frame Sequence")]
    [Tooltip("Leave empty to use just Normal Sprite + Pressed Sprite above.")]
    [SerializeField] private Sprite[] clickFrameSprites;
    [SerializeField] private float frameInterval = 0.05f;

    [Header("World 2D Object Support")]
    [SerializeField] private bool supportWorldMouseDown = true;

    private Image uiImage;
    private SpriteRenderer worldSpriteRenderer;
    private Button uiButton;
    private Coroutine activeRoutine;

    void Awake()
    {
        uiImage = GetComponent<Image>();
        worldSpriteRenderer = GetComponent<SpriteRenderer>();
        uiButton = GetComponent<Button>();

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
        // UI Buttons already invoke TriggerClickPng via onClick
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
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        if (clickFrameSprites != null && clickFrameSprites.Length > 0)
        {
            activeRoutine = StartCoroutine(PlayFrameSequence());
        }
        else if (pressedSprite != null)
        {
            activeRoutine = StartCoroutine(FlashPressedSprite());
        }
    }

    private IEnumerator FlashPressedSprite()
    {
        SetSprite(pressedSprite);
        yield return new WaitForSecondsRealtime(clickFlashDuration);

        if (normalSprite != null)
        {
            SetSprite(normalSprite);
        }

        activeRoutine = null;
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

        activeRoutine = null;
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