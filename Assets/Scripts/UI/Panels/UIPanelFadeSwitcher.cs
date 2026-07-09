using System.Collections;
using UnityEngine;

public class UIPanelFadeSwitcher : MonoBehaviour
{
    [Header("Fade")]
    public float fadeDuration = 0.18f;
    public float startScale = 0.92f;

    private Coroutine routine;

    public void SwitchPanel(GameObject fromPanel, GameObject toPanel)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(SwitchRoutine(fromPanel, toPanel));
    }

    public void ShowPanel(GameObject panel)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ShowRoutine(panel));
    }

    public void HidePanel(GameObject panel)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(HidePanelRoutine(panel));
    }

    public IEnumerator HidePanelRoutine(GameObject panel)
    {
        yield return HideRoutine(panel);
        routine = null;
    }

    public void SetInstant(GameObject panel, bool state)
    {
        if (panel == null) return;

        CanvasGroup cg = GetCanvasGroup(panel);

        panel.SetActive(state);
        panel.transform.localScale = Vector3.one;

        cg.alpha = state ? 1f : 0f;
        cg.interactable = state;
        cg.blocksRaycasts = state;
    }

    private IEnumerator SwitchRoutine(GameObject fromPanel, GameObject toPanel)
    {
        if (fromPanel == toPanel)
        {
            routine = null;
            yield break;
        }

        if (fromPanel != null && fromPanel.activeSelf)
            yield return HideRoutine(fromPanel);

        if (toPanel != null)
            yield return ShowRoutine(toPanel);

        routine = null;
    }

    private IEnumerator ShowRoutine(GameObject panel)
    {
        if (panel == null) yield break;

        CanvasGroup cg = GetCanvasGroup(panel);

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        panel.transform.localScale = Vector3.one * startScale;

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        float timer = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            cg.alpha = t;
            panel.transform.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one, t);

            yield return null;
        }

        cg.alpha = 1f;
        panel.transform.localScale = Vector3.one;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private IEnumerator HideRoutine(GameObject panel)
    {
        if (panel == null) yield break;

        CanvasGroup cg = GetCanvasGroup(panel);

        cg.interactable = false;
        cg.blocksRaycasts = false;

        float timer = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        float startAlpha = cg.alpha;
        Vector3 startLocalScale = panel.transform.localScale;
        Vector3 endScale = Vector3.one * startScale;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            cg.alpha = Mathf.Lerp(startAlpha, 0f, t);
            panel.transform.localScale = Vector3.Lerp(startLocalScale, endScale, t);

            yield return null;
        }

        cg.alpha = 0f;
        panel.transform.localScale = Vector3.one;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        panel.SetActive(false);
    }

    private CanvasGroup GetCanvasGroup(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();

        if (cg == null)
            cg = panel.AddComponent<CanvasGroup>();

        return cg;
    }
}
