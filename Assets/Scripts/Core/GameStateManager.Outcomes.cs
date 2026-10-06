using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Same Unity component. Inspector data and lifecycle entry points remain in GameStateManager.cs.
public partial class GameStateManager
{
    public void CheckScoreObjective(int currentScore)
    {
        if (!IsGameplayStarted)
            return;

        if (gameEnded)
            return;

        LevelConfig currentLevel = CurrentLevel;

        if (currentLevel == null)
            return;

        if (!currentLevel.UsesScore)
            return;

        switch (currentLevel.winCondition)
        {
            case WinConditionType.ReachScore:

                if (currentScore >=
                    currentLevel.SafeWinScore)
                {
                    WinGame(currentScore);
                }

                break;

            case WinConditionType.ReachScoreWithinTime:

                if (gameTimer >
                    currentLevel.SafeTimeLimit)
                {
                    return;
                }

                if (currentScore >=
                    currentLevel.SafeWinScore)
                {
                    WinGame(currentScore);
                }

                break;
        }
    }

    private void CheckTimeObjective()
    {
        LevelConfig currentLevel = CurrentLevel;

        if (currentLevel == null)
            return;

        if (!currentLevel.UsesTime)
            return;

        if (gameTimer < currentLevel.SafeTimeLimit)
            return;

        gameTimer = currentLevel.SafeTimeLimit;

        switch (currentLevel.winCondition)
        {
            case WinConditionType.SurviveTime:
                WinGame(CurrentScore);
                break;

            case WinConditionType.ReachScoreWithinTime:
                GameOver(
                    CurrentScore,
                    "TIME EXPIRED"
                );
                break;
        }
    }

    public void WinGame(int score)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        IsGameplayStarted = false;
        IsGameplayEnded = true;

        Time.timeScale = 1f;

        RunNonCritical(
            () => TimeSlowController.Instance?.ForceStopForGameEnd(),
            "stop slow motion on win"
        );

        RunNonCritical(
            () => GameAudioMixerController.ResetTransientState(0.12f),
            "reset audio mixer on win"
        );

        if (playerMovement != null)
            playerMovement.SetGameOver(true);

        if (playerDash != null)
            playerDash.StopDash();

        // The footer represents the result of the player's most recent
        // real numbered-level attempt. Dev Room does not affect it.
        if (SelectedLevelData.isLevelMode &&
            CurrentLevel != null)
        {
            RunNonCritical(
                SignalStatusState.MarkStable,
                "set signal footer stable after level win"
            );
        }

        int completedLevelNumber = 0;
        bool isFirstCompletion = false;
        bool isNewBestTime = false;

        if (SelectedLevelData.isLevelMode &&
            CurrentLevel != null &&
            CurrentLevel.CanSaveBestTime)
        {
            RunNonCritical(
                () =>
                {
                    isNewBestTime = SaveBestTime();
                },
                "save best time"
            );
        }

        if (levelManager != null &&
            levelManager.currentLevel != null &&
            SelectedLevelData.isLevelMode)
        {
            int levelNumber =
                levelManager.currentLevel.levelNumber;

            completedLevelNumber = levelNumber;

            // Progress persistence is important, but the win screen is more
            // important. Storage failure must never strand a completed run.
            RunNonCritical(
                () =>
                {
                    isFirstCompletion =
                        PlayerPrefs.GetInt(
                            "CompletedLevel_" + levelNumber,
                            0
                        ) == 0;

                    int unlockedLevel =
                        PlayerPrefs.GetInt(
                            "UnlockedLevel",
                            1
                        );

                    if (levelNumber >= unlockedLevel)
                    {
                        PlayerPrefs.SetInt(
                            "UnlockedLevel",
                            levelNumber + 1
                        );
                    }

                    PlayerPrefs.SetInt(
                        "CompletedLevel_" + levelNumber,
                        1
                    );

                    // Queue the end credits after the FIRST completion
                    // of Level 40. The flag stays alive until the player
                    // presses CONTINUE at the end of the Credits scene.
                    if (levelNumber == 40 &&
                        isFirstCompletion)
                    {
                        PlayerPrefs.SetInt(
                            FatefulRushCreditsController.PendingKey,
                            1
                        );
                    }

                    PlayerPrefs.Save();
                },
                "persist level completion"
            );
        }

        SetHUD(false);
        StopGameplayImmediately();

        // The result screen is core gameplay. Show it before telemetry,
        // achievements, audio and other optional subsystems so a platform SDK
        // failure can never swallow the end-of-run UI.
        EnsureGameResultUIReference();

        if (gameResultUI != null)
        {
            gameResultUI.ShowWin(
                score,
                gameTimer,
                completedLevelNumber,
                isFirstCompletion,
                isNewBestTime
            );
        }

        RunNonCritical(
            () =>
            {
                StatsManager.AddRun();
                StatsManager.AddWin();
                StatsManager.AddPlayTime(gameTimer);
                StatsManager.RecordRunDetails(
                    true,
                    score,
                    gameTimer,
                    CurrentLevel != null
                        ? CurrentLevel.winCondition
                        : WinConditionType.ReachScore,
                    playerCoinCollector != null
                        ? playerCoinCollector.CoinsCollectedThisRun
                        : 0
                );
                StatsManager.SaveIfDirty();
            },
            "record win stats"
        );

        RunNonCritical(
            () => FatefulRushGameStats.EndRun(
                true,
                score,
                gameTimer,
                playerCoinCollector != null
                    ? playerCoinCollector.CoinsCollectedThisRun
                    : 0
            ),
            "record Google Play Game Stats win event"
        );

        if (completedLevelNumber > 0)
        {
            int levelToNotify = completedLevelNumber;
            RunNonCritical(
                () => GooglePlayGamesManager.NotifyLevelCompleted(levelToNotify),
                "notify Google Play level completion"
            );
        }

        RunNonCritical(
            () => gameTimerComponent?.StopTimer(),
            "stop timer on win"
        );

        RunNonCritical(
            () => gameplayMusic?.ResetTension(true),
            "reset music tension on win"
        );

        RunNonCritical(StopMusic, "stop gameplay music on win");

        RunNonCritical(
            () => soundManager?.PlayWinSound(),
            "play win sound"
        );

        RunNonCritical(
            () => VibrationManager.Instance?.VibrateSuccess(),
            "win vibration"
        );
    }

    public void GameOver(int score)
    {
        GameOver(
            score,
            LastDeathInfo.Cause
        );
    }

    public void GameOver(
        int score,
        string cause)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        IsGameplayStarted = false;
        IsGameplayEnded = true;

        Time.timeScale = 1f;

        RunNonCritical(
            () => TimeSlowController.Instance?.ForceStopForGameEnd(),
            "stop slow motion on game over"
        );

        RunNonCritical(
            () => GameAudioMixerController.ResetTransientState(0.12f),
            "reset audio mixer on game over"
        );

        if (playerMovement != null)
            playerMovement.SetGameOver(true);

        if (playerDash != null)
            playerDash.StopDash();

        // A failed numbered-level attempt returns the Main Menu signal
        // to UNSTABLE. Dev Room failures do not affect the footer.
        if (SelectedLevelData.isLevelMode &&
            CurrentLevel != null)
        {
            RunNonCritical(
                SignalStatusState.MarkUnstable,
                "set signal footer unstable after level loss"
            );
        }

        // Both outcomes keep feedback running on unscaled time after the freeze.
        // Physical death gets a firmer impact; a time limit ends with a softer pulse.
        bool timeExpired = string.Equals(
            cause, "TIME EXPIRED", System.StringComparison.OrdinalIgnoreCase);
        RunNonCritical(
            () => CameraShake.Instance?.Shake(
                timeExpired ? 0.38f : 0.50f,
                timeExpired ? 0.22f : 0.50f),
            "loss camera shake"
        );

        SetHUD(false);
        StopGameplayImmediately();

        // Show the lose panel before any stats/achievement work. Those systems
        // are useful, but they are never allowed to block the core result flow.
        EnsureGameResultUIReference();

        if (gameResultUI != null)
        {
            gameResultUI.ShowLose(
                score,
                gameTimer,
                cause
            );
        }

        RunNonCritical(
            () =>
            {
                StatsManager.AddRun();
                StatsManager.AddDeath();
                StatsManager.AddPlayTime(gameTimer);
                StatsManager.RecordRunDetails(
                    false,
                    score,
                    gameTimer,
                    CurrentLevel != null
                        ? CurrentLevel.winCondition
                        : WinConditionType.ReachScore,
                    playerCoinCollector != null
                        ? playerCoinCollector.CoinsCollectedThisRun
                        : 0,
                    cause
                );
                StatsManager.SaveIfDirty();
            },
            "record game-over stats"
        );

        RunNonCritical(
            () => FatefulRushGameStats.EndRun(
                false,
                score,
                gameTimer,
                playerCoinCollector != null
                    ? playerCoinCollector.CoinsCollectedThisRun
                    : 0
            ),
            "record Google Play Game Stats loss event"
        );

        RunNonCritical(
            () => gameTimerComponent?.StopTimer(),
            "stop timer on game over"
        );

        RunNonCritical(
            () => gameplayMusic?.ResetTension(true),
            "reset music tension on game over"
        );

        RunNonCritical(StopMusic, "stop gameplay music on game over");

        RunNonCritical(
            () => soundManager?.PlayLoseSound(),
            "play lose sound"
        );

        RunNonCritical(
            () => VibrationManager.Instance?.VibrateFailure(),
            "lose vibration"
        );
    }

    private bool SaveBestTime()
    {
        if (!SelectedLevelData.isLevelMode ||
            levelManager == null ||
            levelManager.currentLevel == null)
        {
            return false;
        }

        string bestTimeKey =
            "BestTime_Level_" +
            levelManager.currentLevel.levelNumber;

        float bestTime =
            PlayerPrefs.GetFloat(
                bestTimeKey,
                Mathf.Infinity
            );

        if (gameTimer >= bestTime)
            return false;

        PlayerPrefs.SetFloat(
            bestTimeKey,
            gameTimer
        );

        if (levelManager != null &&
            levelManager.currentLevel != null)
        {
            GooglePlayGamesLeaderboards.SubmitBestTime(
                levelManager.currentLevel.levelNumber,
                gameTimer
            );
        }

        return true;
    }

    private void StopGameplayImmediately()
    {
        if (gameFrozen)
            return;

        gameFrozen = true;

        // Time scale is the authoritative freeze. Everything below is cleanup
        // and must never be able to prevent the result UI from appearing.
        Time.timeScale = 0f;

        RunNonCritical(StopLaserSystems, "stop laser systems");
        RunNonCritical(StopBossAoeSystems, "stop boss AOE systems");
        RunNonCritical(StopActiveGameplayAudio, "stop gameplay audio");
        RunNonCritical(FreezeActiveRigidbodies, "zero active rigidbody velocity");
    }

    private void RunNonCritical(
        System.Action action,
        string operation)
    {
        if (action == null)
            return;

        try
        {
            action.Invoke();
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "[GameStateManager] Non-critical operation failed: " +
                operation +
                ". Core game state will continue safely.",
                this
            );
            Debug.LogException(exception, this);
            FatefulRushFirebaseServices.RecordNonFatal(
                exception,
                operation
            );
        }
    }

    private static void StopBossAoeSystems()
    {
        BossEnemyFollow[] bosses =
            UnityFindCompat.FindObjectsByType<BossEnemyFollow>(
                FindObjectsInactive.Exclude
            );

        for (int i = 0; i < bosses.Length; i++)
        {
            if (bosses[i] != null)
                bosses[i].StopForGameEnd();
        }

        MiniBossFollow[] miniBosses =
            UnityFindCompat.FindObjectsByType<MiniBossFollow>(
                FindObjectsInactive.Exclude
            );

        for (int i = 0; i < miniBosses.Length; i++)
        {
            if (miniBosses[i] != null)
                miniBosses[i].StopForGameEnd();
        }
    }

    private void StopActiveGameplayAudio()
    {
        soundManager?.StopAllSfx();

        // Hunter/projectile/laser gibi kendi AudioSource'unu kullanan gameplay
        // objeleri de sonuç ekranından sonra ses üretmeye devam etmesin.
        AudioSource[] activeSources =
            UnityFindCompat.FindObjectsByType<AudioSource>(
                FindObjectsInactive.Exclude
            );

        for (int i = 0; i < activeSources.Length; i++)
        {
            AudioSource source = activeSources[i];

            if (source == null || !source.isPlaying)
                continue;

            // Music owns its unscaled result fade; stop only gameplay SFX here.
            if (gameplayMusic != null && source.GetComponent<GameplayMusicFade>() == gameplayMusic)
                continue;

            // A lethal Space Bomb explosion is intentionally allowed to
            // finish after the gameplay freeze. Everything else is stopped.
            if (source.GetComponentInParent<GameEndPersistentAudio>() != null)
                continue;

            source.Stop();
        }
    }

    private static void FreezeActiveRigidbodies()
    {
        Rigidbody2D[] bodies =
            UnityFindCompat.FindObjectsByType<Rigidbody2D>(
                FindObjectsInactive.Exclude
            );

        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody2D body = bodies[i];

            if (body == null)
                continue;

            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;

            // Time.timeScale is already 0 on the result screen. Keeping
            // simulation enabled avoids leaking a disabled Rigidbody2D state
            // into restart/scene-transition edge cases on mobile.
        }
    }

    private void StopLaserSystems()
    {
        if (laserWallSpawner != null)
        {
            laserWallSpawner
                .StopLaserSystem();
        }

        if (horizontalLaserWallSpawner != null)
        {
            horizontalLaserWallSpawner
                .StopLaserSystem();
        }
    }

    private void StopMusic()
    {
        gameplayMusic?.FadeOutForResult(1.1f);
    }

    public void RestartGame()
    {
        IsGameplayStarted = false;
        IsGameplayEnded = false;
        Time.timeScale = 0f;

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance
                .LoadSceneWithFade(
                    SceneManager
                        .GetActiveScene()
                        .name
                );
        }
        else
        {
            SceneManager.LoadScene(
                SceneManager
                    .GetActiveScene()
                    .buildIndex
            );
        }
    }
}
