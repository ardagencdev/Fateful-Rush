using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class StatsPanelUI
{
    private sealed class ConfirmationSourceState
    {
        public Transform transform;
        public CanvasGroup group;
        public bool active;
        public float alpha;
        public Vector3 scale;
        public bool interactable;
        public bool blocksRaycasts;
    }

    private readonly List<ConfirmationSourceState> confirmationSourceStates =
        new List<ConfirmationSourceState>();

    private void CaptureConfirmationSource()
    {
        // Keep the original state through interrupted open/close transitions.
        if (confirmationSourceStates.Count != 0)
            return;

        if (statsPanel == null)
            return;

        // Keep the StatsPanel background and confirmation hierarchy active.
        // Fade only siblings of the ResetConfirmationPanel.
        foreach (Transform child in statsPanel.transform)
        {
            if (child.name == "DarkOverlay" || child.name == "BlackOverlay" ||
                child.name == "FullscreenOverlay" ||
                (resetConfirmationPanel != null && child == resetConfirmationPanel.transform))
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
            scale = content.localScale,
            interactable = group.interactable,
            blocksRaycasts = group.blocksRaycasts
        });
        group.interactable = false;
        group.blocksRaycasts = false;
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
            state.group.interactable = state.interactable;
            state.group.blocksRaycasts = state.blocksRaycasts;
            state.transform.localScale = state.scale;
            state.transform.gameObject.SetActive(state.active);
        }
        confirmationSourceStates.Clear();
    }

    private Coroutine resetConfirmationRoutine;
    private bool resetConfirmationOpen;
    private bool resetScrollAfterConfirmation;
    private CanvasGroup resetConfirmationGroup;
    private Vector3 resetConfirmationRestScale;
    private bool resetConfirmationPoseCached;

    private IEnumerator ResetConfirmationTransition(bool show)
    {
        if (show)
            yield return AnimateConfirmationSource(true);
        yield return AnimateResetConfirmation(show);
        if (!show)
        {
            yield return AnimateConfirmationSource(false);
            resetConfirmationOpen = false;
            if (resetScrollAfterConfirmation)
            {
                resetScrollAfterConfirmation = false;
                ResetScrollToTop();
            }
        }
        resetConfirmationRoutine = null;
    }

    private IEnumerator AnimateResetConfirmation(bool show)
    {
        if (resetConfirmationPanel == null)
            yield break;
        if (resetConfirmationGroup == null)
        {
            resetConfirmationGroup = resetConfirmationPanel.GetComponent<CanvasGroup>();
            if (resetConfirmationGroup == null)
                resetConfirmationGroup = resetConfirmationPanel.AddComponent<CanvasGroup>();
        }
        if (!resetConfirmationPoseCached)
        {
            resetConfirmationRestScale = resetConfirmationPanel.transform.localScale;
            if (resetConfirmationRestScale == Vector3.zero)
                resetConfirmationRestScale = Vector3.one;
            resetConfirmationPoseCached = true;
        }
        if (!show && !resetConfirmationPanel.activeSelf)
            yield break;

        resetConfirmationGroup.interactable = false;
        resetConfirmationGroup.blocksRaycasts = false;
        if (show && !resetConfirmationPanel.activeSelf)
        {
            resetConfirmationGroup.alpha = 0f;
            resetConfirmationPanel.transform.localScale = resetConfirmationRestScale * 0.95f;
            UIPanelAnimation animation = resetConfirmationPanel.GetComponent<UIPanelAnimation>();
            if (animation != null)
                animation.SuppressNextEnableAnimation();
            resetConfirmationPanel.SetActive(true);
            resetConfirmationPanel.transform.SetAsLastSibling();
        }
        float startAlpha = resetConfirmationGroup.alpha;
        Vector3 startScale = resetConfirmationPanel.transform.localScale;
        Vector3 targetScale = resetConfirmationRestScale * (show ? 1f : 0.97f);
        const float duration = 0.22f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            resetConfirmationGroup.alpha = Mathf.Lerp(startAlpha, show ? 1f : 0f, eased);
            resetConfirmationPanel.transform.localScale =
                Vector3.LerpUnclamped(startScale, targetScale, eased);
            yield return null;
        }
        resetConfirmationGroup.alpha = show ? 1f : 0f;
        resetConfirmationGroup.interactable = show;
        resetConfirmationGroup.blocksRaycasts = show;
        resetConfirmationPanel.transform.localScale = resetConfirmationRestScale;
        if (!show)
            resetConfirmationPanel.SetActive(false);
    }

    private void StopResetConfirmationRoutine()
    {
        if (resetConfirmationRoutine == null)
            return;
        StopCoroutine(resetConfirmationRoutine);
        resetConfirmationRoutine = null;
    }

    private void HideResetConfirmationImmediate()
    {
        StopResetConfirmationRoutine();
        RestoreConfirmationSourceImmediate();
        resetConfirmationOpen = false;
        resetScrollAfterConfirmation = false;
        if (resetConfirmationPanel == null)
            return;
        if (resetConfirmationGroup != null)
        {
            resetConfirmationGroup.alpha = 0f;
            resetConfirmationGroup.interactable = false;
            resetConfirmationGroup.blocksRaycasts = false;
        }
        if (resetConfirmationPoseCached)
            resetConfirmationPanel.transform.localScale = resetConfirmationRestScale;
        resetConfirmationPanel.SetActive(false);
    }

    private void OnDisable()
    {
        HideResetConfirmationImmediate();
    }
}
