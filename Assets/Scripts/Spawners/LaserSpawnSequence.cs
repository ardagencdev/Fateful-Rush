using System;
using System.Collections;
using UnityEngine;

/// <summary>Shared laser timing. The component owns active objects and orientation.</summary>
public static class LaserSpawnSequence
{
    public static IEnumerator Run(Func<bool> active, Func<bool> gameOver,
        Func<float> minimum, Func<float> maximum, Func<IEnumerator> spawn)
    {
        while (active())
        {
            yield return new WaitUntil(() => !active() || GameStateManager.IsGameplayStarted);
            if (!active() || gameOver()) yield break;
            float waitTime = UnityEngine.Random.Range(minimum(), maximum());
            float elapsedTime = 0f;
            while (elapsedTime < waitTime)
            {
                if (!active() || gameOver()) yield break;
                if (GameStateManager.IsGameplayStarted) elapsedTime += Time.deltaTime;
                yield return null;
            }
            if (!active() || gameOver() || !GameStateManager.IsGameplayStarted) continue;
            yield return spawn();
        }
    }

    public static IEnumerator PlayWarning(GameObject activeWarning, float warningDuration,
        bool logMissingComponent)
    {
        if (activeWarning == null)
        {
            yield return new WaitForSeconds(warningDuration);
            yield break;
        }
        LaserWarning warning = activeWarning.GetComponent<LaserWarning>();
        if (warning != null)
        {
            warning.blinkDuration = warningDuration;
            yield return warning.PlayWarning();
        }
        else
        {
            if (logMissingComponent)
                Debug.LogWarning("[HorizontalLaser] Warning prefab üzerinde LaserWarning componenti yok.", activeWarning);
            yield return new WaitForSeconds(warningDuration);
        }
    }
}
