using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MapSlider : MonoBehaviour
{
    [Serializable]
    public class MapEntry
    {
        public string locationName = "Forest";
        public Transform mapTransform;
        public BGMController.BgmTrack bgmTrack = BGMController.BgmTrack.Forest;

        [Header("Lock & Currency Unlock")]
        public bool unlockedByDefault = true;
        public string saveKey = "";
        [Min(0)] public int unlockCost = 100;
        public UnityEvent onMapUnlocked;
    }

    [Header("Maps (Order: Left -> Right)")]
    [SerializeField] private List<MapEntry> maps = new List<MapEntry>
    {
        new MapEntry { locationName = "Forest", bgmTrack = BGMController.BgmTrack.Forest, unlockedByDefault = true, saveKey = "MapUnlocked_Forest", unlockCost = 0 },
        new MapEntry { locationName = "Beach", bgmTrack = BGMController.BgmTrack.Beach, unlockedByDefault = false, saveKey = "MapUnlocked_Beach", unlockCost = 100 },
        new MapEntry { locationName = "City Park", bgmTrack = BGMController.BgmTrack.City, unlockedByDefault = false, saveKey = "MapUnlocked_CityPark", unlockCost = 200 }
    };

    [Header("Scenery Label & Slide Layout")]
    [Tooltip("Drag the [TEMPLATE] TMP_Text inside Window > Scenery here.")]
    [SerializeField] private TMP_Text sceneryTitleLabel;
    [SerializeField] private float mapSpacingX = 12f;
    [SerializeField] private Vector3 centerLocalPosition = Vector3.zero;
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private bool loopAround = true;
    [SerializeField] private bool playBgmOnStart = true;

    [Header("Map Locked UI / Object")]
    [Tooltip("Drag your Map Locked GameObject here.")]
    [SerializeField] private GameObject mapLockedRoot;
    [Tooltip("Optional: Assign your prepared Unlock Button here (or hook UnlockCurrentMap() in its OnClick).")]
    [SerializeField] private Button unlockButton;
    [Tooltip("Optional: TMP_Text showing the unlock cost (e.g., '100').")]
    [SerializeField] private TMP_Text unlockCostLabel;
    [Tooltip("Optional: TMP_Text showing current player currency.")]
    [SerializeField] private TMP_Text currencyLabel;
    [SerializeField] private string currencySaveKey = "PlayerCurrency";
    [SerializeField] private int startingCurrency = 250;

    public int CurrentIndex { get; private set; } = 0;
    public int CurrentCurrency
    {
        get => PlayerPrefs.GetInt(currencySaveKey, startingCurrency);
        private set
        {
            PlayerPrefs.SetInt(currencySaveKey, Mathf.Max(0, value));
            PlayerPrefs.Save();
            RefreshCurrencyUI();
        }
    }

    private Coroutine slideRoutine;

    void Awake()
    {
        if (!PlayerPrefs.HasKey(currencySaveKey))
        {
            PlayerPrefs.SetInt(currencySaveKey, startingCurrency);
        }

        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveListener(UnlockCurrentMap);
            unlockButton.onClick.AddListener(UnlockCurrentMap);
        }
    }

    void Start()
    {
        if (maps == null || maps.Count == 0) return;

        // Auto-detect horizontal spacing from Map1 and Map2 if they are already spaced in Scene view
        if (maps.Count >= 2 && maps[0].mapTransform != null && maps[1].mapTransform != null)
        {
            float detectedSpacing = Mathf.Abs(maps[1].mapTransform.localPosition.x - maps[0].mapTransform.localPosition.x);
            if (detectedSpacing > 0.01f)
            {
                mapSpacingX = detectedSpacing;
            }
            centerLocalPosition = maps[0].mapTransform.localPosition;
        }

        ApplyImmediate(CurrentIndex, playBgmOnStart);
    }

    // Hook to Right button OnClick
    public void NextMap()
    {
        if (maps == null || maps.Count == 0) return;

        int next = CurrentIndex + 1;
        if (next >= maps.Count)
        {
            if (!loopAround) return;
            next = 0;
        }

        GoToMap(next);
    }

    // Hook to Left button OnClick
    public void PreviousMap()
    {
        if (maps == null || maps.Count == 0) return;

        int prev = CurrentIndex - 1;
        if (prev < 0)
        {
            if (!loopAround) return;
            prev = maps.Count - 1;
        }

        GoToMap(prev);
    }

    public void GoToMap(int targetIndex)
    {
        if (maps == null || targetIndex < 0 || targetIndex >= maps.Count) return;

        CurrentIndex = targetIndex;
        RefreshMapState(CurrentIndex, playBgm: true);

        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
        }

        if (gameObject.activeInHierarchy)
        {
            slideRoutine = StartCoroutine(SlideToIndexRoutine(CurrentIndex));
        }
        else
        {
            ApplyImmediate(CurrentIndex, playBgm: false);
        }
    }

    public bool IsMapUnlocked(int index)
    {
        if (maps == null || index < 0 || index >= maps.Count) return true;
        MapEntry entry = maps[index];

        if (string.IsNullOrWhiteSpace(entry.saveKey))
        {
            return entry.unlockedByDefault;
        }

        int defaultInt = entry.unlockedByDefault ? 1 : 0;
        return PlayerPrefs.GetInt(entry.saveKey, defaultInt) == 1;
    }

    // Hook to your prepared Unlock button OnClick (or assign via Unlock Button slot)
    public void UnlockCurrentMap()
    {
        if (maps == null || CurrentIndex < 0 || CurrentIndex >= maps.Count) return;

        MapEntry entry = maps[CurrentIndex];
        if (IsMapUnlocked(CurrentIndex)) return;

        if (CurrentCurrency < entry.unlockCost)
        {
            Debug.Log($"Not enough currency to unlock {entry.locationName}. Need {entry.unlockCost}, have {CurrentCurrency}.");
            return;
        }

        CurrentCurrency -= entry.unlockCost;

        if (!string.IsNullOrWhiteSpace(entry.saveKey))
        {
            PlayerPrefs.SetInt(entry.saveKey, 1);
            PlayerPrefs.Save();
        }
        else
        {
            entry.unlockedByDefault = true;
        }

        entry.onMapUnlocked?.Invoke();
        RefreshMapState(CurrentIndex, playBgm: true);
    }

    public void AddCurrency(int amount)
    {
        if (amount <= 0) return;
        CurrentCurrency += amount;
    }

    private void ApplyImmediate(int index, bool playBgm)
    {
        for (int i = 0; i < maps.Count; i++)
        {
            Transform map = maps[i].mapTransform;
            if (map == null) continue;

            float offsetX = (i - index) * mapSpacingX;
            map.localPosition = new Vector3(centerLocalPosition.x + offsetX, centerLocalPosition.y, centerLocalPosition.z);
        }

        RefreshMapState(index, playBgm);
    }

    private IEnumerator SlideToIndexRoutine(int targetIndex)
    {
        Vector3[] startPositions = new Vector3[maps.Count];
        Vector3[] endPositions = new Vector3[maps.Count];

        for (int i = 0; i < maps.Count; i++)
        {
            Transform map = maps[i].mapTransform;
            if (map == null) continue;

            startPositions[i] = map.localPosition;
            float offsetX = (i - targetIndex) * mapSpacingX;
            endPositions[i] = new Vector3(centerLocalPosition.x + offsetX, centerLocalPosition.y, centerLocalPosition.z);
        }

        float duration = Mathf.Max(0.01f, slideDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].mapTransform != null)
                {
                    maps[i].mapTransform.localPosition = Vector3.Lerp(startPositions[i], endPositions[i], t);
                }
            }

            yield return null;
        }

        for (int i = 0; i < maps.Count; i++)
        {
            if (maps[i].mapTransform != null)
            {
                maps[i].mapTransform.localPosition = endPositions[i];
            }
        }

        slideRoutine = null;
    }

    private void RefreshMapState(int index, bool playBgm)
    {
        MapEntry active = maps[index];
        bool unlocked = IsMapUnlocked(index);

        if (sceneryTitleLabel != null)
        {
            sceneryTitleLabel.text = active.locationName;
        }

        if (mapLockedRoot != null)
        {
            mapLockedRoot.SetActive(!unlocked);
        }

        if (unlockButton != null)
        {
            unlockButton.gameObject.SetActive(!unlocked);
            unlockButton.interactable = !unlocked && CurrentCurrency >= active.unlockCost;
        }

        if (unlockCostLabel != null)
        {
            unlockCostLabel.text = unlocked ? "" : active.unlockCost.ToString();
        }

        RefreshCurrencyUI();

        if (playBgm && BGMController.Instance != null)
        {
            BGMController.Instance.PlayTrack(active.bgmTrack);
        }
    }

    private void RefreshCurrencyUI()
    {
        if (currencyLabel != null)
        {
            currencyLabel.text = CurrentCurrency.ToString();
        }
    }
}