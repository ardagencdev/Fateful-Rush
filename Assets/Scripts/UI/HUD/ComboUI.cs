using UnityEngine;
using TMPro;
using System.Collections;
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
    public float pulseScale = 1.25f;
    public float pulseDuration = 0.12f;

    [Header("Max Combo")]
    public float maxComboShakeDuration = 0.18f;
    public float maxComboShakeAmount = 8f;
    public float maxComboScale = 1.4f;

    [Header("Timer Bar")]
    public Image comboTimerBar;
    public Color timerFullColor = Color.green;
    public Color timerLowColor = Color.red;

    [Header("Reset")]
    public float resetFadeDuration = 0.25f;

    private Coroutine routine;

    private Vector3 originalScale;
    private Vector3 originalPos;

    private bool timerBarVisible;

    private void Awake()
    {
        if (comboText == null)
            comboText = GetComponent<TextMeshProUGUI>();

        originalScale = comboText.transform.localScale;
        originalPos = comboText.transform.localPosition;

        UpdateCombo(1);
        UpdateTimerBar(0f, 1);

        SetTimerBarVisible(false);
    }

    public void ShowCombo(int gainedScore, int combo)
    {
        if (!gameObject.activeInHierarchy)
            return;

        UpdateCombo(combo);

        if (routine != null)
            StopCoroutine(routine);

        if (combo >= 3)
            routine = StartCoroutine(MaxComboRoutine());
        else
            routine = StartCoroutine(PulseRoutine());
    }

    public void UpdateTimerBar(float normalizedTime, int combo)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (comboTimerBar == null) return;

        normalizedTime = Mathf.Clamp01(normalizedTime);

        bool shouldShow = combo > 1 && normalizedTime > 0f;

        SetTimerBarVisible(shouldShow);

        if (!shouldShow) return;

        comboTimerBar.fillAmount = normalizedTime;
        comboTimerBar.color = Color.Lerp(timerLowColor, timerFullColor, normalizedTime);
    }

    private void SetTimerBarVisible(bool state)
    {
        if (comboTimerBar == null) return;
        if (timerBarVisible == state) return;

        timerBarVisible = state;
        comboTimerBar.gameObject.SetActive(state);
    }

    public void UpdateCombo(int combo)
    {
        if (comboText == null) return;

        if (!comboText.gameObject.activeSelf)
            comboText.gameObject.SetActive(true);

        comboText.text = "x" + combo;
        comboText.color = GetComboColor(combo);
    }

    public void ResetCombo()
    {
        if (comboText == null)
            return;

        if (!gameObject.activeInHierarchy)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ResetRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        float time = 0f;

        Vector3 targetScale = originalScale * pulseScale;

        while (time < pulseDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = time / pulseDuration;

            comboText.transform.localScale =
                Vector3.Lerp(originalScale, targetScale, t);

            yield return null;
        }

        time = 0f;

        while (time < pulseDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = time / pulseDuration;

            comboText.transform.localScale =
                Vector3.Lerp(targetScale, originalScale, t);

            yield return null;
        }

        comboText.transform.localScale = originalScale;
        routine = null;
    }

    private IEnumerator MaxComboRoutine()
    {
        float time = 0f;

        Vector3 targetScale = originalScale * maxComboScale;

        while (time < maxComboShakeDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = time / maxComboShakeDuration;

            comboText.transform.localScale =
                Vector3.Lerp(originalScale, targetScale, t);

            Vector2 shakeOffset = Random.insideUnitCircle * maxComboShakeAmount;

            comboText.transform.localPosition =
                originalPos + (Vector3)shakeOffset;

            yield return null;
        }

        time = 0f;

        while (time < maxComboShakeDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = time / maxComboShakeDuration;

            comboText.transform.localScale =
                Vector3.Lerp(targetScale, originalScale, t);

            comboText.transform.localPosition =
                Vector3.Lerp(comboText.transform.localPosition, originalPos, t);

            yield return null;
        }

        comboText.transform.localScale = originalScale;
        comboText.transform.localPosition = originalPos;

        routine = null;
    }

    private IEnumerator ResetRoutine()
    {
        Color startColor = comboText.color;

        float time = 0f;

        while (time < resetFadeDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = time / resetFadeDuration;

            comboText.color =
                Color.Lerp(startColor, combo1Color, t);

            yield return null;
        }

        UpdateCombo(1);

        comboText.transform.localScale = originalScale;
        comboText.transform.localPosition = originalPos;

        routine = null;
    }

    private Color GetComboColor(int combo)
    {
        if (combo >= 3)
            return combo3Color;

        if (combo == 2)
            return combo2Color;

        return combo1Color;
    }
}