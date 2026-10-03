using System.Collections;
using UnityEngine;

public partial class MainMenuStarColorRandomizer : MonoBehaviour
{
    public static MainMenuStarColorRandomizer Instance
    {
        get;
        private set;
    }

    /// <summary>
    /// The exact color currently being used by NearStars.
    /// Alpha is included; UI callers can override alpha if needed.
    /// </summary>
    public Color CurrentColor => currentColor;

    [Header("Reference")]
    [SerializeField]
    private ParticleSystem nearStars;

    [Tooltip("Boş bırakılırsa Main Camera otomatik kullanılır.")]
    [SerializeField]
    private Camera nearStarsCamera;

    [Header("Skin Theme NearStars")]
    [Tooltip("NearStars use the selected player's UI accent color on every Main Menu panel.")]
    [SerializeField, Range(0f, 1f)]
    private float skinThemeAlpha = 0.9f;

    [Header("Base Panel Density")]
    [SerializeField, Min(0f)]
    private float basePanelEmissionRate = 1.5f;

    [SerializeField, Min(1)]
    private int basePanelMaxParticles = 50;

    [Header("Level Page Progression")]
    [Tooltip("Screen-edge sisteminde eski Volume emission değerlerinden daha düşük tutulmalı.")]
    [SerializeField, Min(0f)]
    private float firstPageEmissionRate = 4.25f;

    [Tooltip("Son level sayfasına doğru NearStars yoğunluğu artar.")]
    [SerializeField, Min(0f)]
    private float lastPageEmissionRate = 8.5f;

    [SerializeField, Min(1)]
    private int firstPageMaxParticles = 140;

    [SerializeField, Min(1)]
    private int lastPageMaxParticles = 260;

    [SerializeField, Min(0f)]
    private float firstPageFlowMultiplier = 0.7f;

    [SerializeField, Min(0f)]
    private float lastPageFlowMultiplier = 1.75f;

    [Header("Screen Edge Near Stars")]
    [Tooltip("NearStars ekrana giriş yapan kenarlardan doğar ve karşı kenardan tamamen çıktıktan sonra silinir.")]
    [SerializeField]
    private bool useScreenEdgeNearStars = true;

    [Tooltip("Lifetime görünürlük süresini belirlemez; yıldız karşı kenardan çıkınca script siler. Bu sadece güvenlik payıdır.")]
    [SerializeField]
    private Vector2 nearStarsLifetimeRange =
        new Vector2(90f, 120f);

    [Tooltip("Yıldızların ekranın biraz dışından doğması için dünya birimi cinsinden pay.")]
    [SerializeField, Min(0f)]
    private float nearStarsSpawnPadding = 0.35f;

    [Tooltip("Yıldız tamamen ekran dışına çıktıktan sonra silinmesi için pay.")]
    [SerializeField, Min(0f)]
    private float nearStarsExitPadding = 0.65f;

    [Tooltip("Main Menu açıldığında NearStars'ın hemen dolu görünmesi için başlangıç doluluk oranı.")]
    [SerializeField, Range(0f, 1f)]
    private float nearStarsInitialFill = 0.65f;

    [Header("Transition")]
    [SerializeField, Min(0.01f)]
    private float transitionDuration = 0.45f;

    [SerializeField, HideInInspector]
    private int screenEdgeSettingsVersion;

    private ParticleSystem.Particle[] particles;
    private Coroutine transitionRoutine;

    private Color currentColor;
    private float currentEmissionRate;
    private float currentMaxParticles;
    private float currentFlowMultiplier = 1f;

    private float originalEmissionRate;
    private int originalMaxParticles;
    private ParticleSystem.MinMaxCurve originalVelocityX;
    private ParticleSystem.MinMaxCurve originalVelocityY;
    private ParticleSystem.MinMaxCurve originalVelocityZ;

    private bool skinPreviewActive;
    private float skinPreviewRestoreEmissionRate;
    private float skinPreviewRestoreMaxParticles;
    private float skinPreviewRestoreFlowMultiplier;

    private float nearEmissionAccumulator;
    private bool nearFlowInitialized;
    private bool nearStarsSuspended;

    private const float MaxNearStarsFrameDelta = 0.1f;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        PlayerSkinCatalog.SelectedSkinChanged -=
            HandleSelectedSkinChanged;
        PlayerSkinCatalog.SelectedSkinChanged +=
            HandleSelectedSkinChanged;

        if (nearStars == null)
            nearStars = GetComponent<ParticleSystem>();

        MigrateLegacySettingsIfNeeded();
        CacheOriginalParticleSettings();
        ConfigureScreenEdgeFlow();

        currentColor = GetSelectedSkinThemeColor();
        currentEmissionRate = basePanelEmissionRate;
        currentMaxParticles = basePanelMaxParticles;
        currentFlowMultiplier = 1f;

        ApplyStateInstant(
            currentColor,
            currentEmissionRate,
            Mathf.RoundToInt(currentMaxParticles),
            currentFlowMultiplier
        );
    }

    private void Start()
    {
        InitializeScreenEdgeFlow();
    }

    private void Update()
    {
        if (!useScreenEdgeNearStars ||
            !nearFlowInitialized ||
            nearStarsSuspended ||
            nearStars == null)
        {
            return;
        }

        ResolveCamera();

        if (nearStarsCamera == null)
            return;

        CullExitedNearStars();
        EmitNearStars(
            Mathf.Min(Time.unscaledDeltaTime, MaxNearStarsFrameDelta)
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

    private void OnDestroy()
    {
        PlayerSkinCatalog.SelectedSkinChanged -=
            HandleSelectedSkinChanged;

        if (Instance == this)
            Instance = null;
    }

    private void HandleSelectedSkinChanged()
    {
        skinPreviewActive = false;

        ChangeState(
            GetSelectedSkinThemeColor(),
            currentEmissionRate,
            Mathf.RoundToInt(currentMaxParticles),
            currentFlowMultiplier
        );
    }

    public void ShowMainMenuColor()
    {
        ChangeState(
            GetSelectedSkinThemeColor(),
            basePanelEmissionRate,
            basePanelMaxParticles,
            1f
        );
    }

    public void ShowLevelSelectionColor()
    {
        ShowLevelSelectionPage(0, 1);
    }

    public void ShowLevelSelectionPage(
        int pageIndex,
        int totalPageCount
    )
    {
        int safePageCount =
            Mathf.Max(1, totalPageCount);

        int safePageIndex =
            Mathf.Clamp(
                pageIndex,
                0,
                safePageCount - 1
            );

        float progress =
            safePageCount <= 1
                ? 0f
                : safePageIndex /
                  (float)(safePageCount - 1);

        Color targetColor =
            GetSelectedSkinThemeColor();

        float targetEmissionRate =
            Mathf.Lerp(
                firstPageEmissionRate,
                lastPageEmissionRate,
                progress
            );

        int targetMaxParticles =
            Mathf.RoundToInt(
                Mathf.Lerp(
                    firstPageMaxParticles,
                    lastPageMaxParticles,
                    progress
                )
            );

        float targetFlowMultiplier =
            Mathf.Lerp(
                firstPageFlowMultiplier,
                lastPageFlowMultiplier,
                progress
            );

        ChangeState(
            targetColor,
            targetEmissionRate,
            targetMaxParticles,
            targetFlowMultiplier
        );
    }

    public void ShowMissionBriefingColor()
    {
        ChangeState(
            GetSelectedSkinThemeColor(),
            basePanelEmissionRate,
            basePanelMaxParticles,
            1f
        );
    }

    public void ShowOptionsColor()
    {
        ChangeState(
            GetSelectedSkinThemeColor(),
            basePanelEmissionRate,
            basePanelMaxParticles,
            1f
        );
    }

    public void ShowStatsColor()
    {
        ChangeState(
            GetSelectedSkinThemeColor(),
            basePanelEmissionRate,
            basePanelMaxParticles,
            1f
        );
    }

    public void BeginSkinPreview()
    {
        if (nearStars == null || skinPreviewActive)
            return;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        skinPreviewRestoreEmissionRate = currentEmissionRate;
        skinPreviewRestoreMaxParticles = currentMaxParticles;
        skinPreviewRestoreFlowMultiplier = currentFlowMultiplier;
        skinPreviewActive = true;
    }

    public void ShowSkinPreviewSkin(
        PlayerSkinCatalog.SkinEntry skin)
    {
        if (skin == null)
            return;

        ShowSkinPreviewColor(
            GetSkinThemeColor(skin)
        );
    }

    public void ShowSkinPreviewColor(Color skinColor)
    {
        if (nearStars == null)
            return;

        if (!skinPreviewActive)
            BeginSkinPreview();

        ChangeState(
            skinColor,
            currentEmissionRate,
            Mathf.RoundToInt(currentMaxParticles),
            currentFlowMultiplier
        );
    }

    public void EndSkinPreview()
    {
        if (!skinPreviewActive)
            return;

        skinPreviewActive = false;

        ChangeState(
            GetSelectedSkinThemeColor(),
            skinPreviewRestoreEmissionRate,
            Mathf.RoundToInt(skinPreviewRestoreMaxParticles),
            skinPreviewRestoreFlowMultiplier
        );
    }

    private void OnValidate()
    {
        if (nearStars == null)
            nearStars = GetComponent<ParticleSystem>();

        MigrateLegacySettingsIfNeeded();

        transitionDuration =
            Mathf.Max(
                0.01f,
                transitionDuration
            );

        skinThemeAlpha =
            Mathf.Clamp01(skinThemeAlpha);

        basePanelEmissionRate =
            Mathf.Max(
                0f,
                basePanelEmissionRate
            );

        basePanelMaxParticles =
            Mathf.Max(
                1,
                basePanelMaxParticles
            );

        firstPageEmissionRate =
            Mathf.Max(
                0f,
                firstPageEmissionRate
            );

        lastPageEmissionRate =
            Mathf.Max(
                firstPageEmissionRate,
                lastPageEmissionRate
            );

        firstPageMaxParticles =
            Mathf.Max(
                1,
                firstPageMaxParticles
            );

        lastPageMaxParticles =
            Mathf.Max(
                firstPageMaxParticles,
                lastPageMaxParticles
            );

        firstPageFlowMultiplier =
            Mathf.Max(
                0f,
                firstPageFlowMultiplier
            );

        lastPageFlowMultiplier =
            Mathf.Max(
                firstPageFlowMultiplier,
                lastPageFlowMultiplier
            );

        nearStarsSpawnPadding =
            Mathf.Max(
                0f,
                nearStarsSpawnPadding
            );

        nearStarsExitPadding =
            Mathf.Max(
                0f,
                nearStarsExitPadding
            );

        float minLifetime =
            Mathf.Max(
                1f,
                Mathf.Min(
                    nearStarsLifetimeRange.x,
                    nearStarsLifetimeRange.y
                )
            );

        float maxLifetime =
            Mathf.Max(
                minLifetime,
                Mathf.Max(
                    nearStarsLifetimeRange.x,
                    nearStarsLifetimeRange.y
                )
            );

        nearStarsLifetimeRange =
            new Vector2(
                minLifetime,
                maxLifetime
            );
    }
}
