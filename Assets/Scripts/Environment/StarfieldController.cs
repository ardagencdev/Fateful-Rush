using UnityEngine;

[DisallowMultipleComponent]
public class StarfieldController : MonoBehaviour
{
    [Header("STAR LAYERS")]
    [SerializeField] private ParticleSystem farStars;
    [SerializeField] private ParticleSystem midStars;
    [SerializeField] private ParticleSystem nearStars;
    [SerializeField] private ParticleSystem sparkleStars;

    [Header("COLOR MIX")]
    [Tooltip("MidStars renginin level rengine ne kadar yaklaşacağı.")]
    [Range(0f, 1f)]
    [SerializeField] private float midColorInfluence = 0.25f;

    [Tooltip("SparkleStars renginin level rengine ne kadar yaklaşacağı.")]
    [Range(0f, 1f)]
    [SerializeField] private float sparkleColorInfluence = 0.12f;

    [Header("SAFE RUNTIME LIMITS")]
    [Tooltip("LevelConfig içindeki yoğunluk çarpanının güvenli alt sınırı.")]
    [Range(0.25f, 2f)]
    [SerializeField] private float minDensityMultiplier = 0.75f;

    [Tooltip("LevelConfig içindeki yoğunluk çarpanının güvenli üst sınırı.")]
    [Range(0.25f, 2f)]
    [SerializeField] private float maxDensityMultiplier = 1.25f;

    private LayerDefaults midDefaults;
    private LayerDefaults nearDefaults;
    private LayerDefaults sparkleDefaults;

    private bool defaultsCached;

    private struct LayerDefaults
    {
        public ParticleSystem.MinMaxCurve startSize;
        public ParticleSystem.MinMaxCurve emissionRate;
        public ParticleSystem.MinMaxCurve velocityX;
        public ParticleSystem.MinMaxCurve velocityY;
        public ParticleSystem.MinMaxCurve velocityZ;
    }

    private void Awake()
    {
        ResolveLayerReferences();
        CacheDefaults();
    }

    private void Reset()
    {
        ResolveLayerReferences();
    }

    public void ApplyLevel(LevelConfig level)
    {
        if (level == null)
            return;

        ResolveLayerReferences();
        CacheDefaults();

        Color levelColor = level.randomizeNearStarsColor
            ? GenerateRandomStarColor()
            : ForceOpaque(level.nearStarsColor);

        float speedMultiplier = Mathf.Max(0f, level.nearStarsSpeedMultiplier);
        float sizeMultiplier = Mathf.Max(0f, level.nearStarsSizeMultiplier);

        float densityMultiplier = level.starfieldDensityMultiplier;
        if (densityMultiplier <= 0f)
            densityMultiplier = 1f;

        densityMultiplier = Mathf.Clamp(
            densityMultiplier,
            Mathf.Min(minDensityMultiplier, maxDensityMultiplier),
            Mathf.Max(minDensityMultiplier, maxDensityMultiplier)
        );

        // FarStars is intentionally stable: white, static and burst-only.
        ApplyColor(farStars, Color.white);

        Color midColor = Color.Lerp(
            Color.white,
            levelColor,
            midColorInfluence
        );

        Color sparkleColor = Color.Lerp(
            Color.white,
            levelColor,
            sparkleColorInfluence
        );

        ApplyLayer(
            midStars,
            midDefaults,
            midColor,
            speedMultiplier,
            sizeMultiplier,
            densityMultiplier,
            applyVelocity: true,
            applyEmission: true
        );

        ApplyLayer(
            nearStars,
            nearDefaults,
            levelColor,
            speedMultiplier,
            sizeMultiplier,
            densityMultiplier,
            applyVelocity: true,
            applyEmission: true
        );

        // Sparkles do not move. They only fade in/out through Color over Lifetime.
        ApplyLayer(
            sparkleStars,
            sparkleDefaults,
            sparkleColor,
            speedMultiplier: 1f,
            sizeMultiplier: Mathf.Lerp(1f, sizeMultiplier, 0.35f),
            densityMultiplier: densityMultiplier,
            applyVelocity: false,
            applyEmission: true
        );
    }

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
        multiplier = Mathf.Max(0f, multiplier);

        switch (source.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return new ParticleSystem.MinMaxCurve(
                    source.constant * multiplier
                );

            case ParticleSystemCurveMode.TwoConstants:
                return new ParticleSystem.MinMaxCurve(
                    source.constantMin * multiplier,
                    source.constantMax * multiplier
                );

            case ParticleSystemCurveMode.Curve:
                return new ParticleSystem.MinMaxCurve(
                    source.curveMultiplier * multiplier,
                    source.curve
                );

            case ParticleSystemCurveMode.TwoCurves:
                return new ParticleSystem.MinMaxCurve(
                    source.curveMultiplier * multiplier,
                    source.curveMin,
                    source.curveMax
                );

            default:
                return source;
        }
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
