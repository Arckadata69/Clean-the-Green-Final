using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CatalogDetailWindow : MonoBehaviour
{
    [Header("Window & Flip")]
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private RectTransform cardFlipRoot;
    [SerializeField] private GameObject frontFace;
    [SerializeField] private GameObject backFace;
    [SerializeField] private float flipHalfDuration = 0.15f;

    [Header("Front Face UI")]
    [SerializeField] private TMP_Text trashNameText;
    [SerializeField] private Image trashIconImage;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text locationText;
    [SerializeField] private Button canCauseButton;
    [SerializeField] private Button closeWindowButton;

    [Header("Back Face UI")]
    [SerializeField] private TMP_Text warningHeaderText;
    [SerializeField] private Image hazardImage;
    [SerializeField] private TMP_Text consequenceText;
    [SerializeField] private TMP_Text properBinText;
    [SerializeField] private TMP_Text pickedUpCountText;
    [SerializeField] private Button backButton;

    [Header("Undiscovered")]
    [SerializeField] private string undiscoveredName = "???";
    [SerializeField] private string undiscoveredDetails = "Not picked up yet.";

    private Coroutine _flipRoutine;

    private void Awake()
    {
        if (windowRoot == null) windowRoot = gameObject;
        if (cardFlipRoot == null && windowRoot != null) cardFlipRoot = windowRoot.GetComponent<RectTransform>();

        if (canCauseButton != null) canCauseButton.onClick.AddListener(FlipToBack);
        if (backButton != null) backButton.onClick.AddListener(FlipToFront);
        if (closeWindowButton != null) closeWindowButton.onClick.AddListener(Close);

        Close();
    }

    private void OnDestroy()
    {
        if (canCauseButton != null) canCauseButton.onClick.RemoveListener(FlipToBack);
        if (backButton != null) backButton.onClick.RemoveListener(FlipToFront);
        if (closeWindowButton != null) closeWindowButton.onClick.RemoveListener(Close);
    }

    public void OpenTrashEntry(
        bool isDiscovered,
        string trashName,
        Sprite trashSprite,
        Sprite silhouetteSprite,
        string details,
        string valueAndPoints,
        string location,
        string warningHeader,
        Sprite hazardSprite,
        string consequence,
        string properBin,
        int pickedUpCount)
    {
        if (trashNameText != null) trashNameText.text = isDiscovered ? trashName : undiscoveredName;
        if (detailsText != null) detailsText.text = isDiscovered ? details : undiscoveredDetails;
        if (valueText != null) valueText.text = isDiscovered ? valueAndPoints : "???";
        if (locationText != null) locationText.text = isDiscovered ? location : "???";

        Sprite frontArt = isDiscovered ? trashSprite : (silhouetteSprite != null ? silhouetteSprite : trashSprite);
        SetSprite(trashIconImage, frontArt);

        if (canCauseButton != null) canCauseButton.interactable = isDiscovered;

        if (warningHeaderText != null) warningHeaderText.text = string.IsNullOrEmpty(warningHeader) ? "If left unnoticed" : warningHeader;
        if (consequenceText != null) consequenceText.text = isDiscovered ? consequence : undiscoveredDetails;
        if (properBinText != null) properBinText.text = isDiscovered ? properBin : "???";
        if (pickedUpCountText != null) pickedUpCountText.text = pickedUpCount.ToString();

        SetSprite(hazardImage, isDiscovered ? hazardSprite : null);

        ResetPositionAndScale();
        SetFaceImmediate(showBack: false);
        windowRoot.SetActive(true);
    }

    public void FlipToBack() => StartFlip(true);
    public void FlipToFront() => StartFlip(false);

    public void Close()
    {
        if (_flipRoutine != null)
        {
            StopCoroutine(_flipRoutine);
            _flipRoutine = null;
        }

        ResetPositionAndScale();
        if (windowRoot != null) windowRoot.SetActive(false);
    }

    private void StartFlip(bool showBack)
    {
        if (_flipRoutine != null) StopCoroutine(_flipRoutine);

        if (cardFlipRoot == null || flipHalfDuration <= 0f)
        {
            SetFaceImmediate(showBack);
            return;
        }

        _flipRoutine = StartCoroutine(FlipRoutine(showBack));
    }

    private IEnumerator FlipRoutine(bool showBack)
    {
        yield return ScaleX(1f, 0f);
        SetFaceImmediate(showBack);
        yield return ScaleX(0f, 1f);
        _flipRoutine = null;
    }

    private IEnumerator ScaleX(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < flipHalfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float x = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / flipHalfDuration));
            if (cardFlipRoot != null)
                cardFlipRoot.localScale = new Vector3(x, 1f, 1f);
            yield return null;
        }

        if (cardFlipRoot != null)
            cardFlipRoot.localScale = new Vector3(to, 1f, 1f);
    }

    private void ResetPositionAndScale()
    {
        if (cardFlipRoot != null)
        {
            cardFlipRoot.localScale = Vector3.one;
            cardFlipRoot.anchoredPosition = Vector2.zero;
        }
    }

    private void SetFaceImmediate(bool showBack)
    {
        if (frontFace != null) frontFace.SetActive(!showBack);
        if (backFace != null) backFace.SetActive(showBack);
    }

    private static void SetSprite(Image target, Sprite sprite)
    {
        if (target == null) return;
        target.sprite = sprite;
        target.preserveAspect = true;
        target.enabled = sprite != null;
    }
}