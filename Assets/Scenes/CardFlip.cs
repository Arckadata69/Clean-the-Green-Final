using System.Collections;
using UnityEngine;

public class CardFlip : MonoBehaviour
{
    [SerializeField] GameObject front;
    [SerializeField] GameObject back;
    [SerializeField] float duration = 0.3f;

    bool showingFront = true;
    bool flipping;

    public void Flip()
    {
        if (!flipping) StartCoroutine(FlipRoutine());
    }

    public void ResetToFront()
    {
        StopAllCoroutines();
        flipping = false;
        showingFront = true;
        front.SetActive(true);
        back.SetActive(false);
        transform.localScale = Vector3.one;
    }

    IEnumerator FlipRoutine()
    {
        flipping = true;
        yield return Scale(1f, 0f);
        showingFront = !showingFront;
        front.SetActive(showingFront);
        back.SetActive(!showingFront);
        yield return Scale(0f, 1f);
        flipping = false;
    }

    IEnumerator Scale(float from, float to)
    {
        float half = duration / 2f, t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(from, to, t / half);
            transform.localScale = new Vector3(k, 1f, 1f);
            yield return null;
        }
        transform.localScale = new Vector3(to, 1f, 1f);
    }
}
