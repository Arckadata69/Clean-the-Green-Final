using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TrashNSortaController : MonoBehaviour
{
    [Header("Bins (Sprite / Transform)")]
    [SerializeField] private Transform recyclingBin;        // Blue
    [SerializeField] private Transform biodegradableBin;    // Green
    [SerializeField] private Transform nonBiodegradableBin; // Yellow
    [SerializeField] private Transform hazardousBin;        // Red
    [SerializeField] private float worldDropSnapRadius = 1.5f;

    [Header("Spawn & Drag (SpriteRenderer)")]
    [SerializeField] private Transform spawnSlot;           // Bottom gold square
    [SerializeField] private DraggableTrashPiece trashPiecePrefab;
    [SerializeField] private SpriteRenderer dragHandCursor;
    [SerializeField] private Sprite openHandSprite;
    [SerializeField] private Sprite grabHandSprite;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int binSortingOrder = 5;
    [SerializeField] private int pieceSortingOrder = 15;
    [SerializeField] private int handSortingOrder = 25;

    [Header("Score & Multiplier UI")]
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private TMP_Text remainingCountText;
    [SerializeField] private Image multiplierBadgeImage;
    [SerializeField] private Sprite[] multiplierSprites;       // Index 0..5 => 1x, 2x, 4x, 8x, 16x, 32x
    [SerializeField] private TMP_Text scorePopupText;

    [Header("Navigation")]
    [SerializeField] private Button returnButton;
    [SerializeField] private string returnSceneName = "Almanac";
    [SerializeField] private int wrongBinPenalty = 15;

    private static readonly int[] Multipliers = { 1, 2, 4, 8, 16, 32 };

    private readonly Queue<TrashItemDefinition> _trashQueue = new Queue<TrashItemDefinition>();
    private DraggableTrashPiece _activePiece;
    private Camera _mainCam;
    private int _sessionScore;
    private int _multiplierIndex;

    private void Awake()
    {
        _mainCam = Camera.main;

        if (returnButton != null)
            returnButton.onClick.AddListener(FinishAndReturn);

        ApplyBinSorting(recyclingBin);
        ApplyBinSorting(biodegradableBin);
        ApplyBinSorting(nonBiodegradableBin);
        ApplyBinSorting(hazardousBin);

        if (dragHandCursor != null)
        {
            ApplySpriteSorting(dragHandCursor, handSortingOrder);
            dragHandCursor.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (GameInventoryManager.Instance != null)
        {
            List<TrashItemDefinition> collected = GameInventoryManager.Instance.ConsumeCollectedTrashForMinigame();
            foreach (TrashItemDefinition item in collected)
                _trashQueue.Enqueue(item);
        }

        UpdateMultiplierUI();
        UpdateScoreUI();
        SpawnNextTrash();
    }

    private void OnDestroy()
    {
        if (returnButton != null)
            returnButton.onClick.RemoveListener(FinishAndReturn);
    }

    public Vector3 ScreenToWorld(Vector2 screenPos)
    {
        if (_mainCam == null)
            _mainCam = Camera.main;

        float zDist = spawnSlot != null
            ? Mathf.Abs(spawnSlot.position.z - _mainCam.transform.position.z)
            : Mathf.Abs(_mainCam.transform.position.z);

        Vector3 world = _mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDist));
        world.z = spawnSlot != null ? spawnSlot.position.z : 0f;
        return world;
    }

    public void SetDraggingVisual(bool isDragging, Vector2 screenPos)
    {
        if (dragHandCursor == null) return;

        dragHandCursor.gameObject.SetActive(isDragging);
        if (isDragging)
        {
            dragHandCursor.transform.position = ScreenToWorld(screenPos);
            if (grabHandSprite != null)
                dragHandCursor.sprite = grabHandSprite;
        }
        else if (openHandSprite != null)
        {
            dragHandCursor.sprite = openHandSprite;
        }
    }

    public void EvaluateDrop(DraggableTrashPiece piece, Vector2 screenPosition)
    {
        SetDraggingVisual(false, screenPosition);
        Vector3 worldPos = ScreenToWorld(screenPosition);

        if (!TryGetTargetBin(worldPos, out TrashBinType droppedBin))
        {
            piece.ReturnToSpawn();
            return;
        }

        TrashItemDefinition data = piece.Data;
        bool isCorrect = droppedBin == data.correctBin;

        if (isCorrect)
        {
            int mult = Multipliers[_multiplierIndex];
            int rarityBonus = data.rarity == TrashRarity.Gold ? 2 : 1;
            int earned = data.basePoints * mult * rarityBonus;

            _sessionScore += earned;
            _multiplierIndex = Mathf.Min(_multiplierIndex + 1, Multipliers.Length - 1);

            if (GameInventoryManager.Instance != null)
                GameInventoryManager.Instance.AddRewards(earned, data.baseCaps * rarityBonus);

            ShowPopup($"+{earned}", data.rarity == TrashRarity.Gold ? Color.yellow : Color.green);
        }
        else
        {
            _sessionScore = Mathf.Max(0, _sessionScore - wrongBinPenalty);
            _multiplierIndex = 0; // Reset to 1x

            if (GameInventoryManager.Instance != null)
                GameInventoryManager.Instance.AddRewards(-wrongBinPenalty, 0);

            ShowPopup($"-{wrongBinPenalty}", Color.red);
        }

        Destroy(piece.gameObject);
        _activePiece = null;

        UpdateMultiplierUI();
        UpdateScoreUI();
        SpawnNextTrash();
    }

    private void SpawnNextTrash()
    {
        if (remainingCountText != null)
            remainingCountText.text = _trashQueue.Count.ToString();

        if (_trashQueue.Count == 0)
        {
            if (returnButton != null)
                returnButton.gameObject.SetActive(true);
            return;
        }

        TrashItemDefinition nextItem = _trashQueue.Dequeue();
        if (remainingCountText != null)
            remainingCountText.text = _trashQueue.Count.ToString();

        _activePiece = Instantiate(trashPiecePrefab, spawnSlot);
        SpriteRenderer pieceSr = _activePiece.GetComponent<SpriteRenderer>();
        ApplySpriteSorting(pieceSr, pieceSortingOrder);
        _activePiece.Initialize(this, nextItem, spawnSlot);
    }

    private bool TryGetTargetBin(Vector3 worldPos, out TrashBinType binType)
    {
        float bestDist = worldDropSnapRadius;
        binType = TrashBinType.Recycling;
        bool found = false;

        CheckBin(recyclingBin, TrashBinType.Recycling, worldPos, ref bestDist, ref binType, ref found);
        CheckBin(biodegradableBin, TrashBinType.Biodegradable, worldPos, ref bestDist, ref binType, ref found);
        CheckBin(nonBiodegradableBin, TrashBinType.NonBiodegradable, worldPos, ref bestDist, ref binType, ref found);
        CheckBin(hazardousBin, TrashBinType.Hazardous, worldPos, ref bestDist, ref binType, ref found);

        return found;
    }

    private static void CheckBin(
        Transform binTf,
        TrashBinType type,
        Vector3 worldPos,
        ref float bestDist,
        ref TrashBinType bestType,
        ref bool found)
    {
        if (binTf == null) return;
        float dist = Vector2.Distance(worldPos, binTf.position);
        if (dist <= bestDist)
        {
            bestDist = dist;
            bestType = type;
            found = true;
        }
    }

    private void ApplyBinSorting(Transform binTf)
    {
        if (binTf == null) return;
        SpriteRenderer sr = binTf.GetComponent<SpriteRenderer>();
        ApplySpriteSorting(sr, binSortingOrder);
    }

    private void ApplySpriteSorting(SpriteRenderer sr, int order)
    {
        if (sr == null) return;
        if (!string.IsNullOrEmpty(sortingLayerName))
            sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = order;
    }

    private void UpdateMultiplierUI()
    {
        if (multiplierBadgeImage != null &&
            multiplierSprites != null &&
            _multiplierIndex < multiplierSprites.Length)
        {
            multiplierBadgeImage.sprite = multiplierSprites[_multiplierIndex];
        }
    }

    private void UpdateScoreUI()
    {
        if (totalScoreText != null)
            totalScoreText.text = _sessionScore.ToString();
    }

    private void ShowPopup(string text, Color color)
    {
        if (scorePopupText == null) return;
        scorePopupText.text = text;
        scorePopupText.color = color;
    }

    private void FinishAndReturn()
    {
        SceneManager.LoadScene(returnSceneName);
    }
}