using System.Collections;
using UnityEngine;

public class PopupWindowController : MonoBehaviour
{
    [Header("One-Window-At-A-Time")]
    [Tooltip("Other popup windows that should automatically close when this window opens.")]
    [SerializeField] private PopupWindowController[] otherWindowsToClose;

    [Header("Click-Blocking Backdrop (Low-Opacity White / Overlay)")]
    [SerializeField] private GameObject backdropObject;
    [SerializeField] private bool snapBackdropToAnchor = true;
    [SerializeField] private Vector3 backdropOffset = new Vector3(0f, 0f, -0.5f);

    [Header("Optional Snap-To-Center")]
    [SerializeField] private bool snapToAnchorOnOpen = false;
    [SerializeField] private Transform anchorTarget;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0f, -1f);

    [Header("Optional Background Blockers")]
    [SerializeField] private Behaviour[] componentsToDisableWhileOpen;

    [Header("Pop-In Animation")]
    [SerializeField] private bool animatePopIn = true;
    [SerializeField] private float popDuration = 0.16f;

    private Vector3 originalScale;
    private Coroutine activeRoutine;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void Open()
    {
        // 1. Ensure only one window is active at a time
        if (otherWindowsToClose != null)
        {
            foreach (PopupWindowController other in otherWindowsToClose)
            {
                if (other != null && other != this)
                {
                    other.CloseWithoutEnablingBlockers();
                }
            }
        }

        // 2. Show and position the low-opacity white click-blocking backdrop
        Transform anchor = anchorTarget != null ? anchorTarget : transform;
        if (backdropObject != null)
        {
            if (snapBackdropToAnchor && anchorTarget != null)
            {
                backdropObject.transform.position = anchor.position + backdropOffset;
            }
            backdropObject.SetActive(true);
        }

        // 3. Position and open this window
        if (snapToAnchorOnOpen && anchorTarget != null)
        {
            transform.position = anchor.position + localOffset;
        }

        gameObject.SetActive(true);
        SetBlockersEnabled(false);

        if (animatePopIn)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(PopInScale());
        }
        else
        {
            transform.localScale = originalScale;
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);

        // Only hide backdrop if no other window in otherWindowsToClose is currently open
        if (!AnyOtherWindowOpen() && backdropObject != null)
        {
            backdropObject.SetActive(false);
        }

        SetBlockersEnabled(true);
    }

    private void CloseWithoutEnablingBlockers()
    {
        gameObject.SetActive(false);
    }

    private bool AnyOtherWindowOpen()
    {
        if (otherWindowsToClose == null) return false;

        foreach (PopupWindowController other in otherWindowsToClose)
        {
            if (other != null && other != this && other.gameObject.activeSelf)
            {
                return true;
            }
        }
        return false;
    }

    private void SetBlockersEnabled(bool enabled)
    {
        if (componentsToDisableWhileOpen == null) return;

        foreach (Behaviour item in componentsToDisableWhileOpen)
        {
            if (item != null)
            {
                item.enabled = enabled;
            }
        }
    }

    private IEnumerator PopInScale()
    {
        float elapsed = 0f;
        transform.localScale = originalScale * 0.8f;

        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float easeOut = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.Lerp(originalScale * 0.8f, originalScale, easeOut);
            yield return null;
        }

        transform.localScale = originalScale;
        activeRoutine = null;
    }
}