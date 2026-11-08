using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Non-invasive visual add-on for Fateful Rush's existing ComboUI.
/// x4: restrained magnetic arcs; x5/x6: progressively brighter arcs.
/// Combo expiration: 0.30s scale punch / micro-shake / fade using a TMP echo.
/// Does NOT write to gameplay state or ComboUI's animated RectTransform.
/// Add to the SAME GameObject as ComboUI in GameScene.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ComboUI))]
[DefaultExecutionOrder(500)]
public sealed class ComboPolishVisual : MonoBehaviour
{
    [Header("References (auto-resolved)")]
    [SerializeField] private ComboUI comboUI;
    [SerializeField] private PlayerCoinCollector collector;

    [Header("Magnet Visual")]
    [SerializeField] private bool magnetVisualEnabled = true;
    [SerializeField, Range(8f, 50f)] private float horizontalPadding = 19f;
    [SerializeField, Range(6f, 40f)] private float verticalPadding = 13f;
    [SerializeField, Range(0.05f, 1f)] private float combo4Opacity = 0.38f;
    [SerializeField, Range(0.05f, 1f)] private float combo5Opacity = 0.57f;
    [SerializeField, Range(0.05f, 1f)] private float combo6Opacity = 0.72f;
    [SerializeField, Range(8f, 80f)] private float rotationDegreesPerSecond = 28f;
    [SerializeField, Min(0.01f)] private float ringFadeSpeed = 7.5f;

    [Header("Combo Break")]
    [SerializeField] private bool comboBreakEnabled = true;
    [SerializeField, Range(0.15f, 0.60f)] private float breakDuration = 0.30f;
    [SerializeField, Range(1.04f, 1.30f)] private float breakPunchScale = 1.14f;
    [SerializeField, Range(0f, 4f)] private float breakShakePixels = 1.4f;
    [SerializeField, Range(0f, 16f)] private float breakRisePixels = 5f;

    [Header("Editor Test (Play Mode Only)")]
    [SerializeField] private bool previewInPlayMode;
    [SerializeField, Range(1, 6)] private int previewCombo = 4;

    private TextMeshProUGUI sourceText;
    private RectTransform sourceRect;
    private RectTransform ringRect;
    private ComboMagnetRingGraphic ringGraphic;
    private CanvasGroup ringGroup;

    private GameObject ghostObject;
    private RectTransform ghostRect;
    private TextMeshProUGUI ghostText;
    private Vector2 ghostPosition;
    private Vector3 ghostScale;
    private Quaternion ghostRotation;
    private Color ghostColor;

    private int lastCombo = 1;
    private int ringStage;
    private Rect ringSourceRect;
    private bool initialized;
    private float currentRingAlpha;
    private float spin;
    private float breakElapsed = -1f;
    private int breakCombo;
    private float breakStartRingAlpha;

    private void Awake()
    {
        if (comboUI == null)
            comboUI = GetComponent<ComboUI>();
        TryInitialize();
    }

    private void OnEnable()
    {
        currentRingAlpha = 0f;
        ringStage = 0;
        CancelBreak();
        if (ringGroup != null) ringGroup.alpha = 0f;
        lastCombo = collector != null ? collector.Combo : 1;
    }

    private void TryInitialize()
    {
        if (initialized || comboUI == null || comboUI.comboText == null)
            return;

        sourceText = comboUI.comboText;
        sourceRect = sourceText.rectTransform;

        // This is a child of the text, so stage-up animations naturally carry
        // the magnet indicator without a second script fighting UI positions.
        GameObject ringObject = new GameObject("Combo Magnet Field",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(CanvasGroup), typeof(ComboMagnetRingGraphic));
        ringObject.transform.SetParent(sourceRect, false);
        ringRect = (RectTransform)ringObject.transform;
        ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringRect.pivot = new Vector2(0.5f, 0.5f);
        ringRect.anchoredPosition = Vector2.zero;
        ringGroup = ringObject.GetComponent<CanvasGroup>();
        ringGroup.alpha = 0f;
        ringGraphic = ringObject.GetComponent<ComboMagnetRingGraphic>();
        ringGraphic.raycastTarget = false;
        ringGraphic.Configure(0, Color.white);

        // One reusable TMP echo: no repeated Instantiate/Destroy when a combo breaks.
        ghostObject = new GameObject("Combo Break Echo",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        ghostObject.transform.SetParent(sourceRect.parent, false);
        ghostObject.AddComponent<LayoutElement>().ignoreLayout = true;
        ghostRect = (RectTransform)ghostObject.transform;
        ghostText = ghostObject.GetComponent<TextMeshProUGUI>();
        CopyTextStyle();
        ghostText.raycastTarget = false;
        ghostObject.SetActive(false);
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();
            if (!initialized) return;
        }

        if (collector == null)
            collector = PlayerCoinCollector.Instance;
        if (collector == null) return;

        if (Time.timeScale <= 0f) return; // Freeze visual effects on pause.
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        bool preview = Application.isEditor && previewInPlayMode;
        bool active = preview ||
            (GameStateManager.IsGameplayStarted && !GameStateManager.IsGameplayEnded);
        int combo = active ? (preview ? previewCombo : collector.Combo) : 1;
        combo = Mathf.Clamp(combo, 1, 6);

        bool magnetAllowed = magnetVisualEnabled &&
            (preview || (collector.comboEnabled && collector.comboMagnetEnabled));

        // Inspector preview drives the real TMP stage animation too, so the
        // artist can check position and ring intensity without collecting coins.
        if (preview && combo != lastCombo)
        {
            if (combo <= 1) comboUI.ResetCombo();
            else comboUI.ShowCombo(0, combo);
        }

        Rect textRect = sourceRect.rect;
        if (combo >= 4 && magnetAllowed && (combo != ringStage || textRect != ringSourceRect))
        {
            if (ResizeRingToText())
            {
                ringGraphic.Configure(combo, GetComboColor(combo));
                ringSourceRect = textRect;
                ringStage = combo;
            }
        }

        lastCombo = combo;

        AnimateRing(dt, combo, magnetAllowed && active && ringStage == combo);
        AnimateBreak(dt);
    }

    private bool ResizeRingToText()
    {
        sourceText.ForceMeshUpdate();
        TMP_TextInfo textInfo = sourceText.textInfo;
        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo glyph = textInfo.characterInfo[i];
            if (!glyph.isVisible)
                continue;

            min = Vector2.Min(min, glyph.bottomLeft);
            max = Vector2.Max(max, glyph.topRight);
        }

        if (float.IsInfinity(min.x))
            return false;

        Vector2 center = (min + max) * 0.5f;
        ringRect.localPosition = new Vector3(center.x, center.y, 0f);

        // The glyph diagonal keeps all four corners inside the circular ring.
        float spacing = Mathf.Max(horizontalPadding, verticalPadding) * 0.8f;
        float diameter = Mathf.Max(36f, (max - min).magnitude + spacing);
        ringRect.sizeDelta = new Vector2(diameter, diameter);
        ringRect.localScale = Vector3.one;
        return true;
    }

    private void AnimateRing(float dt, int combo, bool magnetAllowed)
    {
        if (ringGroup == null) return;
        float target = 0f;
        if (magnetAllowed && combo >= 4)
            target = combo >= 6 ? combo6Opacity : combo == 5 ? combo5Opacity : combo4Opacity;

        if (breakElapsed >= 0f)
        {
            float t = Mathf.Clamp01(breakElapsed / Mathf.Max(0.01f, breakDuration));
            // On combo loss the magnetic field expands slightly and evaporates.
            currentRingAlpha = breakStartRingAlpha * (1f - t) * (1f - t);
            ringRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.19f, t);
        }
        else
        {
            ringRect.localScale = Vector3.one;
        }

        if (breakElapsed < 0f)
        {
            currentRingAlpha = Mathf.MoveTowards(currentRingAlpha, target,
                ringFadeSpeed * dt * 0.22f);
        }
        float pulse = 0.94f + 0.06f * Mathf.Sin(Time.unscaledTime * 3.8f);
        ringGroup.alpha = currentRingAlpha * pulse;
        if (currentRingAlpha > 0.001f)
        {
            ringGraphic.UpdateSparks(Time.time);
            spin += rotationDegreesPerSecond * dt * (combo >= 6 ? 1.5f : combo == 5 ? 1.25f : 1f);
            if (spin > 360f) spin -= 360f;
            ringRect.localRotation = Quaternion.Euler(0f, 0f, spin);
        }
    }

    public bool TryPlayBreak(int combo)
    {
        bool preview = Application.isEditor && previewInPlayMode;
        bool active = GameStateManager.IsGameplayStarted && !GameStateManager.IsGameplayEnded;
        if (!isActiveAndEnabled || !comboBreakEnabled || combo < 2 || (!active && !preview))
            return false;

        TryInitialize();
        if (!initialized)
            return false;

        CopyTextStyle();
        ghostText.text = "x" + combo;
        ghostColor = GetComboColor(combo);
        ghostColor.a = 1f;
        ghostText.color = ghostColor;

        ghostRect.anchorMin = sourceRect.anchorMin;
        ghostRect.anchorMax = sourceRect.anchorMax;
        ghostRect.pivot = sourceRect.pivot;
        ghostRect.sizeDelta = sourceRect.sizeDelta;
        ghostRect.anchoredPosition = sourceRect.anchoredPosition;
        ghostRect.localRotation = sourceRect.localRotation;
        ghostRect.localScale = sourceRect.localScale;
        ghostPosition = ghostRect.anchoredPosition;
        ghostRotation = ghostRect.localRotation;
        ghostScale = ghostRect.localScale;
        ghostRect.SetSiblingIndex(sourceRect.GetSiblingIndex() + 1);
        ghostObject.SetActive(true);
        breakCombo = combo;
        breakStartRingAlpha = currentRingAlpha;
        breakElapsed = 0f;
        return true;
    }

    public void CancelBreak()
    {
        if (breakElapsed < 0f)
            return;

        breakElapsed = -1f;
        ghostObject.SetActive(false);
        ringRect.localScale = Vector3.one;
    }

    private void AnimateBreak(float dt)
    {
        if (breakElapsed < 0f || ghostObject == null) return;
        breakElapsed += dt;
        float t = Mathf.Clamp01(breakElapsed / Mathf.Max(0.01f, breakDuration));
        float punchPart = Mathf.Clamp01(t / 0.28f);
        float scale = t < 0.28f
            ? Mathf.Lerp(1f, breakPunchScale, EaseOutCubic(punchPart))
            : Mathf.Lerp(breakPunchScale, 0.80f,
                EaseInCubic((t - 0.28f) / 0.72f));
        float decay = 1f - t;
        float shake = Mathf.Sin(breakElapsed * 85f + breakCombo) *
                      breakShakePixels * decay;
        ghostRect.localScale = ghostScale * scale;
        ghostRect.localRotation = ghostRotation *
            Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI) * 1.8f);
        ghostRect.anchoredPosition = ghostPosition +
            new Vector2(shake, breakRisePixels * EaseOutCubic(t));
        Color c = ghostColor;
        c.a *= 1f - EaseInCubic(t);
        ghostText.color = c;
        if (t >= 1f)
            CancelBreak();
    }

    private void CopyTextStyle()
    {
        if (sourceText == null || ghostText == null) return;
        ghostText.font = sourceText.font;
        ghostText.fontSharedMaterial = sourceText.fontSharedMaterial;
        ghostText.fontSize = sourceText.fontSize;
        ghostText.fontStyle = sourceText.fontStyle;
        ghostText.fontWeight = sourceText.fontWeight;
        ghostText.alignment = sourceText.alignment;
        ghostText.enableAutoSizing = sourceText.enableAutoSizing;
        ghostText.fontSizeMin = sourceText.fontSizeMin;
        ghostText.fontSizeMax = sourceText.fontSizeMax;
        ghostText.characterSpacing = sourceText.characterSpacing;
        ghostText.margin = sourceText.margin;
        ghostText.overflowMode = sourceText.overflowMode;
    }

    private Color GetComboColor(int combo)
    {
        if (combo >= 6) return comboUI.combo6Color;
        if (combo == 5) return comboUI.combo5Color;
        if (combo == 4) return comboUI.combo4Color;
        if (combo == 3) return comboUI.combo3Color;
        return comboUI.combo2Color;
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t) * (1f - t);
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    private void OnDisable()
    {
        CancelBreak();
        currentRingAlpha = 0f;
        if (ringGroup != null) ringGroup.alpha = 0f;
    }

    private void OnValidate()
    {
        breakDuration = Mathf.Max(0.15f, breakDuration);
        ringFadeSpeed = Mathf.Max(0.01f, ringFadeSpeed);
        horizontalPadding = Mathf.Max(0f, horizontalPadding);
        verticalPadding = Mathf.Max(0f, verticalPadding);
    }
}
