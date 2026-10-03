using System.Collections;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in MainMenuStarColorRandomizer.cs.
public partial class MainMenuStarColorRandomizer
{
    private void ChangeState(
        Color targetColor,
        float targetEmissionRate,
        int targetMaxParticles,
        float targetFlowMultiplier
    )
    {
        if (nearStars == null)
            return;

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(
            StateTransitionRoutine(
                targetColor,
                Mathf.Max(0f, targetEmissionRate),
                Mathf.Max(1, targetMaxParticles),
                Mathf.Max(0f, targetFlowMultiplier)
            )
        );
    }

    private IEnumerator StateTransitionRoutine(
        Color targetColor,
        float targetEmissionRate,
        int targetMaxParticles,
        float targetFlowMultiplier
    )
    {
        Color startColor = currentColor;
        float startEmissionRate = currentEmissionRate;
        float startMaxParticles = currentMaxParticles;
        float startFlowMultiplier = currentFlowMultiplier;

        float timer = 0f;

        while (timer < transitionDuration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                timer / transitionDuration
            );

            float easedProgress =
                EaseInOutCubic(progress);

            currentColor = Color.Lerp(
                startColor,
                targetColor,
                easedProgress
            );

            currentEmissionRate = Mathf.Lerp(
                startEmissionRate,
                targetEmissionRate,
                easedProgress
            );

            currentMaxParticles = Mathf.Lerp(
                startMaxParticles,
                targetMaxParticles,
                easedProgress
            );

            currentFlowMultiplier = Mathf.Lerp(
                startFlowMultiplier,
                targetFlowMultiplier,
                easedProgress
            );

            ApplyStateInstant(
                currentColor,
                currentEmissionRate,
                Mathf.RoundToInt(currentMaxParticles),
                currentFlowMultiplier
            );

            yield return null;
        }

        currentColor = targetColor;
        currentEmissionRate = targetEmissionRate;
        currentMaxParticles = targetMaxParticles;
        currentFlowMultiplier = targetFlowMultiplier;

        ApplyStateInstant(
            currentColor,
            currentEmissionRate,
            targetMaxParticles,
            currentFlowMultiplier
        );

        transitionRoutine = null;
    }

    private void ApplyStateInstant(
        Color color,
        float emissionRate,
        int maxParticles,
        float flowMultiplier
    )
    {
        if (nearStars == null)
            return;

        ParticleSystem.MainModule main =
            nearStars.main;

        main.startColor =
            new ParticleSystem.MinMaxGradient(color);

        main.maxParticles =
            Mathf.Max(1, maxParticles);

        ParticleSystem.EmissionModule emission =
            nearStars.emission;

        if (useScreenEdgeNearStars)
        {
            emission.enabled = false;
        }
        else
        {
            emission.enabled = true;
            emission.rateOverTime =
                Mathf.Max(0f, emissionRate);
        }

        ParticleSystem.VelocityOverLifetimeModule velocity =
            nearStars.velocityOverLifetime;

        velocity.x = ScaleCurve(
            originalVelocityX,
            flowMultiplier
        );

        velocity.y = ScaleCurve(
            originalVelocityY,
            flowMultiplier
        );

        velocity.z = ScaleCurve(
            originalVelocityZ,
            flowMultiplier
        );

        ApplyColorToLivingParticles(
            color,
            main.maxParticles
        );
    }

    private void ApplyColorToLivingParticles(
        Color color,
        int maxParticles
    )
    {
        EnsureParticleBuffer();

        int particleCount =
            nearStars.GetParticles(
                particles
            );

        particleCount =
            Mathf.Min(
                particleCount,
                maxParticles
            );

        for (int i = 0;
             i < particleCount;
             i++)
        {
            particles[i].startColor = color;
        }

        nearStars.SetParticles(
            particles,
            particleCount
        );
    }

    private void CacheOriginalParticleSettings()
    {
        if (nearStars == null)
            return;

        ParticleSystem.MainModule main =
            nearStars.main;

        ParticleSystem.EmissionModule emission =
            nearStars.emission;

        ParticleSystem.VelocityOverLifetimeModule velocity =
            nearStars.velocityOverLifetime;

        originalEmissionRate =
            GetRepresentativeCurveValue(
                emission.rateOverTime
            );

        originalMaxParticles =
            Mathf.Max(
                1,
                main.maxParticles
            );

        originalVelocityX = velocity.x;
        originalVelocityY = velocity.y;
        originalVelocityZ = velocity.z;
    }

    private static ParticleSystem.MinMaxCurve ScaleCurve(
        ParticleSystem.MinMaxCurve source,
        float multiplier
    )
    {
        return StarfieldFlowUtility.ScaleCurve(source, multiplier);
    }

    private static float GetRepresentativeCurveValue(
        ParticleSystem.MinMaxCurve curve
    )
    {
        return StarfieldFlowUtility.GetRepresentativeCurveValue(curve);
    }

    private void MigrateLegacySettingsIfNeeded()
    {
        if (screenEdgeSettingsVersion >= 2)
            return;

        if (screenEdgeSettingsVersion < 1)
        {
            firstPageEmissionRate = 4.25f;
            lastPageEmissionRate = 8.5f;
            firstPageMaxParticles = 140;
            lastPageMaxParticles = 260;
        }

        basePanelEmissionRate = 1.5f;
        basePanelMaxParticles = 50;

        screenEdgeSettingsVersion = 2;
    }

    private Color GetSelectedSkinThemeColor()
    {
        PlayerSkinCatalog catalog = ResolveSkinCatalog();

        return GetSkinThemeColor(
            catalog != null
                ? catalog.GetSelectedSkin()
                : null
        );
    }

    private Color GetSkinThemeColor(
        PlayerSkinCatalog.SkinEntry skin)
    {
        Color color = skin != null
            ? PlayerSkinCatalog.GetUIThemeColor(skin)
            : Color.white;

        float highestChannel = Mathf.Max(
            color.r,
            color.g,
            color.b
        );

        if (highestChannel > 1f)
        {
            color.r /= highestChannel;
            color.g /= highestChannel;
            color.b /= highestChannel;
        }

        color.a = Mathf.Clamp01(skinThemeAlpha);
        return color;
    }

    private static PlayerSkinCatalog ResolveSkinCatalog()
    {
        return PlayerSkinCatalog.ResolveLoadedCatalog();
    }

    private static float EaseInOutCubic(
        float value
    )
    {
        value = Mathf.Clamp01(value);

        if (value < 0.5f)
            return 4f * value * value * value;

        float inverse =
            -2f * value + 2f;

        return 1f -
               inverse *
               inverse *
               inverse /
               2f;
    }
}
