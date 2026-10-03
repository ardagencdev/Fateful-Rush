using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in StarfieldController.cs.
public partial class StarfieldController
{
    private void ApplyLayer(
        ParticleSystem system,
        LayerDefaults defaults,
        Color color,
        float speedMultiplier,
        float sizeMultiplier,
        float densityMultiplier,
        bool applyVelocity,
        bool applyEmission)
    {
        if (system == null)
            return;

        ParticleSystem.MainModule main = system.main;
        main.startColor = ForceOpaque(color);
        main.startSize = ScaleCurve(defaults.startSize, sizeMultiplier);

        if (applyVelocity)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity =
                system.velocityOverLifetime;

            velocity.x = ScaleCurve(defaults.velocityX, speedMultiplier);
            velocity.y = ScaleCurve(defaults.velocityY, speedMultiplier);
            velocity.z = ScaleCurve(defaults.velocityZ, speedMultiplier);
        }

        if (applyEmission)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = ScaleCurve(
                defaults.emissionRate,
                densityMultiplier
            );
        }

        ApplyToExistingParticles(
            system,
            ForceOpaque(color),
            sizeMultiplier
        );
    }

    private void ApplyColor(ParticleSystem system, Color color)
    {
        if (system == null)
            return;

        ParticleSystem.MainModule main = system.main;
        main.startColor = ForceOpaque(color);
        ApplyColorToExistingParticles(system, ForceOpaque(color));
    }

    private void CacheDefaults()
    {
        if (defaultsCached)
            return;

        midDefaults = CaptureDefaults(midStars);
        nearDefaults = CaptureDefaults(nearStars);
        sparkleDefaults = CaptureDefaults(sparkleStars);

        defaultsCached = true;
    }

    private static LayerDefaults CaptureDefaults(ParticleSystem system)
    {
        if (system == null)
            return default;

        ParticleSystem.MainModule main = system.main;
        ParticleSystem.EmissionModule emission = system.emission;
        ParticleSystem.VelocityOverLifetimeModule velocity =
            system.velocityOverLifetime;

        return new LayerDefaults
        {
            startSize = main.startSize,
            emissionRate = emission.rateOverTime,
            velocityX = velocity.x,
            velocityY = velocity.y,
            velocityZ = velocity.z
        };
    }

    private void ResolveLayerReferences()
    {
        if (farStars != null &&
            midStars != null &&
            nearStars != null &&
            sparkleStars != null)
        {
            return;
        }

        ParticleSystem[] systems =
            GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem system = systems[i];

            switch (system.gameObject.name)
            {
                case "FarStars":
                    if (farStars == null)
                        farStars = system;
                    break;

                case "MidStars":
                    if (midStars == null)
                        midStars = system;
                    break;

                case "NearStars":
                    if (nearStars == null)
                        nearStars = system;
                    break;

                case "SparkleStars":
                    if (sparkleStars == null)
                        sparkleStars = system;
                    break;
            }
        }
    }

    private static void ApplyColorToExistingParticles(
        ParticleSystem system,
        Color color)
    {
        if (system == null)
            return;

        int maxParticles = system.main.maxParticles;
        if (maxParticles <= 0)
            return;

        ParticleSystem.Particle[] particles =
            new ParticleSystem.Particle[maxParticles];

        int particleCount = system.GetParticles(particles);

        for (int i = 0; i < particleCount; i++)
            particles[i].startColor = color;

        if (particleCount > 0)
            system.SetParticles(particles, particleCount);
    }

    private static void ApplyToExistingParticles(
        ParticleSystem system,
        Color color,
        float sizeMultiplier)
    {
        if (system == null)
            return;

        int maxParticles = system.main.maxParticles;
        if (maxParticles <= 0)
            return;

        ParticleSystem.Particle[] particles =
            new ParticleSystem.Particle[maxParticles];

        int particleCount = system.GetParticles(particles);

        for (int i = 0; i < particleCount; i++)
        {
            particles[i].startColor = color;
            particles[i].startSize *= sizeMultiplier;
        }

        if (particleCount > 0)
            system.SetParticles(particles, particleCount);
    }

    private static ParticleSystem.MinMaxCurve ScaleCurve(
        ParticleSystem.MinMaxCurve source,
        float multiplier)
    {
        return StarfieldFlowUtility.ScaleCurve(source, multiplier);
    }

    private static float GetRepresentativeCurveValue(
        ParticleSystem.MinMaxCurve curve)
    {
        return StarfieldFlowUtility.GetRepresentativeCurveValue(curve);
    }

    private static Color ForceOpaque(Color color)
    {
        color.a = 1f;
        return color;
    }

    private static Color GenerateRandomStarColor()
    {
        Color color = Random.ColorHSV(
            0f,
            1f,
            0.65f,
            1f,
            0.8f,
            1f,
            1f,
            1f
        );

        color.a = 1f;
        return color;
    }
}
