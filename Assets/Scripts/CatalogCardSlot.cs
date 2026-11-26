using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CatalogCardSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text trashNumberText;
    [SerializeField] private TMP_Text trashNameText;
    [SerializeField] private Button button;

    public event Action<CatalogCardSlot> OnSlotClicked;

    public ScriptableObject ItemData { get; private set; }
    public int SlotIndex { get; private set; }
    public bool IsDiscovered { get; private set; }

    private void Reset()
    {
        button = GetComponent<Button>();
        if (iconImage == null)
            iconImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    public void Setup(
        ScriptableObject data,
        string trashNumber,
        string trashName,
        Sprite discoveredSprite,
        Sprite silhouetteSprite,
        bool isDiscovered,
        int index = -1)
    {
        ItemData = data;
        SlotIndex = index;
        IsDiscovered = isDiscovered;

        if (trashNumberText != null)
            trashNumberText.text = trashNumber;

        if (trashNameText != null)
            trashNameText.text = isDiscovered ? trashName : "???";

        Sprite spriteToShow = isDiscovered ? discoveredSprite : silhouetteSprite;

        if (iconImage != null && spriteToShow != null)
        {
            iconImage.sprite = spriteToShow;
            iconImage.preserveAspect = true;
        }
    }

    private void HandleClick()
    {
        OnSlotClicked?.Invoke(this);
    }
}