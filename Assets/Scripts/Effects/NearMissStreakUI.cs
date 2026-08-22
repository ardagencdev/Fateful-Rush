using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class NearMissStreakUI : MonoBehaviour
{
    private static NearMissStreakUI instance;
    private static TMP_FontAsset cachedNearMissFont;
    private static bool fontLookupAttempted;

    [Header("Colors")]
    [SerializeField]
    private Color firstColor = Color.white;

    [SerializeField]
    private Color maxColor = new Color(1f, 0.08f, 0.04f, 1f);

    [Header("Streak Visual")]
    [SerializeField, Min(2)]
    private int maxVisualStreak = 6;

    [SerializeField, Min(1f)]
    private float minPunchScale = 1.14f;

    [SerializeField, Min(1f)]
    private float maxPunchScale = 1.36f;

    [SerializeField, Min(0f)]
    private float minShakePixels = 1.5f;

    [SerializeField, Min(0f)]
    private float maxShakePixels = 7f;

    [SerializeField, Min(0f)]
    private float maxTiltDegrees = 3.5f;

    [SerializeField, Min(0.01f)]
    private float impactDuration = 0.18f;

    [Header("Disappear")]
    [SerializeField, Min(0.01f)]
    private float fadeDuration = 0.22f;

    private TextMeshProUGUI text;
    private RectTransform rectTransform;
    private Coroutine activeRoutine;

    private Vector2 baseAnchoredPosition;
    private Vector3 baseScale;
    private Quaternion baseRotation;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();

        ApplyNearMissFont(text);
        CaptureBaseTransform();
        SetVisible(false);

        if (instance == null)
            instance = this;
    }

    private void Update()
    {
        if (GameStateManager.IsGameplayEnded)
        {
            HideImmediately();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void OnDisable()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        ResetTransform();
    }

    public static void ShowNearMiss(
        int streak,
        float closeness01)
    {
        NearMissStreakUI ui = GetOrCreate();

        if (ui == null)
            return;

        ui.Play(
            Mathf.Max(1, streak),
            Mathf.Clamp01(closeness01)
        );
    }

    private static NearMissStreakUI GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance =
            Object.FindAnyObjectByType<NearMissStreakUI>();

        if (instance != null)
            return instance;

        return CreateRuntimeUI();
    }

    private static NearMissStreakUI CreateRuntimeUI()
    {
        ComboUI comboUI =
            Object.FindAnyObjectByType<ComboUI>();

        Canvas canvas = null;

        if (comboUI != null)
            canvas = comboUI.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Canvas[] canvases =
                Object.FindObjectsByType<Canvas>();

            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null &&
                    canvases[i].isActiveAndEnabled)
                {
                    canvas = canvases[i];
                    break;
                }
            }
        }

        if (canvas == null)
        {
            Debug.LogWarning(
                "NearMissStreakUI could not find an active HUD Canvas."
            );

            return null;
        }

        GameObject go = new GameObject(
            "NearMissStreakUI",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI),
            typeof(NearMissStreakUI)
        );

        RectTransform rect =
            go.GetComponent<RectTransform>();

        rect.SetParent(canvas.transform, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(520f, 70f);
        rect.anchoredPosition = new Vector2(0f, -92f);

        TextMeshProUGUI tmp =
            go.GetComponent<TextMeshProUGUI>();

        tmp.SetText("NEAR MISS  x1");
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.fontSize = 14f;
        tmp.color = new Color(1f, 1f, 1f, 0f);

        if (comboUI != null &&
            comboUI.comboText != null)
        {
            // Keep the existing sizing/style relationship with Combo UI,
            // but the actual font is always Orbitron.
            tmp.fontSize = Mathf.Max(
                14f,
                comboUI.comboText.fontSize * 0.65f
            );
            tmp.fontStyle = comboUI.comboText.fontStyle;
            tmp.characterSpacing =
                comboUI.comboText.characterSpacing;
        }

        ApplyNearMissFont(tmp);

        NearMissStreakUI ui =
            go.GetComponent<NearMissStreakUI>();

        ui.text = tmp;
        ui.rectTransform = rect;
        ui.CaptureBaseTransform();
        ui.SetVisible(false);

        instance = ui;
        return ui;
    }

    private static void ApplyNearMissFont(TextMeshProUGUI targetText)
    {
        if (targetText == null)
            return;

        TMP_FontAsset orbitron = ResolveNearMissFont();

        if (orbitron != null)
            targetText.font = orbitron;
    }

    private static TMP_FontAsset ResolveNearMissFont()
    {
        if (cachedNearMissFont != null)
            return cachedNearMissFont;

        if (!fontLookupAttempted)
        {
            fontLookupAttempted = true;

            NearMissUISettings settings =
                Resources.Load<NearMissUISettings>(
                    "FatefulRush/NearMissUISettings"
                );

            if (settings != null)
                cachedNearMissFont = settings.NearMissFont;
        }

        if (cachedNearMissFont != null)
            return cachedNearMissFont;

        // Safe fallback for Editor/domain-reload cases where the Resources
        // settings asset has not been imported yet.
        TMP_FontAsset[] loadedFonts =
            Resources.FindObjectsOfTypeAll<TMP_FontAsset>();

        for (int i = 0; i < loadedFonts.Length; i++)
        {
            TMP_FontAsset font = loadedFonts[i];

            if (font == null || string.IsNullOrEmpty(font.name))
                continue;

            if (font.name.IndexOf(
                    "Orbitron",
                    System.StringComparison.OrdinalIgnoreCase
                ) >= 0)
            {
                cachedNearMissFont = font;
                break;
            }
        }

        return cachedNearMissFont;
    }

    private void Play(
        int streak,
        float closeness01)
    {
        if (!isActiveAndEnabled ||
            text == null ||
            rectTransform == null)
        {
            return;
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        ResetTransform();

        text.SetText(
            "NEAR MISS  x{0}",
            streak
        );

        float streak01 = Mathf.InverseLerp(
            1f,
            Mathf.Max(2, maxVisualStreak),
            Mathf.Clamp(
                streak,
                1,
                Mathf.Max(2, maxVisualStreak)
            )
        );

        Color targetColor =
            Color.Lerp(
                firstColor,
                maxColor,
                streak01
            );

        text.color =
            SetAlpha(targetColor, 1f);

        activeRoutine = StartCoroutine(
            ImpactAndFadeRoutine(
                streak01,
                closeness01,
                targetColor
            )
        );
    }

    private IEnumerator ImpactAndFadeRoutine(
        float streak01,
        float closeness01,
        Color targetColor)
    {
        float closenessFactor =
            Mathf.Lerp(0.85f, 1f, closeness01);

        float punchScale =
            Mathf.Lerp(
                minPunchScale,
                maxPunchScale,
                streak01
            ) *
            Mathf.Lerp(0.96f, 1f, closeness01);

        float shakePixels =
            Mathf.Lerp(
                minShakePixels,
                maxShakePixels,
                streak01
            ) *
            closenessFactor;

        float tilt =
            maxTiltDegrees *
            streak01 *
            closenessFactor;

        float elapsed = 0f;

        while (elapsed < impactDuration)
        {
            if (Time.timeScale <= 0f)
            {
                yield return null;
                continue;
            }

            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / impactDuration
            );

            float impact = 1f - progress;

            rectTransform.localScale =
                baseScale *
                Mathf.Lerp(
                    1f,
                    punchScale,
                    impact
                );

            rectTransform.anchoredPosition =
                baseAnchoredPosition +
                Random.insideUnitCircle *
                shakePixels *
                impact;

            rectTransform.localRotation =
                baseRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    Random.Range(-tilt, tilt) *
                    impact
                );

            yield return null;
        }

        ResetTransform();
        text.color = SetAlpha(targetColor, 1f);

        float holdDuration = Mathf.Max(
            0f,
            NearMissFeedback.StreakTimeout -
            impactDuration
        );

        float holdElapsed = 0f;

        while (holdElapsed < holdDuration)
        {
            if (Time.timeScale > 0f)
                holdElapsed += Time.unscaledDeltaTime;

            yield return null;
        }

        float fadeElapsed = 0f;

        while (fadeElapsed < fadeDuration)
        {
            if (Time.timeScale <= 0f)
            {
                yield return null;
                continue;
            }

            fadeElapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                fadeElapsed / fadeDuration
            );

            text.color = SetAlpha(
                targetColor,
                1f - progress
            );

            rectTransform.localScale =
                baseScale *
                Mathf.Lerp(1f, 0.92f, progress);

            yield return null;
        }

        SetVisible(false);
        ResetTransform();
        activeRoutine = null;
    }

    private void CaptureBaseTransform()
    {
        if (rectTransform == null)
            return;

        baseAnchoredPosition =
            rectTransform.anchoredPosition;

        baseScale =
            rectTransform.localScale;

        baseRotation =
            rectTransform.localRotation;
    }

    private void ResetTransform()
    {
        if (rectTransform == null)
            return;

        rectTransform.anchoredPosition =
            baseAnchoredPosition;

        rectTransform.localScale =
            baseScale;

        rectTransform.localRotation =
            baseRotation;
    }

    private void SetVisible(bool visible)
    {
        if (text == null)
            return;

        Color color = text.color;
        color.a = visible ? 1f : 0f;
        text.color = color;
    }

    private static Color SetAlpha(
        Color color,
        float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }

    private void HideImmediately()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        SetVisible(false);
        ResetTransform();
    }

    private void OnValidate()
    {
        maxVisualStreak =
            Mathf.Max(2, maxVisualStreak);

        minPunchScale =
            Mathf.Max(1f, minPunchScale);

        maxPunchScale =
            Mathf.Max(minPunchScale, maxPunchScale);

        minShakePixels =
            Mathf.Max(0f, minShakePixels);

        maxShakePixels =
            Mathf.Max(minShakePixels, maxShakePixels);

        maxTiltDegrees =
            Mathf.Max(0f, maxTiltDegrees);

        impactDuration =
            Mathf.Max(0.01f, impactDuration);

        fadeDuration =
            Mathf.Max(0.01f, fadeDuration);
    }
}
