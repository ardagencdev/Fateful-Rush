using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Same Unity component. Inspector data and lifecycle entry points remain in GameStateManager.cs.
public partial class GameStateManager
{
    private void SetHUD(bool state)
    {
        LevelConfig level = CurrentLevel;

        if (!state || level == null)
        {
            SetHUDObject(scoreHUD, false);
            SetHUDObject(timeHUD, false);
            SetHUDObject(joystickHUD, false);
            SetHUDObject(dashHUD, false);
            SetHUDObject(cloneHUD, false);
            SetHUDObject(pauseButtonHUD, false);
            SetCurrentLevelHUDVisible(false);
            return;
        }

        SetHUDObject(
            scoreHUD,
            level.ScoreHUDEnabled
        );

        SetHUDObject(
            timeHUD,
            level.TimerHUDEnabled
        );

        SetHUDObject(joystickHUD, true);

        SetHUDObject(
            dashHUD,
            level.dashEnabled
        );

        SetHUDObject(
            cloneHUD,
            level.cloneEnabled
        );

        SetHUDObject(pauseButtonHUD, true);
        SetCurrentLevelHUDVisible(true);
    }

    private void EnsureCurrentLevelHUD(
        LevelConfig level)
    {
        if (currentLevelHUD != null ||
            level == null ||
            level.levelNumber <= 0)
        {
            return;
        }

        Canvas hudCanvas = FindHUDCanvas();

        if (hudCanvas == null)
        {
            Debug.LogWarning(
                "[GameStateManager] Current Level HUD için Canvas bulunamadı.",
                this
            );
            return;
        }

        Color appliedNearStarsColor =
            level.nearStarsColor;

        if (levelManager != null &&
            levelManager.starfieldController != null)
        {
            // StarfieldController already resolved the level color before the
            // HUD is created, so this is the exact color visible in NearStars.
            appliedNearStarsColor =
                levelManager.starfieldController.CurrentNearStarsColor;
        }

        int siblingIndex =
            GetHUDInsertSiblingIndex(hudCanvas);

        currentLevelHUD =
            CurrentLevelHUD.Create(
                level,
                appliedNearStarsColor,
                hudCanvas,
                siblingIndex
            );
    }

    private void EnsureHUDPlayerOcclusion()
    {
        if (playerMovement == null)
            return;

        Canvas hudCanvas = FindHUDCanvas();

        if (hudCanvas == null)
            return;

        if (hudPlayerOcclusion == null)
        {
            hudPlayerOcclusion =
                GetComponent<HUDPlayerOcclusionController>();

            if (hudPlayerOcclusion == null)
            {
                hudPlayerOcclusion =
                    gameObject.AddComponent
                        <HUDPlayerOcclusionController>();
            }
        }

        hudPlayerOcclusion.Configure(
            playerMovement.transform,
            hudCanvas,
            hudIntroAnimator,
            scoreHUD,
            timeHUD,
            joystickHUD,
            dashHUD,
            cloneHUD,
            pauseButtonHUD,
            currentLevelHUD != null
                ? currentLevelHUD.gameObject
                : null
        );

        // Root referanslarının yanında gerçek TMP componentlerini de doğrudan
        // kaydet. Böylece sahne hiyerarşisi değişse bile Score / Timer kesin
        // olarak aynı occlusion sistemine girer.
        if (playerCoinCollector != null &&
            playerCoinCollector.scoreText != null)
        {
            hudPlayerOcclusion.RegisterHUDRoot(
                playerCoinCollector.scoreText.gameObject
            );
        }

        if (gameTimerComponent != null &&
            gameTimerComponent.timerText != null)
        {
            hudPlayerOcclusion.RegisterHUDRoot(
                gameTimerComponent.timerText.gameObject
            );
        }

        if (currentLevelHUD != null)
        {
            hudPlayerOcclusion.RegisterHUDRoot(
                currentLevelHUD.gameObject
            );
        }
    }

    private void RegisterCurrentLevelHUDForIntro()
    {
        if (hudIntroAnimator == null ||
            currentLevelHUD == null)
        {
            return;
        }

        hudIntroAnimator.RegisterRuntimeItem(
            currentLevelHUD.gameObject
        );
    }

    private Canvas FindHUDCanvas()
    {
        GameObject[] hudObjects =
        {
            scoreHUD,
            timeHUD,
            joystickHUD,
            dashHUD,
            cloneHUD,
            pauseButtonHUD
        };

        for (int i = 0;
             i < hudObjects.Length;
             i++)
        {
            GameObject hudObject = hudObjects[i];

            if (hudObject == null)
                continue;

            Canvas canvas =
                hudObject.GetComponentInParent
                    <Canvas>(true);

            if (canvas != null)
                return canvas;
        }

        return FindAnyObjectByType<Canvas>();
    }

    private int GetHUDInsertSiblingIndex(
        Canvas hudCanvas)
    {
        if (hudCanvas == null)
            return 0;

        GameObject[] hudObjects =
        {
            scoreHUD,
            timeHUD,
            joystickHUD,
            dashHUD,
            cloneHUD,
            pauseButtonHUD
        };

        int highestHudSiblingIndex = -1;

        for (int i = 0;
             i < hudObjects.Length;
             i++)
        {
            Transform topLevelHudTransform =
                GetTopLevelChildUnderCanvas(
                    hudObjects[i],
                    hudCanvas.transform
                );

            if (topLevelHudTransform == null)
                continue;

            highestHudSiblingIndex =
                Mathf.Max(
                    highestHudSiblingIndex,
                    topLevelHudTransform.GetSiblingIndex()
                );
        }

        if (highestHudSiblingIndex < 0)
            return hudCanvas.transform.childCount;

        return Mathf.Min(
            highestHudSiblingIndex + 1,
            hudCanvas.transform.childCount
        );
    }

    private static Transform GetTopLevelChildUnderCanvas(
        GameObject hudObject,
        Transform canvasTransform)
    {
        if (hudObject == null ||
            canvasTransform == null)
        {
            return null;
        }

        Transform current = hudObject.transform;

        while (current != null &&
               current.parent != null &&
               current.parent != canvasTransform)
        {
            current = current.parent;
        }

        return current != null &&
               current.parent == canvasTransform
            ? current
            : null;
    }

    private void SetCurrentLevelHUDVisible(
        bool visible)
    {
        if (currentLevelHUD != null)
            currentLevelHUD.SetVisible(visible);
    }

    private static void SetHUDObject(
        GameObject target,
        bool state)
    {
        if (target != null)
            target.SetActive(state);
    }

    private bool ShouldAnimateHUDItem(
        GameObject target)
    {
        if (target == null)
            return false;

        LevelConfig level = CurrentLevel;

        if (level == null)
            return false;

        if (target == scoreHUD)
            return level.ScoreHUDEnabled;

        if (target == timeHUD)
            return level.TimerHUDEnabled;

        if (target == dashHUD)
            return level.dashEnabled;

        if (target == cloneHUD)
            return level.cloneEnabled;

        return true;
    }

    private void EnsureDynamicMusicTension(
        LevelConfig currentLevel)
    {
        if (currentLevel == null ||
            gameplayMusic == null)
        {
            return;
        }

        if (dynamicMusicTension == null)
        {
            dynamicMusicTension =
                GetComponent<DynamicMusicTension>();
        }

        if (dynamicMusicTension == null)
        {
            dynamicMusicTension =
                gameObject.AddComponent<DynamicMusicTension>();
        }

        dynamicMusicTension.Configure(
            this,
            playerCoinCollector,
            gameplayMusic,
            currentLevel
        );
    }
}
