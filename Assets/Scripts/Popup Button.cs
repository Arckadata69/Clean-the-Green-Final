using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuPopupButton : MonoBehaviour
{
    [Header("Target Window")]
    [SerializeField] private GameObject targetWindow;
    [SerializeField] private GameObject optionalDimOverlay;

    [Header("Optional: Close Other Windows First")]
    [SerializeField] private GameObject[] otherWindowsToClose;

    [Header("Behavior")]
    [SerializeField] private bool toggleIfAlreadyOpen = false;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleButtonClick);
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClick);
        }
    }

    private void HandleButtonClick()
    {
        if (targetWindow == null)
        {
            Debug.LogWarning($"[MenuPopupButton] No targetWindow assigned on '{gameObject.name}'.", this);
            return;
        }

        // Close other floating windows so only one opens at a time
        if (otherWindowsToClose != null)
        {
            foreach (GameObject window in otherWindowsToClose)
            {
                if (window != null && window != targetWindow)
                {
                    window.SetActive(false);
                }
            }
        }

        bool shouldOpen = toggleIfAlreadyOpen ? !targetWindow.activeSelf : true;
        targetWindow.SetActive(shouldOpen);

        if (optionalDimOverlay != null)
        {
            optionalDimOverlay.SetActive(shouldOpen);
        }
    }

    // Call this from a Close (X) button inside the floating window
    public void CloseTargetWindow()
    {
        if (targetWindow != null)
        {
            targetWindow.SetActive(false);
        }

        if (optionalDimOverlay != null)
        {
            optionalDimOverlay.SetActive(false);
        }
    }
}