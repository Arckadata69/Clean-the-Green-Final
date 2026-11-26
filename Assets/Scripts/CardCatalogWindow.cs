using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CatalogDetailWindow : MonoBehaviour
{
    [Header("Window & Flip Containers")]
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

    [Header("Undiscovered Display")]
    [SerializeField] private string undiscoveredName = "???";
    [SerializeField] private string undiscoveredDetails = "Not picked up yet.";

    private Coroutine _flipRoutine;

    private void Awake()
    {
        if (windowRoot == null)
            windowRoot = gameObject;

        if (cardFlipRoot == null && windowRoot != null)
            cardFlipRoot = windowRoot.GetComponent<RectTransform>();

        if (canCauseButton != null)
            canCauseButton.onClick.AddListener(FlipToBack);

        if (backButton != null)
            backButton.onClick.AddListener(FlipToFront);

        if (closeWindowButton != null)
            closeWindowButton.onClick.AddListener(Close);

        Close();
    }

    private void OnDestroy()
    {
        if (canCauseButton != null)
            canCauseButton.onClick.RemoveListener(FlipToBack);

        if (backButton != null)
            backButton.onClick.RemoveListener(FlipToFront);

        if (closeWindowButton != null)
            closeWindowButton.onClick.RemoveListener(Close);
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
        if (trashNameText != null)
            trashNameText.text = isDiscovered ? trashName : undiscoveredName;

        if (detailsText != null)
            detailsText.text = isDiscovered ? details : undiscoveredDetails;

        if (valueText != null)
            valueText.text = isDiscovered ? valueAndPoints : "???";

        if (locationText != null)
            locationText.text = isDiscovered ? location : "???";

        Sprite frontArt = isDiscovered ? trashSprite : (silhouetteSprite != null ? silhouetteSprite : trashSprite);
        SetSprite(trashIconImage, frontArt);

        if (canCauseButton != null)
            canCauseButton.interactable = isDiscovered;

        if (warningHeaderText != null)
            warningHeaderText.text = string.IsNullOrEmpty(warningHeader) ? "If left unnoticed" : warningHeader;

        if (consequenceText != null)
            consequenceText.text = isDiscovered ? consequence : undiscoveredDetails;

        if (properBinText != null)
            properBinText.text = isDiscovered ? properBin : "???";

        if (pickedUpCountText != null)
            pickedUpCountText.text = pickedUpCount.ToString();

        SetSprite(hazardImage, isDiscovered ? hazardSprite : null);

        SetFaceImmediate(showBack: false);
        windowRoot.SetActive(true);
    }

    public void FlipToBack()
    {
        StartFlip(showBack: true);
    }

    public void FlipToFront()
    {
        StartFlip(showBack: false);
    }

    public void Close()
    {
        if (_flipRoutine != null)
        {
            StopCoroutine(_flipRoutine);
            _flipRoutine = null;
        }

        if (cardFlipRoot != null)
            cardFlipRoot.localScale = Vector3.one;

        if (windowRoot != null)
            windowRoot.SetActive(false);
    }

    private void StartFlip(bool showBack)
    {
        if (cardFlipRoot == null)
        {
            SetFaceImmediate(showBack);
            return;
        }

        if (_flipRoutine != null)
            StopCoroutine(_flipRoutine);

        _flipRoutine = StartCoroutine(FlipRoutine(showBack));
    }

    private IEnumerator FlipRoutine(bool showBack)
    {
        float elapsed = 0f;
        Vector3 baseScale = Vector3.one;

        while (elapsed < flipHalfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flipHalfDuration);
            cardFlipRoot.localScale = new Vector3(Mathf.Lerp(1f, 0f, t), baseScale.y, baseScale.z);
            yield return null;
        }

        cardFlipRoot.localScale = new Vector3(0f, baseScale.y, baseScale.z);
        SetFaceImmediate(showBack);

        elapsed = 0f;
        while (elapsed < flipHalfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flipHalfDuration);
            cardFlipRoot.localScale = new Vector3(Mathf.Lerp(0f, 1f, t), baseScale.y, baseScale.z);
            yield return null;
        }

        cardFlipRoot.localScale = baseScale;
        _flipRoutine = null;
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