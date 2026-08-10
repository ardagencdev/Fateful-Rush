using System.Collections;
using UnityEngine;

public class MainMenuStarColorRandomizer : MonoBehaviour
{
    public static MainMenuStarColorRandomizer Instance
    {
        get;
        private set;
    }

    [Header("Reference")]
    [SerializeField]
    private ParticleSystem nearStars;

    [Header("Panel Colors")]
    [SerializeField]
    private Color mainMenuColor =
        new Color(0.65f, 0.2f, 1f, 0.9f);

    [SerializeField]
    private Color missionBriefingColor =
        new Color(1f, 0.2f, 0.55f, 0.9f);

    [SerializeField]
    private Color optionsColor =
        new Color(0.2f, 1f, 0.65f, 0.9f);

    [SerializeField]
    private Color statsColor =
        new Color(1f, 0.75f, 0.2f, 0.9f);

    [Header("Level Page Progression")]
    [SerializeField]
    private Color firstLevelPageColor =
        new Color(1f, 1f, 1f, 0.9f);

    [SerializeField]
    private Color lastLevelPageColor =
        new Color(1f, 0.08f, 0.08f, 0.9f);

    [SerializeField, Min(0f)]
    private float firstPageEmissionRate = 12f;

    [SerializeField, Min(0f)]
    private float lastPageEmissionRate = 32f;

    [SerializeField, Min(1)]
    private int firstPageMaxParticles = 80;

    [SerializeField, Min(1)]
    private int lastPageMaxParticles = 200;

    [SerializeField, Min(0f)]
    private float firstPageFlowMultiplier = 0.7f;

    [SerializeField, Min(0f)]
    private float lastPageFlowMultiplier = 1.75f;

    [Header("Transition")]
    [SerializeField, Min(0.01f)]
    private float transitionDuration = 0.45f;

    private ParticleSystem.Particle[] particles;
    private Coroutine transitionRoutine;

    private Color currentColor;
    private float currentEmissionRate;
    private float currentMaxParticles;
    private float currentFlowMultiplier = 1f;

    private float originalEmissionRate;
    private int originalMaxParticles;
    private float originalVelocityXMultiplier;
    private float originalVelocityYMultiplier;
    private float originalVelocityZMultiplier;

    private bool skinPreviewActive;
    private Color skinPreviewRestoreColor;
    private float skinPreviewRestoreEmissionRate;
    private float skinPreviewRestoreMaxParticles;
    private float skinPreviewRestoreFlowMultiplier;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (nearStars == null)
            nearStars = GetComponent<ParticleSystem>();

        CacheOriginalParticleSettings();

        currentColor = mainMenuColor;
        currentEmissionRate = originalEmissionRate;
        currentMaxParticles = originalMaxParticles;
        currentFlowMultiplier = 1f;

        ApplyStateInstant(
            currentColor,
            currentEmissionRate,
            Mathf.RoundToInt(currentMaxParticles),
            currentFlowMultiplier
        );
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowMainMenuColor()
    {
        ChangeState(
            mainMenuColor,
            originalEmissionRate,
            originalMaxParticles,
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
            Color.Lerp(
                firstLevelPageColor,
                lastLevelPageColor,
                progress
            );

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
            missionBriefingColor,
            originalEmissionRate,
            originalMaxParticles,
            1f
        );
    }

    public void ShowOptionsColor()
    {
        ChangeState(
            optionsColor,
            originalEmissionRate,
            originalMaxParticles,
            1f
        );
    }

    public void ShowStatsColor()
    {
        ChangeState(
            statsColor,
            originalEmissionRate,
            originalMaxParticles,
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

        skinPreviewRestoreColor = currentColor;
        skinPreviewRestoreEmissionRate = currentEmissionRate;
        skinPreviewRestoreMaxParticles = currentMaxParticles;
        skinPreviewRestoreFlowMultiplier = currentFlowMultiplier;
        skinPreviewActive = true;
    }

    public void ShowSkinPreviewColor(Color skinColor)
    {
        if (nearStars == null)
            return;

        if (!skinPreviewActive)
            BeginSkinPreview();

        Color targetColor = NormalizePreviewColor(skinColor);
        targetColor.a = currentColor.a;

        ChangeState(
            targetColor,
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
            skinPreviewRestoreColor,
            skinPreviewRestoreEmissionRate,
            Mathf.RoundToInt(skinPreviewRestoreMaxParticles),
            skinPreviewRestoreFlowMultiplier
        );
    }

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

        emission.rateOverTime =
            Mathf.Max(0f, emissionRate);

        ParticleSystem.VelocityOverLifetimeModule velocity =
            nearStars.velocityOverLifetime;

        velocity.xMultiplier =
            originalVelocityXMultiplier * flowMultiplier;

        velocity.yMultiplier =
            originalVelocityYMultiplier * flowMultiplier;

        velocity.zMultiplier =
            originalVelocityZMultiplier * flowMultiplier;

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
        int requiredSize = Mathf.Max(
            1,
            Mathf.Max(
                maxParticles,
                nearStars.particleCount
            )
        );

        if (particles == null ||
            particles.Length < requiredSize)
        {
            particles =
                new ParticleSystem.Particle[
                    requiredSize
                ];
        }

        int particleCount =
            nearStars.GetParticles(particles);

        particleCount = Mathf.Min(
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
            emission.rateOverTime.constant;

        originalMaxParticles =
            Mathf.Max(1, main.maxParticles);

        originalVelocityXMultiplier =
            velocity.xMultiplier;

        originalVelocityYMultiplier =
            velocity.yMultiplier;

        originalVelocityZMultiplier =
            velocity.zMultiplier;
    }

    private void OnValidate()
    {
        if (nearStars == null)
            nearStars = GetComponent<ParticleSystem>();

        transitionDuration =
            Mathf.Max(0.01f, transitionDuration);

        firstPageEmissionRate =
            Mathf.Max(0f, firstPageEmissionRate);

        lastPageEmissionRate =
            Mathf.Max(
                firstPageEmissionRate,
                lastPageEmissionRate
            );

        firstPageMaxParticles =
            Mathf.Max(1, firstPageMaxParticles);

        lastPageMaxParticles =
            Mathf.Max(
                firstPageMaxParticles,
                lastPageMaxParticles
            );

        firstPageFlowMultiplier =
            Mathf.Max(0f, firstPageFlowMultiplier);

        lastPageFlowMultiplier =
            Mathf.Max(
                firstPageFlowMultiplier,
                lastPageFlowMultiplier
            );
    }

    private static Color NormalizePreviewColor(Color color)
    {
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

        color.a = 1f;
        return color;
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
