using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CatalogEntry
{
    public string trashNumber;
    public string trashName;
    public Sprite trashIcon;
    public Sprite silhouetteIcon;
    public bool unlockedByDefault;

    [TextArea] public string details;
    public string valueAndPoints;
    public string location;

    [Header("Back Side (If Left Unnoticed)")]
    public string warningHeader = "If left unnoticed";
    public Sprite hazardIcon;
    public string consequence;
    public string properBin;
    public int pickedUpCount;

    public bool IsDiscovered => unlockedByDefault || pickedUpCount > 0;
}

public class CatalogGridController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private CatalogDetailWindow detailWindow;

    [Header("Entries (Almanac or Shop)")]
    [SerializeField] private List<CatalogEntry> entries = new List<CatalogEntry>();

    private readonly List<CatalogCardSlot> _slots = new List<CatalogCardSlot>();

    private void Awake()
    {
        RefreshGrid();
    }

    private void OnDestroy()
    {
        foreach (CatalogCardSlot slot in _slots)
        {
            if (slot != null)
                slot.OnSlotClicked -= HandleSlotClicked;
        }
    }

    public void RefreshGrid()
    {
        foreach (CatalogCardSlot slot in _slots)
        {
            if (slot != null)
                slot.OnSlotClicked -= HandleSlotClicked;
        }
        _slots.Clear();

        if (contentParent != null)
            contentParent.GetComponentsInChildren(true, _slots);

        for (int i = 0; i < _slots.Count; i++)
        {
            CatalogCardSlot slot = _slots[i];
            int capturedIndex = i;

            if (i < entries.Count)
            {
                CatalogEntry entry = entries[i];
                string numberLabel = string.IsNullOrEmpty(entry.trashNumber)
                    ? $"#{i + 1:00}"
                    : entry.trashNumber;

                slot.Setup(
                    null,
                    numberLabel,
                    entry.trashName,
                    entry.trashIcon,
                    entry.silhouetteIcon,
                    entry.IsDiscovered,
                    capturedIndex
                );
                slot.gameObject.SetActive(true);
            }
            else
            {
                slot.Setup(null, $"#{i + 1:00}", "???", null, null, false, capturedIndex);
            }

            slot.OnSlotClicked += HandleSlotClicked;
        }
    }

    private void HandleSlotClicked(CatalogCardSlot clickedSlot)
    {
        if (detailWindow == null) return;

        int index = clickedSlot.SlotIndex;
        if (index >= 0 && index < entries.Count)
        {
            CatalogEntry entry = entries[index];
            detailWindow.OpenTrashEntry(
                entry.IsDiscovered,
                entry.trashName,
                entry.trashIcon,
                entry.silhouetteIcon,
                entry.details,
                entry.valueAndPoints,
                entry.location,
                entry.warningHeader,
                entry.hazardIcon,
                entry.consequence,
                entry.properBin,
                entry.pickedUpCount
            );
        }
    }
}