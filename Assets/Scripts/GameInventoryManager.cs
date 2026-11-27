using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrashBinType
{
    Recycling,
    Biodegradable,
    NonBiodegradable,
    Hazardous
}

public enum TrashRarity
{
    White, // Normal trash
    Gold   // Rare trash (double score)
}

[Serializable]
public class TrashItemDefinition
{
    public string id;
    public string trashName;
    public Sprite icon;
    public TrashBinType correctBin;
    public TrashRarity rarity = TrashRarity.White;
    public int basePoints = 10;
    public int baseCaps = 5;
}

public class GameInventoryManager : MonoBehaviour
{
    public static GameInventoryManager Instance { get; private set; }

    public enum BagTier
    {
        PlasticBag = 0, // 10 capacity
        TrashBag = 1,   // 15 capacity
        Sack = 2        // 25 capacity
    }

    [Header("Bag State")]
    [SerializeField] private BagTier equippedBag = BagTier.PlasticBag;
    private readonly HashSet<BagTier> _unlockedBags = new HashSet<BagTier> { BagTier.PlasticBag };

    [Header("Currency & Totals")]
    [SerializeField] private int totalCaps = 0;
    [SerializeField] private int totalScore = 0;

    private readonly List<TrashItemDefinition> _collectedTrash = new List<TrashItemDefinition>();
    private readonly Dictionary<string, int> _almanacPickupCounts = new Dictionary<string, int>();

    public event Action<int, int, bool> OnBagUpdated; // (currentCount, maxCapacity, isFull)
    public event Action<int> OnCurrencyUpdated;

    public int CurrentBagCapacity => equippedBag switch
    {
        BagTier.PlasticBag => 10,
        BagTier.TrashBag => 15,
        BagTier.Sack => 25,
        _ => 10
    };

    public int CollectedCount => _collectedTrash.Count;
    public bool IsBagFull => _collectedTrash.Count >= CurrentBagCapacity;
    public BagTier EquippedBag => equippedBag;
    public int TotalCaps => totalCaps;
    public int TotalScore => totalScore;
    public IReadOnlyList<TrashItemDefinition> CollectedTrash => _collectedTrash;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool TryCollectTrash(TrashItemDefinition item)
    {
        if (item == null || IsBagFull)
            return false;

        _collectedTrash.Add(item);

        if (!string.IsNullOrEmpty(item.id))
        {
            _almanacPickupCounts.TryGetValue(item.id, out int count);
            _almanacPickupCounts[item.id] = count + 1;
        }

        NotifyBagChanged();
        return true;
    }

    public List<TrashItemDefinition> ConsumeCollectedTrashForMinigame()
    {
        List<TrashItemDefinition> batch = new List<TrashItemDefinition>(_collectedTrash);
        _collectedTrash.Clear();
        NotifyBagChanged();
        return batch;
    }

    public bool IsBagUnlocked(BagTier tier) => _unlockedBags.Contains(tier);

    public bool TryBuyBag(BagTier tier, int cost)
    {
        if (_unlockedBags.Contains(tier)) return true;
        if (totalCaps < cost) return false;

        totalCaps -= cost;
        _unlockedBags.Add(tier);
        OnCurrencyUpdated?.Invoke(totalCaps);
        return true;
    }

    public void EquipBag(BagTier tier)
    {
        if (!_unlockedBags.Contains(tier)) return;
        equippedBag = tier;
        NotifyBagChanged();
    }

    public void AddRewards(int scoreDelta, int capsDelta)
    {
        totalScore = Mathf.Max(0, totalScore + scoreDelta);
        totalCaps = Mathf.Max(0, totalCaps + capsDelta);
        OnCurrencyUpdated?.Invoke(totalCaps);
    }

    public int GetPickupCount(string trashId)
    {
        return !string.IsNullOrEmpty(trashId) && _almanacPickupCounts.TryGetValue(trashId, out int count)
            ? count
            : 0;
    }

    private void NotifyBagChanged()
    {
        OnBagUpdated?.Invoke(CollectedCount, CurrentBagCapacity, IsBagFull);
    }
}