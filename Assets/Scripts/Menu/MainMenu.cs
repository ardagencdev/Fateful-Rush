using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("Scene")]
    public string gameSceneName = "a";

    [Header("UI")]
    [SerializeField] private UIPanelFadeSwitcher fadeSwitcher;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Dev Room")]
    public LevelConfig devRoomConfig;

    public void StartGame()
    {
        SelectedLevelData.SetDevRoom(devRoomConfig);

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade(gameSceneName);
        else
            SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.QuitGameWithFade();
        else
            StartCoroutine(QuitRoutine());
    }

    private IEnumerator QuitRoutine()
    {
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.QuitGameWithFade();
            yield break;
        }

        if (fadeSwitcher != null && mainMenuPanel != null)
        {
            fadeSwitcher.HidePanel(mainMenuPanel);
            yield return new WaitForSecondsRealtime(0.35f);
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}