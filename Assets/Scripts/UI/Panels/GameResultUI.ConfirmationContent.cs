using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameResultUI
{
    private sealed class ConfirmationSourceState
    {
        public Transform transform;
        public CanvasGroup group;
        public bool active;
        public float alpha;
        public Vector3 scale;
    }

    private readonly List<ConfirmationSourceState> confirmationSourceStates =
        new List<ConfirmationSourceState>();

    private void CaptureConfirmationSource()
    {
        // Keep the original state through interrupted open/close transitions.
        if (confirmationSourceStates.Count != 0)
            return;

        if (menuConfirmationOpenedFromPause)
        {
            Transform content = GetGameQuit()?.PauseConfirmationContent;
            if (content != null)
                CaptureConfirmationSourceItem(content);
            return;
        }

        if (resultPanel == null)
            return;

        // The modal has already moved to its own Canvas. Keep the ResultsPanel
        // background and its DarkOverlay; hide only its content children.
        foreach (Transform child in resultPanel.transform)
        {
            if (child.name == "DarkOverlay" || child.name == "BlackOverlay" ||
                child.name == "FullscreenOverlay" ||
                (menuConfirmationPanel != null && child == menuConfirmationPanel.transform))
                continue;
            CaptureConfirmationSourceItem(child);
        }
    }

    private void CaptureConfirmationSourceItem(Transform content)
    {
        CanvasGroup group = content.GetComponent<CanvasGroup>();
        if (group == null)
            group = content.gameObject.AddComponent<CanvasGroup>();
        confirmationSourceStates.Add(new ConfirmationSourceState
        {
            transform = content,
            group = group,
            active = content.gameObject.activeSelf,
            alpha = group.alpha,
            scale = content.localScale
        });
    }

    private IEnumerator AnimateConfirmationSource(bool hide)
    {
        if (hide)
            CaptureConfirmationSource();
        if (confirmationSourceStates.Count == 0)
            yield break;

        float[] startAlphas = new float[confirmationSourceStates.Count];
        Vector3[] startScales = new Vector3[confirmationSourceStates.Count];
        for (int i = 0; i < confirmationSourceStates.Count; i++)
        {
            ConfirmationSourceState state = confirmationSourceStates[i];
            if (state.transform == null || state.group == null)
                continue;
            if (!hide && state.active && !state.transform.gameObject.activeSelf)
            {
                // Set the starting pose before enabling to avoid a one-frame flash.
                state.group.alpha = 0f;
                state.transform.localScale = state.scale * 1.018f;
                state.transform.gameObject.SetActive(true);
            }
            startAlphas[i] = state.group.alpha;
            startScales[i] = state.transform.localScale;
        }

        const float duration = 0.28f; // MainMenu full-panel switch timing.
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = hide
                ? (t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f)
                : 1f - Mathf.Pow(1f - t, 3f);
            for (int i = 0; i < confirmationSourceStates.Count; i++)
            {
                ConfirmationSourceState state = confirmationSourceStates[i];
                if (state.transform == null || state.group == null || !state.active)
                    continue;
                state.group.alpha = Mathf.Lerp(startAlphas[i], hide ? 0f : state.alpha, eased);
                state.transform.localScale = Vector3.LerpUnclamped(startScales[i],
                    hide ? state.scale * 0.985f : state.scale, eased);
            }
            yield return null;
        }

        if (hide)
        {
            foreach (ConfirmationSourceState state in confirmationSourceStates)
            {
                if (state.transform == null || state.group == null)
                    continue;
                state.group.alpha = 0f;
                state.transform.gameObject.SetActive(false);
            }
        }
        else
        {
            RestoreConfirmationSourceImmediate();
        }
    }

    private void RestoreConfirmationSourceImmediate()
    {
        foreach (ConfirmationSourceState state in confirmationSourceStates)
        {
            if (state.transform == null || state.group == null)
                continue;
            state.group.alpha = state.alpha;
            state.transform.localScale = state.scale;
            state.transform.gameObject.SetActive(state.active);
        }
        confirmationSourceStates.Clear();
    }
}
