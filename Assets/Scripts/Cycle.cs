using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightCycle2D : MonoBehaviour
{
    public enum TimePhase
    {
        Dawn,
        Day,
        Dusk,
        Night
    }

    [Serializable]
    public class PhaseConfig
    {
        public TimePhase phase = TimePhase.Day;
        public Sprite sprite;
        [Tooltip("How long this phase stays active in seconds.")]
        [Min(0.5f)] public float durationSeconds = 30f;
        [Tooltip("How long the fade transition into this phase takes in seconds.")]
        [Min(0.05f)] public float fadeDurationSeconds = 2f;
    }

    [Header("Sprite Renderers (Overlay Crossfade)")]
    [Tooltip("Main background/sky SpriteRenderer.")]
    [SerializeField] private SpriteRenderer primaryRenderer;
    [Tooltip("Second SpriteRenderer stacked directly in front of Primary Renderer for smooth crossfading.")]
    [SerializeField] private SpriteRenderer fadeOverlayRenderer;

    [Header("Editable Cycle Phases (Order = Cycle Sequence)")]
    [SerializeField] private List<PhaseConfig> phases = new List<PhaseConfig>
    {
        new PhaseConfig { phase = TimePhase.Dawn, durationSeconds = 25f, fadeDurationSeconds = 2.5f },
        new PhaseConfig { phase = TimePhase.Day, durationSeconds = 45f, fadeDurationSeconds = 2.5f },
        new PhaseConfig { phase = TimePhase.Dusk, durationSeconds = 25f, fadeDurationSeconds = 2.5f },
        new PhaseConfig { phase = TimePhase.Night, durationSeconds = 40f, fadeDurationSeconds = 2.5f }
    };

    [Header("Playback Options")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool useUnscaledTime = false;

    public TimePhase CurrentPhase { get; private set; }
    private int currentIndex = 0;
    private Coroutine cycleRoutine;

    void Awake()
    {
        if (primaryRenderer == null)
        {
            primaryRenderer = GetComponent<SpriteRenderer>();
        }

        if (fadeOverlayRenderer != null)
        {
            SetAlpha(fadeOverlayRenderer, 0f);
            if (primaryRenderer != null)
            {
                fadeOverlayRenderer.sortingLayerID = primaryRenderer.sortingLayerID;
                fadeOverlayRenderer.sortingOrder = primaryRenderer.sortingOrder + 1;
            }
        }
    }

    void Start()
    {
        if (phases == null || phases.Count == 0) return;

        PhaseConfig first = phases[0];
        CurrentPhase = first.phase;
        if (primaryRenderer != null && first.sprite != null)
        {
            primaryRenderer.sprite = first.sprite;
            SetAlpha(primaryRenderer, 1f);
        }

        if (playOnStart)
        {
            StartCycle();
        }
    }

    public void StartCycle()
    {
        StopCycle();
        if (phases != null && phases.Count > 0 && gameObject.activeInHierarchy)
        {
            cycleRoutine = StartCoroutine(CycleRoutine());
        }
    }

    public void StopCycle()
    {
        if (cycleRoutine != null)
        {
            StopCoroutine(cycleRoutine);
            cycleRoutine = null;
        }
    }

    public void JumpToPhase(TimePhase targetPhase)
    {
        int foundIndex = phases.FindIndex(p => p.phase == targetPhase);
        if (foundIndex < 0) return;

        currentIndex = foundIndex;
        StartCycle();
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            PhaseConfig activePhase = phases[currentIndex];
            CurrentPhase = activePhase.phase;

            // Hold current phase for its configured interval
            float holdTimer = 0f;
            while (holdTimer < activePhase.durationSeconds)
            {
                holdTimer += DeltaTime();
                yield return null;
            }

            // Advance to next phase and crossfade
            int nextIndex = (currentIndex + 1) % phases.Count;
            PhaseConfig nextPhase = phases[nextIndex];

            yield return StartCoroutine(CrossfadeTo(nextPhase));

            currentIndex = nextIndex;
            CurrentPhase = nextPhase.phase;
        }
    }

    private IEnumerator CrossfadeTo(PhaseConfig nextPhase)
    {
        if (primaryRenderer == null || nextPhase.sprite == null)
        {
            yield break;
        }

        if (fadeOverlayRenderer == null)
        {
            primaryRenderer.sprite = nextPhase.sprite;
            yield break;
        }

        fadeOverlayRenderer.sprite = nextPhase.sprite;
        SetAlpha(fadeOverlayRenderer, 0f);

        float duration = Mathf.Max(0.01f, nextPhase.fadeDurationSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += DeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(fadeOverlayRenderer, t);
            yield return null;
        }

        SetAlpha(fadeOverlayRenderer, 1f);

        // Commit new sprite to primary renderer and reset overlay alpha
        primaryRenderer.sprite = nextPhase.sprite;
        SetAlpha(fadeOverlayRenderer, 0f);
        fadeOverlayRenderer.sprite = null;
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = Mathf.Clamp01(alpha);
        sr.color = c;
    }
}