using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Same Unity component. Inspector data and lifecycle entry points remain in GameResultUI.cs.
public partial class GameResultUI
{
    private LevelConfig GetCurrentLevel()
    {
        return levelManager != null
            ? levelManager.currentLevel
            : null;
    }

    private void UpdateNextLevelButton()
    {
        if (nextLevelButton == null)
            return;

        LevelConfig currentLevel =
            GetCurrentLevel();

        bool hasNextLevel =
            SelectedLevelData.IsLevelMode &&
            currentLevel != null &&
            GetNextLevel(currentLevel) != null;

        nextLevelButton.SetActive(
            hasNextLevel
        );
    }

    private LevelConfig GetNextLevel(
        LevelConfig currentLevel)
    {
        if (currentLevel == null ||
            levels == null ||
            levels.Length == 0)
        {
            return null;
        }

        int nextLevelNumber =
            currentLevel.levelNumber + 1;

        foreach (LevelConfig level in levels)
        {
            if (level != null &&
                level.levelNumber ==
                nextLevelNumber)
            {
                return level;
            }
        }

        return null;
    }

    public void NextLevel()
    {
        if (!TryBeginSceneChange())
            return;

        PrepareForSceneChange();

        LevelConfig currentLevel =
            levelManager != null
                ? levelManager.currentLevel
                : null;

        LevelConfig nextLevel =
            GetNextLevel(currentLevel);

        if (nextLevel == null)
        {
            SelectedLevelData.Clear();

            string destinationScene =
                GetPostFinalLevelDestination();

            if (!LoadScene(destinationScene))
                CancelSceneChangeRequest();

            return;
        }

        SelectedLevelData.SetMission(
            nextLevel
        );

        if (!LoadScene(gameSceneName))
            CancelSceneChangeRequest();
    }

    public void TryAgain()
    {
        if (!TryBeginSceneChange())
            return;

        PrepareForSceneChange();

        if (!LoadScene(
            SceneManager
                .GetActiveScene()
                .name
        ))
        {
            CancelSceneChangeRequest();
        }
    }

    public void GoMenu()
    {
        menuConfirmationOpenedFromPause = false;
        pauseConfirmationActivatedResultPanel = false;

        if (menuConfirmationPanel == null)
        {
            Debug.LogWarning(
                "[GameResultUI] Menu Confirmation Panel atanmamış. " +
                "Main Menu'ye doğrudan dönülüyor.",
                this
            );

            ConfirmGoMenu();
            return;
        }

        if (isSceneChangeRequested)
            return;

        SetSceneButtonsInteractable(false);
        EnableMenuConfirmationModalLayer();
        StartMenuConfirmationAnimation(true);
    }

    public bool ShowPauseMenuConfirmation()
    {
        if (menuConfirmationPanel == null)
            return false;

        if (isSceneChangeRequested || isMenuConfirmationOpen)
            return true;

        menuConfirmationOpenedFromPause = true;
        pauseConfirmationActivatedResultPanel = false;

        SetSceneButtonsInteractable(false);

        // Lock Pause at the source too: no child button, nested raycaster or
        // Escape/Resume path is allowed while this modal is open.
        GetGameQuit()?.SetPauseMenuModalState(true);

        EnableMenuConfirmationModalLayer();
        StartMenuConfirmationAnimation(true);
        return true;
    }

    public void ConfirmGoMenu()
    {
        if (!TryBeginSceneChange())
            return;

        StopMenuConfirmationRoutine();
        menuConfirmationRoutine =
            StartCoroutine(
                ConfirmGoMenuRoutine()
            );
    }

    public void CancelGoMenu()
    {
        if (isSceneChangeRequested)
            return;

        StartMenuConfirmationAnimation(false);
        SetSceneButtonsInteractable(true);
    }

    private IEnumerator ConfirmGoMenuRoutine()
    {
        yield return AnimateMenuConfirmation(false);

        menuConfirmationRoutine = null;
        DisableMenuConfirmationModalLayer();

        GetGameQuit()?.SetPauseMenuModalState(false);

        menuConfirmationOpenedFromPause = false;
        pauseConfirmationActivatedResultPanel = false;

        PrepareForSceneChange();
        SelectedLevelData.Clear();

        // Level 40's first completion goes straight to the Credits scene.
        // Do not interrupt the ending with an attempt ad.
        if (ShouldOpenEndCredits())
        {
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadSceneWithFade(
                    CreditsSceneName
                );
            }
            else
            {
                SceneManager.LoadScene(
                    CreditsSceneName
                );
            }

            yield break;
        }

        // Once normal scene fade'ini baslat. Attempt reklami ancak fade alpha
        // tamamen 1 oldugunda (ekran simsiyahken) acilir. Reklam kapaninca
        // SceneTransition MainMenu'yu yukler ve yeni sahneyi siyahtan acar.
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadSceneWithFade(
                "MainMenu",
                continueTransition =>
                    FatefulRushAdManager
                        .TryShowAttemptAdBeforeReturningToMenu(
                            continueTransition
                        )
            );

            yield break;
        }

        // Fade sistemi yoksa son guvenli fallback: eski davranisla reklami
        // sahne yuklemeden once dene.
        bool adStarted =
            FatefulRushAdManager.TryShowAttemptAdBeforeReturningToMenu(
                ContinueToMainMenuFallbackAfterAd
            );

        if (!adStarted)
            ContinueToMainMenuFallbackAfterAd();
    }

    private void ContinueToMainMenuFallbackAfterAd()
    {
        if (this == null)
            return;

        SceneManager.LoadScene("MainMenu");
    }

    private GameQuit GetGameQuit()
    {
        if (cachedGameQuit == null)
        {
            cachedGameQuit =
                FindAnyObjectByType<GameQuit>(
                    FindObjectsInactive.Include
                );
        }

        return cachedGameQuit;
    }

    private void EnableMenuConfirmationModalLayer()
    {
        if (menuConfirmationPanel == null)
            return;

        PrepareMenuConfirmationUI();
        PrepareMenuConfirmationModalRoot();

        if (menuConfirmationModalRoot == null)
            return;

        CacheMenuConfirmationHome();

        if (!menuConfirmationDetachedToModal)
        {
            menuConfirmationPanel.transform.SetParent(
                menuConfirmationModalRoot.transform,
                true
            );

            menuConfirmationDetachedToModal = true;
        }

        if (menuConfirmationInputShield != null)
        {
            menuConfirmationInputShield.SetActive(true);
            menuConfirmationInputShield.transform.SetAsFirstSibling();
        }

        menuConfirmationPanel.transform.SetAsLastSibling();
        menuConfirmationModalRoot.SetActive(true);
        isMenuConfirmationOpen = true;
    }

    private void DisableMenuConfirmationModalLayer()
    {
        if (menuConfirmationInputShield != null)
            menuConfirmationInputShield.SetActive(false);

        RestoreMenuConfirmationHome();

        if (menuConfirmationModalRoot != null)
            menuConfirmationModalRoot.SetActive(false);

        isMenuConfirmationOpen = false;
    }

    private void CacheMenuConfirmationHome()
    {
        if (menuConfirmationPanel == null ||
            menuConfirmationOriginalParent != null)
        {
            return;
        }

        menuConfirmationOriginalParent =
            menuConfirmationPanel.transform.parent;

        menuConfirmationOriginalSiblingIndex =
            menuConfirmationPanel.transform.GetSiblingIndex();
    }

    private void RestoreMenuConfirmationHome()
    {
        if (!menuConfirmationDetachedToModal ||
            menuConfirmationPanel == null)
        {
            return;
        }

        if (menuConfirmationOriginalParent != null)
        {
            menuConfirmationPanel.transform.SetParent(
                menuConfirmationOriginalParent,
                true
            );

            int siblingIndex = Mathf.Clamp(
                menuConfirmationOriginalSiblingIndex,
                0,
                Mathf.Max(
                    0,
                    menuConfirmationOriginalParent.childCount - 1
                )
            );

            menuConfirmationPanel.transform.SetSiblingIndex(siblingIndex);
        }

        menuConfirmationDetachedToModal = false;
    }

    private void PrepareMenuConfirmationModalRoot()
    {
        if (menuConfirmationModalRoot != null)
            return;

        Canvas sourceCanvas = null;

        if (menuConfirmationPanel != null)
            sourceCanvas = menuConfirmationPanel.GetComponentInParent<Canvas>(true);

        if (sourceCanvas == null && resultPanel != null)
            sourceCanvas = resultPanel.GetComponentInParent<Canvas>(true);

        GameObject root = new GameObject(
            "[Menu Confirmation Modal Canvas]",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        // Keep it as a scene root. A Screen Space Overlay canvas with a very
        // high sorting order will always render above the Pause canvas.
        root.transform.SetParent(null, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.localScale = Vector3.one;

        menuConfirmationModalCanvas = root.GetComponent<Canvas>();
        menuConfirmationModalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuConfirmationModalCanvas.overrideSorting = true;
        menuConfirmationModalCanvas.sortingOrder = 32760;

        if (sourceCanvas != null)
            menuConfirmationModalCanvas.targetDisplay = sourceCanvas.targetDisplay;

        menuConfirmationModalScaler = root.GetComponent<CanvasScaler>();
        CopyCanvasScalerSettings(sourceCanvas, menuConfirmationModalScaler);

        menuConfirmationModalRaycaster = root.GetComponent<GraphicRaycaster>();
        menuConfirmationModalRaycaster.enabled = true;

        PrepareMenuConfirmationInputShield(root.transform);

        menuConfirmationModalRoot = root;
        menuConfirmationModalRoot.SetActive(false);
    }

    private static void CopyCanvasScalerSettings(
        Canvas sourceCanvas,
        CanvasScaler targetScaler)
    {
        if (targetScaler == null)
            return;

        CanvasScaler sourceScaler =
            sourceCanvas != null
                ? sourceCanvas.GetComponent<CanvasScaler>()
                : null;

        if (sourceScaler == null)
        {
            targetScaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            targetScaler.referenceResolution =
                new Vector2(1920f, 1080f);

            targetScaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            targetScaler.matchWidthOrHeight = 0.5f;
            return;
        }

        targetScaler.uiScaleMode = sourceScaler.uiScaleMode;
        targetScaler.referencePixelsPerUnit =
            sourceScaler.referencePixelsPerUnit;

        targetScaler.scaleFactor = sourceScaler.scaleFactor;
        targetScaler.referenceResolution =
            sourceScaler.referenceResolution;

        targetScaler.screenMatchMode =
            sourceScaler.screenMatchMode;

        targetScaler.matchWidthOrHeight =
            sourceScaler.matchWidthOrHeight;

        targetScaler.physicalUnit = sourceScaler.physicalUnit;
        targetScaler.fallbackScreenDPI =
            sourceScaler.fallbackScreenDPI;

        targetScaler.defaultSpriteDPI =
            sourceScaler.defaultSpriteDPI;
    }

    private void PrepareMenuConfirmationInputShield(Transform parent)
    {
        if (menuConfirmationInputShield != null || parent == null)
            return;

        GameObject shield = new GameObject(
            "[Menu Confirmation Input Shield]",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement)
        );

        shield.transform.SetParent(parent, false);

        RectTransform rect = shield.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Image image = shield.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.001f);
        image.raycastTarget = true;

        LayoutElement layout = shield.GetComponent<LayoutElement>();
        layout.ignoreLayout = true;

        menuConfirmationInputShield = shield;
        menuConfirmationInputShield.SetActive(false);
    }

    private void PrepareMenuConfirmationUI()
    {
        if (menuConfirmationPanel == null)
            return;

        CacheMenuConfirmationHome();

        menuConfirmationCanvasGroup =
            menuConfirmationPanel.GetComponent<CanvasGroup>();

        if (menuConfirmationCanvasGroup == null)
        {
            menuConfirmationCanvasGroup =
                menuConfirmationPanel.AddComponent<CanvasGroup>();
        }

        if (!menuConfirmationScaleCached)
        {
            menuConfirmationRestScale =
                menuConfirmationPanel.transform.localScale;

            if (menuConfirmationRestScale == Vector3.zero)
                menuConfirmationRestScale = Vector3.one;

            menuConfirmationScaleCached = true;
        }
    }

    private void StartMenuConfirmationAnimation(bool show)
    {
        if (menuConfirmationPanel == null)
            return;

        StopMenuConfirmationRoutine();

        menuConfirmationRoutine =
            StartCoroutine(
                MenuConfirmationAnimationRoutine(show)
            );
    }

    private IEnumerator MenuConfirmationAnimationRoutine(bool show)
    {
        yield return AnimateMenuConfirmation(show);
        menuConfirmationRoutine = null;

        if (!show)
        {
            DisableMenuConfirmationModalLayer();

            if (menuConfirmationOpenedFromPause)
                RestoreAfterPauseMenuConfirmation();
        }
    }

    private IEnumerator AnimateMenuConfirmation(bool show)
    {
        if (menuConfirmationPanel == null)
            yield break;

        PrepareMenuConfirmationUI();

        if (menuConfirmationCanvasGroup == null)
            yield break;

        if (show)
        {
            EnableMenuConfirmationModalLayer();
            menuConfirmationPanel.SetActive(true);
            menuConfirmationPanel.transform.SetAsLastSibling();
        }
        else if (!menuConfirmationPanel.activeSelf)
        {
            yield break;
        }

        menuConfirmationCanvasGroup.interactable = false;
        menuConfirmationCanvasGroup.blocksRaycasts = false;

        float safeDuration =
            Mathf.Max(
                0.05f,
                menuConfirmationAnimationDuration
            );

        float startAlpha =
            show
                ? Mathf.Clamp01(menuConfirmationCanvasGroup.alpha)
                : menuConfirmationCanvasGroup.alpha;

        float targetAlpha = show ? 1f : 0f;

        Vector3 hiddenScale =
            menuConfirmationRestScale *
            Mathf.Clamp(
                menuConfirmationStartScale,
                0.8f,
                1f
            );

        Vector3 startScale =
            show
                ? hiddenScale
                : menuConfirmationPanel.transform.localScale;

        Vector3 targetScale =
            show
                ? menuConfirmationRestScale
                : hiddenScale;

        if (show && startAlpha <= 0.001f)
        {
            menuConfirmationCanvasGroup.alpha = 0f;
            menuConfirmationPanel.transform.localScale = hiddenScale;
            startAlpha = 0f;
            startScale = hiddenScale;
        }

        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / safeDuration
                );

            float eased =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            menuConfirmationCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    eased
                );

            menuConfirmationPanel.transform.localScale =
                Vector3.LerpUnclamped(
                    startScale,
                    targetScale,
                    eased
                );

            yield return null;
        }

        menuConfirmationCanvasGroup.alpha = targetAlpha;

        if (show)
        {
            menuConfirmationPanel.transform.localScale =
                menuConfirmationRestScale;

            menuConfirmationCanvasGroup.interactable = true;
            menuConfirmationCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            menuConfirmationCanvasGroup.interactable = false;
            menuConfirmationCanvasGroup.blocksRaycasts = false;

            menuConfirmationPanel.transform.localScale =
                menuConfirmationRestScale;

            menuConfirmationPanel.SetActive(false);
        }
    }

    private void StopMenuConfirmationRoutine()
    {
        if (menuConfirmationRoutine == null)
            return;

        StopCoroutine(menuConfirmationRoutine);
        menuConfirmationRoutine = null;
    }

    private void RestoreAfterPauseMenuConfirmation()
    {
        bool shouldHideResultPanel =
            pauseConfirmationActivatedResultPanel;

        menuConfirmationOpenedFromPause = false;
        pauseConfirmationActivatedResultPanel = false;

        DisableMenuConfirmationModalLayer();
        GetGameQuit()?.SetPauseMenuModalState(false);

        if (shouldHideResultPanel &&
            resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void HideMenuConfirmationImmediate()
    {
        StopMenuConfirmationRoutine();
        DisableMenuConfirmationModalLayer();

        if (menuConfirmationOpenedFromPause)
            GetGameQuit()?.SetPauseMenuModalState(false);

        menuConfirmationOpenedFromPause = false;
        pauseConfirmationActivatedResultPanel = false;

        if (menuConfirmationPanel == null)
            return;

        PrepareMenuConfirmationUI();

        if (menuConfirmationCanvasGroup != null)
        {
            menuConfirmationCanvasGroup.alpha = 0f;
            menuConfirmationCanvasGroup.interactable = false;
            menuConfirmationCanvasGroup.blocksRaycasts = false;
        }

        if (menuConfirmationScaleCached)
        {
            menuConfirmationPanel.transform.localScale =
                menuConfirmationRestScale;
        }

        menuConfirmationPanel.SetActive(false);
    }

    private void PrepareForSceneChange()
    {
        // SceneTransition owns timeScale during the fade/load. Do not globally
        // re-enable Rigidbody2D simulation here: pooled and special-purpose
        // bodies may intentionally be non-simulated.
        Time.timeScale = 1f;
    }

    private bool TryBeginSceneChange()
    {
        if (isSceneChangeRequested)
            return false;

        if (SceneTransition.Instance != null &&
            SceneTransition.Instance.IsTransitioning)
        {
            return false;
        }

        isSceneChangeRequested = true;
        SetSceneButtonsInteractable(false);
        return true;
    }

    private void CancelSceneChangeRequest()
    {
        isSceneChangeRequested = false;
        SetSceneButtonsInteractable(true);
    }

    private void SetSceneButtonsInteractable(bool interactable)
    {
        SetButtonInteractable(nextLevelButton, interactable);
        SetButtonInteractable(tryAgainButton, interactable);
        SetButtonInteractable(menuButton, interactable);
    }

    private static void SetButtonInteractable(
        GameObject buttonObject,
        bool interactable)
    {
        if (buttonObject == null)
            return;

        Button button =
            buttonObject.GetComponent<Button>();

        if (button == null)
        {
            button =
                buttonObject.GetComponentInChildren<Button>(
                    true
                );
        }

        if (button != null)
            button.interactable = interactable;
    }

    private bool ShouldOpenEndCredits()
    {
        return
            PlayerPrefs.GetInt(
                FatefulRushCreditsController.PendingKey,
                0
            ) == 1;
    }

    private string GetPostFinalLevelDestination()
    {
        return
            ShouldOpenEndCredits()
                ? CreditsSceneName
                : "MainMenu";
    }

    private bool LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(
                "[GameResultUI] Yüklenecek sahne adı boş.",
                this
            );

            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"[GameResultUI] Sahne yüklenemiyor: '{sceneName}'. " +
                "Build Profiles ayarını kontrol et.",
                this
            );

            return false;
        }

        if (SceneTransition.Instance != null)
        {
            if (SceneTransition.Instance.IsTransitioning)
                return false;

            SceneTransition.Instance
                .LoadSceneWithFade(
                    sceneName
                );

            return true;
        }

        SceneManager.LoadScene(
            sceneName
        );

        return true;
    }
}
