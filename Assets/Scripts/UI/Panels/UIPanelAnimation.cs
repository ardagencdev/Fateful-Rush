using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIPanelAnimation : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField, Min(0.01f)]
    private float duration = 0.2f;

    [SerializeField, Range(0.01f, 1f)]
    private float startScale = 0.8f;

    private CanvasGroup canvasGroup;
    private Coroutine animationRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        StopAnimation();

        animationRoutine =
            StartCoroutine(AnimatePanel());
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    private IEnumerator AnimatePanel()
    {
        float timer = 0f;

        Vector3 fromScale =
            Vector3.one * startScale;

        Vector3 targetScale =
            Vector3.one;

        transform.localScale = fromScale;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(timer / duration);

            float easedProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            canvasGroup.alpha =
                easedProgress;

            transform.localScale =
                Vector3.LerpUnclamped(
                    fromScale,
                    targetScale,
                    easedProgress
                );

            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        transform.localScale = targetScale;

        animationRoutine = null;
    }

    private void StopAnimation()
    {
        if (animationRoutine == null)
            return;

        StopCoroutine(animationRoutine);
        animationRoutine = null;
    }

    private void OnValidate()
    {
        duration =
            Mathf.Max(0.01f, duration);

        startScale =
            Mathf.Clamp(
                startScale,
                0.01f,
                1f
            );
    }
}