using System.Collections;
using UnityEngine;

public class TimeSlowController : MonoBehaviour
{
    public static TimeSlowController Instance;

    private float originalFixedDeltaTime;
    private Coroutine slowRoutine;

    private PlayerMovement player;
    private GameQuit pauseMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        originalFixedDeltaTime = Time.fixedDeltaTime;
        Time.timeScale = 1f;

        player = FindAnyObjectByType<PlayerMovement>();
        pauseMenu = FindAnyObjectByType<GameQuit>();
    }

    public void StartSlow(float multiplier, float duration)
    {
        if (IsGameOver()) return;

        if (slowRoutine != null)
            StopCoroutine(slowRoutine);

        slowRoutine = StartCoroutine(SlowRoutine(multiplier, duration));
    }

    private IEnumerator SlowRoutine(float multiplier, float duration)
    {
        Time.timeScale = multiplier;
        Time.fixedDeltaTime = originalFixedDeltaTime * multiplier;

        float timer = 0f;

        while (timer < duration)
        {
            if (IsGameOver())
            {
                ForceStopForGameEnd();
                yield break;
            }

            if (pauseMenu == null)
                pauseMenu = FindAnyObjectByType<GameQuit>();

            if (pauseMenu == null || !pauseMenu.IsPaused)
                timer += Time.unscaledDeltaTime;

            yield return null;
        }

        ResetTime();
    }

    public void ResetTime()
    {
        Time.fixedDeltaTime = originalFixedDeltaTime;

        SlowPowerUp.isSlowActive = false;
        slowRoutine = null;

        if (IsGameOver())
            return;

        if (pauseMenu == null)
            pauseMenu = FindAnyObjectByType<GameQuit>();

        if (pauseMenu != null && pauseMenu.IsPaused)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
    }

    public void ResumeAfterPause()
    {
        if (IsGameOver()) return;

        if (SlowPowerUp.isSlowActive)
        {
            Time.timeScale = SlowPowerUp.currentSlowMultiplier;
            Time.fixedDeltaTime = originalFixedDeltaTime * SlowPowerUp.currentSlowMultiplier;
        }
        else
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }
    }

    public void ForceStopForGameEnd()
    {
        if (slowRoutine != null)
        {
            StopCoroutine(slowRoutine);
            slowRoutine = null;
        }

        SlowPowerUp.isSlowActive = false;
        Time.fixedDeltaTime = originalFixedDeltaTime;
    }

    private bool IsGameOver()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerMovement>();

        return player != null && player.IsGameOver;
    }
}