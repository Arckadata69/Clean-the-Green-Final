using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CatalogEntry
{
    public string itemNumber;
    public string trashName;
    public Sprite trashIcon;
    public Sprite silhouetteIcon;
    public bool isDiscovered = true;
    [TextArea] public string details;
    public string valueAndPoints;
    public string location;

    [Header("Back Side (If Left Unnoticed)")]
    public string warningHeader = "If left unnoticed";
    public Sprite hazardIcon;
    public string consequence;
    public string properBin;
    public int pickedUpCount;
}

[System.Serializable]
public class CollectibleEntry
{
    public string itemNumber;
    public string collectibleName;
    public Sprite collectibleIcon;
    public Sprite silhouetteIcon;
    public bool isDiscovered = true;
    [TextArea] public string details;
    public string valueAndPoints;
    public string location;
}

[System.Serializable]
public class ShopEntry
{
    public string itemNumber;
    public string itemName;
    public Sprite itemIcon;
    public Sprite silhouetteIcon;
    public bool isDiscovered = true;
    [TextArea] public string details;
    public string valueAndPoints;
    public string location;
    public int price;
}

public class CatalogGridController : MonoBehaviour
{
    private enum EntryCategory
    {
        Trash,
        Collectible,
        Shop
    }

    private struct SlotBinding
    {
        public CatalogCardSlot slot;
        public EntryCategory category;
        public int index;
    }

    [Header("References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private CatalogDetailWindow detailWindow;

    [Header("Trash")]
    [SerializeField] private List<CatalogEntry> entries = new List<CatalogEntry>();

    [Header("Collectibles (Card Back Only)")]
    [SerializeField] private List<CollectibleEntry> collectibles = new List<CollectibleEntry>();

    [Header("Shop")]
    [SerializeField] private List<ShopEntry> shop = new List<ShopEntry>();

    private readonly List<CatalogCardSlot> _allSlots = new List<CatalogCardSlot>();
    private readonly Dictionary<CatalogCardSlot, SlotBinding> _slotBindings = new Dictionary<CatalogCardSlot, SlotBinding>();

    private void Awake()
    {
        BindAllSlots();
    }

    private void OnDestroy()
    {
        foreach (CatalogCardSlot slot in _allSlots)
        {
            if (slot != null)
                slot.OnSlotClicked -= HandleSlotClicked;
        }
    }

    private void BindAllSlots()
    {
        _allSlots.Clear();
        _slotBindings.Clear();
        if (contentParent == null) return;

        contentParent.GetComponentsInChildren(true, _allSlots);

        int trashCount = entries.Count;
        int collectibleCount = collectibles.Count;
        int shopCount = shop.Count;
        int totalVisible = trashCount + collectibleCount + shopCount;

        for (int i = 0; i < _allSlots.Count; i++)
        {
            CatalogCardSlot slot = _allSlots[i];
            if (slot == null) continue;

            slot.OnSlotClicked -= HandleSlotClicked;

            if (i < trashCount)
            {
                CatalogEntry entry = entries[i];
                string numberLabel = string.IsNullOrEmpty(entry.itemNumber)
                    ? $"No. {i + 1:00}"
                    : entry.itemNumber;

                slot.Setup(
                    null,
                    numberLabel,
                    entry.trashName,
                    entry.trashIcon,
                    entry.silhouetteIcon,
                    entry.isDiscovered,
                    i
                );

                _slotBindings[slot] = new SlotBinding
                {
                    slot = slot,
                    category = EntryCategory.Trash,
                    index = i
                };

                slot.gameObject.SetActive(true);
                slot.OnSlotClicked += HandleSlotClicked;
            }
            else if (i < trashCount + collectibleCount)
            {
                int collectibleIndex = i - trashCount;
                CollectibleEntry item = collectibles[collectibleIndex];
                string numberLabel = string.IsNullOrEmpty(item.itemNumber)
                    ? $"R. {collectibleIndex + 1:00}"
                    : item.itemNumber;

                slot.Setup(
                    null,
                    numberLabel,
                    item.collectibleName,
                    item.collectibleIcon,
                    item.silhouetteIcon,
                    item.isDiscovered,
                    collectibleIndex
                );

                _slotBindings[slot] = new SlotBinding
                {
                    slot = slot,
                    category = EntryCategory.Collectible,
                    index = collectibleIndex
                };

                slot.gameObject.SetActive(true);
                slot.OnSlotClicked += HandleSlotClicked;
            }
            else if (i < totalVisible)
            {
                int shopIndex = i - trashCount - collectibleCount;
                ShopEntry item = shop[shopIndex];
                string numberLabel = string.IsNullOrEmpty(item.itemNumber)
                    ? $"S. {shopIndex + 1:00}"
                    : item.itemNumber;

                slot.Setup(
                    null,
                    numberLabel,
                    item.itemName,
                    item.itemIcon,
                    item.silhouetteIcon,
                    item.isDiscovered,
                    shopIndex
                );

                _slotBindings[slot] = new SlotBinding
                {
                    slot = slot,
                    category = EntryCategory.Shop,
                    index = shopIndex
                };

                slot.gameObject.SetActive(true);
                slot.OnSlotClicked += HandleSlotClicked;
            }
            else
            {
                slot.gameObject.SetActive(false);
            }
        }
    }

    private void HandleSlotClicked(CatalogCardSlot clickedSlot)
    {
        if (detailWindow == null || clickedSlot == null) return;
        if (!_slotBindings.TryGetValue(clickedSlot, out SlotBinding binding)) return;

        switch (binding.category)
        {
            case EntryCategory.Trash:
                if (binding.index >= 0 && binding.index < entries.Count)
                {
                    CatalogEntry entry = entries[binding.index];
                    detailWindow.OpenTrashEntry(
                        entry.isDiscovered,
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
                break;

            case EntryCategory.Collectible:
                if (binding.index >= 0 && binding.index < collectibles.Count)
                {
                    CollectibleEntry item = collectibles[binding.index];
                    detailWindow.OpenTrashEntry(
                        item.isDiscovered,
                        item.collectibleName,
                        item.collectibleIcon,
                        item.silhouetteIcon,
                        item.details,
                        item.valueAndPoints,
                        item.location,
                        string.Empty,
                        null,
                        string.Empty,
                        string.Empty,
                        0
                    );
                    detailWindow.FlipToBack();
                }
                break;

            case EntryCategory.Shop:
                if (binding.index >= 0 && binding.index < shop.Count)
                {
                    ShopEntry item = shop[binding.index];
                    string shopValue = string.IsNullOrEmpty(item.valueAndPoints)
                        ? $"Price: {item.price}"
                        : $"{item.valueAndPoints} (Price: {item.price})";

                    detailWindow.OpenTrashEntry(
                        item.isDiscovered,
                        item.itemName,
                        item.itemIcon,
                        item.silhouetteIcon,
                        item.details,
                        shopValue,
                        item.location,
                        string.Empty,
                        null,
                        string.Empty,
                        string.Empty,
                        0
                    );
                }
                break;
        }
    }
}