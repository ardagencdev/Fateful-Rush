using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PausePanelTransition : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Image backdropImage;
    [SerializeField] private RectTransform pauseMainPanel;
    [SerializeField] private CanvasGroup pauseMainCanvasGroup;

    [Header("Backdrop")]
    [SerializeField, Range(0f, 1f)]
    private float targetBackdropAlpha = 0.68f;

    [Header("Open")]
    [SerializeField, Min(0.05f)]
    private float openDuration = 0.22f;

    [SerializeField, Range(0.8f, 1f)]
    private float startScale = 0.94f;

    [Header("Close")]
    [SerializeField, Min(0.05f)]
    private float closeDuration = 0.18f;

    [SerializeField, Range(0.85f, 1f)]
    private float endScale = 0.97f;

    private Coroutine routine;

    public bool IsTransitioning => routine != null;

    public void Show()
    {
        StopCurrent();

        if (pausePanel == null)
            return;

        pausePanel.SetActive(true);
        routine = StartCoroutine(ShowRoutine());
    }

    public void Hide()
    {
        if (pausePanel == null || !pausePanel.activeSelf)
        {
            SetInstant(false);
            return;
        }

        StopCurrent();
        routine = StartCoroutine(HideRoutine());
    }

    public void SetInstant(bool visible)
    {
        StopCurrent();

        if (pausePanel == null)
            return;

        if (visible)
        {
            pausePanel.SetActive(true);

            SetBackdropAlpha(targetBackdropAlpha);

            if (pauseMainCanvasGroup != null)
            {
                pauseMainCanvasGroup.alpha = 1f;
                pauseMainCanvasGroup.interactable = true;
                pauseMainCanvasGroup.blocksRaycasts = true;
            }

            if (pauseMainPanel != null)
                pauseMainPanel.localScale = Vector3.one;
        }
        else
        {
            SetBackdropAlpha(0f);

            if (pauseMainCanvasGroup != null)
            {
                pauseMainCanvasGroup.alpha = 0f;
                pauseMainCanvasGroup.interactable = false;
                pauseMainCanvasGroup.blocksRaycasts = false;
            }

            if (pauseMainPanel != null)
                pauseMainPanel.localScale = Vector3.one;

            pausePanel.SetActive(false);
        }
    }

    private IEnumerator ShowRoutine()
    {
        SetBackdropAlpha(0f);

        if (pauseMainCanvasGroup != null)
        {
            pauseMainCanvasGroup.alpha = 0f;
            pauseMainCanvasGroup.interactable = false;
            pauseMainCanvasGroup.blocksRaycasts = false;
        }

        if (pauseMainPanel != null)
            pauseMainPanel.localScale = Vector3.one * startScale;

        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / openDuration);
            float fade = EaseOutCubic(t);
            float scale = EaseOutBackSubtle(t);

            SetBackdropAlpha(
                Mathf.Lerp(0f, targetBackdropAlpha, fade)
            );

            if (pauseMainCanvasGroup != null)
                pauseMainCanvasGroup.alpha = fade;

            if (pauseMainPanel != null)
            {
                pauseMainPanel.localScale =
                    Vector3.LerpUnclamped(
                        Vector3.one * startScale,
                        Vector3.one,
                        scale
                    );
            }

            yield return null;
        }

        SetBackdropAlpha(targetBackdropAlpha);

        if (pauseMainCanvasGroup != null)
        {
            pauseMainCanvasGroup.alpha = 1f;
            pauseMainCanvasGroup.interactable = true;
            pauseMainCanvasGroup.blocksRaycasts = true;
        }

        if (pauseMainPanel != null)
            pauseMainPanel.localScale = Vector3.one;

        routine = null;
    }

    private IEnumerator HideRoutine()
    {
        if (pauseMainCanvasGroup != null)
        {
            pauseMainCanvasGroup.interactable = false;
            pauseMainCanvasGroup.blocksRaycasts = false;
        }

        float startBackdrop =
            backdropImage != null
                ? backdropImage.color.a
                : targetBackdropAlpha;

        float startAlpha =
            pauseMainCanvasGroup != null
                ? pauseMainCanvasGroup.alpha
                : 1f;

        Vector3 currentScale =
            pauseMainPanel != null
                ? pauseMainPanel.localScale
                : Vector3.one;

        float elapsed = 0f;

        while (elapsed < closeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / closeDuration);
            float eased = EaseInCubic(t);

            SetBackdropAlpha(
                Mathf.Lerp(startBackdrop, 0f, eased)
            );

            if (pauseMainCanvasGroup != null)
            {
                pauseMainCanvasGroup.alpha =
                    Mathf.Lerp(startAlpha, 0f, eased);
            }

            if (pauseMainPanel != null)
            {
                pauseMainPanel.localScale =
                    Vector3.LerpUnclamped(
                        currentScale,
                        Vector3.one * endScale,
                        eased
                    );
            }

            yield return null;
        }

        routine = null;
        SetInstant(false);
    }

    private void SetBackdropAlpha(float alpha)
    {
        if (backdropImage == null)
            return;

        Color color = backdropImage.color;
        color.a = alpha;
        backdropImage.color = color;
    }

    private void StopCurrent()
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }

    private static float EaseOutCubic(float t)
    {
        float x = 1f - Mathf.Clamp01(t);
        return 1f - x * x * x;
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    private static float EaseOutBackSubtle(float t)
    {
        t = Mathf.Clamp01(t);

        const float c1 = 0.75f;
        const float c3 = c1 + 1f;

        float x = t - 1f;

        return 1f +
               c3 * x * x * x +
               c1 * x * x;
    }
}