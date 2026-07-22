using System.Collections;
using UnityEngine;

public class UIPanelFadeSwitcher : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField, Min(0.01f)]
    private float fadeDuration = 0.18f;

    [SerializeField, Range(0.01f, 1f)]
    private float startScale = 0.92f;

    private Coroutine routine;

    public void SwitchPanel(
        GameObject fromPanel,
        GameObject toPanel
    )
    {
        if (fromPanel == null &&
            toPanel == null)
        {
            return;
        }

        StartManagedRoutine(
            SwitchRoutine(
                fromPanel,
                toPanel
            )
        );
    }

    public void ShowPanel(GameObject panel)
    {
        if (panel == null)
            return;

        StartManagedRoutine(
            ShowRoutine(panel)
        );
    }

    public void HidePanel(GameObject panel)
    {
        if (panel == null)
            return;

        StartManagedRoutine(
            HideRoutine(panel)
        );
    }

    public IEnumerator HidePanelRoutine(
        GameObject panel
    )
    {
        if (panel == null)
            yield break;

        yield return HideRoutine(panel);
    }

    public void SetInstant(
        GameObject panel,
        bool state
    )
    {
        if (panel == null)
            return;

        StopCurrentRoutine();

        CanvasGroup canvasGroup =
            GetCanvasGroup(panel);

        panel.SetActive(state);
        panel.transform.localScale =
            Vector3.one;

        canvasGroup.alpha =
            state ? 1f : 0f;

        canvasGroup.interactable = state;
        canvasGroup.blocksRaycasts = state;
    }

    private void StartManagedRoutine(
        IEnumerator animation
    )
    {
        StopCurrentRoutine();

        routine =
            StartCoroutine(
                ManagedRoutine(animation)
            );
    }

    private IEnumerator ManagedRoutine(
        IEnumerator animation
    )
    {
        yield return animation;
        routine = null;
    }

    private IEnumerator SwitchRoutine(
        GameObject fromPanel,
        GameObject toPanel
    )
    {
        if (fromPanel == toPanel)
            yield break;

        if (fromPanel != null &&
            fromPanel.activeSelf)
        {
            yield return HideRoutine(fromPanel);
        }

        if (toPanel != null)
            yield return ShowRoutine(toPanel);
    }

    private IEnumerator ShowRoutine(
        GameObject panel
    )
    {
        if (panel == null)
            yield break;

        CanvasGroup canvasGroup =
            GetCanvasGroup(panel);

        Vector3 initialScale =
            Vector3.one * startScale;

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        panel.transform.localScale =
            initialScale;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float timer = 0f;
        float safeDuration =
            Mathf.Max(0.01f, fadeDuration);

        while (timer < safeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / safeDuration
                );

            float easedProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            canvasGroup.alpha =
                easedProgress;

            panel.transform.localScale =
                Vector3.LerpUnclamped(
                    initialScale,
                    Vector3.one,
                    easedProgress
                );

            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        panel.transform.localScale =
            Vector3.one;
    }

    private IEnumerator HideRoutine(
        GameObject panel
    )
    {
        if (panel == null)
            yield break;

        CanvasGroup canvasGroup =
            GetCanvasGroup(panel);

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float timer = 0f;
        float safeDuration =
            Mathf.Max(0.01f, fadeDuration);

        float initialAlpha =
            canvasGroup.alpha;

        Vector3 initialScale =
            panel.transform.localScale;

        Vector3 targetScale =
            Vector3.one * startScale;

        while (timer < safeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / safeDuration
                );

            float easedProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            canvasGroup.alpha =
                Mathf.Lerp(
                    initialAlpha,
                    0f,
                    easedProgress
                );

            panel.transform.localScale =
                Vector3.LerpUnclamped(
                    initialScale,
                    targetScale,
                    easedProgress
                );

            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        panel.transform.localScale =
            Vector3.one;

        panel.SetActive(false);
    }

    private static CanvasGroup GetCanvasGroup(
        GameObject panel
    )
    {
        CanvasGroup canvasGroup =
            panel.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                panel.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private void StopCurrentRoutine()
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }

    private void OnDisable()
    {
        StopCurrentRoutine();
    }

    private void OnValidate()
    {
        fadeDuration =
            Mathf.Max(
                0.01f,
                fadeDuration
            );

        startScale =
            Mathf.Clamp(
                startScale,
                0.01f,
                1f
            );
    }
}