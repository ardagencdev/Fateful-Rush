using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(12000)]
public sealed class LocalizedUILayoutPolish : MonoBehaviour
{
    private const float CacheRefreshInterval = 0.75f;
    private static LocalizedUILayoutPolish instance;

    private readonly List<TMP_Text> replayTexts =
        new List<TMP_Text>();

    private readonly List<TMP_Text> nextLevelTexts =
        new List<TMP_Text>();

    private readonly List<TMP_Text> confirmationTexts =
        new List<TMP_Text>();

    private readonly List<TMP_Text> pauseRestartTexts =
        new List<TMP_Text>();

    private readonly Dictionary<Transform, List<TMP_Text>>
        optionLabelGroups =
            new Dictionary<Transform, List<TMP_Text>>();

    private readonly Dictionary<TMP_Text, LabelSnapshot>
        labelSnapshots =
            new Dictionary<TMP_Text, LabelSnapshot>();

    private float cacheRefreshTimer;
    private bool typographyDirty = true;
    private int lastOptionsTextSignature;

    private sealed class LabelSnapshot
    {
        public bool captured;
        public float fontSize;
        public bool autoSizing;
        public Vector2 sizeDelta;
        public Vector2 anchoredPosition;
        public Vector2 offsetMin;
        public Vector2 offsetMax;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject root =
            new GameObject("Localized UI Layout Polish");

        DontDestroyOnLoad(root);

        instance =
            root.AddComponent<LocalizedUILayoutPolish>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += HandleSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged +=
            HandleLocaleChanged;

        RefreshCache();
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged -=
            HandleLocaleChanged;

        instance = null;
    }

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        cacheRefreshTimer = CacheRefreshInterval;
        typographyDirty = true;
        RefreshCache();
    }

    private void HandleLocaleChanged(Locale locale)
    {
        typographyDirty = true;
    }

    private void LateUpdate()
    {
        cacheRefreshTimer += Time.unscaledDeltaTime;

        if (cacheRefreshTimer >= CacheRefreshInterval)
        {
            cacheRefreshTimer = 0f;
            RefreshCache();
        }

        // Existing localization runtime refreshes localized strings regularly.
        // This runs later and only applies the intentionally shorter Turkish
        // wording to the three cramped result UI texts.
        if (IsTurkish())
            ApplyCompactTurkishResultText();

        int signature = BuildOptionsTextSignature();

        if (signature != lastOptionsTextSignature)
        {
            lastOptionsTextSignature = signature;
            typographyDirty = true;
        }

        if (typographyDirty)
        {
            typographyDirty = false;
            NormalizeOptionsTypography();
        }

        // LayoutGroups finish before this LateUpdate pass. Align after them
        // so the HUD opacity slider visually ends at the same X as the
        // right/off/60 buttons above it without changing any row layout.
        AlignHudOpacitySliders();
    }

    private void RefreshCache()
    {
        replayTexts.Clear();
        nextLevelTexts.Clear();
        confirmationTexts.Clear();
        pauseRestartTexts.Clear();
        optionLabelGroups.Clear();

        TMP_Text[] texts =
            FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include
            );

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null)
                continue;

            CacheResultText(text);
            CacheOptionLabel(text);
        }

        typographyDirty = true;
    }

    private void CacheResultText(TMP_Text text)
    {
        if (HasAncestorNamed(text.transform, "PausePanel") &&
            HasAncestorNamed(text.transform, "RestartButton"))
        {
            pauseRestartTexts.Add(text);
            return;
        }

        if (HasAncestorNamed(text.transform, "ResultsPanel"))
        {
            if (text.gameObject.name == "NextLevelText" ||
                HasAncestorNamed(
                    text.transform,
                    "NextLevelButton"
                ))
            {
                nextLevelTexts.Add(text);
                return;
            }

            if (HasAncestorNamed(
                    text.transform,
                    "RestartButton"
                ))
            {
                replayTexts.Add(text);
                return;
            }
        }

        if (text.gameObject.name == "TitleText" &&
            HasAncestorNamed(
                text.transform,
                "MenuConfirmationPanel"
            ))
        {
            confirmationTexts.Add(text);
        }
    }

    private void CacheOptionLabel(TMP_Text text)
    {
        if (!IsOptionSettingLabelName(
                text.gameObject.name))
        {
            return;
        }

        Transform optionsPanel =
            FindAncestorNamed(
                text.transform,
                "OptionsPanel"
            );

        if (optionsPanel == null)
            return;

        if (!optionLabelGroups.TryGetValue(
                optionsPanel,
                out List<TMP_Text> group))
        {
            group = new List<TMP_Text>();
            optionLabelGroups.Add(
                optionsPanel,
                group
            );
        }

        group.Add(text);
        CaptureLabelSnapshot(text);
    }

    private static bool IsOptionSettingLabelName(
        string objectName)
    {
        switch (objectName)
        {
            case "SoundText":
            case "MenuMusicText":
            case "MusicText":
            case "SFXText":
            case "JoystickText":
            case "VibrationText":
            case "FPSText":
            case "HUDOpacityText":
            case "HUDPositionText":
                return true;

            default:
                return false;
        }
    }

    private void CaptureLabelSnapshot(TMP_Text text)
    {
        if (labelSnapshots.ContainsKey(text))
            return;

        RectTransform rect =
            text.rectTransform;

        LabelSnapshot snapshot =
            new LabelSnapshot
            {
                captured = true,
                fontSize = text.fontSize,
                autoSizing = text.enableAutoSizing,
                sizeDelta = rect.sizeDelta,
                anchoredPosition =
                    rect.anchoredPosition,
                offsetMin = rect.offsetMin,
                offsetMax = rect.offsetMax,
                anchorMin = rect.anchorMin,
                anchorMax = rect.anchorMax,
                pivot = rect.pivot
            };

        labelSnapshots.Add(text, snapshot);
    }

    private void ApplyCompactTurkishResultText()
    {
        for (int i = 0; i < replayTexts.Count; i++)
        {
            SetTextIfDifferent(
                replayTexts[i],
                "TEKRAR"
            );
        }

        for (int i = 0; i < nextLevelTexts.Count; i++)
        {
            SetTextIfDifferent(
                nextLevelTexts[i],
                "DEVAM"
            );
        }

        for (int i = 0; i < confirmationTexts.Count; i++)
        {
            SetTextIfDifferent(
                confirmationTexts[i],
                "ANA MENÜYE DÖN?"
            );
        }

        for (int i = 0; i < pauseRestartTexts.Count; i++)
        {
            SetTextIfDifferent(
                pauseRestartTexts[i],
                "TEKRAR"
            );
        }
    }

    private void NormalizeOptionsTypography()
    {
        foreach (
            KeyValuePair<Transform, List<TMP_Text>>
                pair in optionLabelGroups)
        {
            NormalizeOptionGroup(pair.Value);
        }
    }

    private void NormalizeOptionGroup(
        List<TMP_Text> labels)
    {
        if (labels == null || labels.Count == 0)
            return;

        float targetFontSize = 0f;

        // Short labels such as SES / FPS / SFX preserve the intended design
        // size. Every label in the same OptionsPanel uses that same size.
        for (int i = 0; i < labels.Count; i++)
        {
            TMP_Text label = labels[i];

            if (label == null)
                continue;

            if (!labelSnapshots.TryGetValue(
                    label,
                    out LabelSnapshot snapshot))
            {
                CaptureLabelSnapshot(label);
                labelSnapshots.TryGetValue(
                    label,
                    out snapshot
                );
            }

            if (snapshot != null)
            {
                targetFontSize =
                    Mathf.Max(
                        targetFontSize,
                        snapshot.fontSize
                    );
            }
        }

        if (targetFontSize <= 0.01f)
            return;

        for (int i = 0; i < labels.Count; i++)
        {
            TMP_Text label = labels[i];

            if (label == null)
                continue;

            if (!labelSnapshots.TryGetValue(
                    label,
                    out LabelSnapshot snapshot) ||
                snapshot == null)
            {
                continue;
            }

            // Do not touch RectTransform geometry here.
            // GameScene's option rows are layout-driven; restoring cached
            // anchored positions fights the LayoutGroups and causes labels
            // and controls to jump around when the panel becomes active.
            label.enableAutoSizing = false;
            label.fontSize = targetFontSize;

            // Keep every setting name visually anchored to the same right edge.
            // Long localized strings are allowed to extend left without changing
            // the row geometry or moving the controls on the right.
            label.horizontalAlignment =
                HorizontalAlignmentOptions.Right;

            label.overflowMode =
                TextOverflowModes.Overflow;
        }
    }


    private void AlignHudOpacitySliders()
    {
        foreach (
            KeyValuePair<Transform, List<TMP_Text>>
                pair in optionLabelGroups)
        {
            Transform optionsPanel = pair.Key;

            if (optionsPanel == null ||
                !optionsPanel.gameObject.activeInHierarchy)
            {
                continue;
            }

            Slider hudSlider =
                FindHudOpacitySlider(optionsPanel);

            if (hudSlider == null)
                continue;

            RectTransform referenceRight =
                FindRightEdgeReference(optionsPanel);

            if (referenceRight == null)
                continue;

            RectTransform audioSliderReference =
                FindAudioSliderReference(optionsPanel);

            float targetWidth =
                audioSliderReference != null
                    ? audioSliderReference.rect.width
                    : (hudSlider.transform as RectTransform).rect.width;

            RectTransform hudSliderRect =
                hudSlider.transform as RectTransform;

            SetWidthAndAlignRightEdge(
                hudSliderRect,
                referenceRight,
                targetWidth
            );

            TMP_Text hudLabel =
                FindOptionLabel(
                    pair.Value,
                    "HUDOpacityText"
                );

            float referenceGap =
                CalculateStandardGameRowGap(
                    optionsPanel,
                    pair.Value
                );

            if (hudLabel != null &&
                hudSliderRect != null &&
                referenceGap >= 0f)
            {
                AlignLabelToControlGap(
                    hudLabel.rectTransform,
                    hudSliderRect,
                    referenceGap
                );
            }
        }
    }

    private static TMP_Text FindOptionLabel(
        List<TMP_Text> labels,
        string objectName)
    {
        if (labels == null)
            return null;

        for (int i = 0; i < labels.Count; i++)
        {
            TMP_Text label = labels[i];

            if (label == null)
                continue;

            if (string.Equals(
                    label.gameObject.name,
                    objectName,
                    StringComparison.Ordinal))
            {
                return label;
            }
        }

        return null;
    }

    private static float CalculateStandardGameRowGap(
        Transform optionsPanel,
        List<TMP_Text> labels)
    {
        if (optionsPanel == null ||
            labels == null)
        {
            return -1f;
        }

        Button[] buttons =
            optionsPanel.GetComponentsInChildren<Button>(
                true
            );

        float totalGap = 0f;
        int validRows = 0;

        AddGameRowGap(
            labels,
            buttons,
            "JoystickText",
            "JoystickLeftButton",
            ref totalGap,
            ref validRows
        );

        AddGameRowGap(
            labels,
            buttons,
            "VibrationText",
            "VibrationOnButton",
            ref totalGap,
            ref validRows
        );

        AddGameRowGap(
            labels,
            buttons,
            "FPSText",
            "FPS30Button",
            ref totalGap,
            ref validRows
        );

        return validRows > 0
            ? totalGap / validRows
            : -1f;
    }

    private static void AddGameRowGap(
        List<TMP_Text> labels,
        Button[] buttons,
        string labelName,
        string buttonName,
        ref float totalGap,
        ref int validRows)
    {
        TMP_Text label =
            FindOptionLabel(
                labels,
                labelName
            );

        RectTransform buttonRect =
            FindButtonRect(
                buttons,
                buttonName
            );

        if (label == null ||
            buttonRect == null)
        {
            return;
        }

        float gap =
            GetWorldLeftX(buttonRect) -
            GetWorldRightX(
                label.rectTransform
            );

        // Ignore impossible/overlapping measurements.
        if (gap < 0f || gap > 250f)
            return;

        totalGap += gap;
        validRows++;
    }

    private static RectTransform FindButtonRect(
        Button[] buttons,
        string objectName)
    {
        if (buttons == null)
            return null;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            if (string.Equals(
                    button.gameObject.name,
                    objectName,
                    StringComparison.Ordinal))
            {
                return button.transform
                    as RectTransform;
            }
        }

        return null;
    }

    private static void AlignLabelToControlGap(
        RectTransform label,
        RectTransform control,
        float desiredWorldGap)
    {
        if (label == null ||
            control == null ||
            label.parent == null)
        {
            return;
        }

        Vector3[] labelCorners =
            new Vector3[4];

        Vector3[] controlCorners =
            new Vector3[4];

        label.GetWorldCorners(
            labelCorners
        );

        control.GetWorldCorners(
            controlCorners
        );

        Vector3 currentRightWorld =
            (labelCorners[2] +
             labelCorners[3]) * 0.5f;

        Vector3 controlLeftWorld =
            (controlCorners[0] +
             controlCorners[1]) * 0.5f;

        float desiredRightX =
            controlLeftWorld.x -
            Mathf.Max(
                0f,
                desiredWorldGap
            );

        RectTransform parent =
            label.parent as RectTransform;

        if (parent == null)
            return;

        float currentRightLocal =
            parent.InverseTransformPoint(
                currentRightWorld
            ).x;

        Vector3 desiredRightWorld =
            currentRightWorld;

        desiredRightWorld.x =
            desiredRightX;

        float desiredRightLocal =
            parent.InverseTransformPoint(
                desiredRightWorld
            ).x;

        float shift =
            desiredRightLocal -
            currentRightLocal;

        if (Mathf.Abs(shift) < 0.01f)
            return;

        Vector2 position =
            label.anchoredPosition;

        position.x += shift;

        label.anchoredPosition =
            position;
    }

    private static Slider FindHudOpacitySlider(
        Transform optionsPanel)
    {
        Slider[] sliders =
            optionsPanel.GetComponentsInChildren<Slider>(
                true
            );

        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];

            if (slider == null)
                continue;

            if (HasAncestorNamed(
                    slider.transform,
                    "HUDOpacityContainer") ||
                HasAncestorNamed(
                    slider.transform,
                    "HUDOpacityRow"))
            {
                return slider;
            }
        }

        return null;
    }

    private static RectTransform FindAudioSliderReference(
        Transform optionsPanel)
    {
        Slider[] sliders =
            optionsPanel.GetComponentsInChildren<Slider>(
                true
            );

        RectTransform best = null;
        float bestWidth = 0f;

        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];

            if (slider == null)
                continue;

            if (HasAncestorNamed(
                    slider.transform,
                    "HUDOpacityContainer") ||
                HasAncestorNamed(
                    slider.transform,
                    "HUDOpacityRow"))
            {
                continue;
            }

            bool isAudioSlider =
                HasAncestorNamed(
                    slider.transform,
                    "AudioSettingsPanel") ||
                HasAncestorNamed(
                    slider.transform,
                    "AudioContent");

            if (!isAudioSlider)
                continue;

            RectTransform rect =
                slider.transform as RectTransform;

            if (rect == null)
                continue;

            float width =
                rect.rect.width;

            if (width > bestWidth)
            {
                bestWidth = width;
                best = rect;
            }
        }

        return best;
    }

    private static RectTransform FindRightEdgeReference(
        Transform optionsPanel)
    {
        Button[] buttons =
            optionsPanel.GetComponentsInChildren<Button>(
                true
            );

        RectTransform best = null;
        float bestRight = float.NegativeInfinity;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null ||
                !button.gameObject.activeInHierarchy)
            {
                continue;
            }

            string name = button.gameObject.name;

            bool isSecondControl =
                string.Equals(
                    name,
                    "VibrationOffButton",
                    StringComparison.Ordinal) ||
                string.Equals(
                    name,
                    "FPS60Button",
                    StringComparison.Ordinal) ||
                string.Equals(
                    name,
                    "JoystickRightButton",
                    StringComparison.Ordinal);

            if (!isSecondControl)
                continue;

            RectTransform rect =
                button.transform as RectTransform;

            if (rect == null)
                continue;

            float right =
                GetWorldRightX(rect);

            if (right > bestRight)
            {
                bestRight = right;
                best = rect;
            }
        }

        return best;
    }

    private static void SetWidthAndAlignRightEdge(
        RectTransform target,
        RectTransform reference,
        float targetWidth)
    {
        if (target == null ||
            reference == null ||
            target.parent == null)
        {
            return;
        }

        targetWidth =
            Mathf.Max(
                20f,
                targetWidth
            );

        RectTransform parent =
            target.parent as RectTransform;

        if (parent == null)
            return;

        // Match MUSIC / SFX slider width exactly.
        float currentWidth =
            target.rect.width;

        float widthDelta =
            targetWidth -
            currentWidth;

        Vector2 position =
            target.anchoredPosition;

        target.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            targetWidth
        );

        // Preserve the left edge while applying the new width first.
        position.x +=
            widthDelta *
            target.pivot.x;

        target.anchoredPosition =
            position;

        // Then shift the whole slider so its right edge matches
        // RIGHT / OFF / 60.
        Vector3[] targetCorners =
            new Vector3[4];

        Vector3[] referenceCorners =
            new Vector3[4];

        target.GetWorldCorners(
            targetCorners
        );

        reference.GetWorldCorners(
            referenceCorners
        );

        Vector3 targetRightWorld =
            (targetCorners[2] +
             targetCorners[3]) * 0.5f;

        Vector3 referenceRightWorld =
            (referenceCorners[2] +
             referenceCorners[3]) * 0.5f;

        float targetRightLocal =
            parent.InverseTransformPoint(
                targetRightWorld
            ).x;

        float referenceRightLocal =
            parent.InverseTransformPoint(
                referenceRightWorld
            ).x;

        float shift =
            referenceRightLocal -
            targetRightLocal;

        Vector2 alignedPosition =
            target.anchoredPosition;

        alignedPosition.x +=
            shift;

        target.anchoredPosition =
            alignedPosition;
    }

    private static float GetWorldLeftX(
        RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        return Mathf.Min(
            corners[0].x,
            corners[1].x
        );
    }

    private static float GetWorldRightX(
        RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        return Mathf.Max(
            corners[2].x,
            corners[3].x
        );
    }

    private int BuildOptionsTextSignature()
    {
        unchecked
        {
            int hash = 17;

            foreach (
                KeyValuePair<
                    Transform,
                    List<TMP_Text>>
                    pair in optionLabelGroups)
            {
                List<TMP_Text> labels =
                    pair.Value;

                for (int i = 0; i < labels.Count; i++)
                {
                    TMP_Text label = labels[i];

                    if (label == null)
                        continue;

                    hash =
                        hash * 31 +
                        (label.gameObject.name != null
                            ? label.gameObject.name.GetHashCode()
                            : 0);

                    hash =
                        hash * 31 +
                        (label.text != null
                            ? label.text.GetHashCode()
                            : 0);
                }
            }

            return hash;
        }
    }

    private static bool IsTurkish()
    {
        Locale locale =
            LocalizationSettings.SelectedLocale;

        if (locale == null)
            return false;

        string code =
            locale.Identifier.Code;

        return !string.IsNullOrWhiteSpace(code) &&
               code.StartsWith(
                   "tr",
                   StringComparison.OrdinalIgnoreCase
               );
    }

    private static void SetTextIfDifferent(
        TMP_Text text,
        string value)
    {
        if (text == null ||
            string.Equals(
                text.text,
                value,
                StringComparison.Ordinal))
        {
            return;
        }

        text.text = value;
    }

    private static bool HasAncestorNamed(
        Transform start,
        string objectName)
    {
        return
            FindAncestorNamed(
                start,
                objectName
            ) != null;
    }

    private static Transform FindAncestorNamed(
        Transform start,
        string objectName)
    {
        Transform current = start;

        while (current != null)
        {
            if (string.Equals(
                    current.gameObject.name,
                    objectName,
                    StringComparison.Ordinal))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }
}
