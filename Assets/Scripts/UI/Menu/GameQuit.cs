using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameQuit : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string SoundEnabledKey = "SoundOn";
    private const string MusicVolumeKey = "MusicVolume";

    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private UIPanelFadeSwitcher fadeSwitcher;
    [SerializeField] private OptionsUI optionsUI;

    [Header("Audio")]
    [SerializeField] private AudioSource gameplayMusicSource;

    [SerializeField, Min(0f)]
    private float musicFadeDuration = 0.25f;

    private Coroutine musicFadeRoutine;
    private float defaultGameplayMusicVolume = 1f;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        Time.timeScale = 1f;
        IsPaused = false;

        RefreshReferences();

        if (gameplayMusicSource != null)
        {
            defaultGameplayMusicVolume =
                gameplayMusicSource.volume;
        }

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void Update()
    {
        if (!GameStateManager.IsGameplayStarted)
            return;

        if (Keyboard.current == null ||
            !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (IsPaused &&
            optionsUI != null &&
            optionsUI.HandleEscapeBack())
        {
            return;
        }

        TogglePause();
    }

    private void OnDestroy()
    {
        StopMusicFade();
    }

    public void PauseGame()
    {
        if (!GameStateManager.IsGameplayStarted || IsPaused)
            return;

        IsPaused = true;
        Time.timeScale = 0f;

        SoundManager.Instance?.PlayPremiumInterfaceSound();

        FadeGameplayMusicOut();

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (!IsPaused)
            return;

        IsPaused = false;

        SoundManager.Instance?.PlayPremiumInterfaceSound();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (TimeSlowController.Instance != null)
        {
            TimeSlowController.Instance.ResumeAfterPause();
        }
        else
        {
            Time.timeScale = 1f;
        }

        FadeGameplayMusicIn();
    }

    public void TogglePause()
    {
        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void RestartGame()
    {
        PrepareForSceneChange();

        int activeSceneIndex =
            SceneManager.GetActiveScene().buildIndex;

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadSceneWithFade(
                activeSceneIndex
            );
        }
        else
        {
            SceneManager.LoadScene(activeSceneIndex);
        }
    }

    public void BackToMainMenu()
    {
        PrepareForSceneChange();

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadSceneWithFade(
                MainMenuSceneName
            );
        }
        else
        {
            SceneManager.LoadScene(MainMenuSceneName);
        }
    }

    public void ExitToMenu()
    {
        BackToMainMenu();
    }

    public void QuitGame()
    {
        StopMusicFade();
        Time.timeScale = 1f;
        IsPaused = false;

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.QuitGameWithFade();
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void FadeGameplayMusicOut()
    {
        if (gameplayMusicSource == null)
            return;

        StopMusicFade();

        if (musicFadeDuration <= 0f)
        {
            gameplayMusicSource.volume = 0f;
            gameplayMusicSource.Pause();
            return;
        }

        musicFadeRoutine =
            StartCoroutine(FadeMusicOutRoutine());
    }

    private void FadeGameplayMusicIn()
    {
        if (gameplayMusicSource == null)
            return;

        StopMusicFade();

        float targetVolume =
            GetTargetGameplayMusicVolume();

        gameplayMusicSource.UnPause();

        if (musicFadeDuration <= 0f)
        {
            gameplayMusicSource.volume = targetVolume;
            return;
        }

        musicFadeRoutine =
            StartCoroutine(
                FadeMusicInRoutine(targetVolume)
            );
    }

    private IEnumerator FadeMusicOutRoutine()
    {
        float startVolume =
            gameplayMusicSource.volume;

        float elapsedTime = 0f;

        while (elapsedTime < musicFadeDuration)
        {
            if (gameplayMusicSource == null)
                yield break;

            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / musicFadeDuration
            );

            gameplayMusicSource.volume = Mathf.Lerp(
                startVolume,
                0f,
                progress
            );

            yield return null;
        }

        if (gameplayMusicSource != null)
        {
            gameplayMusicSource.volume = 0f;
            gameplayMusicSource.Pause();
        }

        musicFadeRoutine = null;
    }

    private IEnumerator FadeMusicInRoutine(
        float targetVolume)
    {
        float startVolume =
            gameplayMusicSource.volume;

        float elapsedTime = 0f;

        while (elapsedTime < musicFadeDuration)
        {
            if (gameplayMusicSource == null)
                yield break;

            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / musicFadeDuration
            );

            gameplayMusicSource.volume = Mathf.Lerp(
                startVolume,
                targetVolume,
                progress
            );

            yield return null;
        }

        if (gameplayMusicSource != null)
        {
            gameplayMusicSource.volume =
                targetVolume;
        }

        musicFadeRoutine = null;
    }

    private float GetTargetGameplayMusicVolume()
    {
        bool soundEnabled =
            PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;

        if (!soundEnabled)
            return 0f;

        return Mathf.Clamp01(
            PlayerPrefs.GetFloat(
                MusicVolumeKey,
                defaultGameplayMusicVolume
            )
        );
    }

    private void PrepareForSceneChange()
    {
        StopMusicFade();

        IsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void StopMusicFade()
    {
        if (musicFadeRoutine == null)
            return;

        StopCoroutine(musicFadeRoutine);
        musicFadeRoutine = null;
    }

    private void RefreshReferences()
    {
        if (optionsUI == null)
        {
            optionsUI =
                FindAnyObjectByType<OptionsUI>();
        }
    }

    private void OnValidate()
    {
        musicFadeDuration =
            Mathf.Max(0f, musicFadeDuration);
    }
}