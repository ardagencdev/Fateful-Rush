using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Same Unity component. Inspector data and lifecycle entry points remain in GameResultUI.cs.
public partial class GameResultUI
{
    private void PrepareNewBestTimeUI()
    {
        // New Best Time referanslari Inspector'dan atanir.
        // Runtime'da component arama/ekleme yapilmaz.
        if (newBestTimeUI == null ||
            newBestTimeText == null ||
            newBestTimeCanvasGroup == null ||
            newBestTimeRect == null)
        {
            return;
        }

        newBestTimeText.text = FatefulRushLocalization.Text("result.new_best_time", "NEW BEST TIME");

        if (!newBestPositionCached)
        {
            newBestRestPosition =
                newBestTimeRect.anchoredPosition;

            newBestPositionCached = true;
        }
    }

    private bool IsCurrentRunBestTime(float time)
    {
        LevelConfig currentLevel = GetCurrentLevel();

        if (currentLevel == null ||
            !currentLevel.CanSaveBestTime)
        {
            return false;
        }

        if (!SelectedLevelData.IsLevelMode)
            return false;

        string bestTimeKey =
            "BestTime_Level_" +
            currentLevel.levelNumber;

        // Henuz bu level icin kayit yoksa bu ilk gecerli tamamlama
        // otomatik olarak NEW BEST TIME'dir. Kayit ShowWin'den once veya
        // sonra yapilsa da bu kontrol dogru sonucu verir.
        if (!PlayerPrefs.HasKey(bestTimeKey))
            return true;

        float savedBestTime =
            PlayerPrefs.GetFloat(
                bestTimeKey,
                Mathf.Infinity
            );

        return
            !float.IsInfinity(savedBestTime) &&
            Mathf.Abs(savedBestTime - time) <= 0.0001f;
    }

    private void UpdateNewBestTimeReward(
        bool isNewBestTime)
    {
        HideNewBestTimeImmediate();

        if (!isNewBestTime ||
            newBestTimeUI == null)
        {
            return;
        }

        PrepareNewBestTimeUI();

        if (newBestTimeText != null)
            newBestTimeText.text = FatefulRushLocalization.Text("result.new_best_time", "NEW BEST TIME");

        newBestTimeUI.SetActive(true);

        newBestTimeRoutine =
            StartCoroutine(
                AnimateNewBestTime()
            );
    }

    private IEnumerator AnimateNewBestTime()
    {
        if (newBestTimeUI == null)
            yield break;

        PrepareNewBestTimeUI();

        float effectiveDelay =
            Mathf.Max(0f, skinUnlockDelay);

        SoundManager soundManager = SoundManager.Instance;

        if (soundManager != null &&
            soundManager.WinSoundDuration > 0f)
        {
            float delayFromWinSound = Mathf.Max(
                0f,
                soundManager.WinSoundDuration -
                skinUnlockWinSoundTailOverlap
            );

            effectiveDelay = Mathf.Max(
                effectiveDelay,
                delayFromWinSound
            );
        }

        if (effectiveDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    effectiveDelay
                );
        }

        if (newBestTimeUI == null)
            yield break;

        if (!newBestPositionCached &&
            newBestTimeRect != null)
        {
            newBestRestPosition =
                newBestTimeRect.anchoredPosition;

            newBestPositionCached = true;
        }

        Vector2 startPosition =
            newBestRestPosition +
            Vector2.left *
            skinUnlockSlideDistance;

        Vector3 startScale =
            Vector3.one * 0.92f;

        if (newBestTimeRect != null)
        {
            newBestTimeRect.anchoredPosition =
                startPosition;

            newBestTimeRect.localScale =
                startScale;
        }

        if (newBestTimeCanvasGroup != null)
        {
            newBestTimeCanvasGroup.alpha = 0f;
            newBestTimeCanvasGroup.interactable = false;
            newBestTimeCanvasGroup.blocksRaycasts = false;
        }

        float duration =
            Mathf.Max(
                0.05f,
                skinUnlockAnimationDuration
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float eased =
                EaseOutBack(progress);

            if (newBestTimeRect != null)
            {
                newBestTimeRect.anchoredPosition =
                    Vector2.LerpUnclamped(
                        startPosition,
                        newBestRestPosition,
                        eased
                    );

                newBestTimeRect.localScale =
                    Vector3.LerpUnclamped(
                        startScale,
                        Vector3.one,
                        eased
                    );
            }

            if (newBestTimeCanvasGroup != null)
            {
                newBestTimeCanvasGroup.alpha =
                    Mathf.Clamp01(
                        progress / 0.65f
                    );
            }

            yield return null;
        }

        if (newBestTimeRect != null)
        {
            newBestTimeRect.anchoredPosition =
                newBestRestPosition;

            newBestTimeRect.localScale =
                Vector3.one;
        }

        if (newBestTimeCanvasGroup != null)
            newBestTimeCanvasGroup.alpha = 1f;

        newBestTimeRoutine = null;
    }

    private void HideNewBestTimeImmediate()
    {
        if (newBestTimeRoutine != null)
        {
            StopCoroutine(newBestTimeRoutine);
            newBestTimeRoutine = null;
        }

        if (newBestTimeRect != null &&
            newBestPositionCached)
        {
            newBestTimeRect.anchoredPosition =
                newBestRestPosition;

            newBestTimeRect.localScale =
                Vector3.one;
        }

        if (newBestTimeCanvasGroup != null)
        {
            newBestTimeCanvasGroup.alpha = 0f;
            newBestTimeCanvasGroup.interactable = false;
            newBestTimeCanvasGroup.blocksRaycasts = false;
        }

        if (newBestTimeUI != null)
            newBestTimeUI.SetActive(false);
    }

    private void PrepareSkinUnlockUI()
    {
        ResolveSkinUnlockCatalog();

        if (skinUnlockUI == null)
            return;

        ResolveSkinUnlockTextReferences();

        if (skinUnlockRect == null)
        {
            skinUnlockRect =
                skinUnlockUI.GetComponent<RectTransform>();
        }

        if (skinUnlockCanvasGroup == null)
        {
            skinUnlockCanvasGroup =
                skinUnlockUI.GetComponent<CanvasGroup>();

            if (skinUnlockCanvasGroup == null)
            {
                skinUnlockCanvasGroup =
                    skinUnlockUI.AddComponent<CanvasGroup>();
            }
        }

        CacheSkinUnlockRestPosition();
    }

    private void CacheSkinUnlockRestPosition()
    {
        if (skinUnlockRect == null)
            return;

        skinUnlockRestPosition =
            skinUnlockRect.anchoredPosition;

        skinUnlockPositionCached = true;
    }

    private void UpdateSkinUnlockReward(
        int completedLevelNumber,
        bool isFirstCompletion)
    {
        HideSkinUnlockImmediate();
        ResolveSkinUnlockCatalog();

        if (skinUnlockUI != null)
            ResolveSkinUnlockTextReferences();

        if (!isFirstCompletion ||
            completedLevelNumber <= 0 ||
            playerSkinCatalog == null ||
            skinUnlockUI == null)
        {
            return;
        }

        PlayerSkinCatalog.SkinEntry unlockedSkin =
            FindSkinUnlockedByLevel(
                completedLevelNumber
            );

        if (unlockedSkin == null)
            return;

        rewardSkinId = unlockedSkin.id;
        rewardSkinFallback = (string.IsNullOrWhiteSpace(unlockedSkin.displayName) ? unlockedSkin.id : unlockedSkin.displayName).ToUpperInvariant();

        if (skinUnlockedTitleText != null)
        {
            skinUnlockedTitleText.text =
                FatefulRushLocalization.Text("result.new_skin", "NEW SKIN UNLOCKED");
            skinUnlockedTitleText.color = Color.white;
        }

        if (unlockedSkinNameText != null)
        {

            unlockedSkinNameText.text = FatefulRushLocalization.SkinName(rewardSkinId, rewardSkinFallback);

            unlockedSkinNameText.color =
                PlayerSkinCatalog.GetUIThemeColor(
                    unlockedSkin
                );
        }

        PrepareSkinUnlockUI();
        skinUnlockUI.SetActive(true);

        skinUnlockRoutine =
            StartCoroutine(
                AnimateSkinUnlock()
            );
    }

    private PlayerSkinCatalog.SkinEntry
        FindSkinUnlockedByLevel(
            int completedLevelNumber)
    {
        ResolveSkinUnlockCatalog();

        return playerSkinCatalog != null
            ? playerSkinCatalog.FindSkinUnlockedAtLevel(
                completedLevelNumber
            )
            : null;
    }

    private void ResolveSkinUnlockCatalog()
    {
        if (playerSkinCatalog == null &&
            PlayerSkinCatalog.LoadedInstance != null)
        {
            playerSkinCatalog =
                PlayerSkinCatalog.LoadedInstance;
        }
    }

    private void ResolveSkinUnlockTextReferences()
    {
        if (skinUnlockUI == null ||
            (skinUnlockedTitleText != null &&
             unlockedSkinNameText != null))
        {
            return;
        }

        TextMeshProUGUI[] texts =
            skinUnlockUI.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];

            if (text == null)
                continue;

            string normalizedText =
                string.IsNullOrWhiteSpace(text.text)
                    ? string.Empty
                    : text.text
                        .Trim()
                        .ToUpperInvariant();

            if (skinUnlockedTitleText == null &&
                normalizedText.Contains("NEW SKIN UNLOCKED"))
            {
                skinUnlockedTitleText = text;
                continue;
            }

            if (unlockedSkinNameText == null &&
                IsSkinNamePlaceholder(normalizedText))
            {
                unlockedSkinNameText = text;
            }
        }

        // Fallback for prefabs where the placeholder text was renamed.
        if (unlockedSkinNameText == null)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                TextMeshProUGUI text = texts[i];

                if (text != null &&
                    text != skinUnlockedTitleText)
                {
                    unlockedSkinNameText = text;
                    break;
                }
            }
        }
    }

    private static bool IsSkinNamePlaceholder(string text)
    {
        switch (text)
        {
            case "WHITE":
            case "BLUE":
            case "ORANGE":
            case "PURPLE":
            case "GREEN":
            case "PINK":
            case "YELLOW":
            case "LIGHT BLUE":
            case "CYAN":
            case "RED":
            case "DARK":
            case "GOLD":
            case "GOLDEN":
            case "NEW SKIN":
                return true;
            default:
                return false;
        }
    }

    private IEnumerator AnimateSkinUnlock()
    {
        if (skinUnlockUI == null)
            yield break;

        PrepareSkinUnlockUI();

        float effectiveUnlockDelay =
            Mathf.Max(0f, skinUnlockDelay);

        SoundManager soundManager = SoundManager.Instance;

        if (soundManager != null &&
            soundManager.WinSoundDuration > 0f)
        {
            float delayFromWinSound = Mathf.Max(
                0f,
                soundManager.WinSoundDuration -
                skinUnlockWinSoundTailOverlap
            );

            effectiveUnlockDelay = Mathf.Max(
                effectiveUnlockDelay,
                delayFromWinSound
            );
        }

        if (effectiveUnlockDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    effectiveUnlockDelay
                );
        }

        if (skinUnlockUI == null)
            yield break;

        SoundManager.Instance?.PlayNewSkinUnlockedSound(skinUnlockRect);

        if (!skinUnlockPositionCached)
            CacheSkinUnlockRestPosition();

        Vector2 startPosition =
            skinUnlockRestPosition +
            Vector2.right *
            skinUnlockSlideDistance;

        Vector3 startScale =
            Vector3.one * 0.92f;

        if (skinUnlockRect != null)
        {
            skinUnlockRect.anchoredPosition =
                startPosition;

            skinUnlockRect.localScale =
                startScale;
        }

        if (skinUnlockCanvasGroup != null)
        {
            skinUnlockCanvasGroup.alpha = 0f;
            skinUnlockCanvasGroup.interactable = false;
            skinUnlockCanvasGroup.blocksRaycasts = false;
        }

        float duration =
            Mathf.Max(
                0.05f,
                skinUnlockAnimationDuration
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float eased =
                EaseOutBack(progress);

            if (skinUnlockRect != null)
            {
                skinUnlockRect.anchoredPosition =
                    Vector2.LerpUnclamped(
                        startPosition,
                        skinUnlockRestPosition,
                        eased
                    );

                skinUnlockRect.localScale =
                    Vector3.LerpUnclamped(
                        startScale,
                        Vector3.one,
                        eased
                    );
            }

            if (skinUnlockCanvasGroup != null)
            {
                skinUnlockCanvasGroup.alpha =
                    Mathf.Clamp01(
                        progress / 0.65f
                    );
            }

            yield return null;
        }

        if (skinUnlockRect != null)
        {
            skinUnlockRect.anchoredPosition =
                skinUnlockRestPosition;

            skinUnlockRect.localScale =
                Vector3.one;
        }

        if (skinUnlockCanvasGroup != null)
        {
            skinUnlockCanvasGroup.alpha = 1f;
        }

        skinUnlockRoutine = null;
    }

    private void HideSkinUnlockImmediate()
    {
        if (skinUnlockRoutine != null)
        {
            StopCoroutine(skinUnlockRoutine);
            skinUnlockRoutine = null;
        }

        if (skinUnlockRect != null &&
            skinUnlockPositionCached)
        {
            skinUnlockRect.anchoredPosition =
                skinUnlockRestPosition;

            skinUnlockRect.localScale =
                Vector3.one;
        }

        if (skinUnlockCanvasGroup != null)
        {
            skinUnlockCanvasGroup.alpha = 0f;
            skinUnlockCanvasGroup.interactable = false;
            skinUnlockCanvasGroup.blocksRaycasts = false;
        }

        if (skinUnlockUI != null)
        {
            skinUnlockUI.SetActive(false);
        }
    }

    private static Color GetReadableRewardColor(
        Color source)
    {
        Color result = source;
        result.a = 1f;

        float brightness =
            result.r * 0.2126f +
            result.g * 0.7152f +
            result.b * 0.0722f;

        if (brightness < 0.28f)
        {
            result = Color.Lerp(
                result,
                Color.white,
                0.5f
            );
        }

        return result;
    }

    public void RefreshLocalizedText()
    {
        if (newBestTimeText != null)
            newBestTimeText.text = FatefulRushLocalization.Text("result.new_best_time", "NEW BEST TIME").TrimEnd('!', ' ');
        if (skinUnlockedTitleText != null)
            skinUnlockedTitleText.text = FatefulRushLocalization.Text("result.new_skin", "NEW SKIN UNLOCKED").TrimEnd('!', ' ');
        if (unlockedSkinNameText != null && !string.IsNullOrWhiteSpace(rewardSkinId))
            unlockedSkinNameText.text = FatefulRushLocalization.SkinName(rewardSkinId, rewardSkinFallback);
        if (destroyedByText != null && !string.IsNullOrWhiteSpace(displayedDeathCause))
            destroyedByText.text = FatefulRushLocalization.DeathCause(displayedDeathCause);
        LevelConfig level = GetCurrentLevel();
        bool survive = level != null && level.winCondition == WinConditionType.SurviveTime;
        if (!survive)
        {
            if (winTimeLabel != null)
                winTimeLabel.text = FatefulRushLocalization.Text("scene.gamescene.game_0.ui_3.canvas_0.resultsuicontroller_7.resultspanel_0.winui_2.timelabel_4", "TIME");
            if (loseSurvivedLabel != null)
                loseSurvivedLabel.text = FatefulRushLocalization.Text("scene.gamescene.game_0.ui_3.canvas_0.resultsuicontroller_7.resultspanel_0.loseui_3.survivedlabel_6", "SURVIVED");
        }
        else
        {
            if (winUI != null && winUI.activeInHierarchy && winTimeValue != null)
                winTimeValue.text = FatefulRushLocalization.Text("result.you_survived", "YOU SURVIVED");
            if (loseUI != null && loseUI.activeInHierarchy && loseSurvivedLabel != null)
                loseSurvivedLabel.text = FatefulRushLocalization.Text("result.you_survived_for", "YOU SURVIVED FOR");
        }
    }
}
