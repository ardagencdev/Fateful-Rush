using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameQuit : MonoBehaviour
{
    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private UIPanelFadeSwitcher fadeSwitcher;
    [SerializeField] private OptionsUI optionsUI;

    [Header("Audio")]
    [SerializeField] private AudioSource gameplayMusicSource;
    [SerializeField] private float musicFadeDuration = 0.25f;

    private Coroutine musicFadeRoutine;
    private float gameplayMusicVolume = 1f;

    private bool isPaused;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (gameplayMusicSource != null)
            gameplayMusicVolume = gameplayMusicSource.volume;

        if (optionsUI == null)
            optionsUI = FindAnyObjectByType<OptionsUI>();

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void Update()
    {
        if (!GameStateManager.IsGameplayStarted)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused && optionsUI != null && optionsUI.HandleEscapeBack())
                return;

            TogglePause();
        }
    }

    public void PauseGame()
    {
        if (!GameStateManager.IsGameplayStarted)
            return;

        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPremiumInterfaceSound();

        FadeGameplayMusicOut();

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPremiumInterfaceSound();

        FadeGameplayMusicIn();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (TimeSlowController.Instance != null)
            TimeSlowController.Instance.ResumeAfterPause();
        else
            Time.timeScale = 1f;
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade(SceneManager.GetActiveScene().buildIndex);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }

    public void ExitToMenu()
    {
        BackToMainMenu();
    }

    public void QuitGame()
    {
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.QuitGameWithFade();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void FadeGameplayMusicOut()
    {
        if (gameplayMusicSource == null)
            return;

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        musicFadeRoutine = StartCoroutine(FadeMusicOutRoutine());
    }

    private void FadeGameplayMusicIn()
    {
        if (gameplayMusicSource == null)
            return;

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        gameplayMusicSource.UnPause();
        musicFadeRoutine = StartCoroutine(FadeMusicInRoutine());
    }

    private System.Collections.IEnumerator FadeMusicOutRoutine()
    {
        float startVolume = gameplayMusicSource.volume;
        float timer = 0f;

        while (timer < musicFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / musicFadeDuration);

            gameplayMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        gameplayMusicSource.volume = 0f;
        gameplayMusicSource.Pause();

        musicFadeRoutine = null;
    }

    private System.Collections.IEnumerator FadeMusicInRoutine()
    {
        float targetVolume = PlayerPrefs.GetInt("SoundOn", 1) == 1
            ? PlayerPrefs.GetFloat("MusicVolume", gameplayMusicVolume)
            : 0f;

        float startVolume = gameplayMusicSource.volume;
        float timer = 0f;

        while (timer < musicFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / musicFadeDuration);

            gameplayMusicSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        gameplayMusicSource.volume = targetVolume;
        musicFadeRoutine = null;
    }
}