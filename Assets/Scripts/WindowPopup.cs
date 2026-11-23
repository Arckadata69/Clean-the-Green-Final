using System.Collections;
using UnityEngine;

public class PopupWindowController : MonoBehaviour
{
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
        if (snapToAnchorOnOpen && anchorTarget != null)
        {
            transform.position = anchorTarget.position + localOffset;
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
        SetBlockersEnabled(true);
        gameObject.SetActive(false);
    }

    // Helper if one manager or button wants to close another popup before opening this one
    public void CloseOtherWindow(PopupWindowController otherWindow)
    {
        if (otherWindow != null && otherWindow != this)
        {
            otherWindow.Close();
        }
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