using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClickSfxTrigger : MonoBehaviour, IPointerClickHandler
{
    [Header("Click Sound Settings")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private float minPitch = 0.96f;
    [SerializeField] private float maxPitch = 1.04f;

    [Header("Optional Gameplay Event")]
    [SerializeField] private bool triggerOnMouseDownForWorldObjects = true;

    private Button uiButton;

    void Awake()
    {
        uiButton = GetComponent<Button>();
        if (uiButton != null)
        {
            uiButton.onClick.AddListener(PlaySfx);
        }
    }

    void OnDestroy()
    {
        if (uiButton != null)
        {
            uiButton.onClick.RemoveListener(PlaySfx);
        }
    }

    // Handles UI / EventSystem pointer clicks
    public void OnPointerClick(PointerEventData eventData)
    {
        // UI Buttons already fire via onClick, so skip double-playing on Button components
        if (uiButton == null)
        {
            PlaySfx();
        }
    }

    // Handles non-UI world objects with a Collider when clicked
    void OnMouseDown()
    {
        if (!triggerOnMouseDownForWorldObjects || uiButton != null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        PlaySfx();
    }

    public void PlaySfx()
    {
        if (SfxPlayer.Instance == null)
        {
            Debug.LogWarning($"[ClickSfxTrigger] No SfxPlayer found in scene for '{gameObject.name}'.", this);
            return;
        }

        if (randomizePitch)
        {
            SfxPlayer.Instance.PlayClick(clickClip, volume, minPitch, maxPitch);
        }
        else
        {
            SfxPlayer.Instance.PlayClick(clickClip, volume, 1f, 1f);
        }
    }
}