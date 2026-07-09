using UnityEngine;
using System.Collections;
using TMPro;

public class ScoreUIEffect : MonoBehaviour
{
    [Header("Reference")]
    public TextMeshProUGUI scoreText;

    [Header("Pop Settings")]
    public float popScale = 1.25f;
    public float duration = 0.08f;

    private Coroutine scoreCoroutine;

    public void PlayPop()
    {
        if (scoreText == null) return;

        if (scoreCoroutine != null)
            StopCoroutine(scoreCoroutine);

        scoreCoroutine = StartCoroutine(PopEffect());
    }

    private IEnumerator PopEffect()
    {
        Vector3 normalScale = Vector3.one;
        Vector3 bigScale = Vector3.one * popScale;

        yield return ScaleRoutine(normalScale, bigScale, SmoothStep);
        yield return ScaleRoutine(bigScale, normalScale, EaseIn);

        scoreText.transform.localScale = normalScale;
        scoreCoroutine = null;
    }

    private IEnumerator ScaleRoutine(Vector3 from, Vector3 to, System.Func<float, float> easing)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = easing(time / duration);
            scoreText.transform.localScale = Vector3.Lerp(from, to, t);

            yield return null;
        }
    }

    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private float EaseIn(float t)
    {
        return t * t;
    }
}