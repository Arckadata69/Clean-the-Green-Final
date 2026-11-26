using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonRuntimeBinder : MonoBehaviour
{
    public enum ClickActionType
    {
        None,
        LoadSceneByName,
        LoadSceneByIndex,
        ReloadCurrentScene,
        OpenPopupWindow
    }

    [Header("SFX Settings")]
    [SerializeField] private bool playSfxOnClick = true;
    [SerializeField] private AudioClip customSfxClip;
    [SerializeField] private string namedSfxKey = "";

    [Header("Click Action")]
    [SerializeField] private ClickActionType actionType = ClickActionType.None;
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private int targetSceneBuildIndex = -1;
    [SerializeField] private PopupWindowController targetPopupWindow;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (playSfxOnClick && SfxPlayer.Instance != null)
        {
            if (!string.IsNullOrWhiteSpace(namedSfxKey))
            {
                SfxPlayer.Instance.PlayNamedSfx(namedSfxKey);
            }
            else
            {
                SfxPlayer.Instance.PlayClick(customSfxClip);
            }
        }

        switch (actionType)
        {
            case ClickActionType.LoadSceneByName:
                if (!string.IsNullOrWhiteSpace(targetSceneName) && SceneLoader.Instance != null)
                {
                    SceneLoader.Instance.LoadScene(targetSceneName);
                }
                break;

            case ClickActionType.LoadSceneByIndex:
                if (targetSceneBuildIndex >= 0 && SceneLoader.Instance != null)
                {
                    SceneLoader.Instance.LoadScene(targetSceneBuildIndex);
                }
                break;

            case ClickActionType.ReloadCurrentScene:
                if (SceneLoader.Instance != null)
                {
                    SceneLoader.Instance.ReloadCurrentScene();
                }
                break;

            case ClickActionType.OpenPopupWindow:
                if (targetPopupWindow != null)
                {
                    targetPopupWindow.Open();
                }
                break;
        }
    }
}