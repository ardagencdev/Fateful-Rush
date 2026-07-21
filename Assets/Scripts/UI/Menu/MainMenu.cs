using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "a";

    [Header("UI")]
    [SerializeField] private UIPanelFadeSwitcher fadeSwitcher;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Dev Room")]
    [SerializeField] private LevelConfig devRoomConfig;

    [Header("Quit")]
    [SerializeField, Min(0f)]
    private float fallbackQuitDelay = 0.35f;

    private Coroutine quitRoutine;
    private bool isStartingGame;
    private bool isQuitting;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        isStartingGame = false;
        isQuitting = false;
    }

    private void OnDisable()
    {
        StopQuitRoutine();
    }

    public void StartGame()
    {
        if (isStartingGame || isQuitting)
            return;

        if (devRoomConfig == null)
        {
            Debug.LogError(
                "MainMenu devRoomConfig reference is missing.",
                this
            );

            return;
        }

        if (!CanLoadGameScene())
            return;

        isStartingGame = true;
        Time.timeScale = 1f;

        SelectedLevelData.SetDevRoom(devRoomConfig);

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadSceneWithFade(
                gameSceneName
            );
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void QuitGame()
    {
        if (isQuitting || isStartingGame)
            return;

        isQuitting = true;
        Time.timeScale = 1f;

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.QuitGameWithFade();
            return;
        }

        StopQuitRoutine();
        quitRoutine = StartCoroutine(QuitRoutine());
    }

    private IEnumerator QuitRoutine()
    {
        if (fadeSwitcher != null &&
            mainMenuPanel != null)
        {
            fadeSwitcher.HidePanel(mainMenuPanel);

            if (fallbackQuitDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    fallbackQuitDelay
                );
            }
        }

        quitRoutine = null;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private bool CanLoadGameScene()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError(
                "MainMenu game scene name is empty.",
                this
            );

            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError(
                $"Scene '{gameSceneName}' could not be loaded. " +
                "Make sure it is included in Build Profiles.",
                this
            );

            return false;
        }

        return true;
    }

    private void StopQuitRoutine()
    {
        if (quitRoutine == null)
            return;

        StopCoroutine(quitRoutine);
        quitRoutine = null;
    }

    private void OnValidate()
    {
        fallbackQuitDelay =
            Mathf.Max(0f, fallbackQuitDelay);
    }
}