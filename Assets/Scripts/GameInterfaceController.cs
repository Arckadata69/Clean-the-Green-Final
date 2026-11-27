using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class SpriteLocationTheme
{
    public string locationName = "FOREST";
    [Tooltip("Drag your existing Hierarchy object for this map.")]
    public GameObject locationRootObject;
    public Rect spawnArea = new Rect(-2.05f, -2.66f, 4.07f, 4.05f);
    public List<TrashItemDefinition> spawnableTrash = new List<TrashItemDefinition>();

    [Header("Lock & Currency Unlock")]
    public bool unlockedByDefault = true;
    public string saveKey = "";
    [Min(0)] public int unlockCost = 100;
    public UnityEvent onMapUnlocked;
}

public class GameInterfaceController : MonoBehaviour
{
    public enum FillAxis
    {
        Vertical,
        Horizontal
    }

    [Header("Top Bar UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private string almanacOrShopSceneName = "Almanac";
    [SerializeField] private Button quickReturnButton;
    [SerializeField] private MonoBehaviour sfxPlayer;

    [Header("5-Minute Timer (SpriteRenderer)")]
    [SerializeField] private float totalMatchSeconds = 300f;
    [SerializeField] private SpriteRenderer timerFillSprite;
    [SerializeField] private FillAxis fillAxis = FillAxis.Vertical;
    [SerializeField] private bool shrinkFromTop = true;
    [SerializeField] private Transform timerHandle;
    [SerializeField] private Transform timerTrackTop;
    [SerializeField] private Transform timerTrackBottom;
    [SerializeField] private TMP_Text timerText;

    [Header("Sprite Sorting")]
    [SerializeField] private string defaultSortingLayer = "Default";
    [SerializeField] private int timerFillOrder = 5;
    [SerializeField] private int timerHandleOrder = 6;
    [SerializeField] private int spawnedTrashOrder = 10;

    [Header("Location Switcher (Side-by-Side), Lock & Spawn Areas")]
    [SerializeField] private TMP_Text locationLabelText;
    [SerializeField] private Button prevLocationButton;
    [SerializeField] private Button nextLocationButton;
    [Tooltip("Horizontal spacing between side-by-side maps in local X units.")]
    [SerializeField] private float mapSpacingX = 10f;
    [Tooltip("If true, auto-detects mapSpacingX from Map1 and Map2 when already spaced in Scene.")]
    [SerializeField] private bool autoDetectSpacingFromScene = false;
    [SerializeField] private float slideDuration = 0.28f;
    [SerializeField] private GameObject mapLockedRoot;
    [SerializeField] private Button unlockButton;
    [SerializeField] private TMP_Text unlockCostLabel;
    [SerializeField] private TMP_Text currencyLabel;
    [SerializeField] private string currencySaveKey = "PlayerCurrency";
    [SerializeField] private int startingCurrency = 250;
    [SerializeField] private List<SpriteLocationTheme> locations = new List<SpriteLocationTheme>
    {
        new SpriteLocationTheme { locationName = "FOREST", spawnArea = new Rect(-2.05f, -2.66f, 4.07f, 4.05f), unlockedByDefault = true,  saveKey = "MapUnlocked_Forest", unlockCost = 0 },
        new SpriteLocationTheme { locationName = "BEACH",  spawnArea = new Rect(-2.05f, -3.19f, 4f, 3.35f),    unlockedByDefault = false, saveKey = "MapUnlocked_Beach",  unlockCost = 100 },
        new SpriteLocationTheme { locationName = "CITY",   spawnArea = new Rect(-2.05f, -3.19f, 4f, 3.35f),    unlockedByDefault = false, saveKey = "MapUnlocked_City",   unlockCost = 200 }
    };

    [Header("Trash Spawner: Timing & Look")]
    [SerializeField] private SpriteRenderer spawnedTrashPrefab;
    [SerializeField] private float minSpawnInterval = 1f;
    [SerializeField] private float maxSpawnInterval = 8f;
    [SerializeField] private int maxActiveTrashInZone = 12;
    [SerializeField] private float trashSize = 1.5f;

    [Header("Trash Spawner: Variety & Testing")]
    [Range(0f, 1f)]
    [SerializeField] private float mixChance = 0.001f;
    [Min(1f)]
    [SerializeField] private float rareBoost = 1f;

    [Header("Bottom Bar: Bag & Green Bin")]
    [SerializeField] private TMP_Text bagCounterText;
    [SerializeField] private Button greenBinButton;
    [SerializeField] private RectTransform greenBinIcon;
    [SerializeField] private string minigameSceneName = "TrashNSorta";
    [SerializeField] private float pulseSpeed = 5f;
    [SerializeField] private float pulseAmount = 0.15f;

    private readonly List<GameObject> _activeTrashObjects = new List<GameObject>();
    private Vector3 _centerLocalPosition;
    private Coroutine _slideRoutine;
    private Vector3 _timerFillBaseScale = Vector3.one;
    private Vector3 _timerFillBasePos;
    private Vector3 _greenBinBaseScale = Vector3.one;
    private int _currentLocationIndex;
    private float _remainingTime;
    private float _nextSpawnTimer;
    private bool _isPaused;
    private bool _isMatchEnded;
    private bool _pulseGreenBin;

    public int CurrentCurrency
    {
        get
        {
            if (GameInventoryManager.Instance != null)
                return GameInventoryManager.Instance.TotalScore;
            return PlayerPrefs.GetInt(currencySaveKey, startingCurrency);
        }
        private set
        {
            int clamped = Mathf.Max(0, value);
            PlayerPrefs.SetInt(currencySaveKey, clamped);
            PlayerPrefs.Save();
            RefreshCurrencyUI();
        }
    }

    [ContextMenu("Auto-Find Hierarchy Objects")]
    private void AutoFindHierarchyObjects()
    {
        if (timerFillSprite == null)
        {
            GameObject fillObj = GameObject.Find("Icon Timer Fill");
            if (fillObj != null) timerFillSprite = fillObj.GetComponent<SpriteRenderer>();
        }

        if (timerHandle == null)
        {
            GameObject handleObj = GameObject.Find("Icon Timer");
            if (handleObj != null) timerHandle = handleObj.transform;
        }

        if (pausePanel == null)
        {
            GameObject pObj = GameObject.Find("Pause Panel") ?? GameObject.Find("Pause Window");
            if (pObj != null) pausePanel = pObj;
        }

        if (sfxPlayer == null)
        {
            GameObject sfxObj = GameObject.Find("SFX");
            if (sfxObj != null) sfxPlayer = sfxObj.GetComponent<MonoBehaviour>();
        }

        if (mapLockedRoot == null)
        {
            GameObject lockObj = GameObject.Find("Map Locked") ?? GameObject.Find("MapLocked");
            if (lockObj != null) mapLockedRoot = lockObj;
        }

        foreach (SpriteLocationTheme loc in locations)
        {
            if (loc.locationRootObject == null && !string.IsNullOrEmpty(loc.locationName))
            {
                string lower = loc.locationName.ToLower();
                string title = char.ToUpper(lower[0]) + lower.Substring(1);
                GameObject found = GameObject.Find(loc.locationName)
                    ?? GameObject.Find(title)
                    ?? GameObject.Find(title + "1")
                    ?? GameObject.Find("Map1");
                if (found != null)
                    loc.locationRootObject = found;
            }
        }
    }

    private void Awake()
    {
        if (GameInventoryManager.Instance == null && !PlayerPrefs.HasKey(currencySaveKey))
            PlayerPrefs.SetInt(currencySaveKey, startingCurrency);

        if (locations != null && locations.Count > 0 && locations[0].locationRootObject != null)
            _centerLocalPosition = locations[0].locationRootObject.transform.localPosition;

        if (timerFillSprite != null)
        {
            _timerFillBaseScale = timerFillSprite.transform.localScale;
            _timerFillBasePos = timerFillSprite.transform.localPosition;
            ApplySorting(timerFillSprite, timerFillOrder);
        }

        if (timerHandle != null)
        {
            SpriteRenderer handleSr = timerHandle.GetComponent<SpriteRenderer>();
            if (handleSr != null)
                ApplySorting(handleSr, timerHandleOrder);
        }

        if (greenBinIcon != null)
            _greenBinBaseScale = greenBinIcon.localScale;

        if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);
        if (quickReturnButton != null) quickReturnButton.onClick.AddListener(OpenAlmanacOrShop);
        if (prevLocationButton != null) prevLocationButton.onClick.AddListener(PreviousLocation);
        if (nextLocationButton != null) nextLocationButton.onClick.AddListener(NextLocation);
        if (greenBinButton != null) greenBinButton.onClick.AddListener(EnterTrashNSorta);

        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveListener(UnlockCurrentMap);
            unlockButton.onClick.AddListener(UnlockCurrentMap);
        }

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void Start()
    {
        _remainingTime = Mathf.Max(1f, totalMatchSeconds);

        if (autoDetectSpacingFromScene && locations != null && locations.Count >= 2 &&
            locations[0].locationRootObject != null && locations[1].locationRootObject != null)
        {
            float detectedSpacing = Mathf.Abs(
                locations[1].locationRootObject.transform.localPosition.x -
                locations[0].locationRootObject.transform.localPosition.x
            );
            if (detectedSpacing > 0.01f)
                mapSpacingX = detectedSpacing;
        }

        ApplySideBySideLayout(_currentLocationIndex, instant: true);
        UpdateScoreUI();
        RefreshCurrencyUI();
        UpdateTimerVisual(1f);
        ResetSpawnTimer();

        if (GameInventoryManager.Instance != null)
        {
            GameInventoryManager.Instance.OnBagUpdated += HandleBagUpdated;
            GameInventoryManager.Instance.OnCurrencyUpdated += HandleCurrencyUpdated;
            HandleBagUpdated(
                GameInventoryManager.Instance.CollectedCount,
                GameInventoryManager.Instance.CurrentBagCapacity,
                GameInventoryManager.Instance.IsBagFull
            );
        }
    }

    private void OnDestroy()
    {
        if (pauseButton != null) pauseButton.onClick.RemoveListener(TogglePause);
        if (quickReturnButton != null) quickReturnButton.onClick.RemoveListener(OpenAlmanacOrShop);
        if (prevLocationButton != null) prevLocationButton.onClick.RemoveListener(PreviousLocation);
        if (nextLocationButton != null) nextLocationButton.onClick.RemoveListener(NextLocation);
        if (greenBinButton != null) greenBinButton.onClick.RemoveListener(EnterTrashNSorta);

        if (unlockButton != null)
            unlockButton.onClick.RemoveListener(UnlockCurrentMap);

        if (GameInventoryManager.Instance != null)
        {
            GameInventoryManager.Instance.OnBagUpdated -= HandleBagUpdated;
            GameInventoryManager.Instance.OnCurrencyUpdated -= HandleCurrencyUpdated;
        }
    }

    private void Update()
    {
        UpdateGreenBinPulse();

        if (_isPaused || _isMatchEnded)
            return;

        UpdateTimer();
        UpdateTrashSpawner();
    }

    public bool IsMapUnlocked(int index)
    {
        if (locations == null || index < 0 || index >= locations.Count) return true;
        SpriteLocationTheme entry = locations[index];

        if (string.IsNullOrWhiteSpace(entry.saveKey))
            return entry.unlockedByDefault;

        int defaultInt = entry.unlockedByDefault ? 1 : 0;
        return PlayerPrefs.GetInt(entry.saveKey, defaultInt) == 1;
    }

    public void UnlockCurrentMap()
    {
        if (locations == null || _currentLocationIndex < 0 || _currentLocationIndex >= locations.Count) return;

        SpriteLocationTheme entry = locations[_currentLocationIndex];
        if (IsMapUnlocked(_currentLocationIndex)) return;

        if (CurrentCurrency < entry.unlockCost)
        {
            Debug.Log($"Not enough currency to unlock {entry.locationName}. Need {entry.unlockCost}, have {CurrentCurrency}.");
            return;
        }

        if (GameInventoryManager.Instance == null)
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
        RefreshLockState(_currentLocationIndex);
        ResetSpawnTimer();
    }

    private void RefreshLockState(int index)
    {
        if (locations == null || index < 0 || index >= locations.Count) return;

        SpriteLocationTheme active = locations[index];
        bool unlocked = IsMapUnlocked(index);

        if (mapLockedRoot != null)
            mapLockedRoot.SetActive(!unlocked);

        if (unlockButton != null)
        {
            unlockButton.gameObject.SetActive(!unlocked);
            unlockButton.interactable = !unlocked && CurrentCurrency >= active.unlockCost;
        }

        if (unlockCostLabel != null)
            unlockCostLabel.text = unlocked ? "" : active.unlockCost.ToString();

        RefreshCurrencyUI();
    }

    private void RefreshCurrencyUI()
    {
        if (currencyLabel != null)
            currencyLabel.text = CurrentCurrency.ToString();
    }

    private void UpdateTimer()
    {
        _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);
        float progress = _remainingTime / Mathf.Max(1f, totalMatchSeconds);

        UpdateTimerVisual(progress);

        if (timerText != null)
        {
            int mins = Mathf.FloorToInt(_remainingTime / 60f);
            int secs = Mathf.FloorToInt(_remainingTime % 60f);
            timerText.text = $"{mins:00}:{secs:00}";
        }

        if (_remainingTime <= 0f)
        {
            _isMatchEnded = true;
            if (GameInventoryManager.Instance != null && GameInventoryManager.Instance.CollectedCount > 0)
            {
                SceneManager.LoadScene(minigameSceneName);
            }
        }
    }

    private void UpdateTimerVisual(float progress)
    {
        if (timerFillSprite != null)
        {
            Transform fillTf = timerFillSprite.transform;
            float clamped = Mathf.Clamp01(progress);

            if (fillAxis == FillAxis.Vertical)
            {
                fillTf.localScale = new Vector3(_timerFillBaseScale.x, _timerFillBaseScale.y * clamped, _timerFillBaseScale.z);

                if (timerFillSprite.sprite != null)
                {
                    float fullHeightWorld = timerFillSprite.sprite.bounds.size.y * _timerFillBaseScale.y;
                    float deltaHeight = fullHeightWorld * (1f - clamped) * 0.5f;
                    float yOffset = shrinkFromTop ? -deltaHeight : deltaHeight;
                    fillTf.localPosition = _timerFillBasePos + new Vector3(0f, yOffset, 0f);
                }
            }
            else
            {
                fillTf.localScale = new Vector3(_timerFillBaseScale.x * clamped, _timerFillBaseScale.y, _timerFillBaseScale.z);

                if (timerFillSprite.sprite != null)
                {
                    float fullWidthWorld = timerFillSprite.sprite.bounds.size.x * _timerFillBaseScale.x;
                    float deltaWidth = fullWidthWorld * (1f - clamped) * 0.5f;
                    float xOffset = shrinkFromTop ? -deltaWidth : deltaWidth;
                    fillTf.localPosition = _timerFillBasePos + new Vector3(xOffset, 0f, 0f);
                }
            }
        }

        if (timerHandle != null && timerTrackTop != null && timerTrackBottom != null)
        {
            timerHandle.position = Vector3.Lerp(timerTrackBottom.position, timerTrackTop.position, progress);
        }
    }

    private void ResetSpawnTimer()
    {
        float low = Mathf.Max(0.05f, Mathf.Min(minSpawnInterval, maxSpawnInterval));
        float high = Mathf.Max(low, Mathf.Max(minSpawnInterval, maxSpawnInterval));
        _nextSpawnTimer = UnityEngine.Random.Range(low, high);
    }

    private void UpdateTrashSpawner()
    {
        _activeTrashObjects.RemoveAll(go => go == null);

        if (!IsMapUnlocked(_currentLocationIndex))
            return;

        if (GameInventoryManager.Instance != null && GameInventoryManager.Instance.IsBagFull)
            return;

        if (_activeTrashObjects.Count >= maxActiveTrashInZone)
            return;

        _nextSpawnTimer -= Time.deltaTime;
        if (_nextSpawnTimer <= 0f)
        {
            SpawnTrashInArea();
            ResetSpawnTimer();
        }
    }

    private void SpawnTrashInArea()
    {
        if (spawnedTrashPrefab == null || locations == null || locations.Count == 0)
            return;

        SpriteLocationTheme currentLoc = locations[_currentLocationIndex];
        TrashItemDefinition chosenItem = PickTrashItem(_currentLocationIndex, currentLoc);
        if (chosenItem == null)
            return;

        SpriteRenderer trashSr = Instantiate(spawnedTrashPrefab);
        _activeTrashObjects.Add(trashSr.gameObject);

        if (chosenItem.icon != null)
            trashSr.sprite = chosenItem.icon;

        ApplySorting(trashSr, spawnedTrashOrder);
        trashSr.transform.localScale = Vector3.one * trashSize;

        Rect area = currentLoc.spawnArea;
        float randX = UnityEngine.Random.Range(area.xMin, area.xMax);
        float randY = UnityEngine.Random.Range(area.yMin, area.yMax);
        trashSr.transform.position = new Vector3(randX, randY, transform.position.z);

        SpriteTrashTapTarget tapTarget = trashSr.gameObject.GetComponent<SpriteTrashTapTarget>();
        if (tapTarget == null)
            tapTarget = trashSr.gameObject.AddComponent<SpriteTrashTapTarget>();

        if (trashSr.gameObject.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D col = trashSr.gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        tapTarget.Initialize(chosenItem, this);
    }

    private TrashItemDefinition PickTrashItem(int currentIndex, SpriteLocationTheme currentLoc)
    {
        List<TrashItemDefinition> pool = currentLoc.spawnableTrash;

        if (UnityEngine.Random.value < mixChance)
        {
            List<TrashItemDefinition> mixed = new List<TrashItemDefinition>();
            for (int i = 0; i < locations.Count; i++)
            {
                if (i == currentIndex || locations[i].spawnableTrash == null) continue;
                mixed.AddRange(locations[i].spawnableTrash);
            }
            if (mixed.Count > 0)
                pool = mixed;
        }

        if (pool == null || pool.Count == 0)
            return null;

        float totalWeight = 0f;
        float[] weights = new float[pool.Count];

        for (int i = 0; i < pool.Count; i++)
        {
            float w = 1f;
            if (pool[i] != null && pool[i].rarity == TrashRarity.Gold)
                w *= Mathf.Max(1f, rareBoost);

            weights[i] = w;
            totalWeight += w;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return pool[i];
        }

        return pool[pool.Count - 1];
    }

    public void CollectSpriteTrash(TrashItemDefinition item, GameObject trashObject)
    {
        if (GameInventoryManager.Instance == null || item == null)
            return;

        if (GameInventoryManager.Instance.TryCollectTrash(item))
        {
            PlayPickUpSfx();
            _activeTrashObjects.Remove(trashObject);
            Destroy(trashObject);
        }
    }

    private void PlayPickUpSfx()
    {
        if (sfxPlayer == null) return;
        var method = sfxPlayer.GetType().GetMethod("PlayNamedSfx", new[] { typeof(string) });
        method?.Invoke(sfxPlayer, new object[] { "Pick-up" });
    }

    private void ApplySorting(SpriteRenderer sr, int order)
    {
        if (sr == null) return;
        if (!string.IsNullOrEmpty(defaultSortingLayer))
            sr.sortingLayerName = defaultSortingLayer;
        sr.sortingOrder = order;
    }

    private void PreviousLocation()
    {
        if (locations.Count == 0) return;
        _currentLocationIndex = (_currentLocationIndex - 1 + locations.Count) % locations.Count;
        ClearActiveTrash();
        ApplySideBySideLayout(_currentLocationIndex, instant: false);
        ResetSpawnTimer();
    }

    private void NextLocation()
    {
        if (locations.Count == 0) return;
        _currentLocationIndex = (_currentLocationIndex + 1) % locations.Count;
        ClearActiveTrash();
        ApplySideBySideLayout(_currentLocationIndex, instant: false);
        ResetSpawnTimer();
    }

    private void ClearActiveTrash()
    {
        for (int i = _activeTrashObjects.Count - 1; i >= 0; i--)
        {
            if (_activeTrashObjects[i] != null)
                Destroy(_activeTrashObjects[i]);
        }
        _activeTrashObjects.Clear();
    }

    private Vector3 GetTargetLocalPositionForMap(int mapIndex, int activeIndex)
    {
        float offsetX = (mapIndex - activeIndex) * mapSpacingX;
        return new Vector3(_centerLocalPosition.x + offsetX, _centerLocalPosition.y, _centerLocalPosition.z);
    }

    private void ApplySideBySideLayout(int activeIndex, bool instant)
    {
        if (locations == null || locations.Count == 0) return;

        SpriteLocationTheme activeLoc = locations[activeIndex];
        if (locationLabelText != null)
            locationLabelText.text = activeLoc.locationName;

        RefreshLockState(activeIndex);

        // Keep all assigned maps active side-by-side so they slide cleanly next to each other
        for (int i = 0; i < locations.Count; i++)
        {
            GameObject root = locations[i].locationRootObject;
            if (root != null && !root.activeSelf)
                root.SetActive(true);
        }

        if (_slideRoutine != null)
        {
            StopCoroutine(_slideRoutine);
            _slideRoutine = null;
        }

        if (instant || slideDuration <= 0f || !gameObject.activeInHierarchy)
        {
            for (int i = 0; i < locations.Count; i++)
            {
                GameObject root = locations[i].locationRootObject;
                if (root != null)
                    root.transform.localPosition = GetTargetLocalPositionForMap(i, activeIndex);
            }
        }
        else
        {
            _slideRoutine = StartCoroutine(SlideSideBySideRoutine(activeIndex));
        }
    }

    private IEnumerator SlideSideBySideRoutine(int targetIndex)
    {
        int count = locations.Count;
        Vector3[] startPositions = new Vector3[count];
        Vector3[] endPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            GameObject root = locations[i].locationRootObject;
            if (root != null)
            {
                startPositions[i] = root.transform.localPosition;
                endPositions[i] = GetTargetLocalPositionForMap(i, targetIndex);
            }
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, slideDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float linear = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - linear, 3f);

            for (int i = 0; i < count; i++)
            {
                GameObject root = locations[i].locationRootObject;
                if (root != null)
                    root.transform.localPosition = Vector3.Lerp(startPositions[i], endPositions[i], eased);
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject root = locations[i].locationRootObject;
            if (root != null)
                root.transform.localPosition = endPositions[i];
        }

        _slideRoutine = null;
    }

    private void UpdateGreenBinPulse()
    {
        if (greenBinIcon == null) return;

        if (_pulseGreenBin)
        {
            float wave = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            greenBinIcon.localScale = _greenBinBaseScale * (1f + wave * pulseAmount);
        }
        else
        {
            greenBinIcon.localScale = _greenBinBaseScale;
        }
    }

    private void HandleBagUpdated(int current, int max, bool isFull)
    {
        if (bagCounterText != null)
            bagCounterText.text = $"{current}/{max}";

        _pulseGreenBin = isFull;
    }

    private void HandleCurrencyUpdated(int _)
    {
        UpdateScoreUI();
        RefreshLockState(_currentLocationIndex);
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null && GameInventoryManager.Instance != null)
            scoreText.text = GameInventoryManager.Instance.TotalScore.ToString();
    }

    private void TogglePause()
    {
        _isPaused = !_isPaused;
        Time.timeScale = _isPaused ? 0f : 1f;
        if (pausePanel != null)
            pausePanel.SetActive(_isPaused);
    }

    private void OpenAlmanacOrShop()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(almanacOrShopSceneName);
    }

    private void EnterTrashNSorta()
    {
        if (GameInventoryManager.Instance == null || GameInventoryManager.Instance.CollectedCount <= 0)
            return;

        Time.timeScale = 1f;
        SceneManager.LoadScene(minigameSceneName);
    }

    private void OnDrawGizmosSelected()
    {
        if (locations == null) return;

        for (int i = 0; i < locations.Count; i++)
        {
            Rect r = locations[i].spawnArea;
            Vector3 center = new Vector3(r.center.x, r.center.y, transform.position.z);
            Vector3 size = new Vector3(r.width, r.height, 0.1f);

            Gizmos.color = (i == _currentLocationIndex)
                ? new Color(0.2f, 1f, 0.3f, 0.85f)
                : new Color(1f, 0.85f, 0.2f, 0.4f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}

public class SpriteTrashTapTarget : MonoBehaviour
{
    private TrashItemDefinition _item;
    private GameInterfaceController _controller;

    public void Initialize(TrashItemDefinition item, GameInterfaceController controller)
    {
        _item = item;
        _controller = controller;
    }

    private void OnMouseDown()
    {
        if (_controller != null)
            _controller.CollectSpriteTrash(_item, gameObject);
    }
}