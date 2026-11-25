using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LocationCarouselController : MonoBehaviour
{
    [Serializable]
    public class LocationEntry
    {
        public string locationName = "Forest";
        public Transform mapTransform;
        public BGMController.BgmTrack bgmTrack = BGMController.BgmTrack.Forest;
    }

    [Header("Locations (Order: Left -> Right)")]
    [SerializeField] private List<LocationEntry> locations = new List<LocationEntry>
    {
        new LocationEntry { locationName = "Forest", bgmTrack = BGMController.BgmTrack.Forest },
        new LocationEntry { locationName = "Beach", bgmTrack = BGMController.BgmTrack.Beach },
        new LocationEntry { locationName = "City", bgmTrack = BGMController.BgmTrack.City }
    };

    [Header("Center Label & Layout")]
    [SerializeField] private TMP_Text locationLabel;
    [Tooltip("Horizontal spacing between each map in world units (match your Scene spacing).")]
    [SerializeField] private float mapSpacingX = 12f;
    [SerializeField] private Vector3 centerLocalPosition = Vector3.zero;

    [Header("Slide Animation")]
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private bool loopAround = true;
    [SerializeField] private bool playBgmOnStart = true;

    public int CurrentIndex { get; private set; } = 0;
    private Coroutine slideRoutine;

    void Start()
    {
        if (locations == null || locations.Count == 0) return;

        // Auto-calculate spacing if Map1 and Map2 are already placed side-by-side in Scene view
        if (locations.Count >= 2 && locations[0].mapTransform != null && locations[1].mapTransform != null)
        {
            float detectedSpacing = Mathf.Abs(locations[1].mapTransform.localPosition.x - locations[0].mapTransform.localPosition.x);
            if (detectedSpacing > 0.01f)
            {
                mapSpacingX = detectedSpacing;
            }
            centerLocalPosition = locations[0].mapTransform.localPosition;
        }

        ApplyImmediate(CurrentIndex, playBgmOnStart);
    }

    // Hook this to your Right button
    public void NextLocation()
    {
        if (locations == null || locations.Count == 0) return;

        int next = CurrentIndex + 1;
        if (next >= locations.Count)
        {
            if (!loopAround) return;
            next = 0;
        }

        GoToLocation(next);
    }

    // Hook this to your Left button
    public void PreviousLocation()
    {
        if (locations == null || locations.Count == 0) return;

        int prev = CurrentIndex - 1;
        if (prev < 0)
        {
            if (!loopAround) return;
            prev = locations.Count - 1;
        }

        GoToLocation(prev);
    }

    public void GoToLocation(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= locations.Count) return;

        CurrentIndex = targetIndex;
        UpdateLabelAndBgm(CurrentIndex, playBgm: true);

        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
        }

        if (gameObject.activeInHierarchy)
        {
            slideRoutine = StartCoroutine(SlideToIndexRoutine(CurrentIndex));
        }
        else
        {
            ApplyImmediate(CurrentIndex, playBgm: false);
        }
    }

    private void ApplyImmediate(int index, bool playBgm)
    {
        for (int i = 0; i < locations.Count; i++)
        {
            Transform map = locations[i].mapTransform;
            if (map == null) continue;

            float offsetX = (i - index) * mapSpacingX;
            map.localPosition = new Vector3(centerLocalPosition.x + offsetX, centerLocalPosition.y, centerLocalPosition.z);
        }

        UpdateLabelAndBgm(index, playBgm);
    }

    private IEnumerator SlideToIndexRoutine(int targetIndex)
    {
        Vector3[] startPositions = new Vector3[locations.Count];
        Vector3[] endPositions = new Vector3[locations.Count];

        for (int i = 0; i < locations.Count; i++)
        {
            Transform map = locations[i].mapTransform;
            if (map == null) continue;

            startPositions[i] = map.localPosition;
            float offsetX = (i - targetIndex) * mapSpacingX;
            endPositions[i] = new Vector3(centerLocalPosition.x + offsetX, centerLocalPosition.y, centerLocalPosition.z);
        }

        float duration = Mathf.Max(0.01f, slideDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            for (int i = 0; i < locations.Count; i++)
            {
                if (locations[i].mapTransform != null)
                {
                    locations[i].mapTransform.localPosition = Vector3.Lerp(startPositions[i], endPositions[i], t);
                }
            }

            yield return null;
        }

        for (int i = 0; i < locations.Count; i++)
        {
            if (locations[i].mapTransform != null)
            {
                locations[i].mapTransform.localPosition = endPositions[i];
            }
        }

        slideRoutine = null;
    }

    private void UpdateLabelAndBgm(int index, bool playBgm)
    {
        LocationEntry active = locations[index];

        if (locationLabel != null)
        {
            locationLabel.text = active.locationName;
        }

        if (playBgm && BGMController.Instance != null)
        {
            BGMController.Instance.PlayTrack(active.bgmTrack);
        }
    }
}