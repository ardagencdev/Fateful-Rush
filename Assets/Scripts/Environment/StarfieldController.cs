using UnityEngine;

[DisallowMultipleComponent]
public partial class StarfieldController : MonoBehaviour
{
    public Color CurrentNearStarsColor { get; private set; } = Color.white;

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

    [Header("NEAR STARS SCREEN FLOW")]
    [Tooltip("NearStars sadece ekrana giriş yapan kenarlardan doğar ve karşı kenardan çıktıktan sonra silinir.")]
    [SerializeField] private bool useScreenEdgeNearStars = true;

    [Tooltip("Boş bırakılırsa Main Camera otomatik kullanılır.")]
    [SerializeField] private Camera nearStarsCamera;

    [Tooltip("Yeni NearStar üretim hızı. Eski Box/Volume sisteminden farklı olarak üretilen yıldızların neredeyse tamamı ekrandan geçer.")]
    [Min(0f)]
    [SerializeField] private float nearStarsBaseEmissionRate = 4.25f;

    [Tooltip("NearStars için güvenli üst particle limiti.")]
    [Min(1)]
    [SerializeField] private int nearStarsMaxParticles = 220;

    [Tooltip("Lifetime artık görünürlük süresini belirlemez; yıldız karşı kenardan çıkınca script tarafından silinir. Bu değer sadece güvenlik payıdır.")]
    [SerializeField] private Vector2 nearStarsLifetimeRange = new Vector2(90f, 120f);

    [Tooltip("Yıldızın ekranın biraz dışından doğması için dünya birimi cinsinden pay.")]
    [Min(0f)]
    [SerializeField] private float nearStarsSpawnPadding = 0.35f;

    [Tooltip("Yıldızın tamamen ekran dışına çıktıktan sonra silinmesi için dünya birimi cinsinden pay.")]
    [Min(0f)]
    [SerializeField] private float nearStarsExitPadding = 0.65f;

    [Tooltip("Scene açıldığında ekranın boş başlayıp dolmasını beklememek için ilk doluluk oranı.")]
    [Range(0f, 1f)]
    [SerializeField] private float nearStarsInitialFill = 0.45f;

    private LayerDefaults midDefaults;
    private LayerDefaults nearDefaults;
    private LayerDefaults sparkleDefaults;

    private bool defaultsCached;

    private float currentNearEmissionRate;
    private bool nearLevelSettingsApplied;
    private float currentNearSpeedMultiplier = 1f;
    private float nearEmissionAccumulator;
    private bool nearFlowInitialized;
    private bool nearStarsSuspended;
    private ParticleSystem.Particle[] nearParticleBuffer;

    private const float MaxNearStarsFrameDelta = 0.1f;

    private ParticleSystem[] resultStarLayers;
    private readonly bool[] resultOriginalUnscaled = new bool[4];
    private readonly float[] resultOriginalSpeed = new float[4];
    private bool resultStarClockActive;

    private void SetResultStarClock(bool active)
    {
        if (active == resultStarClockActive || resultStarLayers == null) return;
        for (int i = 0; i < resultStarLayers.Length; i++)
        {
            ParticleSystem layer = resultStarLayers[i];
            if (layer == null) continue;
            var main = layer.main;
            if (active)
            {
                resultOriginalUnscaled[i] = main.useUnscaledTime;
                resultOriginalSpeed[i] = main.simulationSpeed;
                main.useUnscaledTime = true;
                main.simulationSpeed = resultOriginalSpeed[i] * 0.4f;
            }
            else
            {
                main.useUnscaledTime = resultOriginalUnscaled[i];
                main.simulationSpeed = resultOriginalSpeed[i];
            }
        }
        resultStarClockActive = active;
    }

    private void OnDisable()
    {
        SetResultStarClock(false);
    }

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
        ConfigureNearStarsBaseSettings();
        CacheDefaults();
        resultStarLayers = new[] { farStars, midStars, nearStars, sparkleStars };

        if (!nearLevelSettingsApplied)
            currentNearEmissionRate = Mathf.Max(0f, nearStarsBaseEmissionRate);
    }

    private void Start()
    {
        InitializeNearStarsFlow();
    }

    private void Update()
    {
        SetResultStarClock(GameStateManager.IsGameplayEnded);
        if (!useScreenEdgeNearStars ||
            !nearFlowInitialized ||
            nearStarsSuspended ||
            nearStars == null)
        {
            return;
        }

        ResolveNearStarsCamera();
        if (nearStarsCamera == null)
            return;

        CullExitedNearStars();
        EmitNearStars(
            Mathf.Min(resultStarClockActive ? Time.unscaledDeltaTime * 0.4f : Time.deltaTime,
                MaxNearStarsFrameDelta)
        );
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            ResumeNearStarsFlow();
        else
            SuspendNearStarsFlow();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SuspendNearStarsFlow();
        else
            ResumeNearStarsFlow();
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
        ConfigureNearStarsBaseSettings();
        CacheDefaults();

        Color levelColor = level.randomizeNearStarsColor
            ? GenerateRandomStarColor()
            : ForceOpaque(level.nearStarsColor);

        // Store the exact color applied to gameplay NearStars so HUD elements
        // can match it one-to-one (including randomized test levels).
        CurrentNearStarsColor = levelColor;

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
            applyEmission: !useScreenEdgeNearStars
        );

        if (useScreenEdgeNearStars)
        {
            currentNearEmissionRate =
                Mathf.Max(0f, GetRepresentativeCurveValue(nearDefaults.emissionRate)) *
                densityMultiplier;

            currentNearSpeedMultiplier = speedMultiplier;
            nearLevelSettingsApplied = true;
        }

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

    private void OnValidate()
    {
        nearStarsBaseEmissionRate = Mathf.Max(0f, nearStarsBaseEmissionRate);
        nearStarsMaxParticles = Mathf.Max(1, nearStarsMaxParticles);
        nearStarsSpawnPadding = Mathf.Max(0f, nearStarsSpawnPadding);
        nearStarsExitPadding = Mathf.Max(0f, nearStarsExitPadding);

        float minLifetime = Mathf.Max(1f, Mathf.Min(
            nearStarsLifetimeRange.x,
            nearStarsLifetimeRange.y
        ));

        float maxLifetime = Mathf.Max(minLifetime, Mathf.Max(
            nearStarsLifetimeRange.x,
            nearStarsLifetimeRange.y
        ));

        nearStarsLifetimeRange = new Vector2(minLifetime, maxLifetime);
    }
}
