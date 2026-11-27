using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class ResponsiveOrthoCamera : MonoBehaviour
{
    public enum FramingMode
    {
        MatchHeight,
        MatchWidth,
        FitInsideReference
    }

    [Header("Reference 1080x1920 Framing")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
    [SerializeField] private float referenceOrthoSize = 5f;
    [SerializeField] private FramingMode framingMode = FramingMode.FitInsideReference;

    [Header("Optional Clamp Limits")]
    [SerializeField] private bool clampOrthoSize = true;
    [SerializeField] private float minOrthoSize = 4f;
    [SerializeField] private float maxOrthoSize = 8f;

    private Camera _cam;
    private int _lastScreenWidth;
    private int _lastScreenHeight;
    private float _lastReferenceSize;
    private FramingMode _lastFramingMode;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        ApplyCameraFraming(force: true);
    }

    private void OnEnable()
    {
        if (_cam == null)
            _cam = GetComponent<Camera>();
        ApplyCameraFraming(force: true);
    }

    private void Update()
    {
        ApplyCameraFraming(force: false);
    }

    public void ApplyCameraFraming(bool force = false)
    {
        if (_cam == null || !_cam.orthographic)
            return;

        int w = Mathf.Max(1, Screen.width);
        int h = Mathf.Max(1, Screen.height);

        if (!force &&
            w == _lastScreenWidth &&
            h == _lastScreenHeight &&
            Mathf.Approximately(referenceOrthoSize, _lastReferenceSize) &&
            framingMode == _lastFramingMode)
        {
            return;
        }

        _lastScreenWidth = w;
        _lastScreenHeight = h;
        _lastReferenceSize = referenceOrthoSize;
        _lastFramingMode = framingMode;

        float targetAspect = Mathf.Max(0.01f, referenceResolution.x / Mathf.Max(1f, referenceResolution.y));
        float currentAspect = (float)w / h;

        float computedSize = referenceOrthoSize;

        switch (framingMode)
        {
            case FramingMode.MatchHeight:
                computedSize = referenceOrthoSize;
                break;

            case FramingMode.MatchWidth:
                computedSize = referenceOrthoSize * (targetAspect / currentAspect);
                break;

            case FramingMode.FitInsideReference:
                // If screen is narrower than 9:16, widen orthographic size so left/right edges do not crop.
                // If screen is wider than 9:16, keep reference vertical height so top/bottom stay framed.
                if (currentAspect < targetAspect)
                    computedSize = referenceOrthoSize * (targetAspect / currentAspect);
                else
                    computedSize = referenceOrthoSize;
                break;
        }

        if (clampOrthoSize)
        {
            float lo = Mathf.Min(minOrthoSize, maxOrthoSize);
            float hi = Mathf.Max(minOrthoSize, maxOrthoSize);
            computedSize = Mathf.Clamp(computedSize, lo, hi);
        }

        _cam.orthographicSize = computedSize;
    }
}