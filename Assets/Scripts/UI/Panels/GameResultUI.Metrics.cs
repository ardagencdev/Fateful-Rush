using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Same Unity component. Inspector data and lifecycle entry points remain in GameResultUI.cs.
public partial class GameResultUI
{
    private void ApplyMetricVisibility(bool won)
    {
        LevelConfig currentLevel = GetCurrentLevel();

        bool isSurviveTime =
            currentLevel != null &&
            currentLevel.winCondition ==
            WinConditionType.SurviveTime;

        bool showScore = !isSurviveTime;

        SetMetricVisible(
            winScoreValue,
            winUI,
            showScore,
            "SCORE"
        );

        SetMetricVisible(
            loseScoreValue,
            loseUI,
            showScore,
            "SCORE"
        );

        RestoreTimeMetricLabels();
        RestoreTimeMetricColors();

        if (isSurviveTime)
        {
            if (won)
            {
                ApplySurviveWinMetric();
            }
            else
            {
                ApplySurviveLoseMetric();
            }
        }
        else
        {
            SetTimeMetricVisibility(
                winTimeValue,
                winTimeLabel,
                true
            );

            SetTimeMetricVisibility(
                loseSurvivedValue,
                loseSurvivedLabel,
                true
            );
        }

        ApplyMetricLayout(currentLevel);
    }

    private void ApplySurviveWinMetric()
    {
        if (winTimeLabel != null)
            winTimeLabel.gameObject.SetActive(false);

        if (winTimeValue != null)
        {
            winTimeValue.gameObject.SetActive(true);
            winTimeValue.text = "YOU SURVIVED";
            SetMetricColor(winTimeValue, SurviveWinColor);
        }
    }

    private void ApplySurviveLoseMetric()
    {
        if (loseSurvivedLabel != null)
        {
            loseSurvivedLabel.text = "YOU SURVIVED FOR";
            loseSurvivedLabel.gameObject.SetActive(true);
            SetMetricColor(loseSurvivedLabel, SurviveLoseColor);
        }

        if (loseSurvivedValue != null)
        {
            loseSurvivedValue.gameObject.SetActive(true);
            SetMetricColor(loseSurvivedValue, SurviveLoseColor);
        }
    }

    private static void SetTimeMetricVisibility(
        TextMeshProUGUI valueText,
        TextMeshProUGUI labelText,
        bool visible)
    {
        if (valueText != null)
            valueText.gameObject.SetActive(visible);

        if (labelText != null)
            labelText.gameObject.SetActive(visible);
    }

    private void RestoreTimeMetricLabels()
    {
        if (winTimeLabel != null &&
            !string.IsNullOrEmpty(winTimeLabelDefaultText))
        {
            winTimeLabel.text = winTimeLabelDefaultText;
        }

        if (loseSurvivedLabel != null &&
            !string.IsNullOrEmpty(loseSurvivedLabelDefaultText))
        {
            loseSurvivedLabel.text =
                loseSurvivedLabelDefaultText;
        }
    }

    private void RestoreTimeMetricColors()
    {
        if (winTimeLabel != null)
            winTimeLabel.color = winTimeLabelDefaultColor;

        if (winTimeValue != null)
            winTimeValue.color = winTimeValueDefaultColor;

        if (loseSurvivedLabel != null)
            loseSurvivedLabel.color = loseSurvivedLabelDefaultColor;

        if (loseSurvivedValue != null)
            loseSurvivedValue.color = loseSurvivedValueDefaultColor;
    }

    private static void SetMetricColor(
        TextMeshProUGUI text,
        Color targetColor)
    {
        if (text == null)
            return;

        Color currentColor = text.color;
        targetColor.a = currentColor.a;
        text.color = targetColor;
    }

    private void CacheMetricLayout()
    {
        winTimeValueRect =
            winTimeValue != null
                ? winTimeValue.rectTransform
                : null;

        loseSurvivedValueRect =
            loseSurvivedValue != null
                ? loseSurvivedValue.rectTransform
                : null;

        winTimeLabel =
            FindMetricLabel(
                winTimeValue,
                winUI,
                "TIME",
                "SURVIVED"
            );

        loseSurvivedLabel =
            FindMetricLabel(
                loseSurvivedValue,
                loseUI,
                "SURVIVED",
                "TIME"
            );

        winTimeLabelRect =
            winTimeLabel != null
                ? winTimeLabel.rectTransform
                : null;

        loseSurvivedLabelRect =
            loseSurvivedLabel != null
                ? loseSurvivedLabel.rectTransform
                : null;

        winTimeLabelDefaultText =
            winTimeLabel != null
                ? winTimeLabel.text
                : string.Empty;

        loseSurvivedLabelDefaultText =
            loseSurvivedLabel != null
                ? loseSurvivedLabel.text
                : string.Empty;

        winTimeLabelDefaultColor =
            winTimeLabel != null
                ? winTimeLabel.color
                : Color.white;

        winTimeValueDefaultColor =
            winTimeValue != null
                ? winTimeValue.color
                : Color.white;

        loseSurvivedLabelDefaultColor =
            loseSurvivedLabel != null
                ? loseSurvivedLabel.color
                : Color.white;

        loseSurvivedValueDefaultColor =
            loseSurvivedValue != null
                ? loseSurvivedValue.color
                : Color.white;

        if (winTimeLabelRect != null)
        {
            winTimeLabelDefaultPosition =
                winTimeLabelRect.anchoredPosition;
        }

        if (winTimeValueRect != null)
        {
            winTimeValueDefaultPosition =
                winTimeValueRect.anchoredPosition;
        }

        if (loseSurvivedLabelRect != null)
        {
            loseSurvivedLabelDefaultPosition =
                loseSurvivedLabelRect.anchoredPosition;
        }

        if (loseSurvivedValueRect != null)
        {
            loseSurvivedValueDefaultPosition =
                loseSurvivedValueRect.anchoredPosition;
        }

        metricLayoutCached = true;
    }

    private void ApplyMetricLayout(
        LevelConfig currentLevel)
    {
        if (!metricLayoutCached)
        {
            CacheMetricLayout();
        }

        bool centerTime =
            currentLevel != null &&
            currentLevel.winCondition ==
            WinConditionType.SurviveTime;

        SetMetricHorizontalPosition(
            winTimeLabelRect,
            winTimeLabelDefaultPosition,
            centerTime
        );

        SetMetricHorizontalPosition(
            winTimeValueRect,
            winTimeValueDefaultPosition,
            centerTime
        );

        SetMetricHorizontalPosition(
            loseSurvivedLabelRect,
            loseSurvivedLabelDefaultPosition,
            centerTime
        );

        SetMetricHorizontalPosition(
            loseSurvivedValueRect,
            loseSurvivedValueDefaultPosition,
            centerTime
        );
    }

    private static void SetMetricHorizontalPosition(
        RectTransform rect,
        Vector2 defaultPosition,
        bool centered)
    {
        if (rect == null)
            return;

        Vector2 position = defaultPosition;

        if (centered)
        {
            position.x = 0f;
        }

        rect.anchoredPosition = position;
    }

    private static TextMeshProUGUI FindMetricLabel(
        TextMeshProUGUI valueText,
        GameObject uiGroup,
        params string[] labelKeywords)
    {
        if (valueText == null)
            return null;

        Transform parent = valueText.transform.parent;

        if (parent == null)
            return null;

        TextMeshProUGUI[] siblingTexts =
            parent.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < siblingTexts.Length; i++)
        {
            TextMeshProUGUI text = siblingTexts[i];

            if (text == null || text == valueText)
                continue;

            if (uiGroup != null &&
                !text.transform.IsChildOf(uiGroup.transform))
            {
                continue;
            }

            if (MatchesAnyMetricKeyword(
                    text,
                    labelKeywords))
            {
                return text;
            }
        }

        return null;
    }

    private static void SetMetricVisible(
        TextMeshProUGUI valueText,
        GameObject uiGroup,
        bool visible,
        params string[] labelKeywords
    )
    {
        if (valueText == null)
            return;

        valueText.gameObject.SetActive(visible);

        Transform parent = valueText.transform.parent;

        if (parent == null)
            return;

        // Result rows in the existing UI use a label + value under the same parent.
        // Hide only matching labels so we never risk disabling the whole result group.
        TextMeshProUGUI[] siblingTexts =
            parent.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < siblingTexts.Length; i++)
        {
            TextMeshProUGUI text = siblingTexts[i];

            if (text == null || text == valueText)
                continue;

            if (uiGroup != null &&
                !text.transform.IsChildOf(uiGroup.transform))
            {
                continue;
            }

            if (!MatchesAnyMetricKeyword(
                    text,
                    labelKeywords))
            {
                continue;
            }

            text.gameObject.SetActive(visible);
        }
    }

    private static bool MatchesAnyMetricKeyword(
        TextMeshProUGUI text,
        string[] keywords
    )
    {
        if (text == null ||
            keywords == null ||
            keywords.Length == 0)
        {
            return false;
        }

        string objectName =
            text.gameObject.name.ToUpperInvariant();

        string visibleText =
            string.IsNullOrWhiteSpace(text.text)
                ? string.Empty
                : text.text
                    .Trim()
                    .TrimEnd(':')
                    .ToUpperInvariant();

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];

            if (string.IsNullOrWhiteSpace(keyword))
                continue;

            keyword = keyword.ToUpperInvariant();

            if (visibleText.Contains(keyword) ||
                objectName.Contains(keyword))
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatTime(float time)
    {
        return
            Mathf.Max(0f, time)
                .ToString("F1") +
            " s";
    }
}
