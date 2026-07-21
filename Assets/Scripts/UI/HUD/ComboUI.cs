using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComboUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI comboText;

    [Header("Colors")]
    public Color combo1Color = Color.gray;
    public Color combo2Color = new Color(1f, 0.78f, 0.1f);
    public Color combo3Color = new Color(1f, 0.15f, 0.1f);

    [Header("Pulse")]
    [Min(0f)]
    public float pulseScale = 1.25f;

    [Min(0f)]
    public float pulseDuration = 0.12f;

    [Header("Max Combo")]
    [Min(0f)]
    public float maxComboShakeDuration = 0.18f;

    [Min(0f)]
    public float maxComboShakeAmount = 8f;

    [Min(0f)]
    public float maxComboScale = 1.4f;

    [Header("Timer Bar")]
    public Image comboTimerBar;
    public Color timerFullColor = Color.green;
    public Color timerLowColor = Color.red;

    [Header("Reset")]
    [Min(0f)]
    public float resetFadeDuration = 0.25f;

    private Coroutine activeRoutine;

    private Vector3 originalScale;
    private Vector3 originalPosition;

    private bool timerBarVisible;

    private void Awake()
    {
        RefreshReferences();

        if (comboText == null)
        {
            Debug.LogError(
                "ComboUI could not find a TextMeshProUGUI component.",
                this
            );

            enabled = false;
            return;
        }

        originalScale = comboText.transform.localScale;
        originalPosition = comboText.transform.localPosition;

        UpdateCombo(1);

        if (comboTimerBar != null)
        {
            comboTimerBar.fillAmount = 0f;
            timerBarVisible = comboTimerBar.gameObject.activeSelf;
            SetTimerBarVisible(false);
        }
    }

    private void OnDisable()
    {
        StopActiveRoutine();
        ResetTextTransform();

        if (comboTimerBar != null)
        {
            comboTimerBar.fillAmount = 0f;
            SetTimerBarVisible(false);
        }
    }

    public void ShowCombo(int gainedScore, int combo)
    {
        if (!isActiveAndEnabled || comboText == null)
            return;

        UpdateCombo(combo);

        StopActiveRoutine();
        ResetTextTransform();

        activeRoutine = combo >= 3
            ? StartCoroutine(MaxComboRoutine())
            : StartCoroutine(PulseRoutine());
    }

    public void UpdateTimerBar(float normalizedTime, int combo)
    {
        if (!isActiveAndEnabled || comboTimerBar == null)
            return;

        normalizedTime = Mathf.Clamp01(normalizedTime);

        bool shouldShow =
            combo > 1 &&
            normalizedTime > 0f;

        SetTimerBarVisible(shouldShow);

        if (!shouldShow)
        {
            comboTimerBar.fillAmount = 0f;
            return;
        }

        comboTimerBar.fillAmount = normalizedTime;

        comboTimerBar.color = Color.Lerp(
            timerLowColor,
            timerFullColor,
            normalizedTime
        );
    }

    public void UpdateCombo(int combo)
    {
        if (comboText == null)
            return;

        combo = Mathf.Max(1, combo);

        if (!comboText.gameObject.activeSelf)
            comboText.gameObject.SetActive(true);

        comboText.SetText("x{0}", combo);
        comboText.color = GetComboColor(combo);
    }

    public void ResetCombo()
    {
        if (!isActiveAndEnabled || comboText == null)
            return;

        StopActiveRoutine();
        ResetTextTransform();

        activeRoutine = StartCoroutine(ResetRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        if (pulseDuration <= 0f)
        {
            ResetTextTransform();
            activeRoutine = null;
            yield break;
        }

        Vector3 targetScale =
            originalScale * pulseScale;

        yield return AnimateScale(
            originalScale,
            targetScale,
            pulseDuration
        );

        yield return AnimateScale(
            targetScale,
            originalScale,
            pulseDuration
        );

        ResetTextTransform();
        activeRoutine = null;
    }

    private IEnumerator MaxComboRoutine()
    {
        if (maxComboShakeDuration <= 0f)
        {
            ResetTextTransform();
            activeRoutine = null;
            yield break;
        }

        Vector3 targetScale =
            originalScale * maxComboScale;

        float elapsedTime = 0f;

        while (elapsedTime < maxComboShakeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / maxComboShakeDuration
            );

            comboText.transform.localScale = Vector3.Lerp(
                originalScale,
                targetScale,
                progress
            );

            Vector2 shakeOffset =
                Random.insideUnitCircle * maxComboShakeAmount;

            comboText.transform.localPosition =
                originalPosition + (Vector3)shakeOffset;

            yield return null;
        }

        elapsedTime = 0f;
        Vector3 returnStartPosition =
            comboText.transform.localPosition;

        while (elapsedTime < maxComboShakeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / maxComboShakeDuration
            );

            comboText.transform.localScale = Vector3.Lerp(
                targetScale,
                originalScale,
                progress
            );

            comboText.transform.localPosition = Vector3.Lerp(
                returnStartPosition,
                originalPosition,
                progress
            );

            yield return null;
        }

        ResetTextTransform();
        activeRoutine = null;
    }

    private IEnumerator ResetRoutine()
    {
        if (resetFadeDuration <= 0f)
        {
            UpdateCombo(1);
            ResetTextTransform();

            activeRoutine = null;
            yield break;
        }

        Color startColor = comboText.color;
        float elapsedTime = 0f;

        while (elapsedTime < resetFadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / resetFadeDuration
            );

            comboText.color = Color.Lerp(
                startColor,
                combo1Color,
                progress
            );

            yield return null;
        }

        UpdateCombo(1);
        ResetTextTransform();

        activeRoutine = null;
    }

    private IEnumerator AnimateScale(
        Vector3 startScale,
        Vector3 endScale,
        float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / duration
            );

            comboText.transform.localScale = Vector3.Lerp(
                startScale,
                endScale,
                progress
            );

            yield return null;
        }

        comboText.transform.localScale = endScale;
    }

    private void SetTimerBarVisible(bool visible)
    {
        if (comboTimerBar == null)
            return;

        timerBarVisible = visible;

        if (comboTimerBar.gameObject.activeSelf != visible)
            comboTimerBar.gameObject.SetActive(visible);
    }

    private void StopActiveRoutine()
    {
        if (activeRoutine == null)
            return;

        StopCoroutine(activeRoutine);
        activeRoutine = null;
    }

    private void ResetTextTransform()
    {
        if (comboText == null)
            return;

        comboText.transform.localScale = originalScale;
        comboText.transform.localPosition = originalPosition;
    }

    private void RefreshReferences()
    {
        if (comboText == null)
            comboText = GetComponent<TextMeshProUGUI>();
    }

    private Color GetComboColor(int combo)
    {
        if (combo >= 3)
            return combo3Color;

        if (combo == 2)
            return combo2Color;

        return combo1Color;
    }

    private void OnValidate()
    {
        pulseScale = Mathf.Max(0f, pulseScale);
        pulseDuration = Mathf.Max(0f, pulseDuration);

        maxComboShakeDuration =
            Mathf.Max(0f, maxComboShakeDuration);

        maxComboShakeAmount =
            Mathf.Max(0f, maxComboShakeAmount);

        maxComboScale =
            Mathf.Max(0f, maxComboScale);

        resetFadeDuration =
            Mathf.Max(0f, resetFadeDuration);
    }
}