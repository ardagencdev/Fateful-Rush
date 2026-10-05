using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Same Unity component. Inspector data and lifecycle entry points remain in GameResultUI.cs.
public partial class GameResultUI
{
    private void PrepareResultIntroUI()
    {
        if (resultPanel == null)
            return;

        if (resultPanelCanvasGroup == null)
        {
            resultPanelCanvasGroup =
                resultPanel.GetComponent<CanvasGroup>();

            if (resultPanelCanvasGroup == null)
            {
                resultPanelCanvasGroup =
                    resultPanel.AddComponent<CanvasGroup>();
            }
        }

        CacheResultScales();
    }

    private void CacheResultScales()
    {
        if (resultScalesCached)
            return;

        if (winUI != null)
        {
            winUIRestScale =
                winUI.transform.localScale;

            if (winUIRestScale == Vector3.zero)
                winUIRestScale = Vector3.one;
        }

        if (loseUI != null)
        {
            loseUIRestScale =
                loseUI.transform.localScale;

            if (loseUIRestScale == Vector3.zero)
                loseUIRestScale = Vector3.one;
        }

        resultScalesCached = true;
    }

    private void StartResultIntro(bool won)
    {
        StopResultIntro();
        PrepareResultIntroUI();

        resultIntroRoutine =
            StartCoroutine(
                ResultIntroRoutine(won)
            );
    }

    private IEnumerator ResultIntroRoutine(bool won)
    {
        if (resultPanel == null)
        {
            resultIntroRoutine = null;
            yield break;
        }

        PrepareResultIntroUI();

        GameObject content =
            won ? winUI : loseUI;

        Vector3 restScale =
            won
                ? winUIRestScale
                : loseUIRestScale;

        float duration =
            Mathf.Max(
                0.05f,
                resultIntroDuration
            );

        float startScaleFactor =
            Mathf.Clamp(
                resultIntroStartScale,
                0.85f,
                1f
            );

        Vector3 startScale =
            restScale * startScaleFactor;

        if (resultPanelCanvasGroup != null)
        {
            resultPanelCanvasGroup.alpha = 0f;
            resultPanelCanvasGroup.interactable = false;
            resultPanelCanvasGroup.blocksRaycasts = false;
        }

        if (content != null)
        {
            content.transform.localScale =
                startScale;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float eased =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            if (resultPanelCanvasGroup != null)
            {
                resultPanelCanvasGroup.alpha =
                    eased;
            }

            if (content != null)
            {
                content.transform.localScale =
                    Vector3.LerpUnclamped(
                        startScale,
                        restScale,
                        eased
                    );
            }

            yield return null;
        }

        if (resultPanelCanvasGroup != null)
        {
            resultPanelCanvasGroup.alpha = 1f;
            resultPanelCanvasGroup.interactable = true;
            resultPanelCanvasGroup.blocksRaycasts = true;
        }

        if (content != null)
        {
            content.transform.localScale =
                restScale;
        }

        resultIntroRoutine = null;
    }

    private void StopResultIntro()
    {
        if (resultIntroRoutine == null)
            return;

        StopCoroutine(resultIntroRoutine);
        resultIntroRoutine = null;
    }

    private void StartResultButtonsIntro(bool won)
    {
        StopResultButtonsIntro();

        Canvas.ForceUpdateCanvases();

        ResultButtonIntroState[] states = won
            ? new[]
            {
                PrepareResultButtonIntroState(
                    nextLevelButton,
                    nextLevelButtonIntroState
                ),
                PrepareResultButtonIntroState(
                    tryAgainButton,
                    tryAgainButtonIntroState
                ),
                PrepareResultButtonIntroState(
                    menuButton,
                    menuButtonIntroState
                )
            }
            : new[]
            {
                PrepareResultButtonIntroState(
                    tryAgainButton,
                    tryAgainButtonIntroState
                ),
                PrepareResultButtonIntroState(
                    menuButton,
                    menuButtonIntroState
                )
            };

        // Görünür olmayan butonları (ör. son level'daki Next Level)
        // animasyon sırasına hiç alma.
        int activeCount = 0;

        for (int i = 0; i < states.Length; i++)
        {
            ResultButtonIntroState state = states[i];

            if (state == null ||
                state.gameObject == null ||
                !state.gameObject.activeInHierarchy)
            {
                continue;
            }

            states[activeCount] = state;
            activeCount++;
        }

        if (activeCount == 0)
            return;

        ResultButtonIntroState[] activeStates =
            new ResultButtonIntroState[activeCount];

        for (int i = 0; i < activeCount; i++)
        {
            activeStates[i] = states[i];
            ApplyResultButtonHiddenState(activeStates[i]);
        }

        resultButtonsIntroRoutine =
            StartCoroutine(
                ResultButtonsIntroRoutine(activeStates)
            );
    }

    private ResultButtonIntroState PrepareResultButtonIntroState(
        GameObject buttonObject,
        ResultButtonIntroState state)
    {
        if (state == null)
            return null;

        if (buttonObject == null)
        {
            state.gameObject = null;
            state.rect = null;
            state.canvasGroup = null;
            state.button = null;
            state.cached = false;
            return state;
        }

        if (state.gameObject != buttonObject)
        {
            state.gameObject = buttonObject;
            state.rect =
                buttonObject.GetComponent<RectTransform>();

            state.canvasGroup =
                buttonObject.GetComponent<CanvasGroup>();

            if (state.canvasGroup == null)
            {
                state.canvasGroup =
                    buttonObject.AddComponent<CanvasGroup>();
            }

            state.button =
                buttonObject.GetComponent<Button>();

            if (state.button == null)
            {
                state.button =
                    buttonObject.GetComponentInChildren<Button>(true);
            }

            state.cached = false;
        }

        if (!state.cached && state.rect != null)
        {
            state.restPosition =
                state.rect.anchoredPosition;

            state.restScale =
                state.rect.localScale;

            if (state.restScale == Vector3.zero)
                state.restScale = Vector3.one;

            state.cached = true;
        }

        return state;
    }

    private void ApplyResultButtonHiddenState(
        ResultButtonIntroState state)
    {
        if (state == null ||
            state.gameObject == null ||
            !state.gameObject.activeInHierarchy)
        {
            return;
        }

        if (state.rect != null && state.cached)
        {
            state.rect.anchoredPosition =
                state.restPosition +
                Vector2.down *
                Mathf.Max(0f, resultButtonSlideDistance);

            state.rect.localScale =
                state.restScale *
                Mathf.Clamp(
                    resultButtonStartScale,
                    0.85f,
                    1f
                );
        }

        if (state.canvasGroup != null)
        {
            state.canvasGroup.alpha = 0f;
            state.canvasGroup.interactable = false;
            state.canvasGroup.blocksRaycasts = false;
        }

        if (state.button != null)
            state.button.interactable = false;
    }

    private IEnumerator ResultButtonsIntroRoutine(
        ResultButtonIntroState[] states)
    {
        if (states == null || states.Length == 0)
        {
            resultButtonsIntroRoutine = null;
            yield break;
        }

        float baseDelay =
            Mathf.Max(0.05f, resultIntroDuration) +
            Mathf.Max(0f, resultButtonStartDelay);

        if (baseDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                baseDelay
            );
        }

        float duration =
            Mathf.Max(
                0.05f,
                resultButtonAnimationDuration
            );

        float stagger =
            Mathf.Max(
                0f,
                resultButtonStagger
            );

        float totalDuration =
            duration +
            stagger *
            Mathf.Max(0, states.Length - 1);

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            for (int i = 0; i < states.Length; i++)
            {
                ResultButtonIntroState state = states[i];

                if (state == null ||
                    state.gameObject == null ||
                    !state.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float localElapsed =
                    elapsed - i * stagger;

                float progress =
                    Mathf.Clamp01(
                        localElapsed / duration
                    );

                float eased =
                    EaseOutCubic(progress);

                if (state.canvasGroup != null)
                {
                    state.canvasGroup.alpha =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            progress
                        );
                }

                if (state.rect != null && state.cached)
                {
                    Vector2 startPosition =
                        state.restPosition +
                        Vector2.down *
                        Mathf.Max(
                            0f,
                            resultButtonSlideDistance
                        );

                    Vector3 startScale =
                        state.restScale *
                        Mathf.Clamp(
                            resultButtonStartScale,
                            0.85f,
                            1f
                        );

                    state.rect.anchoredPosition =
                        Vector2.LerpUnclamped(
                            startPosition,
                            state.restPosition,
                            eased
                        );

                    state.rect.localScale =
                        Vector3.LerpUnclamped(
                            startScale,
                            state.restScale,
                            eased
                        );
                }

                bool finished =
                    progress >= 1f;

                if (state.canvasGroup != null)
                {
                    state.canvasGroup.interactable =
                        finished;

                    state.canvasGroup.blocksRaycasts =
                        finished;
                }

                if (state.button != null)
                {
                    state.button.interactable =
                        finished;
                }
            }

            yield return null;
        }

        for (int i = 0; i < states.Length; i++)
        {
            RestoreResultButtonState(states[i]);
        }

        resultButtonsIntroRoutine = null;
    }

    private static float EaseOutCubic(float value)
    {
        value = Mathf.Clamp01(value);
        float inverse = 1f - value;
        return 1f - inverse * inverse * inverse;
    }

    private void StopResultButtonsIntro()
    {
        if (resultButtonsIntroRoutine == null)
            return;

        StopCoroutine(resultButtonsIntroRoutine);
        resultButtonsIntroRoutine = null;
    }

    private void RestoreResultButtonsState()
    {
        RestoreResultButtonState(nextLevelButtonIntroState);
        RestoreResultButtonState(tryAgainButtonIntroState);
        RestoreResultButtonState(menuButtonIntroState);
    }

    private static void RestoreResultButtonState(
        ResultButtonIntroState state)
    {
        if (state == null || state.gameObject == null)
            return;

        if (state.rect != null && state.cached)
        {
            state.rect.anchoredPosition =
                state.restPosition;

            state.rect.localScale =
                state.restScale;
        }

        if (state.canvasGroup != null)
        {
            state.canvasGroup.alpha = 1f;
            state.canvasGroup.interactable = true;
            state.canvasGroup.blocksRaycasts = true;
        }

        if (state.button != null)
            state.button.interactable = true;
    }

    private void RestoreResultIntroState()
    {
        PrepareResultIntroUI();

        if (resultPanelCanvasGroup != null)
        {
            resultPanelCanvasGroup.alpha = 1f;
            resultPanelCanvasGroup.interactable = true;
            resultPanelCanvasGroup.blocksRaycasts = true;
        }

        if (winUI != null)
        {
            winUI.transform.localScale =
                winUIRestScale;
        }

        if (loseUI != null)
        {
            loseUI.transform.localScale =
                loseUIRestScale;
        }
    }

    private void StartResultEdgeGlow(bool won)
    {
        HideResultEdgeGlowImmediate();

        if (resultEdgeGlow == null)
            return;

        Color glowColor = won ? winEdgeGlowColor : loseEdgeGlowColor;
        glowColor.a = 0f;

        PrepareResultEdgeGlowMaterial();
        SetResultEdgeGlowOpacity(glowColor, 0f);
        resultEdgeGlow.raycastTarget = false;
        resultEdgeGlow.gameObject.SetActive(true);
        resultEdgeGlowRoutine = StartCoroutine(AnimateResultEdgeGlow(glowColor));
    }

    private IEnumerator AnimateResultEdgeGlow(Color glowColor)
    {
        // Result panelinin kendi intro animasyonu bitsin, ardından ayrıca
        // ayarlanan süre kadar bekle. Bu sırada glow tamamen görünmez kalır.
        float initialDelay =
            Mathf.Max(0f, resultIntroDuration) +
            Mathf.Max(0f, edgeGlowStartDelay);

        if (initialDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                initialDelay
            );
        }

        if (resultEdgeGlow == null)
        {
            resultEdgeGlowRoutine = null;
            yield break;
        }

        float minAlpha =
            Mathf.Clamp01(
                Mathf.Min(
                    edgeGlowMinAlpha,
                    edgeGlowMaxAlpha
                )
            );

        float maxAlpha =
            Mathf.Clamp01(
                Mathf.Max(
                    edgeGlowMinAlpha,
                    edgeGlowMaxAlpha
                )
            );

        float fadeDuration = Mathf.Max(0.5f, edgeGlowFadeInDuration);
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(fadeElapsed / fadeDuration);
            float smoothT = t * t * t * (t * (t * 6f - 15f) + 10f);
            SetResultEdgeGlowOpacity(glowColor, maxAlpha * smoothT);
            yield return null;
        }
        SetResultEdgeGlowOpacity(glowColor, maxAlpha);

        float phase = 0f;
        float duration = Mathf.Max(0.25f, edgeGlowBreathDuration);
        while (resultEdgeGlow != null)
        {
            phase = Mathf.Repeat(phase + Time.unscaledDeltaTime / duration, 1f);
            // One sinusoid: flattening it again with SmootherStep concentrates
            // most changes into a short steep interval, which looks stepped.
            float pulse = (Mathf.Cos(phase * Mathf.PI * 2f) + 1f) * 0.5f;
            SetResultEdgeGlowOpacity(glowColor, Mathf.Lerp(minAlpha, maxAlpha, pulse));
            yield return null;
        }
        resultEdgeGlowRoutine = null;
    }

    private void PrepareResultEdgeGlowMaterial()
    {
        if (resultEdgeGlowMaterial == null)
        {
            Shader shader = Resources.Load<Shader>("ResultEdgeGlow/ResultEdgeGlow");
            if (shader == null || !shader.isSupported) return;
            resultEdgeGlowMaterial = new Material(shader) { name = "Result rounded edge glow" };
        }
        resultEdgeGlow.overrideSprite = null;
        resultEdgeGlow.sprite = null;
        resultEdgeGlow.type = Image.Type.Simple;
        resultEdgeGlow.preserveAspect = false;
        resultEdgeGlow.material = resultEdgeGlowMaterial;
        ResultEdgeGlowGeometry geometry = resultEdgeGlow.GetComponent<ResultEdgeGlowGeometry>();
        if (geometry == null) geometry = resultEdgeGlow.gameObject.AddComponent<ResultEdgeGlowGeometry>();
        geometry.SetShape(edgeGlowCornerRadius, edgeGlowSoftness);
        UpdateResultEdgeGlowShape();
    }

    private void UpdateResultEdgeGlowShape()
    {
        if (resultEdgeGlow == null || resultEdgeGlowMaterial == null) return;
        Rect rect = resultEdgeGlow.rectTransform.rect;
        float radius = Mathf.Clamp(edgeGlowCornerRadius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
        float softness = Mathf.Max(1f, edgeGlowSoftness);
        if (rect == resultEdgeGlowLastRect && radius == resultEdgeGlowLastRadius &&
            softness == resultEdgeGlowLastSoftness) return;
        resultEdgeGlowLastRect = rect;
        resultEdgeGlowLastRadius = radius;
        resultEdgeGlowLastSoftness = softness;
        resultEdgeGlowMaterial.SetVector(GlowBoundsId, new Vector4(rect.center.x, rect.center.y,
            rect.width * 0.5f, rect.height * 0.5f));
        resultEdgeGlowMaterial.SetFloat(GlowRadiusId, radius);
        resultEdgeGlowMaterial.SetFloat(GlowSoftnessId, softness);
        ResultEdgeGlowGeometry geometry = resultEdgeGlow.GetComponent<ResultEdgeGlowGeometry>();
        if (geometry != null) geometry.SetShape(radius, softness);
    }

    private void SetResultEdgeGlowOpacity(Color glowColor, float opacity)
    {
        if (resultEdgeGlow == null) return;
        if (resultEdgeGlowMaterial != null)
        {
            // Keep the UI vertex alpha constant at 1. Animate a float uniform
            // rather than the Graphic Color32 channel (only 15 steps at .06).
            glowColor.a = 1f;
            if (resultEdgeGlow.color != glowColor) resultEdgeGlow.color = glowColor;
            resultEdgeGlowMaterial.SetFloat(GlowOpacityId, Mathf.Clamp01(opacity));
            UpdateResultEdgeGlowShape();
        }
        else
        {
            glowColor.a = Mathf.Clamp01(opacity);
            resultEdgeGlow.color = glowColor;
        }
    }

    private void HideResultEdgeGlowImmediate()
    {
        if (resultEdgeGlowRoutine != null)
        {
            StopCoroutine(resultEdgeGlowRoutine);
            resultEdgeGlowRoutine = null;
        }

        if (resultEdgeGlow != null)
            resultEdgeGlow.gameObject.SetActive(false);
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.18f;
        float shifted = value - 1f;

        return 1f +
               (overshoot + 1f) *
               shifted *
               shifted *
               shifted +
               overshoot *
               shifted *
               shifted;
    }
}

