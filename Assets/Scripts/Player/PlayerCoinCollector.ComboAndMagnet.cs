using TMPro;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in PlayerCoinCollector.cs.
public partial class PlayerCoinCollector
{
    private int UpdateCombo()
    {
        if (!comboEnabled)
        {
            combo = 1;
            comboChain = 0;
            comboTimer = 0f;

            return 1;
        }

        int previousCombo = combo;

        comboTimer = 0f;
        comboChain++;

        combo =
            GetComboFromChain();

        RunOptional(
            () => StatsManager.RecordComboProgress(
                combo,
                comboChain,
                combo > previousCombo
            ),
            "combo stat/achievement update"
        );

        return combo;
    }

    private int GetComboFromChain()
    {
        if (comboSpeedStages != null &&
            comboSpeedStages.Length > 0)
        {
            int result = 1;

            for (int i = 0;
                 i < comboSpeedStages.Length;
                 i++)
            {
                ComboSpeedStage stage =
                    comboSpeedStages[i];

                if (stage == null)
                    continue;

                if (stage.comboMultiplier < 2)
                    continue;

                if (stage.coinsRequired < 1)
                    continue;

                if (comboChain >=
                    stage.coinsRequired)
                {
                    result = Mathf.Max(
                        result,
                        stage.comboMultiplier
                    );
                }
            }

            return Mathf.Max(1, result);
        }

        int fallbackResult = 1;

        if (comboChain >= coinsForCombo3)
        {
            fallbackResult = 3;
        }
        else if (comboChain >= coinsForCombo2)
        {
            fallbackResult = 2;
        }

        return Mathf.Max(1, fallbackResult);
    }

    private void ResetCombo()
    {
        combo = 1;
        comboChain = 0;
        comboTimer = 0f;

        if (comboUI == null)
            return;

        comboUI.ResetCombo();

        comboUI.UpdateTimerBar(
            0f,
            combo
        );
    }

    public bool TryGetComboMagnetSettings(
        Vector3 coinPosition,
        out float maxSpeed,
        out float smoothTime)
    {
        maxSpeed = 0f;
        smoothTime = comboMagnetSmoothTime;

        if (!comboMagnetEnabled ||
            !comboEnabled ||
            IsGameOver() ||
            !GameStateManager.IsGameplayStarted)
        {
            return false;
        }

        if (combo == 4)
            return TryGetCombo4MagnetSettings(coinPosition, out maxSpeed, out smoothTime);

        float radius;
        float baseMaxSpeed;

        if (combo >= 6)
        {
            radius = combo6MagnetRadius;
            baseMaxSpeed = combo6MagnetMaxSpeed;
        }
        else if (combo >= 5)
        {
            radius = combo5MagnetRadius;
            baseMaxSpeed = combo5MagnetMaxSpeed;
        }
        else
        {
            return false;
        }

        Vector2 delta =
            (Vector2)transform.position -
            (Vector2)coinPosition;

        float radiusSquared = radius * radius;
        float distanceSquared = delta.sqrMagnitude;

        if (distanceSquared > radiusSquared)
            return false;

        float distance = Mathf.Sqrt(distanceSquared);
        float closeness =
            1f - Mathf.Clamp01(distance / radius);

        // Menzilin kenarında hafif, oyuncuya yaklaştıkça daha güçlü çekim.
        float strength = Mathf.SmoothStep(
            comboMagnetEdgeSpeedFactor,
            1f,
            closeness
        );

        maxSpeed = Mathf.Max(
            0.1f,
            baseMaxSpeed * strength
        );

        smoothTime = comboMagnetSmoothTime;
        return true;
    }

    private bool TryGetCombo4MagnetSettings(Vector3 coinPosition, out float maxSpeed, out float smoothTime)
    {

        maxSpeed = 0f;
        smoothTime = Mathf.Max(0.04f, comboMagnetSmoothTime);

        if (!comboMagnetEnabled ||
            !comboEnabled ||
            combo != 4 ||
            GameStateManager.IsGameplayEnded ||
            !GameStateManager.IsGameplayStarted)
        {
            return false;
        }

        float distance = Vector2.Distance(
            transform.position,
            coinPosition
        );

        if (distance > Combo4MagnetRadius)
            return false;

        float normalized = 1f - Mathf.Clamp01(
            distance / Combo4MagnetRadius
        );

        float edgeFactor = Mathf.Clamp01(
            comboMagnetEdgeSpeedFactor
        );

        float speedFactor = Mathf.SmoothStep(
            edgeFactor,
            1f,
            normalized
        );

        maxSpeed = Mathf.Max(
            0.1f,
            Combo4MagnetMaxSpeed * speedFactor
        );
        return true;
    
    }
}
